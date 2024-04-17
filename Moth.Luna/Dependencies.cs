using Tomlet.Attributes;

namespace Moth.Luna;

public class Dependencies
{
    [TomlProperty("local")]
    public Dictionary<string, string> Local { get; set; }
    
    [TomlProperty("remote")]
    public Dictionary<string, string> Remote { get; set; }
    
    [TomlProperty("git")]
    public Dictionary<string, GitSource> Git { get; set; }
}
