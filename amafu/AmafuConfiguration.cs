namespace Amafu;

internal sealed record DetectedCloudProvider(string Name, string RootPath);

internal sealed record CloudDetectionResult(
	IReadOnlyList<DetectedCloudProvider> Providers,
	IReadOnlyList<string> CheckedPaths);

internal sealed record AmafuConfiguration(
	string HomeDirectory,
	IReadOnlyList<DetectedCloudProvider> Providers);
