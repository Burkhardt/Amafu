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
	public void Detect_PrefersExistingCleanShortcutsBeforeVendorRoots()
	{
		using var fixture = new TestDirectory();
		var cleanOneDrive = fixture.CreateDirectory(".CloudStorage", "OneDrive");
		var cleanGoogle = fixture.CreateDirectory(".CloudStorage", "GoogleDriveRainer");
		var cleanDropbox = fixture.CreateDirectory(".CloudStorage", "Dropbox");
		var cleanICloud = fixture.CreateDirectory(".CloudStorage", "ICloudDrive");
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive", "OneDriveData");
		fixture.CreateDirectory("Library", "CloudStorage", "GoogleDrive-rainer.burkhardt@example.com", "My Drive", "GDriveData");
		fixture.CreateDirectory("Library", "CloudStorage", "Dropbox", "DropboxData");
		fixture.CreateDirectory("Library", "Mobile Documents", "com~apple~CloudDocs", "ICloudDriveData");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath));

		Assert.Equal(
			[cleanOneDrive, cleanDropbox, cleanGoogle, cleanICloud],
			result.Providers.Select(provider => provider.RootPath));
		Assert.Equal(
			["OneDrive", "Dropbox", "GoogleDriveRainer", "ICloudDrive"],
			result.Providers.Select(provider => provider.Name));
		Assert.True(result.CheckedPaths.ToList().IndexOf(cleanOneDrive) < result.CheckedPaths.ToList().IndexOf(
			Path.Combine(fixture.FullPath, "Library", "CloudStorage", "OneDrive")));
	}

	[Fact]
	public void Detect_UsesOneDrivePersonalShortcutWhenGenericShortcutIsAbsent()
	{
		using var fixture = new TestDirectory();
		var shortcut = fixture.CreateDirectory(".CloudStorage", "OneDrivePersonal");
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive-Personal(2)", "OneDriveData");

		var provider = Assert.Single(CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath)).Providers);

		Assert.Equal("OneDrive", provider.Name);
		Assert.Equal(shortcut, provider.RootPath);
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

	[Fact]
	public void Detect_LinuxFindsMountedServerDataRoots()
	{
		using var fixture = new TestDirectory();
		var serverData = fixture.CreateDirectory("srv", "ServerData");
		foreach (var directory in new[] { "OneDriveData", "GDriveData", "DropboxData", "ICloudDriveData" })
			fixture.CreateDirectory("srv", "ServerData", directory);

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath, AmafuPlatform.Linux));

		Assert.Equal(["OneDrive", "Dropbox", "GoogleDrive", "ICloudDrive"],
			result.Providers.Select(provider => provider.Name));
		Assert.Equal(Path.Combine(serverData, "OneDriveData"), result.Providers[0].RootPath);
		Assert.Contains(Path.Combine(serverData, "GDriveData"), result.CheckedPaths);
	}

	[Fact]
	public void Detect_LinuxPrefersShortcutsThenConfiguredRoots()
	{
		using var fixture = new TestDirectory();
		var shortcut = fixture.CreateDirectory(".CloudStorage", "OneDrive");
		fixture.CreateDirectory("srv", "ServerData", "OneDriveData");
		var google = fixture.CreateDirectory("mounted", "GooglePhotos");
		var config = fixture.CreateDirectory(".config");
		File.WriteAllText(Path.Combine(config, "RAIkeep.json5"),
			"{ Cloud: { OneDrive: \"/missing/OneDrive\", GoogleDrive: \"" + google + "\" } }");

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath, AmafuPlatform.Linux));

		Assert.Equal(shortcut, result.Providers.Single(provider => provider.Name == "OneDrive").RootPath);
		Assert.Equal(google, result.Providers.Single(provider => provider.Name == "GoogleDrive").RootPath);
	}

	[Theory]
	[InlineData((int)AmafuPlatform.Windows)]
	[InlineData((int)AmafuPlatform.Other)]
	public void Detect_UnsupportedPlatformsDoNotAssumeCloudRoots(int platformValue)
	{
		using var fixture = new TestDirectory();
		fixture.CreateDirectory("Library", "CloudStorage", "OneDrive");
		var platform = (AmafuPlatform)platformValue;

		var result = CloudStorageDetector.Detect(TestRuntime.Create(fixture.FullPath, platform));

		Assert.Empty(result.Providers);
		Assert.Empty(result.CheckedPaths);
	}
}
