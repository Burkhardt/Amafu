using System.Text.Json;

namespace Amafu.Tests;

public sealed class AmafuConfigurationRendererTests
{
	[Fact]
	public void Render_ProducesDeterministicContractShapeAndPortablePaths()
	{
		using var fixture = new TestDirectory();
		var providerPath = Path.Combine(fixture.FullPath, "Library", "CloudStorage", "OneDrive-Personal(2)", "OneDriveData");
		var configuration = new AmafuConfiguration(
			fixture.FullPath,
			[new DetectedCloudProvider("OneDrive", providerPath)]);

		var payload = AmafuConfigurationRenderer.Render(configuration);

		Assert.Contains("TempDir: \"~/temp/\"", payload);
		Assert.Contains("LocalBackupDir: \"~/backup/\"", payload);
		Assert.Contains("SyncPropagationDelayMs: 10000", payload);
		Assert.Contains("\"OneDrive\"", payload);
		Assert.Contains("~/Library/CloudStorage/OneDrive-Personal(2)/OneDriveData/", payload);
		Assert.EndsWith("}\n", payload);
	}

	[Fact]
	public void Render_NoProvidersProducesEditableTemplateWithoutInventingRoot()
	{
		using var fixture = new TestDirectory();

		var payload = AmafuConfigurationRenderer.Render(new AmafuConfiguration(fixture.FullPath, []));

		Assert.Contains("No supported cloud provider was detected", payload);
		Assert.Contains("No provider root was created or assumed", payload);
		Assert.DoesNotContain("\"OneDrive\": \"~/", payload);
	}

	[Fact]
	public void Render_EscapesQuotesControlsAndPreservesUnicode()
	{
		using var fixture = new TestDirectory();
		var configuration = new AmafuConfiguration(
			fixture.FullPath,
			[new DetectedCloudProvider("Dróps\"box", Path.Combine(fixture.FullPath, "Nkosikazi\nCloud"))]);

		var payload = AmafuConfigurationRenderer.Render(configuration);

		Assert.Contains("Dróps\\\"box", payload);
		Assert.Contains("Nkosikazi\\nCloud", payload);
	}

	[Fact]
	public void RenderDetectionJson_IsValidJson()
	{
		using var fixture = new TestDirectory();
		var result = new CloudDetectionResult(
			[new DetectedCloudProvider("OneDrive", Path.Combine(fixture.FullPath, "OneDriveData"))],
			[Path.Combine(fixture.FullPath, "OneDrive")]);

		var payload = AmafuConfigurationRenderer.RenderDetectionJson(result, fixture.FullPath);
		using var json = JsonDocument.Parse(payload);

		Assert.Equal("OneDrive", json.RootElement.GetProperty("providers")[0].GetProperty("name").GetString());
		Assert.Equal("~/OneDriveData/", json.RootElement.GetProperty("providers")[0].GetProperty("path").GetString());
	}
}
