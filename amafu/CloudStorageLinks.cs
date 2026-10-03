namespace Amafu;

/// <summary>Creates user-owned shortcuts without moving or changing provider data.</summary>
internal static class CloudStorageLinks
{
	internal static string AliasPath(string homeDirectory, string providerName)
		=> Path.Combine(homeDirectory, ".CloudStorage", providerName);

	internal static void Ensure(string homeDirectory, IReadOnlyList<DetectedCloudProvider> providers)
	{
		if (providers.Count == 0) return;
		var directory = Path.Combine(homeDirectory, ".CloudStorage");
		if (new DirectoryInfo(directory).LinkTarget is not null || File.Exists(directory))
			throw new IOException($"Cannot create cloud shortcuts: '{directory}' must be an ordinary directory, not a file or symbolic link.");

		// Check every destination before creating anything. Never repoint an existing
		// shortcut: it may identify another account or contain the user's own data.
		foreach (var provider in providers) Validate(homeDirectory, provider);
		Directory.CreateDirectory(directory);
		foreach (var provider in providers)
		{
			if (Validate(homeDirectory, provider)) continue;
			Directory.CreateSymbolicLink(AliasPath(homeDirectory, provider.Name), Path.GetFullPath(provider.RootPath));
		}
	}

	/// <returns>True when an existing shortcut already points to this root.</returns>
	private static bool Validate(string homeDirectory, DetectedCloudProvider provider)
	{
		if (provider.Name is not ("OneDrive" or "Dropbox" or "GoogleDrive" or "ICloudDrive"))
			throw new IOException($"Unsupported cloud shortcut name '{provider.Name}'.");
		if (!Directory.Exists(provider.RootPath))
			throw new IOException($"Detected cloud directory '{provider.RootPath}' is no longer available.");

		var alias = AliasPath(homeDirectory, provider.Name);
		var info = new DirectoryInfo(alias);
		if (info.LinkTarget is { } target)
		{
			var resolved = Path.GetFullPath(target, Path.GetDirectoryName(alias)!);
			if (string.Equals(Path.TrimEndingDirectorySeparator(resolved),
				Path.TrimEndingDirectorySeparator(Path.GetFullPath(provider.RootPath)), StringComparison.Ordinal)) return true;
			throw new IOException($"Cloud shortcut '{alias}' already points to '{target}'. Existing links are never replaced.");
		}
		if (Directory.Exists(alias) || File.Exists(alias))
			throw new IOException($"Cloud shortcut '{alias}' is occupied by an existing file or directory. Nothing was replaced.");
		return false;
	}
}
