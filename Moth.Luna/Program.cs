using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using Spectre.Console;
using Tomlet;
using Tomlet.Models;
using Color = System.Drawing.Color;

namespace Moth.Luna;

internal class Program
{
    public static Logger Logger { get; } = new Logger("luna");

    public static string CacheDir
    {
        get
        {
            string dir = "cache";
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string CurrentOS
    {
        get
        {
            if (OperatingSystem.IsLinux())
            {
                return "linux";
            }
            else if (OperatingSystem.IsWindows())
            {
                return "windows";
            }
            else
            {
                throw new Exception("Unsupported OS, aborting.");
            }
        }
    }

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
                    case "build":
                        ExecuteBuild(options);
                        break;
                    case "run":
                        var proj = ExecuteBuild(options);
                        Logger.Log("Running project...");
                        Logger.WriteSeparator();

                        var exitCode = ExecuteRun(options, proj);
                        Logger.WriteEmptyLine();
                        Logger.WriteSeparator();
                        new Logger(proj.Name).ExitCode(exitCode);

                        break;
                    case "init":
                        ExecuteInit(options);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            });

        return 0;
    }

    private static Project ExecuteBuild(Options options)
    {
        var logger = Logger.MakeSubLogger("build");
        string projfile = options.ProjFile;

        if (projfile == null)
            projfile = "Luna.toml";

        if (options.ClearCache)
            Directory.Delete(CacheDir, true);

        Project project = TomletMain.To<Project>(File.ReadAllText(projfile));
        CallMothc(options, project, logger);
        return project;
    }

    private static int ExecuteRun(Options options, Project project)
    {
        string defaultRunDir = "run";
        Directory.CreateDirectory(defaultRunDir);

        var run = Process.Start(
            new ProcessStartInfo($"{project.FullOutputPath}", options.RunArgs)
            {
                WorkingDirectory = options.RunDir == null ? defaultRunDir : options.RunDir
            }
        );

        if (run == null)
            throw new Exception($"Call to {project.FullOutputPath} failed.");

        run.WaitForExit();
        return run.ExitCode;
    }

    private static void ExecuteInit(Options options)
    {
        string projDir = options.ProjName == null ? QueryProjName() : options.ProjName;
        string mainDir = Path.Combine(projDir, "main");
        string includeDir = Path.Combine(projDir, "include");

        if (Directory.Exists(projDir))
        {
            throw new Exception(
                $"Cannot create new Luna project as directory \"{projDir}\" already exists."
            );
        }

        try
        {
            Directory.CreateDirectory(projDir);
            Directory.CreateDirectory(mainDir);
            Directory.CreateDirectory(includeDir);

            var project = new Project()
            {
                Name = projDir,
                Version = "1.0",
                Type = options.InitLib ? "lib" : "exe",
                PlatformTargets = new string[] { CurrentOS }
            };

            string tomlString = TomletMain.TomlStringFrom(project);
            string gitignoreString = "[Bb]uild/\n[Cc]ache/\n[Rr]un/\n[Ll]ogs/";
            string programString = options.InitLib
                ? $"namespace {projDir};\n\nwith core;\n\npublic func Add(left #i32, right #i32) #i32 {{\n    return left + right;\n}}"
                : $"namespace {projDir};\n\nwith core;\n\nfunc main() #i32 {{\n    WriteLine(\"Hello World!\");\n    return 0;\n}}";

            using (var file = File.OpenWrite(Path.Combine(projDir, "Luna.toml")))
            {
                file.Write(Encoding.UTF8.GetBytes(tomlString));
            }

            using (var file = File.OpenWrite(Path.Combine(projDir, ".gitignore")))
            {
                file.Write(Encoding.UTF8.GetBytes(gitignoreString));
            }

            using (var file = File.OpenWrite(Path.Combine(mainDir, "main.moth")))
            {
                file.Write(Encoding.UTF8.GetBytes(programString));
            }

            var silkInit = Process.Start(
                new ProcessStartInfo("silk", "init") { WorkingDirectory = includeDir }
            );

            if (silkInit == null)
                throw new Exception("Call to silk init failed.");

            silkInit.WaitForExit();

            if (silkInit.ExitCode != 0)
                throw new Exception($"silk init finished with exit code {silkInit.ExitCode}");

            var gitInit = Process.Start(
                new ProcessStartInfo("git", "init") { WorkingDirectory = projDir }
            );

            if (gitInit == null)
                throw new Exception("Call to git init failed.");

            gitInit.WaitForExit();

            if (gitInit.ExitCode != 0)
                throw new Exception($"git init finished with exit code {gitInit.ExitCode}");

            var gitAdd = Process.Start(
                new ProcessStartInfo("git", "add --all") { WorkingDirectory = projDir }
            );

            if (gitAdd == null)
                throw new Exception("Call to git add failed.");

            gitAdd.WaitForExit();

            if (gitAdd.ExitCode != 0)
                throw new Exception($"git add finished with exit code {gitAdd.ExitCode}");

            Console.WriteLine("Creating initial commit...");

            var gitCommit = Process.Start(
                new ProcessStartInfo("git", "commit -m \"Initial Commit\"")
                {
                    WorkingDirectory = projDir
                }
            );

            if (gitCommit == null)
                throw new Exception("Call to git commit failed.");

            gitCommit.WaitForExit();

            if (gitCommit.ExitCode != 0)
                throw new Exception($"git commit finished with exit code {gitCommit.ExitCode}");

            Console.WriteLine($"Successfully initialized new project: {projDir}");
        }
        catch (Exception e)
        {
            try
            {
                if (Directory.Exists(projDir))
                    Directory.Delete(projDir, true);
            }
            catch (Exception e2) { }
        }
    }

    private static void CallMothc(Options options, Project project, Logger logger)
    {
        var args = new StringBuilder();

        if (!project.PlatformTargets.Contains(CurrentOS))
        {
            throw new Exception(
                $"Cannot build project \"{project.Name}\" for the current operating system."
            );
        }

        string buildDir = Path.Combine(Environment.CurrentDirectory, project.Out);
        var files = new StringBuilder();

        foreach (
            var file in Directory.GetFiles(project.Root, "*.moth", SearchOption.AllDirectories)
        )
        {
            files.Append($"{Path.Combine(Environment.CurrentDirectory, file)} ");
        }

        if (files.Length > 0)
            files.Remove(files.Length - 1, 1);
        if (options.Verbose)
            args.Append("--verbose ");
        if (options.NoMetadata)
            args.Append("--no-meta ");

        string compLevel = options.NoCompress ? "none" : "high";
        args.Append($"--compression-level {compLevel} ");
        args.Append($"--output-file {project.OutputName} ");
        args.Append($"--output-type {project.Type} ");

        if (project.Dependencies != null)
            args.Append($"--moth-libs {DependencyBuildScheduler.Build(project.Dependencies)}");

        if (project.CLibraryFiles != null)
        {
            var clibs = new StringBuilder();

            foreach (var lib in project.CLibraryFiles)
            {
                clibs.Append($"{lib} ");
            }

            args.Append($"--c-libs {clibs}");
        }

        if (project.LanguageTargets != null)
        {
            var langs = new StringBuilder();

            foreach (var lang in project.LanguageTargets)
            {
                langs.Append($"{lang} ");
            }

            args.Append($"--export-for {langs}");
        }

        args.Append($"--module-version {project.Version} ");
        args.Append($"--input {files}");

        Directory.CreateDirectory(buildDir);
        Logger.Call("mothc", args);

        var oldDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = buildDir;

        var mothc = Moth.Compiler.Program.Main(args.ToString().Split(' '));
        Environment.CurrentDirectory = oldDir;

        if (mothc != 0)
            throw new Exception($"mothc finished with exit code {mothc}");
    }

    private static string QueryProjName()
    {
        Console.Write("Enter a name for the new project: ");
        return Console.ReadLine();
    }
}
