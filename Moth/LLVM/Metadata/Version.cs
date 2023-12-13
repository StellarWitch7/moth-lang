namespace Moth.LLVM.Metadata;

public struct Version
{
    public uint Major;
    public uint Minor;

    public Version(int major, int minor)
    {
        if (major < 0 || minor < 0)
        {
            throw new Exception("Version number cannot be negative!");
        }

        Major = (uint)major;
        Minor = (uint)minor;
    }

    public override string ToString() => $"{Major}.{Minor}";
}
