using System.Globalization;
using System.Text;

namespace Amafu;

/// <summary>Reads just the Cloud/DefaultCloudOrder contract and preserves unrelated JSON5 text.</summary>
internal sealed class ConfigurationDocument
{
	private sealed record Node(int Start, int End, string? Text = null,
		Dictionary<string, Node>? Properties = null, List<Node>? Items = null);
	private readonly string source;
	private readonly Node root;
	private int position;

	internal ConfigurationDocument(string source)
	{
		this.source = source;
		root = Parse();
		Skip();
		if (root.Properties is null || position != source.Length) throw Invalid();
	}

	internal Dictionary<string, string> CloudPaths()
	{
		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		if (!root.Properties!.TryGetValue("Cloud", out var cloud)) return result;
		if (cloud.Properties is null) throw new IOException("Cloud must be an object in the existing configuration.");
		foreach (var (name, value) in cloud.Properties)
			if (value.Text is not null && !string.IsNullOrWhiteSpace(value.Text)) result.Add(name, value.Text);
		return result;
	}

	internal string Rewrite(IReadOnlyDictionary<string, string> paths, IReadOnlyList<string> addedNames)
	{
		var properties = root.Properties!;
		var cloudValues = new Dictionary<string, string>(StringComparer.Ordinal);
		if (properties.TryGetValue("Cloud", out var cloud))
		{
			if (cloud.Properties is null) throw new IOException("Cloud must be an object.");
			foreach (var (name, value) in cloud.Properties) cloudValues[name] = source[value.Start..value.End];
		}
		foreach (var (name, path) in paths) cloudValues[name] = Quote(path);
		var order = new List<string>();
		if (properties.TryGetValue("DefaultCloudOrder", out var defaults))
		{
			if (defaults.Items is null || defaults.Items.Any(item => item.Text is null))
				throw new IOException("DefaultCloudOrder must be an array of strings.");
			order.AddRange(defaults.Items.Select(item => item.Text!));
		}
		foreach (var name in addedNames)
			if (!order.Contains(name, StringComparer.OrdinalIgnoreCase)) order.Add(name);
		var updates = new Dictionary<string, string>
		{
			["Cloud"] = "{\n" + string.Join(",\n", cloudValues.Select(pair => "    " + Quote(pair.Key) + ": " + pair.Value)) + "\n  }",
			["DefaultCloudOrder"] = "[" + string.Join(", ", order.Select(Quote)) + "]"
		};
		var replacements = new List<(int Start, int End, string Value)>();
		var additions = new List<string>();
		foreach (var (name, value) in updates)
		{
			if (properties.TryGetValue(name, out var node)) replacements.Add((node.Start, node.End, value));
			else additions.Add(Quote(name) + ": " + value);
		}
		if (additions.Count > 0)
		{
			// Insert after the opening brace, retaining existing comments and properties.
			replacements.Add((root.Start + 1, root.Start + 1,
				"\n  " + string.Join(",\n  ", additions) + (properties.Count > 0 ? "," : "") + "\n"));
		}
		var result = source;
		foreach (var edit in replacements.OrderByDescending(edit => edit.Start))
			result = result[..edit.Start] + edit.Value + result[edit.End..];
		return result;
	}

	private static string Quote(string value) => "\"" + AmafuConfigurationRenderer.Escape(value) + "\"";
	private IOException Invalid() => new($"Cannot safely read existing JSON5 configuration near character {position}. Nothing was changed.");
	private void Skip()
	{
		while (position < source.Length)
		{
			if (char.IsWhiteSpace(source[position]) || source[position] == '\uFEFF') { position++; continue; }
			if (source.AsSpan(position).StartsWith("//"))
			{
				while (position < source.Length && source[position] != '\n') position++;
				continue;
			}
			if (source.AsSpan(position).StartsWith("/*"))
			{
				var end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
				if (end < 0) throw Invalid();
				position = end + 2;
				continue;
			}
			break;
		}
	}
	private bool Take(char character)
	{
		Skip();
		if (position >= source.Length || source[position] != character) return false;
		position++; return true;
	}
	private Node Parse()
	{
		Skip();
		var start = position;
		if (position >= source.Length) throw Invalid();
		if (Take('{'))
		{
			var properties = new Dictionary<string, Node>(StringComparer.Ordinal);
			while (!Take('}'))
			{
				Skip();
				var key = position < source.Length && source[position] is '\'' or '"' ? ReadString() : ReadAtom();
				if (!Take(':') || !properties.TryAdd(key, Parse())) throw Invalid();
				if (Take('}')) break;
				if (!Take(',')) throw Invalid();
			}
			return new(start, position, Properties: properties);
		}
		if (Take('['))
		{
			var items = new List<Node>();
			while (!Take(']'))
			{
				items.Add(Parse());
				if (Take(']')) break;
				if (!Take(',')) throw Invalid();
			}
			return new(start, position, Items: items);
		}
		if (source[position] is '\'' or '"')
		{
			var value = ReadString();
			return new(start, position, value);
		}
		ReadAtom();
		return new(start, position);
	}
	private string ReadAtom()
	{
		Skip();
		var start = position;
		while (position < source.Length && !char.IsWhiteSpace(source[position]) && !"{}[],:/".Contains(source[position])) position++;
		if (position == start) throw Invalid();
		return source[start..position];
	}
	private string ReadString()
	{
		var quote = source[position++];
		var value = new StringBuilder();
		while (position < source.Length)
		{
			var c = source[position++];
			if (c == quote) return value.ToString();
			if (c is '\n' or '\r') throw Invalid();
			if (c != '\\') { value.Append(c); continue; }
			if (position == source.Length) throw Invalid();
			c = source[position++];
			if (c == '\n') continue;
			if (c == '\r') { if (position < source.Length && source[position] == '\n') position++; continue; }
			if (c is 'u' or 'x')
			{
				var count = c == 'u' ? 4 : 2;
				if (position + count > source.Length || !int.TryParse(source.AsSpan(position, count), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) throw Invalid();
				value.Append((char)code); position += count; continue;
			}
			value.Append(c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', 'v' => '\v', '0' => '\0', _ => c });
		}
		throw Invalid();
	}
}
