using System.Runtime.InteropServices;

namespace Amafu;

internal sealed record AmafuRuntime(
	string HomeDirectory,
	AmafuPlatform Platform,
	string SharedGoogleDriveCandidate,
	bool IsElevated,
	int TerminalWidth,
	TextWriter Output,
	TextWriter Error)
{
	internal static AmafuRuntime FromEnvironment()
	{
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (string.IsNullOrWhiteSpace(home))
			home = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;

		return new AmafuRuntime(
			home,
			AmafuPlatformDetector.Current(),
			"/Users/Shared/ServerData/GDriveData",
			ElevationDetector.IsElevated(),
			ReadTerminalWidth(),
			Console.Out,
			Console.Error);
	}

	internal string ConfigurationFile => Path.Combine(HomeDirectory, ".config", "RAIkeep.json5");

	private static int ReadTerminalWidth()
	{
		try
		{
			return Console.IsOutputRedirected ? 120 : Math.Max(Console.WindowWidth, 40);
		}
		catch
		{
			return 120;
		}
	}
}

internal static class ElevationDetector
{
	internal static bool IsElevated()
	{
		if (OperatingSystem.IsWindows()) return false;
		if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SUDO_USER"))) return true;
		try
		{
			return GetEffectiveUserId() == 0;
		}
		catch
		{
			return string.Equals(Environment.UserName, "root", StringComparison.Ordinal);
		}
	}

	[DllImport("libc", EntryPoint = "geteuid")]
	private static extern uint GetEffectiveUserId();
}
