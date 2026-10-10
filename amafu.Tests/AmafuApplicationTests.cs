namespace Amafu.Tests;

public sealed class AmafuApplicationTests
{
	[Fact]
	public void Help_UsesRaiKeepBannerGlyphsAndAlignedOptions()
	{
		using var fixture = new TestDirectory();
		var output = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, output: output);

		var exitCode = AmafuApplication.Run(["-h"], runtime);

		Assert.Equal(0, exitCode);
		Assert.Contains(" ─────────────────────────", output.ToString());
		Assert.Contains(" Amafu Cloud Bootstrap CLI", output.ToString());
		Assert.Contains("Commands:                 detect, init, reconcile", output.ToString());
		Assert.Contains("-h, --help                print out all options", output.ToString());
	}

	[Fact]
	public void Help_NoLogoSuppressesOnlyBanner()
	{
		using var fixture = new TestDirectory();
		var output = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, output: output);

		var exitCode = AmafuApplication.Run(["--help", "--nologo"], runtime);

		Assert.Equal(0, exitCode);
		Assert.DoesNotContain("Amafu Cloud Bootstrap CLI", output.ToString());
		Assert.Contains("amafu detect", output.ToString());
	}

	[Fact]
	public void Help_NarrowTerminalWrapsDescriptions()
	{
		using var fixture = new TestDirectory();
		var output = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, width: 40, output: output);

		AmafuApplication.Run(["-h", "-n"], runtime);

		Assert.Contains("-h, --help\n    print out all options", Normalize(output.ToString()));
	}

	[Theory]
	[InlineData("-v")]
	[InlineData("--version")]
	public void Version_PrintsCoordinatedVersion(string flag)
	{
		using var fixture = new TestDirectory();
		var output = new StringWriter();

		var exitCode = AmafuApplication.Run([flag], TestRuntime.Create(fixture.FullPath, output: output));

		Assert.Equal(0, exitCode);
		Assert.Equal("amafu v4.5.8", output.ToString().Trim());
	}

	[Fact]
	public void Init_DryRunWritesNothingIncludingConfigDirectory()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive", "OneDriveData");
		var output = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, output: output);

		var exitCode = AmafuApplication.Run(["init", "--dry-run"], runtime);

		Assert.Equal(0, exitCode);
		Assert.Contains("OneDriveData", output.ToString());
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".config")));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void Init_CreatesConfigurationWithUserOnlyWritePermission()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive");
		var runtime = TestRuntime.Create(fixture.FullPath);

		var exitCode = AmafuApplication.Run(["init"], runtime);

		Assert.Equal(0, exitCode);
		Assert.True(File.Exists(runtime.ConfigurationFile));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
		Assert.Contains("\"OneDrive\"", File.ReadAllText(runtime.ConfigurationFile));
		Assert.Contains("~/Library/CloudStorage/OneDrive/", File.ReadAllText(runtime.ConfigurationFile));
		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(
				UnixFileMode.UserRead |
				UnixFileMode.UserWrite |
				UnixFileMode.GroupRead |
				UnixFileMode.OtherRead,
				File.GetUnixFileMode(runtime.ConfigurationFile));
		}
	}

	[Fact]
	public void Init_UsesExistingCleanCloudStoragePathsWithoutCreateLinks()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory(".CloudStorage", "OneDrive");
		fixture.CreateDirectory(".CloudStorage", "GoogleDriveRainer");
		fixture.CreateDirectory(".CloudStorage", "Dropbox");
		fixture.CreateDirectory(".CloudStorage", "ICloudDrive");
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive", "OneDriveData");
		fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer@example.com", "My Drive", "GDriveData");
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox", "DropboxData");
		fixture.CreateDirectory("Library", "Mobile Documents", "com~apple~CloudDocs", "ICloudDriveData");
		var runtime = TestRuntime.Create(fixture.FullPath);

		Assert.Equal(0, AmafuApplication.Run(["init"], runtime));

		var configuration = File.ReadAllText(runtime.ConfigurationFile);
		Assert.Contains("\"OneDrive\": \"~/.CloudStorage/OneDrive/\"", configuration);
		Assert.Contains("\"GoogleDriveRainer\": \"~/.CloudStorage/GoogleDriveRainer/\"", configuration);
		Assert.Contains("\"Dropbox\": \"~/.CloudStorage/Dropbox/\"", configuration);
		Assert.Contains("\"ICloudDrive\": \"~/.CloudStorage/ICloudDrive/\"", configuration);
		Assert.DoesNotContain("~/Library/CloudStorage", configuration);
		Assert.DoesNotContain("~/Library/Mobile Documents", configuration);
	}

	[Fact]
	public void Init_CreateLinksPreservesExistingOneDrivePersonalShortcut()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory(".CloudStorage", "OneDrivePersonal");
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal(2)", "OneDriveData");
		var runtime = TestRuntime.Create(fixture.FullPath);

		Assert.Equal(0, AmafuApplication.Run(["init", "--create-links"], runtime));

		Assert.Contains("\"OneDrive\": \"~/.CloudStorage/OneDrivePersonal/\"", File.ReadAllText(runtime.ConfigurationFile));
	}

	[Fact]
	public void Init_ExistingConfigurationRefusesWithoutMutation()
	{
		using var fixture = new TestDirectory();
		var configDirectory = fixture.CreateDirectory(".config");
		var destination = Path.Combine(configDirectory, "RAIkeep.json5");
		File.WriteAllText(destination, "sacred");
		var error = new StringWriter();

		var exitCode = AmafuApplication.Run(
			["init"],
			TestRuntime.Create(fixture.FullPath, error: error));

		Assert.Equal(3, exitCode);
		Assert.Equal("sacred", File.ReadAllText(destination));
		Assert.Contains("Use --force (-f) to overwrite", error.ToString());
	}

	[Theory]
	[InlineData("-f")]
	[InlineData("--force")]
	public void Init_ForceReplacesExistingConfiguration(string forceFlag)
	{
		using var fixture = new TestDirectory();
		var configDirectory = fixture.CreateDirectory(".config");
		var destination = Path.Combine(configDirectory, "RAIkeep.json5");
		File.WriteAllText(destination, "old");

		var exitCode = AmafuApplication.Run(
			[forceFlag, "init"],
			TestRuntime.Create(fixture.FullPath));

		Assert.Equal(0, exitCode);
		Assert.NotEqual("old", File.ReadAllText(destination));
		Assert.Contains("TempDir", File.ReadAllText(destination));
	}

	[Fact]
	public void InitConfigAliasCreatesSameConfiguration()
	{
		using var fixture = new TestDirectory();

		var exitCode = AmafuApplication.Run(
			["init-config"],
			TestRuntime.Create(fixture.FullPath));

		Assert.Equal(0, exitCode);
		Assert.True(File.Exists(Path.Combine(fixture.FullPath, ".config", "RAIkeep.json5")));
	}

	[Fact]
	public void Detect_JsonIsReadOnly()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox");
		var output = new StringWriter();

		var exitCode = AmafuApplication.Run(
			["--json", "detect"],
			TestRuntime.Create(fixture.FullPath, output: output));

		Assert.Equal(0, exitCode);
		Assert.Contains("\"Dropbox\"", output.ToString());
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".config")));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
	}

	[Theory]
	[InlineData("detect")]
	[InlineData("init")]
	[InlineData("init-config")]
	public void CreateLinks_IsExplicitAndUsesShortcutsInConfiguration(string command)
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode.");
		using var fixture = new TestDirectory();
		var root = fixture.CreateDirectory("Library", "Mobile Documents", "com~apple~CloudDocs");
		var runtime = TestRuntime.Create(fixture.FullPath);

		Assert.Equal(0, AmafuApplication.Run([command, "--create-links"], runtime));
		Assert.Equal(root, new DirectoryInfo(CloudStorageLinks.AliasPath(fixture.FullPath, "ICloudDrive")).LinkTarget);
		if (command == "detect") Assert.False(File.Exists(runtime.ConfigurationFile));
		else
		{
			var config = File.ReadAllText(runtime.ConfigurationFile);
			Assert.Contains("~/.CloudStorage/ICloudDrive/", config);
			Assert.DoesNotContain("~/Library/Mobile Documents/com~apple~CloudDocs/", config);
		}
	}

	[Theory]
	[InlineData("detect")]
	[InlineData("init")]
	public void CreateLinks_DryRunWritesNothing(string command)
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive-user@example.com", "My Drive");
		Assert.Equal(0, AmafuApplication.Run([command, "--create-links", "--dry-run"], TestRuntime.Create(fixture.FullPath)));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".config")));
	}

	[Theory]
	[InlineData("init")]
	[InlineData("init-config")]
	public void Init_CreateLinksPreviewMatchesSavedConfigAndLinksReachEveryRoot(string command)
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode.");
		using var fixture = new TestDirectory();
		var roots = new Dictionary<string, string>
		{
			["GoogleDriveRainer"] = fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer.burkhardt@gmail.com", "My Drive", "GDriveData"),
			["GoogleDriveYebo"] = fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive-yebo@umshadisi.com", "My Drive"),
			["ICloudDrive"] = fixture.CreateDirectory("Library", "Mobile Documents", "com~apple~CloudDocs")
		};
		foreach (var (name, root) in roots) File.WriteAllText(Path.Combine(root, "sample.txt"), name);
		var output = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, output: output);
		Assert.Equal(0, AmafuApplication.Run([command, "--create-links", "--dry-run"], runtime));
		var preview = output.ToString();
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
		Assert.False(File.Exists(runtime.ConfigurationFile));

		Assert.Equal(0, AmafuApplication.Run([command, "--create-links"], runtime));
		var saved = File.ReadAllText(runtime.ConfigurationFile);
		Assert.Equal(preview, saved);
		var paths = new ConfigurationDocument(saved).CloudPaths();
		Assert.Equal(3, paths.Count);
		foreach (var (name, root) in roots)
		{
			Assert.Equal($"~/.CloudStorage/{name}/", paths[name]);
			var alias = CloudStorageLinks.AliasPath(fixture.FullPath, name);
			Assert.Equal(root, new DirectoryInfo(alias).LinkTarget);
			Assert.Equal(name, File.ReadAllText(Path.Combine(alias, "sample.txt")));
		}
	}

	[Fact]
	public void CreateLinks_JsonRemainsParseableAndReportsRealRoot()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode.");
		using var fixture = new TestDirectory();
		var root = fixture.CreateDirectory("Library", "CloudStorage", "Dropbox");
		var output = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(["detect", "--create-links", "--json"], TestRuntime.Create(fixture.FullPath, output: output)));
		using var json = System.Text.Json.JsonDocument.Parse(output.ToString());
		Assert.Contains("~/Library/CloudStorage/Dropbox/", output.ToString());
		Assert.Equal(root, new DirectoryInfo(CloudStorageLinks.AliasPath(fixture.FullPath, "Dropbox")).LinkTarget);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CreateLinks_ConflictPreservesConfigurationEvenWithForce(bool force)
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox");
		fixture.CreateDirectory(".CloudStorage", "Dropbox");
		fixture.CreateDirectory(".config");
		var error = new StringWriter();
		var runtime = TestRuntime.Create(fixture.FullPath, error: error);
		File.WriteAllText(runtime.ConfigurationFile, "keep");
		string[] args = force ? ["init", "--create-links", "--force"] : ["detect", "--create-links"];
		Assert.Equal(4, AmafuApplication.Run(args, runtime));
		Assert.Equal("keep", File.ReadAllText(runtime.ConfigurationFile));
		Assert.Contains("existing file or directory", error.ToString());
		Assert.Null(new DirectoryInfo(CloudStorageLinks.AliasPath(fixture.FullPath, "Dropbox")).LinkTarget);
	}

	[Fact]
	public void Init_ExistingConfigurationPreventsLinkCreation()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox");
		fixture.CreateDirectory(".config");
		var runtime = TestRuntime.Create(fixture.FullPath);
		File.WriteAllText(runtime.ConfigurationFile, "keep");
		Assert.Equal(3, AmafuApplication.Run(["init", "--create-links"], runtime));
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".CloudStorage")));
		Assert.Equal("keep", File.ReadAllText(runtime.ConfigurationFile));
	}

	[Fact]
	public void ElevatedExecutionIsRejectedBeforeFilesystemAccess()
	{
		using var fixture = new TestDirectory();
		var error = new StringWriter();

		var exitCode = AmafuApplication.Run(
			["init"],
			TestRuntime.Create(fixture.FullPath, elevated: true, error: error));

		Assert.Equal(2, exitCode);
		Assert.Contains("Do not use sudo", error.ToString());
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".config")));
	}

	[Fact]
	public void UnknownOptionIsRejectedBeforeFilesystemAccess()
	{
		using var fixture = new TestDirectory();
		var error = new StringWriter();

		var exitCode = AmafuApplication.Run(
			["init", "--surprise"],
			TestRuntime.Create(fixture.FullPath, error: error));

		Assert.Equal(2, exitCode);
		Assert.Contains("Unknown option '--surprise'", error.ToString());
		Assert.False(Directory.Exists(Path.Combine(fixture.FullPath, ".config")));
	}

	private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
