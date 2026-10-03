namespace Amafu;

internal sealed record DetectedCloudProvider(
	string Name,
	string RootPath,
	string? Provider = null,
	string? Account = null);

internal sealed record CloudDetectionResult(
	IReadOnlyList<DetectedCloudProvider> Providers,
	IReadOnlyList<string> CheckedPaths);

internal sealed record AmafuConfiguration(
	string HomeDirectory,
	IReadOnlyList<DetectedCloudProvider> Providers);
