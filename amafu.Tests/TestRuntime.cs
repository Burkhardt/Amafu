// Exercise the standalone bootstrap against an isolated filesystem before OsLib configuration exists.
#pragma warning disable RAI003
namespace Amafu.Tests;

internal static class TestRuntime
{
	internal static AmafuRuntime Create(
		string home,
		AmafuPlatform platform = AmafuPlatform.MacOS,
		bool elevated = false,
		int width = 120,
		StringWriter? output = null,
		StringWriter? error = null,
		string? sharedGoogleDriveCandidate = null,
		string? linuxServerDataDirectory = null)
		=> new(
			home,
			platform,
			sharedGoogleDriveCandidate ?? Path.Combine(home, "Shared", "ServerData", "GDriveData"),
			elevated,
			width,
		output ?? new StringWriter(),
		error ?? new StringWriter(),
		linuxServerDataDirectory ?? Path.Combine(home, "srv", "ServerData"));
}
#pragma warning restore RAI003
