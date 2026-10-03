using System.Text;

namespace Amafu;

internal static class CloudConfigurationReconciler
{
	internal static void Run(AmafuRuntime runtime, string? personalSelection, bool apply)
	{
		if (!File.Exists(runtime.ConfigurationFile))
			throw new IOException("No existing configuration to reconcile. Run 'amafu init --create-links' first.");
		var source = File.ReadAllText(runtime.ConfigurationFile);
		var document = new ConfigurationDocument(source);
		var original = document.CloudPaths();
		var detected = CloudStorageDetector.Detect(runtime, personalSelection).Providers;
		var targets = new Dictionary<string, string>(StringComparer.Ordinal);
		var paths = new Dictionary<string, string>(StringComparer.Ordinal);
		var addedNames = new List<string>();
		var knownRoots = original.Values.Where(value => Directory.Exists(Expand(value, runtime)))
			.Select(value => CloudStorageDetector.CanonicalDirectory(Expand(value, runtime))).ToHashSet(StringComparer.Ordinal);

		foreach (var (name, value) in original)
		{
			var root = Expand(value, runtime);
			if (Directory.Exists(root) && AllowedName(name)) targets.Add(name, CloudStorageDetector.CanonicalDirectory(root));
			else runtime.Output.WriteLine($"Retain {name}: {value} (not an available supported shortcut target)");
		}
		foreach (var provider in detected)
		{
			var root = CloudStorageDetector.CanonicalDirectory(provider.RootPath);
			if (targets.TryGetValue(provider.Name, out var existing) && existing != root && provider.Name != "OneDrive")
				throw new IOException($"Configured '{provider.Name}' points to '{existing}', but discovery found '{root}'. Resolve the conflict before applying.");
			targets[provider.Name] = root;
			if (!original.ContainsKey(provider.Name) && knownRoots.Add(root)) addedNames.Add(provider.Name);
		}
		foreach (var (name, root) in targets)
		{
			paths.Add(name, AmafuConfigurationRenderer.ToPortablePath(CloudStorageLinks.AliasPath(runtime.HomeDirectory, name), runtime.HomeDirectory));
			runtime.Output.WriteLine($"{name}: {(original.TryGetValue(name, out var old) ? old : "(new)")} -> {paths[name]} -> {root}");
		}
		var providers = targets.Select(pair => new DetectedCloudProvider(pair.Key, pair.Value)).ToArray();
		var oneDriveAlias = CloudStorageLinks.AliasPath(runtime.HomeDirectory, "OneDrive");
		var priorLink = new DirectoryInfo(oneDriveAlias).LinkTarget;
		var repoint = personalSelection is not null && priorLink is not null && targets.TryGetValue("OneDrive", out var selectedRoot)
			&& CloudStorageDetector.CanonicalDirectory(oneDriveAlias) != selectedRoot;
		if (repoint)
		{
			if (!original.TryGetValue("OneDrive", out var configured)
				|| CloudStorageDetector.CanonicalDirectory(Expand(configured, runtime)) != CloudStorageDetector.CanonicalDirectory(oneDriveAlias))
				throw new IOException("The existing OneDrive shortcut does not match the configured root; it will not be replaced.");
			runtime.Output.WriteLine($"Repoint selected OneDrive shortcut: {priorLink} -> {targets["OneDrive"]}");
		}
		CloudStorageLinks.Preflight(runtime.HomeDirectory, providers.Select(p => repoint && p.Name == "OneDrive"
			? new DetectedCloudProvider(p.Name, CloudStorageDetector.CanonicalDirectory(oneDriveAlias)) : p).ToArray());
		var payload = document.Rewrite(paths, addedNames);
		runtime.Output.WriteLine("Proposed configuration:");
		runtime.Output.WriteLine(payload);
		if (!apply)
		{
			runtime.Output.WriteLine("Preview only. Use the same command with --apply to back up and update the configuration and shortcuts.");
			return;
		}
		if (payload == source && !repoint && providers.All(provider =>
			new DirectoryInfo(CloudStorageLinks.AliasPath(runtime.HomeDirectory, provider.Name)).LinkTarget is not null))
		{
			runtime.Output.WriteLine("Configuration and shortcuts are already reconciled; no changes were needed.");
			return;
		}
		if (File.ReadAllText(runtime.ConfigurationFile) != source)
			throw new IOException("Configuration changed during reconciliation. Rerun the preview.");
		var backup = runtime.ConfigurationFile + ".before-reconcile-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N")[..8];
		File.Copy(runtime.ConfigurationFile, backup, overwrite: false);
		var temporary = runtime.ConfigurationFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			if (repoint) Directory.Delete(oneDriveAlias); // Validated symbolic link only; never delete its target.
			CloudStorageLinks.Ensure(runtime.HomeDirectory, providers);
			File.WriteAllText(temporary, payload, new UTF8Encoding(false));
			if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, File.GetUnixFileMode(runtime.ConfigurationFile));
			File.Move(temporary, runtime.ConfigurationFile, overwrite: true);
		}
		catch
		{
			if (repoint)
			{
				if (new DirectoryInfo(oneDriveAlias).LinkTarget is not null) Directory.Delete(oneDriveAlias);
				Directory.CreateSymbolicLink(oneDriveAlias, priorLink!);
			}
			throw;
		}
		finally { if (File.Exists(temporary)) File.Delete(temporary); }
		runtime.Output.WriteLine($"Updated {runtime.ConfigurationFile}. Backup: {backup}");
	}

	private static string Expand(string path, AmafuRuntime runtime) => Path.GetFullPath(
		path.StartsWith("~/", StringComparison.Ordinal) ? Path.Combine(runtime.HomeDirectory, path[2..]) : path);

	private static bool AllowedName(string name) => new[] { "OneDrive", "GoogleDrive", "Dropbox", "ICloudDrive" }
		.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		&& System.Text.RegularExpressions.Regex.IsMatch(name, @"\A[A-Za-z0-9_-]+\z");
}
