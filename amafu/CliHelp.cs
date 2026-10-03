namespace Amafu;

internal static class CliHelp
{
	private const char BannerIcon = '\ueb1e';
	private const char InfoIcon = '\uea74';
	private const char HelpIcon = '\uf059';
	private const char ForceIcon = '\uf0e7';
	private const char OutputIcon = '\uea92';
	private const char FolderIcon = '\uea83';

	internal static void Write(TextWriter output, bool noLogo, int width, string? command = null)
	{
		if (!noLogo) WriteBanner(output);

		if (string.Equals(command, "reconcile", StringComparison.OrdinalIgnoreCase))
		{
			output.WriteLine("amafu reconcile [--apply|--dry-run] [--onedrive-personal <folder>]");
			WriteRows(output, width,
				[("--apply", OutputIcon.ToString(), "back up configuration, create shortcuts, and apply the previewed changes"),
				 ("--dry-run", OutputIcon.ToString(), "preview only (the default); never prompt interactively"),
				 ("--onedrive-personal", FolderIcon.ToString(), "select a detected personal OneDrive folder by name or path")]);
			return;
		}

		if (string.Equals(command, "detect", StringComparison.OrdinalIgnoreCase))
		{
			output.WriteLine("amafu detect [--json] [--create-links] [--dry-run] [--onedrive-personal <folder>]");
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
			output.WriteLine("amafu init [--dry-run] [-f|--force] [--create-links] [--onedrive-personal <folder>]");
			output.WriteLine();
			WriteRows(output, width,
				[("-f, --force", ForceIcon.ToString(), "overwrite an existing configuration"),
				 ("--onedrive-personal", FolderIcon.ToString(), "select a personal OneDrive folder; default keeps the configured root, otherwise longest name wins"),
				 ("--create-links", FolderIcon.ToString(), "create ~/.CloudStorage/<provider> shortcuts"),
				 ("--dry-run", OutputIcon.ToString(), "print configuration without creating files or shortcuts")]);
			return;
		}

		WriteRows(output, width,
			[("Commands:", InfoIcon.ToString(), "detect, init, reconcile"),
			 ("  amafu detect [--json] [--create-links] [--dry-run] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("  amafu init [--dry-run] [-f|--force] [--create-links] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("  amafu reconcile [--apply|--dry-run] [--onedrive-personal <folder>]", string.Empty, string.Empty),
			 ("-h, --help", HelpIcon.ToString(), "print out all options"),
			 ("-v, --version", InfoIcon.ToString(), "print version info"),
			 ("-n, --nologo", BannerIcon.ToString(), "do not display the banner"),
			 ("-f, --force", ForceIcon.ToString(), "overwrite an existing configuration"),
			 ("--dry-run", OutputIcon.ToString(), "preview detection or configuration without writing"),
			 ("--json", OutputIcon.ToString(), "print machine-readable cloud detection"),
			 ("--onedrive-personal", FolderIcon.ToString(), "select a personal OneDrive folder; default keeps the configured root, otherwise longest name wins"),
				 ("--create-links", FolderIcon.ToString(), "create ~/.CloudStorage/<provider> shortcuts"),
			 ("Configuration", FolderIcon.ToString(), "~/.config/RAIkeep.json5")]);
	}

	private static void WriteBanner(TextWriter output)
	{
		const string title = "Amafu Cloud Bootstrap CLI";
		output.Write(BannerIcon);
		output.Write(' ');
		output.WriteLine(new string('─', title.Length));
		output.Write(InfoIcon);
		output.Write(' ');
		output.WriteLine(title);
		output.Write(BannerIcon);
		output.Write(' ');
		output.WriteLine(new string('─', title.Length));
	}

	private static void WriteRows(
		TextWriter output,
		int width,
		IEnumerable<(string Option, string Icon, string Description)> rows)
	{
		const int optionWidth = 24;
		foreach (var (option, icon, description) in rows)
		{
			if (string.IsNullOrEmpty(description))
			{
				output.WriteLine(option);
				continue;
			}

			var normal = $"{option.PadRight(optionWidth)}{icon}  {description}  ";
			if (width >= 72 || normal.Length <= width)
			{
				output.WriteLine(normal);
			}
			else
			{
				output.WriteLine(option);
				output.WriteLine($"  {icon}  {description}");
			}
		}
	}
}
