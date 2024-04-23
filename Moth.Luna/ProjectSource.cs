using Tomlet.Attributes;

namespace Moth.Luna;

public class ProjectSource
{
    [TomlProperty("dir")]
    public string Dir { get; set; }
    
    [TomlProperty("build")]
    public Build Build { get; set; } = new Build();
}
