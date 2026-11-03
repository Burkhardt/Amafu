namespace Amafu.Tests;

public sealed class ConfigurationListingTests
{
	[Theory]
	[InlineData("MacOS")]
	[InlineData("Linux")]
	[InlineData("Windows")]
	public void ListAll_PrintsTheCompleteSavedDocumentWithoutChanges(string platform)
	{
		using var home = new TestDirectory();
		home.CreateDirectory(".config");
		var output = new StringWriter();
		var error = new StringWriter();
		var runtime = TestRuntime.Create(home.FullPath, Enum.Parse<AmafuPlatform>(platform), output: output, error: error);
		const string source = "// All settings, not just detected providers\n{ Cloud: { Custom: '~/offline/' }, Observers: { Host: 'user@host' }, Other: [1, true], }";
		File.WriteAllText(runtime.ConfigurationFile, source);
		var before = File.ReadAllBytes(runtime.ConfigurationFile);
		var modified = File.GetLastWriteTimeUtc(runtime.ConfigurationFile);

		Assert.Equal(0, AmafuApplication.Run(["list", "all"], runtime));
		Assert.Equal(source, output.ToString());
		Assert.Equal(string.Empty, error.ToString());
		Assert.Equal(before, File.ReadAllBytes(runtime.ConfigurationFile));
		Assert.Equal(modified, File.GetLastWriteTimeUtc(runtime.ConfigurationFile));
		Assert.Single(Directory.GetFiles(home.FullPath, "*", SearchOption.AllDirectories));
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void ListAll_MissingConfigurationReportsErrorWithoutCreatingAnything()
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();
		var runtime = TestRuntime.Create(home.FullPath, output: output, error: error);

		Assert.Equal(4, AmafuApplication.Run(["list", "all"], runtime));
		Assert.Equal(string.Empty, output.ToString());
		Assert.Contains("Configuration file not found", error.ToString());
		Assert.Empty(Directory.GetFileSystemEntries(home.FullPath));
	}

	[Theory]
	[InlineData("list")]
	[InlineData("list cloud")]
	[InlineData("list all all")]
	[InlineData("list all --force")]
	public void List_RejectsUnsupportedArguments(string arguments)
	{
		using var home = new TestDirectory();
		var output = new StringWriter();
		var error = new StringWriter();

		Assert.Equal(2, AmafuApplication.Run(arguments.Split(' '), TestRuntime.Create(home.FullPath, output: output, error: error)));
		Assert.Equal(string.Empty, output.ToString());
		Assert.NotEmpty(error.ToString());
		Assert.Empty(Directory.GetFileSystemEntries(home.FullPath));
	}

	[Fact]
	public void ListHelp_ExplainsSavedConfigurationWithoutReadingIt()
	{
		using var home = new TestDirectory();
		var output = new StringWriter();

		Assert.Equal(0, AmafuApplication.Run(["list", "-h", "-n"], TestRuntime.Create(home.FullPath, output: output)));
		Assert.Contains("amafu list all", output.ToString());
		Assert.Contains("~/.config/RAIkeep.json5", output.ToString());
		Assert.DoesNotContain("\t", output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain("\u001b", output.ToString(), StringComparison.Ordinal);
		Assert.Empty(Directory.GetFileSystemEntries(home.FullPath));
	}
}
