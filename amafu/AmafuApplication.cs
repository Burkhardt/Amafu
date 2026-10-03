using System.Reflection;

namespace Amafu;

internal static class AmafuApplication
{
	internal static int Run(string[] args, AmafuRuntime runtime)
	{
		var noLogo = Contains(args, "-n", "--nologo");
		var help = Contains(args, "-h", "--help");
		var version = Contains(args, "-v", "--version");
		var command = Command(args);

		if (version)
		{
			runtime.Output.WriteLine($"amafu v{Version()}");
			return 0;
		}

		if (help)
		{
			CliHelp.Write(runtime.Output, noLogo, runtime.TerminalWidth, command);
			return 0;
		}

		if (command is null)
		{
			CliHelp.Write(runtime.Output, noLogo, runtime.TerminalWidth);
			return 1;
		}

		if (runtime.IsElevated)
		{
			runtime.Error.WriteLine("Amafu must run as the ordinary user. Do not use sudo; it would resolve the wrong home directory and create root-owned configuration files.");
			return 2;
		}

		try
		{
			return command.ToLowerInvariant() switch
			{
				"detect" => Detect(args, runtime, command),
				"init" or "init-config" => Initialize(args, runtime, command),
				"reconcile" => Reconcile(args, runtime, command),
				_ => UnknownCommand(command, runtime)
			};
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			runtime.Error.WriteLine($"Cloud setup failed: {exception.Message}");
			return 4;
		}
	}

	private static int Detect(string[] args, AmafuRuntime runtime, string command)
	{
		if (!ValidateOptions(args, command, ["-n", "--nologo", "--json", "--dry-run", "--create-links", "--onedrive-personal"], runtime.Error)) return 2;
		var result = CloudStorageDetector.Detect(runtime, OptionValue(args, "--onedrive-personal"));
		var dryRun = Contains(args, "--dry-run");
		var createLinks = Contains(args, "--create-links");
		if (createLinks && !dryRun)
		{
			try { CloudStorageLinks.Ensure(runtime.HomeDirectory, result.Providers); }
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				runtime.Error.WriteLine($"Unable to create cloud shortcuts: {exception.Message}");
				return 4;
			}
		}

		if (Contains(args, "--json"))
		{
			runtime.Output.Write(AmafuConfigurationRenderer.RenderDetectionJson(result, runtime.HomeDirectory));
			return 0;
		}

		if (result.Providers.Count == 0)
		{
			runtime.Output.WriteLine("No supported cloud-storage provider was detected. No directory was created.");
			if (runtime.Platform != AmafuPlatform.MacOS)
				runtime.Output.WriteLine($"Automatic provider discovery is not yet defined for {runtime.Platform}.");
		}
		else
		{
			foreach (var provider in result.Providers)
			{
				var alias = AmafuConfigurationRenderer.ToPortablePath(
					CloudStorageLinks.AliasPath(runtime.HomeDirectory, provider.Name), runtime.HomeDirectory);
				var root = AmafuConfigurationRenderer.ToPortablePath(provider.RootPath, runtime.HomeDirectory);
				runtime.Output.WriteLine(createLinks
					? $"{provider.Name}: {alias} -> {root}"
					: $"{provider.Name}: {root}");
			}
			if (createLinks && dryRun) runtime.Output.WriteLine("Dry run: no shortcuts were created.");
		}

		if (result.CheckedPaths.Count > 0)
		{
			runtime.Output.WriteLine("Checked:");
			foreach (var path in result.CheckedPaths)
				runtime.Output.WriteLine($"  {AmafuConfigurationRenderer.ToPortablePath(path, runtime.HomeDirectory)}");
		}
		return 0;
	}

	private static int Initialize(string[] args, AmafuRuntime runtime, string command)
	{
		if (!ValidateOptions(args, command, ["-n", "--nologo", "-f", "--force", "--dry-run", "--create-links", "--onedrive-personal"], runtime.Error)) return 2;

		var result = CloudStorageDetector.Detect(runtime, OptionValue(args, "--onedrive-personal"));
		var configuration = new AmafuConfiguration(runtime.HomeDirectory, result.Providers);
		var payload = AmafuConfigurationRenderer.Render(configuration);

		if (Contains(args, "--dry-run"))
		{
			runtime.Output.Write(payload);
			return 0;
		}

		try
		{
			var force = Contains(args, "-f", "--force");
			if (File.Exists(runtime.ConfigurationFile) && !force)
				throw new AmafuConfigurationExistsException(runtime.ConfigurationFile);
			if (Contains(args, "--create-links"))
				CloudStorageLinks.Ensure(runtime.HomeDirectory, result.Providers);
			AmafuConfigurationWriter.Write(
				runtime.ConfigurationFile,
				payload,
				force);
		}
		catch (AmafuConfigurationExistsException)
		{
			runtime.Error.WriteLine("Configuration file already exists at '~/.config/RAIkeep.json5'. Use --force (-f) to overwrite.");
			return 3;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			runtime.Error.WriteLine($"Unable to initialize '~/.config/RAIkeep.json5': {exception.Message}");
			return 4;
		}

		runtime.Output.WriteLine($"Created {runtime.ConfigurationFile}");
		return 0;
	}

	private static int UnknownCommand(string command, AmafuRuntime runtime)
	{
		runtime.Error.WriteLine($"Unknown command '{command}'. Use 'amafu --help'.");
		return 2;
	}

	private static bool ValidateOptions(
		IEnumerable<string> args,
		string command,
		IReadOnlyCollection<string> validOptions,
		TextWriter error)
	{
		var arguments = args.ToArray();
		for (var index = 0; index < arguments.Length; index++)
		{
			var argument = arguments[index];
			if (argument == "--onedrive-personal" && validOptions.Contains(argument))
			{
				if (++index >= arguments.Length || arguments[index].StartsWith("-", StringComparison.Ordinal)
					|| arguments.Count(value => value == argument) != 1)
				{
					error.WriteLine("--onedrive-personal requires one folder name or path and may appear only once.");
					return false;
				}
				continue;
			}
			if (string.Equals(argument, command, StringComparison.Ordinal)) continue;
			if (argument is "-h" or "--help" or "-v" or "--version") continue;
			if (validOptions.Contains(argument, StringComparer.Ordinal)) continue;
			error.WriteLine($"Unknown option '{argument}' for 'amafu {command}'. Use 'amafu {command} --help'.");
			return false;
		}
		return true;
	}

	private static string? Command(string[] args)
	{
		for (var index = 0; index < args.Length; index++)
		{
			if (args[index] == "--onedrive-personal") { index++; continue; }
			if (!args[index].StartsWith("-", StringComparison.Ordinal)) return args[index];
		}
		return null;
	}

	private static string? OptionValue(string[] args, string option)
	{
		var index = Array.IndexOf(args, option);
		return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
	}

	private static int Reconcile(string[] args, AmafuRuntime runtime, string command)
	{
		if (!ValidateOptions(args, command, ["-n", "--nologo", "--apply", "--dry-run", "--onedrive-personal"], runtime.Error)) return 2;
		if (Contains(args, "--apply") && Contains(args, "--dry-run"))
		{
			runtime.Error.WriteLine("Choose either --apply or --dry-run.");
			return 2;
		}
		CloudConfigurationReconciler.Run(runtime, OptionValue(args, "--onedrive-personal"), Contains(args, "--apply"));
		return 0;
	}

	private static bool Contains(IEnumerable<string> args, params string[] values)
		=> args.Any(argument => values.Contains(argument, StringComparer.Ordinal));

	private static string Version()
	{
		var informational = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		return (informational ?? "0.0.0").Split('+')[0];
	}
}
