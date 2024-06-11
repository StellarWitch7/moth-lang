namespace Moth.LLVM.Metadata;

public struct Version
{
    public uint Major;
    public uint Minor;
    public uint Patch;

    public Version(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new Exception("Version number cannot contain negatives!");
        }

        Major = (uint)major;
        Minor = (uint)minor;
        Patch = (uint)patch;
    }

    public override string ToString()
    {
        if (Patch != 0)
            return $"v{Major}.{Minor}.{Patch}";
        else
            return $"v{Major}.{Minor}";
    }

    public static Version Parse(string str)
    {
        (int major, int minor, int patch) = str.Split('.', 3, StringSplitOptions.None);
        return new Version(major, minor, patch);
    }
}
