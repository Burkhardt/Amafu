using System.Text.Json;

namespace Amafu.Tests;

public sealed class MultiAccountTests
{
	[Fact]
	public void GoogleAccounts_HaveIndependentRootsMetadataAndStableNames()
	{
		using var home = new TestDirectory();
		var yebo = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-yebo@umshadisi.com", "My Drive");
		var runtime = TestRuntime.Create(home.FullPath);
		Assert.Equal("GoogleDriveYebo", Assert.Single(CloudStorageDetector.Detect(runtime).Providers).Name);
		var rainer = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer.burkhardt@gmail.com", "My Drive", "GDriveData");
		var result = CloudStorageDetector.Detect(runtime);
		Assert.Equal(["GoogleDriveRainer", "GoogleDriveYebo"], result.Providers.Select(p => p.Name));
		Assert.Equal([rainer, yebo], result.Providers.Select(p => p.RootPath));
		Assert.All(result.Providers, p => Assert.Equal("GoogleDrive", p.Provider));
		Assert.Equal(["rainer.burkhardt@gmail.com", "yebo@umshadisi.com"], result.Providers.Select(p => p.Account));
		Assert.Contains(rainer, result.CheckedPaths);
		Assert.Contains(yebo, result.CheckedPaths);
		using var json = JsonDocument.Parse(AmafuConfigurationRenderer.RenderDetectionJson(result, home.FullPath));
		var accounts = json.RootElement.GetProperty("providers");
		Assert.Equal(2, accounts.GetArrayLength());
		Assert.Equal("GoogleDrive", accounts[0].GetProperty("provider").GetString());
		Assert.Equal("yebo@umshadisi.com", accounts[1].GetProperty("account").GetString());
		var config = AmafuConfigurationRenderer.Render(new(home.FullPath, result.Providers));
		Assert.Contains("\"GoogleDriveRainer\"", config);
		Assert.Contains("\"GoogleDriveYebo\"", config);
	}

	[Theory]
	[InlineData("OneDrive-AfricaStage")]
	[InlineData("OneDrive - AfricaStage")]
	public void OneDrive_PersonalPrecedesOrganization(string organization)
	{
		using var home = new TestDirectory();
		var org = home.CreateDirectory("Library", "CloudStorage", organization, "OneDriveData");
		var personal = home.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal");
		var result = CloudStorageDetector.Detect(TestRuntime.Create(home.FullPath));
		Assert.Equal(["OneDrive", "OneDriveAfricaStage"], result.Providers.Select(p => p.Name));
		Assert.Equal([personal, org], result.Providers.Select(p => p.RootPath));
		Assert.Equal(["Personal", "AfricaStage"], result.Providers.Select(p => p.Account));
	}

	[Theory]
	[InlineData("rainer.other@example.com")]
	[InlineData("RAINER@example.com")]
	public void CollisionsFailBeforeAnyCliWrites(string secondEmail)
	{
		using var home = new TestDirectory();
		var first = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer.burkhardt@gmail.com", "My Drive");
		var second = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-" + secondEmail, "My Drive");
		var error = new StringWriter();
		var runtime = TestRuntime.Create(home.FullPath, error: error);
		Assert.Equal(4, AmafuApplication.Run(["init", "--create-links", "--force"], runtime));
		Assert.Contains(first, error.ToString());
		Assert.Contains(second, error.ToString());
		Assert.Contains("collision", error.ToString());
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".config")));
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void ExistingCleanGenericGoogleDriveShortcutIsPreferred()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlinks require Windows developer mode.");
		using var home = new TestDirectory();
		var root = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer@gmail.com", "My Drive");
		Directory.CreateSymbolicLink(Path.Combine(home.FullPath, "Library", "CloudStorage", "GoogleDrive"), root);
		home.CreateDirectory(".CloudStorage");
		var legacy = Path.Combine(home.FullPath, ".CloudStorage", "GoogleDrive");
		Directory.CreateSymbolicLink(legacy, root);
		var yebo = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-yebo@umshadisi.com", "My Drive");
		var providers = CloudStorageDetector.Detect(TestRuntime.Create(home.FullPath)).Providers;
		Assert.Equal(2, providers.Count);
		File.WriteAllText(Path.Combine(root, "original.txt"), "keep");
		CloudStorageLinks.Ensure(home.FullPath, providers);
		CloudStorageLinks.Ensure(home.FullPath, providers);
		Assert.Equal(root, new DirectoryInfo(legacy).LinkTarget);
		Assert.Null(new DirectoryInfo(CloudStorageLinks.AliasPath(home.FullPath, "GoogleDriveRainer")).LinkTarget);
		Assert.Equal(yebo, new DirectoryInfo(CloudStorageLinks.AliasPath(home.FullPath, "GoogleDriveYebo")).LinkTarget);
		Assert.Equal("keep", File.ReadAllText(Path.Combine(home.FullPath, ".CloudStorage", "GoogleDrive", "original.txt")));
	}

	[Theory]
	[InlineData("GoogleDrive../escape")]
	[InlineData("GoogleDriveBad\n")]
	[InlineData("OtherDrive")]
	public void UnsafeShortcutNamesFailWithoutWrites(string name)
	{
		using var home = new TestDirectory();
		var root = home.CreateDirectory("root");
		Assert.Throws<IOException>(() => CloudStorageLinks.Ensure(home.FullPath, [new(name, root)]));
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void EmptyNormalizedAccountNameIsDiagnostic()
	{
		using var home = new TestDirectory();
		home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-___@example.com", "My Drive");
		var error = new StringWriter();
		Assert.Equal(4, AmafuApplication.Run(["detect"], TestRuntime.Create(home.FullPath, error: error)));
		Assert.Contains("Cannot derive", error.ToString());
	}
}
