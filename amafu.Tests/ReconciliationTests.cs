namespace Amafu.Tests;

public sealed class ReconciliationTests
{
	private static string Configure(TestDirectory home, string cloud, string order = "['OneDrive']")
	{
		var directory = home.CreateDirectory(".config");
		var text = """
		{
		  // Keep user settings exactly, including JSON5 extensions.
		  TempDir: '~/custom-temp/',
		  LocalBackupDir: '~/custom-backup/',
		  SyncPropagationDelayMs: 12345,
		  Observers: [{ Name: 'Mine', Value: 0x10, }],
		  Cloud: CLOUD,
		  DefaultCloudOrder: ORDER,
		}
		""".Replace("CLOUD", cloud).Replace("ORDER", order);
		File.WriteAllText(Path.Combine(directory, "RAIkeep.json5"), text);
		return text;
	}

	[Fact]
	public void PersonalSelectionIsAutomaticLongestThenHighestNumber_AndOverrideRetainsCorporate()
	{
		using var home = new TestDirectory();
		foreach (var name in new[] { "OneDrive", "OneDrive-Personal", "OneDrive-Personal(2)", "OneDrive-Personal(9)", "OneDrive-Contoso" })
			home.CreateDirectory("Library", "CloudStorage", name);
		var runtime = TestRuntime.Create(home.FullPath);
		var result = CloudStorageDetector.Detect(runtime);
		Assert.Equal(["OneDrive", "OneDriveContoso"], result.Providers.Select(p => p.Name));
		Assert.EndsWith("OneDrive-Personal(9)", result.Providers[0].RootPath);
		var selected = CloudStorageDetector.Detect(runtime, "OneDrive-Personal");
		Assert.EndsWith("OneDrive-Personal", selected.Providers[0].RootPath);
		Assert.Equal(2, selected.Providers.Count);
		Assert.Equal(4, AmafuApplication.Run(["detect", "--onedrive-personal", "missing"], runtime));
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void ValidConfiguredSelectionWinsOverLongerFolder()
	{
		using var home = new TestDirectory();
		home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal");
		home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal(99)");
		Configure(home, "{OneDrive: '~/Library/CloudStorage/OneDrive-Personal/'}");
		var result = CloudStorageDetector.Detect(TestRuntime.Create(home.FullPath));
		Assert.EndsWith("OneDrive-Personal", Assert.Single(result.Providers).RootPath);
	}

	[Fact]
	public void PreviewWritesNothing_ApplyBacksUpAndPreservesSettingsAndOrder()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlinks require Windows developer mode.");
		using var home = new TestDirectory();
		home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal");
		home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-yebo@umshadisi.com", "My Drive");
		var original = Configure(home, "{OneDrive: '~/Library/CloudStorage/OneDrive-Personal/'}");
		var output = new StringWriter();
		var runtime = TestRuntime.Create(home.FullPath, output: output);
		Assert.Equal(0, AmafuApplication.Run(["reconcile"], runtime));
		Assert.Equal(original, File.ReadAllText(runtime.ConfigurationFile));
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
		Assert.Single(Directory.GetFiles(Path.GetDirectoryName(runtime.ConfigurationFile)!));
		Assert.Contains("GoogleDriveYebo", output.ToString());
		Assert.Equal(0, AmafuApplication.Run(["reconcile", "--apply"], runtime));
		var updated = File.ReadAllText(runtime.ConfigurationFile);
		Assert.Contains("TempDir: '~/custom-temp/'", updated);
		Assert.Contains("Observers: [{ Name: 'Mine', Value: 0x10, }]", updated);
		Assert.Contains("SyncPropagationDelayMs: 12345", updated);
		Assert.Contains("[\"OneDrive\", \"GoogleDriveYebo\"]", updated);
		Assert.Equal("~/.CloudStorage/OneDrive/", new ConfigurationDocument(updated).CloudPaths()["OneDrive"]);
		var backup = Assert.Single(Directory.GetFiles(Path.GetDirectoryName(runtime.ConfigurationFile)!, "*.before-reconcile-*"));
		Assert.Equal(original, File.ReadAllText(backup));
		Assert.Equal(0, AmafuApplication.Run(["reconcile", "--apply"], runtime));
		Assert.Equal(updated, File.ReadAllText(runtime.ConfigurationFile));
		Assert.Single(Directory.GetFiles(Path.GetDirectoryName(runtime.ConfigurationFile)!, "*.before-reconcile-*"));
	}

	[Fact]
	public void ExplicitPersonalSwitchCanRepointOnlyTheConfiguredLink()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlinks require Windows developer mode.");
		using var home = new TestDirectory();
		var oldRoot = home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal");
		var nextRoot = home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal(2)");
		home.CreateDirectory(".CloudStorage");
		var alias = Path.Combine(home.FullPath, ".CloudStorage", "OneDrive");
		Directory.CreateSymbolicLink(alias, oldRoot);
		Configure(home, "{OneDrive: '~/.CloudStorage/OneDrive/'}");
		File.WriteAllText(Path.Combine(oldRoot, "keep.txt"), "keep");
		var runtime = TestRuntime.Create(home.FullPath);
		Assert.Equal(0, AmafuApplication.Run(["reconcile", "--onedrive-personal", "OneDrive-Personal(2)"], runtime));
		Assert.Equal(oldRoot, new DirectoryInfo(alias).LinkTarget);
		Assert.Equal(0, AmafuApplication.Run(["reconcile", "--apply", "--onedrive-personal", "OneDrive-Personal(2)"], runtime));
		Assert.Equal(CloudStorageDetector.CanonicalDirectory(nextRoot), new DirectoryInfo(alias).LinkTarget);
		Assert.Equal("keep", File.ReadAllText(Path.Combine(oldRoot, "keep.txt")));
	}

	[Fact]
	public void ConflictPreventsBackupsLinksAndConfigWrites()
	{
		using var home = new TestDirectory();
		home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-yebo@umshadisi.com", "My Drive");
		home.CreateDirectory(".CloudStorage", "GoogleDriveYebo");
		var original = Configure(home, "{}", "[]");
		var runtime = TestRuntime.Create(home.FullPath);
		Assert.Equal(4, AmafuApplication.Run(["reconcile", "--apply"], runtime));
		Assert.Equal(original, File.ReadAllText(runtime.ConfigurationFile));
		Assert.Single(Directory.GetFiles(Path.GetDirectoryName(runtime.ConfigurationFile)!));
	}

	[Fact]
	public void ParserPreservesNestedUnrelatedTextAndRejectsAmbiguousDuplicateKeys()
	{
		var doc = new ConfigurationDocument("{ Cloud: {'GoogleDrive': '~/a\\x20b/'}, /*keep*/ Other: [NaN, {x:'}'}], }");
		Assert.Equal("~/a b/", doc.CloudPaths()["GoogleDrive"]);
		var result = doc.Rewrite(new Dictionary<string, string> { ["GoogleDrive"] = "~/.CloudStorage/GoogleDrive/" }, ["GoogleDrive"]);
		Assert.Contains("/*keep*/ Other: [NaN, {x:'}'}]", result);
		Assert.Contains("DefaultCloudOrder", result);
		Assert.Throws<IOException>(() => new ConfigurationDocument("{Cloud:{},Cloud:{}}"));
	}
}
