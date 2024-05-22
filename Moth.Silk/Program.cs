using CommandLine;
using System.Text;

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
        
        Parser.Default.ParseArguments<Options>(args.Skip(1)).WithParsed(options =>
        {
            switch (action)
            {
                case "bind":
                    Directory.CreateDirectory(options.OutputDir);
                    
                    foreach (var path in options.InputFiles)
                    {
                        using var parser = new HeaderParser(options, path);
                        var ast = parser.Parse();
                        
                        if (options.Verbose)
                            Console.WriteLine($"\nGenerated Moth AST:\n{ast.GetDebugString("    ")}\n\n");

                        string outputFile = Path.Combine(options.OutputDir, Path.GetFileName($"{path}.moth"));
                        using var file = File.Open(outputFile, FileMode.Create, FileAccess.Write);
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