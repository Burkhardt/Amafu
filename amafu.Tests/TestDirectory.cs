namespace Amafu.Tests;

internal sealed class TestDirectory : IDisposable
{
	internal TestDirectory()
	{
		FullPath = Path.Combine(Path.GetTempPath(), "amafu-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(FullPath);
	}

	internal string FullPath { get; }

	internal string CreateDirectory(params string[] segments)
	{
		var path = segments.Aggregate(FullPath, Path.Combine);
		Directory.CreateDirectory(path);
		return path;
	}

	public void Dispose()
	{
		if (Directory.Exists(FullPath)) Directory.Delete(FullPath, recursive: true);
	}
}
