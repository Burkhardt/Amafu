namespace Amafu;

internal static class CloudStorageDetector
{
	internal static CloudDetectionResult Detect(AmafuRuntime runtime, string? personalSelection = null)
	{
		var providers = new List<DetectedCloudProvider>();
		var checkedPaths = new List<string>();

		if (runtime.Platform != AmafuPlatform.MacOS)
			return new CloudDetectionResult(providers, checkedPaths);

		// A user-owned shortcut is the stable configuration boundary. Inspect it
		// before vendor-specific locations, whose account directory names change
		// with provider releases and account changes. The shortcut itself, rather
		// than its resolved target, remains the configured root.
		var cleanCloudStorage = Path.Combine(runtime.HomeDirectory, ".CloudStorage");
		if (personalSelection is null)
			AddFirstShortcutExisting(providers, checkedPaths, "OneDrive",
				[Path.Combine(cleanCloudStorage, "OneDrive"), Path.Combine(cleanCloudStorage, "OneDrivePersonal")],
				"OneDrive", "Personal");
		AddShortcutIfExisting(providers, checkedPaths, "GoogleDrive", Path.Combine(cleanCloudStorage, "GoogleDrive"),
			"GoogleDrive", null);
		AddShortcutIfExisting(providers, checkedPaths, "GoogleDriveRainer", Path.Combine(cleanCloudStorage, "GoogleDriveRainer"),
			"GoogleDrive", "Rainer");
		AddShortcutIfExisting(providers, checkedPaths, "Dropbox", Path.Combine(cleanCloudStorage, "Dropbox"),
			"Dropbox", null);
		AddShortcutIfExisting(providers, checkedPaths, "ICloudDrive", Path.Combine(cleanCloudStorage, "ICloudDrive"),
			"ICloudDrive", null);

		var cloudStorage = Path.Combine(runtime.HomeDirectory, "Library", "CloudStorage");

		var oneDriveCandidates = ExpandCandidates(
			[Path.Combine(cloudStorage, "OneDrive"), Path.Combine(cloudStorage, "OneDrive-Personal")],
			cloudStorage, "OneDrive-*", checkedPaths)
			.Concat(EnumerateDirectories(cloudStorage, "OneDrive - *", checkedPaths));
		var oneDriveRoots = DistinctPaths(oneDriveCandidates).Where(root =>
		{
			checkedPaths.Add(root);
			return DirectoryExists(root);
		}).ToArray();
		var personalRoots = oneDriveRoots.Where(root => Path.GetFileName(root) == "OneDrive"
			|| Path.GetFileName(root).StartsWith("OneDrive-Personal", StringComparison.Ordinal)).ToArray();
		var personal = SelectPersonal(runtime, personalRoots, personalSelection);
		if (personal is not null && !HasProvider(providers, "OneDrive"))
			AddAccount(providers, checkedPaths, "OneDrive", personal, "OneDriveData", "OneDrive", "Personal");
		foreach (var root in oneDriveRoots.Except(personalRoots))
		{
			var folder = Path.GetFileName(root);
			var account = folder.StartsWith("OneDrive - ", StringComparison.Ordinal) ? folder[11..] : folder[9..];
			AddAccount(providers, checkedPaths, "OneDrive" + ShortName(account, root), root,
				"OneDriveData", "OneDrive", account);
		}

		if (!HasProvider(providers, "Dropbox"))
			AddFirstExisting(
				providers,
				checkedPaths,
				"Dropbox",
				ExpandCandidates(
					[
						Path.Combine(cloudStorage, "Dropbox"),
						Path.Combine(cloudStorage, "Dropbox-Personal")
					],
					cloudStorage,
					"Dropbox-*",
					checkedPaths),
				"DropboxData");

		// Known accounts precede generic aliases so deduplication retains account metadata.
		foreach (var root in EnumerateDirectories(cloudStorage, "GoogleDrive-*", checkedPaths))
		{
			var email = Path.GetFileName(root)[12..];
			var first = email.Split(['.', '@'])[0];
			var name = "GoogleDrive" + ShortName(first, root);
			if (HasCleanProvider(providers, name, runtime.HomeDirectory)) continue;
			AddAccount(providers, checkedPaths, name,
				Path.Combine(root, "My Drive"), "GDriveData", "GoogleDrive", email);
		}
		foreach (var root in DistinctPaths([Path.Combine(cloudStorage, "GoogleDrive"), runtime.SharedGoogleDriveCandidate]))
			if (!HasProvider(providers, "GoogleDrive"))
				AddAccount(providers, checkedPaths, "GoogleDrive", root, "GDriveData", "GoogleDrive", null);

		if (!HasProvider(providers, "ICloudDrive"))
			AddFirstExisting(
				providers,
				checkedPaths,
				"ICloudDrive",
				[
					Path.Combine(runtime.HomeDirectory, "Library", "Mobile Documents", "com~apple~CloudDocs"),
					Path.Combine(cloudStorage, "ICloudDrive")
				],
				"ICloudDriveData");

		return new CloudDetectionResult(providers
			.OrderBy(p => p.Provider == "OneDrive" && p.Account == "Personal" ? 0 : 1)
			.ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.RootPath, StringComparer.Ordinal).ToArray(), DistinctPaths(checkedPaths));
	}

	private static bool HasProvider(IEnumerable<DetectedCloudProvider> providers, string name)
		=> providers.Any(provider => string.Equals(provider.Name, name, StringComparison.OrdinalIgnoreCase));

	private static bool HasCleanProvider(IEnumerable<DetectedCloudProvider> providers, string name, string homeDirectory)
		=> providers.Any(provider => string.Equals(provider.Name, name, StringComparison.OrdinalIgnoreCase)
			&& IsCleanCloudStoragePath(provider.RootPath, homeDirectory));

	private static bool IsCleanCloudStoragePath(string path, string homeDirectory)
	{
		var cleanRoot = Path.GetFullPath(Path.Combine(homeDirectory, ".CloudStorage"));
		var parent = Path.GetDirectoryName(Path.GetFullPath(path));
		return string.Equals(
			Path.TrimEndingDirectorySeparator(parent ?? string.Empty),
			Path.TrimEndingDirectorySeparator(cleanRoot),
			StringComparison.Ordinal);
	}

	private static void AddFirstShortcutExisting(
		ICollection<DetectedCloudProvider> providers,
		ICollection<string> checkedPaths,
		string name,
		IEnumerable<string> candidates,
		string family,
		string? account)
	{
		foreach (var candidate in candidates)
		{
			if (!AddShortcutIfExisting(providers, checkedPaths, name, candidate, family, account)) continue;
			return;
		}
	}

	/// <returns>True when the named shortcut or directory exists.</returns>
	private static bool AddShortcutIfExisting(
		ICollection<DetectedCloudProvider> providers,
		ICollection<string> checkedPaths,
		string name,
		string candidate,
		string family,
		string? account)
	{
		checkedPaths.Add(candidate);
		if (!DirectoryExists(candidate)) return false;
		if (HasProvider(providers, name)) return true;
		var canonical = CanonicalDirectory(candidate);
		if (providers.Any(provider => string.Equals(CanonicalDirectory(provider.RootPath), canonical, StringComparison.Ordinal))) return true;
		providers.Add(new DetectedCloudProvider(name, Path.GetFullPath(candidate), family, account));
		return true;
	}

	private static IEnumerable<string> ExpandCandidates(
		IEnumerable<string> exactCandidates,
		string parent,
		string pattern,
		ICollection<string> checkedPaths)
	{
		var result = exactCandidates.ToList();
		checkedPaths.Add(Path.Combine(parent, pattern));
		result.AddRange(EnumerateDirectories(parent, pattern, checkedPaths));
		return DistinctPaths(result);
	}

	private static void AddFirstExisting(
		ICollection<DetectedCloudProvider> providers,
		ICollection<string> checkedPaths,
		string providerName,
		IEnumerable<string> candidates,
		string innerDirectory)
	{
		foreach (var candidate in DistinctPaths(candidates))
		{
			checkedPaths.Add(candidate);
			if (!Directory.Exists(candidate)) continue;

			var inner = Path.Combine(candidate, innerDirectory);
			checkedPaths.Add(inner);
			providers.Add(new DetectedCloudProvider(
				providerName,
				Path.GetFullPath(Directory.Exists(inner) ? inner : candidate)));
			return;
		}
	}

	private static IEnumerable<string> EnumerateDirectories(string parent, string pattern, ICollection<string> checkedPaths)
	{
		checkedPaths.Add(Path.Combine(parent, pattern));
		if (!DirectoryExists(parent)) return [];
		try
		{
			return Directory.EnumerateDirectories(parent, pattern, SearchOption.TopDirectoryOnly)
				.Order(StringComparer.Ordinal).ToArray();
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			throw new IOException($"Cannot discover cloud accounts in '{parent}': {exception.Message}", exception);
		}
	}

	private static string? SelectPersonal(AmafuRuntime runtime, string[] roots, string? selection)
	{
		if (selection is not null)
		{
			var requested = selection.StartsWith("~/", StringComparison.Ordinal)
				? Path.Combine(runtime.HomeDirectory, selection[2..]) : selection;
			var selected = roots.FirstOrDefault(root => string.Equals(Path.GetFileName(root), requested, StringComparison.Ordinal)
				|| string.Equals(Path.TrimEndingDirectorySeparator(root), Path.TrimEndingDirectorySeparator(Path.GetFullPath(requested)), StringComparison.Ordinal));
			return selected ?? throw new IOException($"Personal OneDrive selection '{selection}' was not detected. Candidates: {string.Join(", ", roots)}");
		}
		if (roots.Length == 0) return null;
		if (File.Exists(runtime.ConfigurationFile))
		{
			var configured = new ConfigurationDocument(File.ReadAllText(runtime.ConfigurationFile)).CloudPaths();
			foreach (var key in new[] { "OneDrive", "OneDrivePersonal" })
			{
				if (!configured.TryGetValue(key, out var path)) continue;
				var expanded = path.StartsWith("~/", StringComparison.Ordinal) ? Path.Combine(runtime.HomeDirectory, path[2..]) : path;
				if (!DirectoryExists(expanded)) continue;
				var canonical = CanonicalDirectory(expanded);
				var match = roots.FirstOrDefault(root => canonical == CanonicalDirectory(root)
					|| canonical == CanonicalDirectory(Path.Combine(root, "OneDriveData")));
				if (match is not null) return match;
			}
		}
		return roots.OrderByDescending(root => Path.GetFileName(root).Length)
			.ThenByDescending(root => NumericSuffix(Path.GetFileName(root)))
			.ThenBy(root => root, StringComparer.Ordinal).FirstOrDefault();
	}

	private static long NumericSuffix(string name)
	{
		var match = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)\)?$");
		return match.Success && long.TryParse(match.Groups[1].Value, out var value) ? value : 0;
	}

	private static string ShortName(string text, string root)
	{
		var name = new string(text.Where(char.IsAsciiLetterOrDigit).ToArray());
		if (name.Length == 0)
			throw new IOException($"Cannot derive a cloud account name from '{root}'. The account must contain letters or digits.");
		return char.ToUpperInvariant(name[0]) + name[1..];
	}

	private static void AddAccount(List<DetectedCloudProvider> providers, ICollection<string> checkedPaths,
		string name, string root, string innerDirectory, string family, string? account)
	{
		checkedPaths.Add(root);
		if (!DirectoryExists(root)) return;
		var inner = Path.Combine(root, innerDirectory);
		checkedPaths.Add(inner);
		var target = Path.GetFullPath(DirectoryExists(inner) ? inner : root);
		var canonical = CanonicalDirectory(target);
		if (providers.Any(p => string.Equals(CanonicalDirectory(p.RootPath), canonical, StringComparison.Ordinal))) return;
		var conflict = providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
		if (conflict is not null)
			throw new IOException($"Cloud account name collision '{name}': '{conflict.RootPath}' (account '{conflict.Account ?? conflict.Name}') and '{target}' (account '{account ?? name}'). Distinct account names are required; nothing was written.");
		providers.Add(new DetectedCloudProvider(name, target, family, account));
	}

	private static bool DirectoryExists(string path)
	{
		try { return (File.GetAttributes(path) & FileAttributes.Directory) != 0; }
		catch (FileNotFoundException) { return false; }
		catch (DirectoryNotFoundException) { return false; }
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			throw new IOException($"Cannot inspect cloud directory '{path}': {exception.Message}", exception);
		}
	}

	// Resolve parent links too (e.g. GoogleDrive -> GoogleDrive-email/My Drive).
	internal static string CanonicalDirectory(string path, int depth = 0)
	{
		if (depth >= 40) throw new IOException($"Too many symbolic links in cloud directory '{path}'.");
		var full = Path.GetFullPath(path);
		var current = Path.GetPathRoot(full)!;
		foreach (var component in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
		{
			current = Path.Combine(current, component);
			var directory = new DirectoryInfo(current);
			if (directory.LinkTarget is not null)
				current = CanonicalDirectory(directory.ResolveLinkTarget(returnFinalTarget: true)?.FullName
					?? throw new IOException($"Cannot resolve cloud directory link '{current}'."), depth + 1);
		}
		return Path.TrimEndingDirectorySeparator(current);
	}

	private static string[] DistinctPaths(IEnumerable<string> paths)
		=> paths
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
}
