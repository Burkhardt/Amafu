using System.Globalization;
using System.Text;
using OsLib;

namespace Amafu;

internal static class ConfigurationListing
{
	private static readonly string[] BadgeColors = ["96", "92", "94", "91", "35", "95", "36", "34"];

	internal static int Run(string[] args, AmafuRuntime runtime)
	{
		var words = args.Where(value => value is not ("-n" or "--nologo")).ToArray();
		var selector = words.ElementAtOrDefault(1)?.ToLowerInvariant();
		var scalarPath = selector is "temp" or "backup";
		if (selector is not ("temp" or "backup" or "order" or "cloud" or "observer")
			|| words.Length != (scalarPath ? 2 : 3)
			|| words.Any(word => word.StartsWith("-", StringComparison.Ordinal)))
		{
			runtime.Error.WriteLine("Use 'amafu list temp|backup', 'amafu list order <provider|all>', 'amafu list cloud <provider|all>', or 'amafu list observer <name|all>'.");
			return 2;
		}

		var document = new ConfigurationDocument(File.ReadAllText(runtime.ConfigurationFile));
		var color = CliHelpStyle.UseColor(runtime.Output);
		var name = words.ElementAtOrDefault(2) ?? "";
		var all = string.Equals(name, "all", StringComparison.OrdinalIgnoreCase);
		string payload;
		if (scalarPath)
			payload = Expand(document.Setting(selector == "temp" ? "TempDir" : "LocalBackupDir"), runtime.HomeDirectory);
		else if (selector == "order")
		{
			var order = document.Order();
			payload = all ? order.Count == 0 ? "No providers in DefaultCloudOrder." : "  " + string.Join(" ", order.Select((provider, index) => Provider(provider, index, color)))
				: (Array.FindIndex(order.ToArray(), provider => string.Equals(provider, name, StringComparison.OrdinalIgnoreCase)) + 1).ToString(CultureInfo.InvariantCulture);
		}
		else
		{
			var entries = document.ListingEntries(selector == "cloud" ? "Cloud" : "Observers");
			if (!all)
			{
				if (!entries.TryGetValue(name, out var value)) throw new IOException($"No configured {selector} named '{name}'.");
				payload = selector == "cloud" ? Expand(value, runtime.HomeDirectory) : value;
			}
			else if (entries.Count == 0) payload = selector == "cloud" ? "No cloud providers configured." : "No observers configured.";
			else if (selector == "observer")
			{
				var width = entries.Keys.Max(key => (key + ":").EnumerateRunes().Count());
				payload = string.Join(Environment.NewLine, entries.Select(entry => CliHelpStyle.Paint("  " + CliHelpStyle.Pad(entry.Key + ":", width) + "  " + entry.Value, "36", color)));
			}
			else
			{
				var ordered = document.Order(required: false).Where(entries.ContainsKey).ToList();
				ordered.AddRange(entries.Keys.Where(key => !ordered.Contains(key, StringComparer.OrdinalIgnoreCase)));
				payload = string.Join(Environment.NewLine, ordered.Select((provider, index) =>
					"\t" + Provider(entries.Keys.First(key => string.Equals(key, provider, StringComparison.OrdinalIgnoreCase)), index, color)
					+ "\t\t" + CliHelpStyle.Paint("\"" + AmafuConfigurationRenderer.Escape(Expand(entries[provider], runtime.HomeDirectory)) + "\"", "33", color)));
			}
		}
		runtime.Output.WriteLine(payload);
		return 0;
	}

	internal static string Provider(string name, int index, bool color) =>
		CliHelpStyle.Paint(CliHelpStyle.CloudGlyph(name, index + 1), BadgeColors[index % BadgeColors.Length], color)
		+ CliHelpStyle.Paint(" " + name, "33", color);

	private static string Expand(string path, string home)
	{
		try
		{
			var expanded = path == "~" ? home : path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal)
				? Path.Combine(home, path[2..]) : path;
			var fullPath = Path.GetFullPath(expanded, home);
			return Path.EndsInDirectorySeparator(fullPath) ? fullPath : fullPath + Path.DirectorySeparatorChar;
		}
		catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
		{
			throw new IOException("Invalid configured directory path.", exception);
		}
	}
}
