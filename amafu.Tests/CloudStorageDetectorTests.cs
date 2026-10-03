namespace Amafu.Tests;

public sealed class CloudStorageDetectorTests
{
	[Fact]
	public void Detect_UsesPersonalOneDriveAndInnerDataDirectory()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive", "OneDriveData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		var oneDrive = Assert.Single(result.Providers);
		Assert.Equal("OneDrive", oneDrive.Name);
		Assert.Equal(
			Path.Combine(fixture.FullPath, "Library", "CloudStorage", "OneDrive", "OneDriveData"),
			oneDrive.RootPath);
	}

	[Fact]
	public void Detect_FallsBackToProviderRootWhenInnerDirectoryIsMissing()
	{
		using var fixture = new TestDirectory();
		var providerRoot = fixture.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		Assert.Equal(providerRoot, Assert.Single(result.Providers).RootPath);
	}

	[Fact]
	public void Detect_OrdersWildcardCandidatesDeterministically()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox-Zulu");
		var expected = fixture.CreateDirectory("Library", "CloudStorage", "Dropbox-Africa", "DropboxData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		var dropbox = Assert.Single(result.Providers);
		Assert.Equal("Dropbox", dropbox.Name);
		Assert.Equal(expected, dropbox.RootPath);
	}

	[Fact]
	public void Detect_FindsGoogleDriveMyDriveAndInnerDataDirectory()
	{
		using var fixture = new TestDirectory();
		var expected = fixture.CreateDirectory(
			"Library", "CloudStorage", "GoogleDrive-user@example.com", "My Drive", "GDriveData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		var google = Assert.Single(result.Providers);
		Assert.Equal("GoogleDriveUser", google.Name);
		Assert.Equal(expected, google.RootPath);
	}

	[Fact]
	public void Detect_FindsSharedGoogleDriveCandidate()
	{
		using var fixture = new TestDirectory();
		var shared = fixture.CreateDirectory("Shared", "ServerData", "GDriveData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(
			fixture.FullPath,
			sharedGoogleDriveCandidate: shared));

		Assert.Equal(shared, Assert.Single(result.Providers).RootPath);
	}

	[Fact]
	public void Detect_FindsICloudAndInnerDataDirectory()
	{
		using var fixture = new TestDirectory();
		var expected = fixture.CreateDirectory(
			"Library", "Mobile Documents", "com~apple~CloudDocs", "ICloudDriveData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		var iCloud = Assert.Single(result.Providers);
		Assert.Equal("ICloudDrive", iCloud.Name);
		Assert.Equal(expected, iCloud.RootPath);
	}

	[Fact]
	public void Detect_EmitsProvidersInContractOrder()
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive");
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox");
		fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive");
		fixture.CreateDirectory("Library", "CloudStorage", "ICloudDrive");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		Assert.Equal(
			["OneDrive", "Dropbox", "GoogleDrive", "ICloudDrive"],
			result.Providers.Select(provider => provider.Name));
	}

	[Theory]
	[InlineData((int)AmafuPlatform.Linux)]
	[InlineData((int)AmafuPlatform.Windows)]
	[InlineData((int)AmafuPlatform.Other)]
	public void Detect_NonMacPlatformsDoNotAssumeCloudRoots(int platformValue)
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive");
		var platform = (AmafuPlatform)platformValue;

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath, platform));

		Assert.Empty(result.Providers);
		Assert.Empty(result.CheckedPaths);
	}
}
