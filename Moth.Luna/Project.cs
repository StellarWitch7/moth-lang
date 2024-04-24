using System.Text.RegularExpressions;
using Tomlet.Attributes;

namespace Moth.Luna;

public class Project
{
    [TomlProperty("name")]
    public string Name { get; set; }
    
    [TomlProperty("version")]
    public string Version { get; set; }
    
    [TomlProperty("type")]
    public string Type { get; set; }

    [TomlProperty("root")]
    public string Root { get; set; } = "main";

    [TomlProperty("out")]
    public string Out { get; set; } = "build";
    
    [TomlProperty("platforms")]
    public string[] Platforms { get; set; }
    
    [TomlProperty("c-libs")]
    public string[] CLibraryFiles { get; set; }
    
    [TomlProperty("dependencies")]
    public Dependencies Dependencies { get; set; }

    [TomlNonSerialized]
    public string OutputName
    {
        get
        {
            return Type == "lib" ? $"{Name}-{Version}-{Program.CurrentOS}" : Name;
        }
    }

    [TomlNonSerialized]
    public string FullOutputName
    {
        get
        {
            return Type == "lib"
                ? $"{OutputName}.mothlib.bc"
                : OperatingSystem.IsWindows()
                    ? $"{OutputName}.exe"
                    : OutputName;
        }
    }

    [TomlNonSerialized]
    public string FullOutputPath
    {
        get
        {
            return $"{Environment.CurrentDirectory}/{Out}/{FullOutputName}";
        }
    }
}
