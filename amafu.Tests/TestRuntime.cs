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
		string? sharedGoogleDriveCandidate = null)
		=> new(
			home,
			platform,
			sharedGoogleDriveCandidate ?? Path.Combine(home, "Shared", "ServerData", "GDriveData"),
			elevated,
			width,
			output ?? new StringWriter(),
			error ?? new StringWriter());
}
