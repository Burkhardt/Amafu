using System.Text;
using Amafu;

Console.OutputEncoding = Encoding.UTF8;
return AmafuApplication.Run(args, AmafuRuntime.FromEnvironment());
