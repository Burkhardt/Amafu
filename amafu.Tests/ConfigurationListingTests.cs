using OsLib;

namespace Amafu.Tests;

public sealed class ConfigurationListingTests
{
	private const string Configuration = """
		{
		  // Paths need not exist; listing must not create them.
		  TempDir: '~/scratch dir', LocalBackupDir: '~/backup',
		  DefaultCloudOrder: ['OneDrive', 'Dropbox', 'GoogleDrive', 'ICloudDrive'],
		  Cloud: {OneDrive:'~/cloud OneDrive/', Dropbox:'~/dropbox/', GoogleDrive:'~/gdrive/', ICloudDrive:'~/icloud/', Custom:'~/custom/'},
		  Observers: [{Name:'Mzansi', SshTarget:'rsb@Mzansi'}, {Name:'Ingwe', SshTarget:'rsb@ingwe'}, {Name:'Mdlaka', SshTarget:'umshadisi@Mdlaka'}],
		  PrivateSetting: 'not for listing',
		}
		""";

	private static AmafuRuntime Runtime(TestDirectory home, StringWriter output, StringWriter error, string? source = Configuration)
	{
		var runtime = TestRuntime.Create(home.FullPath, output: output, error: error);
		if (source is not null)
		{
			home.CreateDirectory(".config");
			File.WriteAllText(runtime.ConfigurationFile, source);
		}
		return runtime;
	}

	[Theory]
	[InlineData("list order OneDrive", "1")]
	[InlineData("list order iClouddrive", "4")]
	[InlineData("list order Missing", "0")]
	[InlineData("list order Custom", "0")]
	[InlineData("list observer Mzansi", "rsb@Mzansi")]
	[InlineData("list observer ingwe", "rsb@ingwe")]
	public void ScalarQueries_PrintOnlyTheConfiguredValue(string command, string expected)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(command.Split(' '), Runtime(home, output, error)));
		Assert.Equal(expected + Environment.NewLine, output.ToString());
		Assert.Equal("", error.ToString());
	}

	[Theory]
	[InlineData("list temp", "scratch dir")]
	[InlineData("list backup", "backup")]
	[InlineData("list cloud OneDrive", "cloud OneDrive")]
	public void PathQueries_ExpandHomeWithoutCreatingDirectories(string command, string relative)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(command.Split(' '), Runtime(home, output, error)));
		Assert.Equal(Path.Combine(home.FullPath, relative) + Path.DirectorySeparatorChar + Environment.NewLine, output.ToString());
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, relative)));
	}

	[Fact]
	public void OrderAll_UsesExactGlyphsAndConfiguredSequence()
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(["list", "order", "all"], Runtime(home, output, new StringWriter())));
		Assert.Equal("  󰰕 OneDrive 󰯴 Dropbox 󰯽 GoogleDrive 󰰃 ICloudDrive" + Environment.NewLine, output.ToString());
	}

	[Fact]
	public void CloudAll_UsesTwoLinesPerProviderAndIncludesUnorderedEntries()
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(["list", "cloud", "all"], Runtime(home, output, new StringWriter())));
		var names = new[] {"OneDrive", "Dropbox", "GoogleDrive", "ICloudDrive", "Custom"};
		var paths = new[] {"cloud OneDrive", "dropbox", "gdrive", "icloud", "custom"};
		var expected = names.Select((name, index) => CliHelpStyle.CloudGlyph(name, index + 1) + " " + name + Environment.NewLine + "\t\""
			+ AmafuConfigurationRenderer.Escape(Path.Combine(home.FullPath, paths[index]) + Path.DirectorySeparatorChar) + "\"" + Environment.NewLine);
		Assert.Equal(string.Concat(expected), output.ToString());
		Assert.DoesNotContain("\u001b", output.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void ObserverAll_AlignsNamesAndTargetsWithoutUnrelatedSettings()
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(["list", "observer", "all"], Runtime(home, output, new StringWriter())));
		Assert.Equal(string.Join(Environment.NewLine, "  Mzansi:  rsb@Mzansi", "  Ingwe:   rsb@ingwe", "  Mdlaka:  umshadisi@Mdlaka", ""), output.ToString());
	}

	[Theory]
	[InlineData("{Observers: {Mzansi: 'rsb@Mzansi'}}")]
	[InlineData("{Observers: {Mzansi: {SshTarget:'rsb@Mzansi', Token:'never print'}}}")]
	public void ObserverLookup_SupportsNamedEntriesWithoutLeakingOtherFields(string source)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		Assert.Equal(0, AmafuApplication.Run(["list", "observer", "Mzansi"], Runtime(home, output, new StringWriter(), source)));
		Assert.Equal("rsb@Mzansi" + Environment.NewLine, output.ToString());
	}

	[Theory]
	[InlineData("list")]
	[InlineData("list all")]
	[InlineData("list cloud")]
	[InlineData("list order")]
	[InlineData("list observer")]
	[InlineData("list temp all")]
	[InlineData("list backup all")]
	[InlineData("list cloud all extra")]
	[InlineData("list observer all --force")]
	public void InvalidSyntax_FailsBeforeReadingOrCreatingConfiguration(string command)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();
		Assert.Equal(2, AmafuApplication.Run(command.Split(' '), Runtime(home, output, error, null)));
		Assert.Equal("", output.ToString());
		Assert.Contains("amafu list", error.ToString());
		Assert.Empty(Directory.GetFileSystemEntries(home.FullPath));
	}

	[Theory]
	[InlineData("list order OneDrive", null)]
	[InlineData("list order Missing", "{DefaultCloudOrder: nope}")]
	[InlineData("list cloud all", "{Cloud: {OneDrive: null}}")]
	[InlineData("list observer Missing", Configuration)]
	[InlineData("list cloud Missing", Configuration)]
	[InlineData("list temp", "{TempDir: 42}")]
	[InlineData("list backup", "{}")]
	[InlineData("list observer all", "{Observers:[{Name:'Mzansi'}]}")]
	[InlineData("list order all", "{DefaultCloudOrder:['OneDrive','onedrive']}")]
	[InlineData("list cloud all", "{Cloud:{OneDrive:'~/a',onedrive:'~/b'}}")]
	[InlineData("list observer all", "{Observers:{Host:'user@host\\nINJECTED'}}")]
	public void ConfigurationErrors_HaveEmptyStdout(string command, string? source)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();
		Assert.Equal(4, AmafuApplication.Run(command.Split(' '), Runtime(home, output, error, source)));
		Assert.Equal("", output.ToString());
		Assert.NotEmpty(error.ToString());
	}

	[Theory]
	[InlineData("temp", "TempDir")]
	[InlineData("backup", "LocalBackupDir")]
	[InlineData("order", "1-based")]
	[InlineData("cloud", "double-quoted")]
	[InlineData("observer", "SSH target")]
	public void ContextualHelp_WorksWithoutConfiguration(string selector, string detail)
	{
		using var home = new TestDirectory();
		foreach (var flag in new[] {"-h", "--help"})
		{
			var output = new StringWriter();
			Assert.Equal(0, AmafuApplication.Run([flag, "list", selector, "-n"], Runtime(home, output, new StringWriter(), null)));
			Assert.Contains("amafu list " + selector, output.ToString());
			Assert.Contains(detail, output.ToString());
			if (selector == "order") Assert.Contains("print 0", output.ToString());
			Assert.DoesNotContain("\t", output.ToString(), StringComparison.Ordinal);
			Assert.DoesNotContain("\u001b", output.ToString(), StringComparison.Ordinal);
		}
		Assert.Empty(Directory.GetFileSystemEntries(home.FullPath));
	}

	[Fact]
	public void ProviderStyles_MatchSharedFormatterIncludingNumberedBadgesAndPaletteCycling()
	{
		var names = new[] {"OneDrive", "Dropbox", "GoogleDrive", "ICloudDrive", "GoogleDriveRainer", "Extra1", "Extra2", "Extra3", "Extra4", "Extra5"};
		var styled = string.Join(CliHelpStyle.Paint(", ", "33", true), names.Select((name, index) => ConfigurationListing.Provider(name, index, true)));
		Assert.Equal(CliHelpStyle.CloudOptions(names, true), styled);
	}

	[Fact]
	public void Queries_PreserveConfigurationBytesTimestampAndDirectoryTree()
	{
		using var home = new TestDirectory();
		var runtime = Runtime(home, new StringWriter(), new StringWriter());
		var before = File.ReadAllBytes(runtime.ConfigurationFile);
		var modified = File.GetLastWriteTimeUtc(runtime.ConfigurationFile);
		var entries = Directory.GetFileSystemEntries(home.FullPath, "*", SearchOption.AllDirectories);
		foreach (var command in new[] {"list temp", "list backup", "list order OneDrive", "list order Missing", "list order all", "list cloud OneDrive", "list cloud all", "list observer Mzansi", "list observer all"})
			Assert.Equal(0, AmafuApplication.Run(command.Split(' '), runtime));
		Assert.Equal(before, File.ReadAllBytes(runtime.ConfigurationFile));
		Assert.Equal(modified, File.GetLastWriteTimeUtc(runtime.ConfigurationFile));
		Assert.Equal(entries, Directory.GetFileSystemEntries(home.FullPath, "*", SearchOption.AllDirectories));
	}
}
