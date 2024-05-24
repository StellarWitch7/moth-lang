using System.Text;
using CommandLine;
using Tomlet;

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
                        string conf = Path.Combine(Environment.CurrentDirectory, "Silk.toml");
                        Directory.CreateDirectory(options.OutputDir);
                        Includes includes = TomletMain.To<Includes>(File.ReadAllText(conf));

                        foreach (var kv in includes.EnvironmentVariables)
                        {
                            string envVar =
                                Environment.GetEnvironmentVariable(kv.Value)
                                ?? throw new Exception(
                                    $"Environment variable \"{kv.Key} = {kv.Value}\" is not set!"
                                );

                            if (!Directory.Exists(envVar))
                                throw new Exception(
                                    $"Environment variable \"{kv.Key} = {kv.Value}\" does not contain a path to a valid directory!"
                                );

                            includes.Directories.Add(kv.Key, envVar);
                        }

                        foreach (var kv in includes.Directories)
                        {
                            string outputFile = Path.Combine(
                                Environment.CurrentDirectory,
                                Path.GetFileName($"{kv.Key}.h")
                            );
                            using var file = File.Open(
                                outputFile,
                                FileMode.Create,
                                FileAccess.Write
                            );

                            foreach (
                                var header in Directory.GetFiles(
                                    Path.Combine(Environment.CurrentDirectory, kv.Value)
                                )
                            )
                            {
                                file.Write(
                                    Encoding.UTF8.GetBytes(
                                        $"#include \"{Path.GetRelativePath(Environment.CurrentDirectory, header)}\"\n"
                                    )
                                );
                            }
                        }

                        foreach (
                            var header in Directory
                                .GetFiles(Environment.CurrentDirectory)
                                .Except(new string[] { conf })
                        )
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
