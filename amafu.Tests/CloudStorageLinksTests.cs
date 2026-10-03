namespace Amafu.Tests;

public sealed class CloudStorageLinksTests
{
	[Fact]
	public void LinksReachOriginalData_AndRepeatedDetectionPreservesThem()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode; provider discovery is macOS-only.");
		using var home = new TestDirectory();
		var google = home.CreateDirectory("Library", "CloudStorage", "GoogleDrive-user@example.com", "My Drive");
		var icloud = home.CreateDirectory("Library", "Mobile Documents", "com~apple~CloudDocs");
		File.WriteAllText(Path.Combine(google, "example.txt"), "original");
		DetectedCloudProvider[] providers = [new("GoogleDrive", google), new("ICloudDrive", icloud)];

		CloudStorageLinks.Ensure(home.FullPath, providers);
		var alias = CloudStorageLinks.AliasPath(home.FullPath, "GoogleDrive");
		Assert.Equal(google, new DirectoryInfo(alias).LinkTarget);
		Assert.Equal("original", File.ReadAllText(Path.Combine(alias, "example.txt")));
		CloudStorageLinks.Ensure(home.FullPath, providers);
		Assert.Equal(google, new DirectoryInfo(alias).LinkTarget);
		Assert.Equal(icloud, new DirectoryInfo(CloudStorageLinks.AliasPath(home.FullPath, "ICloudDrive")).LinkTarget);
		Assert.Equal("original", File.ReadAllText(Path.Combine(google, "example.txt")));
	}

	[Theory]
	[InlineData("directory")]
	[InlineData("file")]
	[InlineData("different-link")]
	[InlineData("broken-link")]
	public void ConflictPreservesExistingPath_AndPreventsOtherLinks(string kind)
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode; provider discovery is macOS-only.");
		using var home = new TestDirectory();
		var google = home.CreateDirectory("google");
		var icloud = home.CreateDirectory("icloud");
		home.CreateDirectory(".CloudStorage");
		var conflict = CloudStorageLinks.AliasPath(home.FullPath, "ICloudDrive");
		var other = Path.Combine(home.FullPath, "other-account");
		if (kind == "directory") Directory.CreateDirectory(conflict);
		else if (kind == "file") File.WriteAllText(conflict, "keep");
		else
		{
			if (kind == "different-link") Directory.CreateDirectory(other);
			Directory.CreateSymbolicLink(conflict, other);
		}

		Assert.Throws<IOException>(() => CloudStorageLinks.Ensure(home.FullPath,
			[new("GoogleDrive", google), new("ICloudDrive", icloud)]));
		Assert.Null(new DirectoryInfo(CloudStorageLinks.AliasPath(home.FullPath, "GoogleDrive")).LinkTarget);
		if (kind == "file") Assert.Equal("keep", File.ReadAllText(conflict));
		else if (kind == "directory") Assert.True(Directory.Exists(conflict));
		else Assert.Equal(other, new DirectoryInfo(conflict).LinkTarget);
	}

	[Fact]
	public void NoProvidersCreatesNoShortcutDirectory()
	{
		using var home = new TestDirectory();
		CloudStorageLinks.Ensure(home.FullPath, []);
		Assert.False(Directory.Exists(Path.Combine(home.FullPath, ".CloudStorage")));
	}

	[Fact]
	public void ShortcutDirectorySymlinkIsRejectedWithoutWritingToItsTarget()
	{
		if (OperatingSystem.IsWindows()) Assert.Skip("Symlink creation requires Windows developer mode; provider discovery is macOS-only.");
		using var home = new TestDirectory();
		var other = home.CreateDirectory("somewhere-else");
		var root = home.CreateDirectory("icloud");
		Directory.CreateSymbolicLink(Path.Combine(home.FullPath, ".CloudStorage"), other);
		Assert.Throws<IOException>(() => CloudStorageLinks.Ensure(home.FullPath, [new("ICloudDrive", root)]));
		Assert.Empty(Directory.EnumerateFileSystemEntries(other));
	}
}
