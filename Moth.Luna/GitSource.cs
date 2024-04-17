using Tomlet.Attributes;

namespace Moth.Luna;

public class GitSource
{
    [TomlProperty("src")]
    public string Source { get; set; }
    
    [TomlProperty("branch")]
    public string Branch { get; set; }
    
    [TomlProperty("commit")]
    public string Commit { get; set; }

    [TomlProperty("build-command")]
    public string BuildCommand { get; set; } = "luna";
    
    [TomlProperty("build-args")]
    public string BuildArgs { get; set; } = "build";
}
