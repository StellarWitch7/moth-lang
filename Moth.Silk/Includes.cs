using Tomlet.Attributes;

namespace Moth.Silk;

public class Includes
{
    [TomlProperty("env")]
    public Dictionary<string, string> EnvironmentVariables { get; set; }

    [TomlProperty("dir")]
    public Dictionary<string, string> Directories { get; set; }
}
