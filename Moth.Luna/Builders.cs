using System.Diagnostics;
using System.Net;
using System.Text;
using Tomlet;

namespace Moth.Luna;

public static class Builders
{
    public static readonly object ConsoleLock = new object();

    public static string CacheDir
    {
        get => Program.CacheDir;
    }

    public static string FetchRemoteFile(string url)
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

    public static string BuildFromProject(ProjectSource source)
    {
        Project project = TomletMain.To<Project>(
            File.ReadAllText(Path.Combine(source.Dir, "Luna.toml"))
        );
        var build = Process.Start(
            new ProcessStartInfo(source.Build.Command, source.Build.Args)
            {
                WorkingDirectory = source.Dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        );

        if (build == null)
            throw new Exception($"Call to {source.Build.Command} failed.");

        build.WaitForExit();

        lock (ConsoleLock)
        {
            Program.Logger.WriteUnsigned(build.StandardOutput.ReadToEnd());

            if (build.ExitCode != 0)
            {
                Program.Logger.Error(build.StandardError.ReadToEnd());
                throw new Exception(
                    $"{source.Build.Command} finished with exit code {build.ExitCode}"
                );
            }
        }

        return Path.Combine(source.Dir, project.Out, project.FullOutputName);
    }

    public static string BuildFromGit(GitSource source)
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
