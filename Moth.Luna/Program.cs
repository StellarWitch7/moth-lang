﻿using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using Tomlet;
using Tomlet.Models;

namespace Moth.Luna;

internal class Program
{
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
                        Console.WriteLine(
                            "Running project...\n--------------------------------------------"
                        );
                        var exitCode = ExecuteRun(options, proj);
                        Console.WriteLine(
                            $"\n--------------------------------------------\nProject exited with code {exitCode}"
                        );
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
        string projfile = options.ProjFile;

        if (projfile == null)
            projfile = "Luna.toml";

        if (options.ClearCache)
            Directory.Delete(CacheDir, true);

        Project project = TomletMain.To<Project>(File.ReadAllText(projfile));
        CallMothc(options, project);
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
            string gitignoreString = "[Bb]uild/\n[Cc]ache/\n[Rr]un";
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

    private static void CallMothc(Options options, Project project)
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

        args.Append($"--output-file {project.OutputName} ");
        args.Append($"--output-type {project.Type} ");

        if (project.Dependencies != null)
        {
            var mothlibs = new StringBuilder();

            if (project.Dependencies.Local != null)
            {
                foreach (var lib in project.Dependencies.Local.Values)
                {
                    mothlibs.Append($"{lib} ");
                }
            }

            if (project.Dependencies.Remote != null)
            {
                foreach (var lib in project.Dependencies.Remote.Values)
                {
                    mothlibs.Append($"{FetchRemoteFile(lib)} ");
                }
            }

            if (project.Dependencies.Project != null)
            {
                foreach (var lib in project.Dependencies.Project.Values)
                {
                    mothlibs.Append($"{BuildFromProject(lib)} ");
                }
            }

            if (project.Dependencies.Git != null)
            {
                foreach (var lib in project.Dependencies.Git.Values)
                {
                    mothlibs.Append($"{BuildFromGit(lib)} ");
                }
            }

            args.Append($"--moth-libs {mothlibs}");
        }

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
        Console.WriteLine($"Calling mothc with arguments \"{args}\"...");

        var mothc = Process.Start(
            new ProcessStartInfo("mothc", args.ToString()) { WorkingDirectory = buildDir }
        );

        if (mothc == null)
            throw new Exception("Call to mothc failed.");

        mothc.WaitForExit();

        if (mothc.ExitCode != 0)
            throw new Exception($"mothc finished with exit code {mothc.ExitCode}");
    }

    private static string QueryProjName()
    {
        Console.Write("Enter a name for the new project: ");
        return Console.ReadLine();
    }

    private static string FetchRemoteFile(string url)
    {
        string dest = Path.Combine(CacheDir, url.Substring(url.LastIndexOf("/")));

        if (File.Exists(dest))
        {
            return dest;
        }

        using (var client = new WebClient())
        {
            client.DownloadFile(url, dest);
        }

        return dest;
    }

    private static string BuildFromProject(ProjectSource source)
    {
        Project project = TomletMain.To<Project>(
            File.ReadAllText(Path.Combine(source.Dir, "Luna.toml"))
        );
        var build = Process.Start(
            new ProcessStartInfo(source.Build.Command, source.Build.Args)
            {
                WorkingDirectory = source.Dir
            }
        );

        if (build == null)
            throw new Exception($"Call to {source.Build.Command} failed.");

        build.WaitForExit();

        if (build.ExitCode != 0)
            throw new Exception($"{source.Build.Command} finished with exit code {build.ExitCode}");

        return Path.Combine(source.Dir, project.Out, project.FullOutputName);
    }

    private static string BuildFromGit(GitSource source)
    {
        string repoName =
            source.Source != null
                ? source
                    .Source.Remove(source.Source.LastIndexOf('.'))
                    .Substring(source.Source.LastIndexOf('/') + 1)
                : throw new Exception("Git source not set.");
        string repoDir = Path.Combine(CacheDir, repoName);
        var args = new StringBuilder();

        if (source.Branch != null)
            args.Append($"--branch {source.Branch} ");

        if (source.Commit != null)
            args.Append("--depth 1 ");

        var gitClone = Process.Start(
            new ProcessStartInfo("git-force-clone", $"{args}{source.Source} {repoDir}")
            {
                WorkingDirectory = CacheDir
            }
        );

        if (gitClone == null)
            throw new Exception("Call to git-force-clone failed.");

        gitClone.WaitForExit();

        if (gitClone.ExitCode != 0)
            throw new Exception($"git clone finished with exit code {gitClone.ExitCode}");

        if (source.Commit != null)
        {
            var gitCheckout = Process.Start(
                new ProcessStartInfo("git", $"") { WorkingDirectory = repoDir }
            );

            if (gitCheckout == null)
                throw new Exception("Call to git checkout failed.");

            gitCheckout.WaitForExit();

            if (gitCheckout.ExitCode != 0)
                throw new Exception($"git checkout finished with exit code {gitCheckout.ExitCode}");
        }

        return BuildFromProject(new ProjectSource() { Dir = repoDir, Build = source.Build });
    }
}
