namespace Amafu;

internal static class AmafuApplication
{
	internal static int Run(string[] args, AmafuRuntime runtime)
	{
		var noLogo = Contains(args, "-n", "--nologo");
		var help = Contains(args, "-h", "--help");
		var version = Contains(args, "-v", "--version");
		var command = args.FirstOrDefault(argument => !argument.StartsWith("-", StringComparison.Ordinal));

		if (version)
		{
			runtime.Output.WriteLine($"amafu {VersionInfo.Current}");
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

		return command.ToLowerInvariant() switch
		{
			"detect" => Detect(args, runtime, command),
			"init" or "init-config" => Initialize(args, runtime, command),
			_ => UnknownCommand(command, runtime)
		};
	}

	private static int Detect(string[] args, AmafuRuntime runtime, string command)
	{
		if (!ValidateOptions(args, command, ["-n", "--nologo", "--json"], runtime.Error)) return 2;
		var result = CloudStorageDetector.Detect(runtime);

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
				runtime.Output.WriteLine($"{provider.Name}: {AmafuConfigurationRenderer.ToPortablePath(provider.RootPath, runtime.HomeDirectory)}");
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
		if (!ValidateOptions(args, command, ["-n", "--nologo", "-f", "--force", "--dry-run"], runtime.Error)) return 2;

		var result = CloudStorageDetector.Detect(runtime);
		var configuration = new AmafuConfiguration(runtime.HomeDirectory, result.Providers);
		var payload = AmafuConfigurationRenderer.Render(configuration);

		if (Contains(args, "--dry-run"))
		{
			runtime.Output.Write(payload);
			return 0;
		}

		try
		{
			AmafuConfigurationWriter.Write(
				runtime.ConfigurationFile,
				payload,
				Contains(args, "-f", "--force"));
		}
		catch (AmafuConfigurationExistsException)
		{
			runtime.Error.WriteLine("Configuration file already exists at '~/.config/RAIkeep.json5'. Use --force (-f) to overwrite.");
			return 3;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			runtime.Error.WriteLine($"Unable to write '~/.config/RAIkeep.json5': {exception.Message}");
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
		foreach (var argument in args)
		{
			if (string.Equals(argument, command, StringComparison.Ordinal)) continue;
			if (argument is "-h" or "--help" or "-v" or "--version") continue;
			if (validOptions.Contains(argument, StringComparer.Ordinal)) continue;
			error.WriteLine($"Unknown option '{argument}' for 'amafu {command}'. Use 'amafu {command} --help'.");
			return false;
		}
		return true;
	}

	private static bool Contains(IEnumerable<string> args, params string[] values)
		=> args.Any(argument => values.Contains(argument, StringComparer.Ordinal));
}
