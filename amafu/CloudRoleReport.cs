namespace Amafu;

/// <summary>Read-only role diagnostics in the standalone configuration bootstrapper.</summary>
internal sealed record CloudRoleReport(string? LiveCloud, string? TestCloud, IReadOnlyList<string> Warnings)
{
	internal static CloudRoleReport Read(AmafuRuntime runtime)
	{
		if (!File.Exists(runtime.ConfigurationFile)) return new(null, null, []);
		return Evaluate(new ConfigurationDocument(File.ReadAllText(runtime.ConfigurationFile)), runtime.HomeDirectory);
	}
	internal static CloudRoleReport Evaluate(ConfigurationDocument document, string home)
	{
		var roots = document.CloudPaths();
		foreach (var key in new[] { "LiveCloud", "TestCloud" })
		{
			var provider = document.OptionalSetting(key);
			if (provider is not null && !roots.ContainsKey(provider))
				throw new IOException($"{key} must name a configured Cloud entry; got '{provider}'. Configured Cloud entries: {string.Join(", ", roots.Keys)}");
		}
		var live = document.OptionalSetting("LiveCloud") ?? document.Order(required: false).FirstOrDefault();
		var test = document.OptionalSetting("TestCloud");
		var warnings = new List<string>();
		if (test is null && roots.Count > 1) warnings.Add("TestCloud is unset while multiple cloud roots are configured.");
		if (test is not null && test == live) warnings.Add("TestCloud equals LiveCloud; test and live storage are not separated.");
		if (test is not null && live is not null && roots.TryGetValue(live, out var liveRoot))
		{
			string Normalize(string path) => Path.TrimEndingDirectorySeparator(CloudStorageDetector.CanonicalDirectory(
				path.StartsWith("~/", StringComparison.Ordinal) ? Path.Combine(home, path[2..]) : path)) + Path.DirectorySeparatorChar;
			var l = Normalize(liveRoot);
			var t = Normalize(roots[test]);
			var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if (l.StartsWith(t, comparison) || t.StartsWith(l, comparison))
				warnings.Add("LiveCloud and TestCloud roots overlap: one is equal to or nested inside the other.");
		}
		return new(live, test, warnings);
	}
}
