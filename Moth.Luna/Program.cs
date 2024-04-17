﻿using CommandLine;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Tomlet;
using Tomlet.Models;

namespace Moth.Luna;

internal class Program
{
    public static string CacheDir
    {
        get
        {
            string dir = $"{Environment.CurrentDirectory}/cache";
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
        
        Parser.Default.ParseArguments<Options>(args.Skip(1)).WithParsed(options =>
        {
            switch (action)
            {
                case "build":
                    ExecuteBuild(options);
                    break;
                case "run":
                    var proj = ExecuteBuild(options);
                    Console.WriteLine("Running project...\n--------------------------------------------");
                    var exitCode = ExecuteRun(options, proj);
                    Console.WriteLine($"\n--------------------------------------------\nProject exited with code {exitCode}");
                    break;
                case "init":
                    ExecuteInit(options);
                    break;
                case "package":
                    throw new NotImplementedException();
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
        {
            projfile = $"{Environment.CurrentDirectory}/Luna.toml";
        }
        
        Project project = TomletMain.To<Project>(File.ReadAllText(projfile));
        CallMothc(options, project);
        return project;
    }

    private static int ExecuteRun(Options options, Project project)
    {
        var run = Process.Start(new ProcessStartInfo($"{project.FullOutputPath}", options.RunArgs)
        {
            WorkingDirectory = options.RunDir
        });

        if (run == null)
            throw new Exception($"Call to {project.FullOutputPath} failed.");

        run.WaitForExit();
        return run.ExitCode;
    }
    
    private static void ExecuteInit(Options options)
    {
        string projName = options.ProjName == null ? QueryProjName() : options.ProjName;
        string projDir = $"{Environment.CurrentDirectory}/{projName}";

        if (Directory.Exists(projDir))
        {
            throw new Exception($"Cannot create new Luna project as directory \"{projDir}\" already exists.");
        }
        
        Directory.CreateDirectory(projDir);
        
        var project = new Project()
        {
            
        };
        
        string tomlString = TomletMain.TomlStringFrom(project);
        using (var file = File.OpenWrite($"{projDir}/Luna.toml"))
        {
            file.Write(Encoding.UTF8.GetBytes(tomlString));
        }
    }

    private static void CallMothc(Options options, Project project)
    {
        var args = new StringBuilder();

        if (!project.Platforms.Contains(CurrentOS))
        {
            throw new Exception($"Cannot build project \"{project.Name}\" for the current operating system.");
        }
        
        string buildDir = $"{Environment.CurrentDirectory}/{project.Out}";
        var files = new StringBuilder();

        foreach (var file in Directory.GetFiles(project.Root, "*.moth", SearchOption.AllDirectories))
        {
            files.Append($"{Environment.CurrentDirectory}/{file} ");
        }
        
        if (options.Verbose) args.Append("--verbose ");
        if (options.NoMetadata) args.Append("--no-meta ");
        
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
        
        args.Append($"--input {files}");

        Directory.CreateDirectory(buildDir);
        Console.WriteLine($"Calling mothc with arguments \"{args}\"...");
        
        var mothc = Process.Start(new ProcessStartInfo("mothc", args.ToString())
        {
            WorkingDirectory = buildDir
        });

        if (mothc == null)
            throw new Exception("Call to mothc failed.");

        mothc.WaitForExit();
    }

    private static string QueryProjName()
    {
        Console.Write("Enter a name for the new project: ");
        return Console.ReadLine();
    }

    private static string FetchRemoteFile(string url)
    {
        string dest = $"{CacheDir}/{url.Substring(url.LastIndexOf("/"))}";

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

    private static string BuildFromGit(GitSource source)
    {
        string repoName = source.Source != null
            ? source.Source.Remove(source.Source.LastIndexOf('.')).Substring(source.Source.LastIndexOf('/') + 1)
            : throw new Exception("Git source not set.");
        string repoDir = $"{CacheDir}/{repoName}";
        var args = new StringBuilder("clone ");
        
        if (source.Branch != null)
            args.Append($"--branch {source.Branch} ");

        if (source.Commit != null)
            args.Append("--depth 1 ");
        
        var gitClone = Process.Start(new ProcessStartInfo("git", $"{args}{source.Source}")
        {
            WorkingDirectory = CacheDir
        });

        if (gitClone == null)
            throw new Exception("Call to git clone failed.");
        
        gitClone.WaitForExit();

        if (source.Commit != null)
        {
            var gitCheckout = Process.Start(new ProcessStartInfo("git", $"")
            {
                WorkingDirectory = repoDir
            });
            
            if (gitCheckout == null)
                throw new Exception("Call to git checkout failed.");

            gitCheckout.WaitForExit();
        }

        var build = Process.Start(new ProcessStartInfo(source.BuildCommand, source.BuildArgs)
        {
            WorkingDirectory = repoDir
        });

        if (build == null)
            throw new Exception($"Call to {source.BuildCommand} failed.");

        build.WaitForExit();
        
        Project project = TomletMain.To<Project>(File.ReadAllText($"{repoDir}/Luna.toml"));
        return $"{project.FullOutputPath}";
    }
}