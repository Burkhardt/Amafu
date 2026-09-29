namespace Amafu;

internal static class CloudStorageDetector
{
	internal static CloudDetectionResult Detect(AmafuRuntime runtime)
	{
		var providers = new List<DetectedCloudProvider>();
		var checkedPaths = new List<string>();

		if (runtime.Platform != AmafuPlatform.MacOS)
			return new CloudDetectionResult(providers, checkedPaths);

		var cloudStorage = Path.Combine(runtime.HomeDirectory, "Library", "CloudStorage");

		AddFirstExisting(
			providers,
			checkedPaths,
			"OneDrive",
			ExpandCandidates(
				[
					Path.Combine(cloudStorage, "OneDrive"),
					Path.Combine(cloudStorage, "OneDrive-Personal"),
					Path.Combine(cloudStorage, "OneDrive-Personal(2)")
				],
				cloudStorage,
				"OneDrive-*",
				checkedPaths),
			"OneDriveData");

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

		var googleCandidates = new List<string>();
		var googlePattern = Path.Combine(cloudStorage, "GoogleDrive-*");
		checkedPaths.Add(googlePattern);
		foreach (var root in EnumerateDirectories(cloudStorage, "GoogleDrive-*"))
			googleCandidates.Add(Path.Combine(root, "My Drive"));
		googleCandidates.Add(Path.Combine(cloudStorage, "GoogleDrive"));
		googleCandidates.Add(runtime.SharedGoogleDriveCandidate);
		AddFirstExisting(
			providers,
			checkedPaths,
			"GoogleDrive",
			googleCandidates,
			"GDriveData");

		AddFirstExisting(
			providers,
			checkedPaths,
			"ICloudDrive",
			[
				Path.Combine(runtime.HomeDirectory, "Library", "Mobile Documents", "com~apple~CloudDocs"),
				Path.Combine(cloudStorage, "ICloudDrive")
			],
			"ICloudDriveData");

		return new CloudDetectionResult(providers, DistinctPaths(checkedPaths));
	}

	private static IEnumerable<string> ExpandCandidates(
		IEnumerable<string> exactCandidates,
		string parent,
		string pattern,
		ICollection<string> checkedPaths)
	{
		var result = exactCandidates.ToList();
		checkedPaths.Add(Path.Combine(parent, pattern));
		result.AddRange(EnumerateDirectories(parent, pattern));
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

	private static IEnumerable<string> EnumerateDirectories(string parent, string pattern)
	{
		if (!Directory.Exists(parent)) return [];
		try
		{
			return Directory
				.EnumerateDirectories(parent, pattern, SearchOption.TopDirectoryOnly)
				.Order(StringComparer.Ordinal)
				.ToArray();
		}
		catch (IOException)
		{
			return [];
		}
		catch (UnauthorizedAccessException)
		{
			return [];
		}
	}

	private static string[] DistinctPaths(IEnumerable<string> paths)
		=> paths
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
}
