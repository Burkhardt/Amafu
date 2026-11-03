using OsLib;

namespace Amafu;

internal static class CliHelp
{
	private const char BannerIcon = '\ueb1e';
	private const char InfoIcon = '\uea74';
	private const char HelpIcon = '\uf059';
	private const char ForceIcon = '\uf0e7';
	private const char OutputIcon = '\uea92';
	private const char FolderIcon = '\uea83';

	internal static void Write(TextWriter output, bool noLogo, int width, string? command = null, string? selector = null)
	{
		if (!noLogo) WriteBanner(output);

		if (string.Equals(command, "list", StringComparison.OrdinalIgnoreCase))
		{
			WriteListHelp(output, width, selector?.ToLowerInvariant());
			return;
		}

		if (string.Equals(command, "reconcile", StringComparison.OrdinalIgnoreCase))
		{
			WriteSyntax(output, "amafu reconcile [--apply|--dry-run] [--onedrive-personal <folder>]");
			WriteRows(output, width,
				[("--apply", OutputIcon.ToString(), "back up configuration, create shortcuts, and apply the previewed changes"),
				 ("--dry-run", OutputIcon.ToString(), "preview only (the default); never prompt interactively"),
				 ("--onedrive-personal", FolderIcon.ToString(), "select a detected personal OneDrive folder by name or path")]);
			return;
		}

		if (string.Equals(command, "detect", StringComparison.OrdinalIgnoreCase))
		{
			WriteSyntax(output, "amafu detect [--json] [--create-links] [--dry-run] [--onedrive-personal <folder>]");
			output.WriteLine();
			WriteRows(output, width,
				[("--json", OutputIcon.ToString(), "print detected roots as JSON"),
				 ("--dry-run", OutputIcon.ToString(), "preview detection without creating shortcuts"),
				 ("--onedrive-personal", FolderIcon.ToString(), "select a personal OneDrive folder; default keeps the configured root, otherwise longest name wins"),
				 ("--create-links", FolderIcon.ToString(), "create ~/.CloudStorage/<provider> shortcuts")]);
			return;
		}

		if (string.Equals(command, "init", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(command, "init-config", StringComparison.OrdinalIgnoreCase))
		{
			WriteSyntax(output, "amafu init [--dry-run] [-f|--force] [--create-links] [--onedrive-personal <folder>]");
			output.WriteLine();
			WriteRows(output, width,
				[("-f, --force", ForceIcon.ToString(), "overwrite an existing configuration"),
				 ("--onedrive-personal", FolderIcon.ToString(), "select a personal OneDrive folder; default keeps the configured root, otherwise longest name wins"),
				 ("--create-links", FolderIcon.ToString(), "create ~/.CloudStorage/<provider> shortcuts"),
				 ("--dry-run", OutputIcon.ToString(), "print configuration without creating files or shortcuts")]);
			return;
		}

		WriteRows(output, width,
			[("Commands:", InfoIcon.ToString(), "detect, list, init, reconcile"),
			 ("  amafu detect [--json] [--create-links] [--dry-run] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("  amafu list temp", string.Empty, string.Empty),
			 ("  amafu list backup", string.Empty, string.Empty),
			 ("  amafu list order <provider|all>", string.Empty, string.Empty),
			 ("  amafu list cloud <provider|all>", string.Empty, string.Empty),
			 ("  amafu list observer <name|all>", string.Empty, string.Empty),
			 ("  amafu init [--dry-run] [-f|--force] [--create-links] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("  amafu reconcile [--apply|--dry-run] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("-h, --help", HelpIcon.ToString(), "print out all options"),
			 ("-v, --version", InfoIcon.ToString(), "print version info"),
			 ("-n, --nologo", BannerIcon.ToString(), "do not display the banner"),
			 ("-f, --force", ForceIcon.ToString(), "overwrite an existing configuration"),
			 ("--dry-run", OutputIcon.ToString(), "preview detection or configuration without writing"),
			 ("--json", OutputIcon.ToString(), "print machine-readable cloud detection"),
			 ("--onedrive-personal", FolderIcon.ToString(), "select a personal OneDrive folder; default keeps the configured root, otherwise longest name wins"),
			 ("--create-links", FolderIcon.ToString(), "create ~/.CloudStorage/<provider> shortcuts")]);
	}

	private static void WriteListHelp(TextWriter output, int width, string? selector)
	{
		if (selector is "temp" or "backup")
		{
			var key = selector == "temp" ? "TempDir" : "LocalBackupDir";
			WriteSyntax(output, $"amafu list {selector}");
			WriteRows(output, width,
				[(key, FolderIcon.ToString(), "print the configured absolute directory path with a trailing separator; expand ~ to the current user's home"),
				 ("Example", InfoIcon.ToString(), $"'amafu list {selector}' prints only the path and a newline")]);
			return;
		}
		if (selector == "order")
		{
			WriteSyntax(output, "amafu list order <provider|all>");
			WriteRows(output, width,
				[("<provider>", InfoIcon.ToString(), "print its 1-based position in DefaultCloudOrder; print 0 when the provider is not in the configured order"),
				 ("all", FolderIcon.ToString(), "list badged provider names in their configured order"),
				 ("Example", InfoIcon.ToString(), "'amafu list order all' shows the ordered collection; a named lookup prints only its number and a newline")]);
			return;
		}
		if (selector == "cloud")
		{
			WriteSyntax(output, "amafu list cloud <provider|all>");
			WriteRows(output, width,
				[("<provider>", FolderIcon.ToString(), "print only its configured absolute directory path; expand ~ and retain a trailing separator"),
				 ("all", FolderIcon.ToString(), "list provider badges and double-quoted paths; one leading tab and two tabs before each path"),
				 ("Example", InfoIcon.ToString(), "'amafu list cloud all' shows every configured cloud without checking or creating directories")]);
			return;
		}
		if (selector == "observer")
		{
			WriteSyntax(output, "amafu list observer <name|all>");
			WriteRows(output, width,
				[("<name>", InfoIcon.ToString(), "print only the configured SSH target and a newline; never connect to the observer"),
				 ("all", InfoIcon.ToString(), "list configured observer names and SSH targets in space-aligned columns"),
				 ("Example", InfoIcon.ToString(), "'amafu list observer Mzansi' prints the configured target, for example 'rsb@Mzansi'")]);
			return;
		}
		WriteSyntax(output, "amafu list <temp|backup|order|cloud|observer>");
		WriteRows(output, width,
			[("temp", FolderIcon.ToString(), "print the expanded TempDir path"),
			 ("backup", FolderIcon.ToString(), "print the expanded LocalBackupDir path"),
			 ("order <provider|all>", InfoIcon.ToString(), "print a 1-based position (0 if unlisted), or the ordered provider collection"),
			 ("cloud <provider|all>", FolderIcon.ToString(), "print a configured path, or the provider/path table"),
			 ("observer <name|all>", InfoIcon.ToString(), "print an SSH target, or the observer/target table"),
			 ("-h, --help", HelpIcon.ToString(), "use 'amafu list order -h', or another selector, for detailed help")]);
	}

	private static void WriteSyntax(TextWriter output, string text)
		=> output.WriteLine(CliHelpStyle.Syntax(text, "36", CliHelpStyle.UseColor(output)));

	private static void WriteBanner(TextWriter output)
	{
		var version = typeof(CliHelp).Assembly.GetName().Version?.ToString(3) ?? "unknown";
		foreach (var line in CliHelpStyle.Banner($"{InfoIcon} Amafu Cloud Bootstrap CLI (amafu v{version})", CliHelpStyle.UseColor(output)))
			output.WriteLine(line);
	}

	private static void WriteRows(TextWriter output, int width,
		IEnumerable<(string Option, string Icon, string Description)> rows)
	{
		var table = rows.Select(row => new CliHelpStyle.Row(row.Option, row.Icon, row.Description));
		foreach (var line in CliHelpStyle.Rows(table, CliHelpStyle.UseColor(output), width)) output.WriteLine(line);
	}
}
