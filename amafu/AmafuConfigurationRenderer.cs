using System.Text;

namespace Amafu;

internal static class AmafuConfigurationRenderer
{
	internal static string Render(AmafuConfiguration configuration)
	{
		var builder = new StringBuilder();
		builder.AppendLine("{");
		builder.AppendLine("  TempDir: \"~/temp/\",");
		builder.AppendLine("  LocalBackupDir: \"~/backup/\",");
		builder.AppendLine("  SyncPropagationDelayMs: 10000,");
		builder.AppendLine("  DefaultCloudOrder: [");
		if (configuration.Providers.Count == 0)
		{
			builder.AppendLine("    // No supported cloud provider was detected. Add provider names after configuring Cloud.");
		}
		else
		{
			for (var index = 0; index < configuration.Providers.Count; index++)
			{
				var suffix = index + 1 == configuration.Providers.Count ? string.Empty : ",";
				builder.Append("    \"")
					.Append(Escape(configuration.Providers[index].Name))
					.Append("\"")
					.AppendLine(suffix);
			}
		}
		builder.AppendLine("  ],");
		builder.AppendLine("  Cloud: {");
		if (configuration.Providers.Count == 0)
		{
			builder.AppendLine("    // No provider root was created or assumed. Add confirmed local mount paths here.");
			builder.AppendLine("    // \"OneDrive\": \"/path/to/OneDriveData/\"");
		}
		else
		{
			for (var index = 0; index < configuration.Providers.Count; index++)
			{
				var provider = configuration.Providers[index];
				var suffix = index + 1 == configuration.Providers.Count ? string.Empty : ",";
				builder.Append("    \"")
					.Append(Escape(provider.Name))
					.Append("\": \"")
					.Append(Escape(ToPortablePath(provider.RootPath, configuration.HomeDirectory)))
					.Append("\"")
					.AppendLine(suffix);
			}
		}
		builder.AppendLine("  },");
		builder.AppendLine("  Observers: []");
		builder.AppendLine("}");
		return builder.ToString();
	}

	internal static string RenderDetectionJson(CloudDetectionResult result, string homeDirectory)
	{
		var builder = new StringBuilder();
		builder.AppendLine("{");
		builder.AppendLine("  \"providers\": [");
		for (var index = 0; index < result.Providers.Count; index++)
		{
			var provider = result.Providers[index];
			var suffix = index + 1 == result.Providers.Count ? string.Empty : ",";
			builder.Append("    { \"name\": \"")
				.Append(Escape(provider.Name))
				.Append("\", \"path\": \"")
				.Append(Escape(ToPortablePath(provider.RootPath, homeDirectory)))
				.Append("\" }")
				.AppendLine(suffix);
		}
		builder.AppendLine("  ],");
		builder.AppendLine("  \"checkedPaths\": [");
		for (var index = 0; index < result.CheckedPaths.Count; index++)
		{
			var suffix = index + 1 == result.CheckedPaths.Count ? string.Empty : ",";
			builder.Append("    \"")
				.Append(Escape(ToPortablePath(result.CheckedPaths[index], homeDirectory)))
				.Append("\"")
				.AppendLine(suffix);
		}
		builder.AppendLine("  ]");
		builder.AppendLine("}");
		return builder.ToString();
	}

	internal static string ToPortablePath(string path, string homeDirectory)
	{
		var fullPath = Path.GetFullPath(path);
		var fullHome = Path.TrimEndingDirectorySeparator(Path.GetFullPath(homeDirectory));
		var comparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		string display;
		if (string.Equals(fullPath, fullHome, comparison))
			display = "~";
		else if (fullPath.StartsWith(fullHome + Path.DirectorySeparatorChar, comparison))
			display = "~/" + fullPath[(fullHome.Length + 1)..];
		else
			display = fullPath;

		display = display.Replace('\\', '/');
		return display.EndsWith("/", StringComparison.Ordinal) ? display : display + "/";
	}

	internal static string Escape(string value)
	{
		var builder = new StringBuilder(value.Length + 8);
		foreach (var character in value)
		{
			switch (character)
			{
				case '\\': builder.Append("\\\\"); break;
				case '"': builder.Append("\\\""); break;
				case '\b': builder.Append("\\b"); break;
				case '\f': builder.Append("\\f"); break;
				case '\n': builder.Append("\\n"); break;
				case '\r': builder.Append("\\r"); break;
				case '\t': builder.Append("\\t"); break;
				default:
					if (character < ' ')
						builder.Append("\\u").Append(((int)character).ToString("x4"));
					else
						builder.Append(character);
					break;
			}
		}
		return builder.ToString();
	}
}
