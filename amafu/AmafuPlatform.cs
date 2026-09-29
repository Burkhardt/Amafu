namespace Amafu;

internal enum AmafuPlatform
{
	MacOS,
	Linux,
	Windows,
	Other
}

internal static class AmafuPlatformDetector
{
	internal static AmafuPlatform Current()
	{
		if (OperatingSystem.IsMacOS()) return AmafuPlatform.MacOS;
		if (OperatingSystem.IsLinux()) return AmafuPlatform.Linux;
		if (OperatingSystem.IsWindows()) return AmafuPlatform.Windows;
		return AmafuPlatform.Other;
	}
}
