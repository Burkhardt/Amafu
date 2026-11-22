namespace Amafu.Tests;

public class CloudRoleReportTests
{
	[Theory]
	[InlineData("", "unset")]
	[InlineData(", TestCloud:'OneDrive'", "equals")]
	[InlineData(", TestCloud:'Dropbox'", "nested")]
	public void DetectWarnsForEveryBoundaryHazard(string setting, string expected)
	{
		var document = new ConfigurationDocument("{DefaultCloudOrder:['OneDrive'], Cloud:{OneDrive:'./live/', Dropbox:'./live/test/'}" + setting + "}");
		var report = CloudRoleReport.Evaluate(document, ".");
		Assert.Contains(report.Warnings, w => w.Contains(expected));
	}
	[Fact]
	public void SiblingPrefixDoesNotCountAsNested()
	{
		var report = CloudRoleReport.Evaluate(new ConfigurationDocument("{LiveCloud:'OneDrive',TestCloud:'Dropbox',Cloud:{OneDrive:'./live/',Dropbox:'./live-test/'}}"), ".");
		Assert.Empty(report.Warnings);
	}
}
