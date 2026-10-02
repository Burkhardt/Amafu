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
		Assert.Contains("Commands:                 detect, init", output.ToString());
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
		Assert.Equal("amafu 4.4.8", output.ToString().Trim());
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
		Assert.Contains("\"OneDrive\"", File.ReadAllText(runtime.ConfigurationFile));
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
