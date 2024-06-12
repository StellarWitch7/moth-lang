using Tomlet.Attributes;

namespace Moth.Luna;

public class Build
{
    [TomlProperty("cmd")]
    public string Command { get; set; } = "luna";

    [TomlProperty("args")]
    public string Args { get; set; } = "build -d";
}
