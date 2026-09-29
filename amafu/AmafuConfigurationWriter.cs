using System.Text;

namespace Amafu;

internal static class AmafuConfigurationWriter
{
	internal static void Write(string destination, string payload, bool force)
	{
		if (File.Exists(destination) && !force)
			throw new AmafuConfigurationExistsException(destination);

		var parent = Path.GetDirectoryName(destination)
			?? throw new ArgumentException("Configuration destination has no parent directory.", nameof(destination));
		Directory.CreateDirectory(parent);
		File.WriteAllText(destination, payload, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(
				destination,
				UnixFileMode.UserRead |
				UnixFileMode.UserWrite |
				UnixFileMode.GroupRead |
				UnixFileMode.OtherRead);
		}
	}
}

internal sealed class AmafuConfigurationExistsException(string destination) : IOException
{
	internal string Destination { get; } = destination;
}
