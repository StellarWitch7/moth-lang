using System.Text;
using CommandLine;

namespace Moth.Silk;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            return 1;
        }

        var action = args[0];

        Parser
            .Default.ParseArguments<Options>(args.Skip(1))
            .WithParsed(options =>
            {
                switch (action)
                {
                    case "bind":
                        Directory.CreateDirectory(options.OutputDir);

                        foreach (var dir in Directory.GetDirectories(options.Include))
                        {
                            string outputFile = Path.Combine(
                                options.Include,
                                Path.GetFileName($"{dir}.h")
                            );
                            using var file = File.Open(
                                outputFile,
                                FileMode.Create,
                                FileAccess.Write
                            );

                            foreach (var header in Directory.GetFiles(dir))
                            {
                                file.Write(
                                    Encoding.UTF8.GetBytes(
                                        $"#include \"{Path.GetRelativePath(options.Include, header)}\"\n"
                                    )
                                );
                            }
                        }

                        foreach (var header in Directory.GetFiles(options.Include))
                        {
                            using var parser = new HeaderParser(options, header);
                            var ast = parser.Parse();
                            string outputFile = Path.Combine(
                                options.OutputDir,
                                Path.GetFileName($"{header}.moth")
                            );
                            using var file = File.Open(
                                outputFile,
                                FileMode.Create,
                                FileAccess.Write
                            );
                            file.Write(Encoding.UTF8.GetBytes(ast.GetSource()));
                        }

                        break;
                    default:
                        throw new NotImplementedException();
                }
            });

        return 0;
    }
}
