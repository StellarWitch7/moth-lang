namespace Moth.LLVM;

public sealed class CallingConventionAttribute : IAttributeImpl
{
    public static string Identifier => Reserved.CallConv;
    
    public LLVMCallConv CallingConvention { get; private init; }
    
    public static IAttribute Create(IReadOnlyList<object> parameters)
    {
        if (parameters is not [string str])
            throw new ArgumentException("Invalid parameters.", nameof(parameters));

        return new CallingConventionAttribute
        {
            CallingConvention = Utils.StringToCallConv(str)
        };
    }
}

public sealed class TargetOSAttribute : IAttributeImpl
{
    public static string Identifier => Reserved.TargetOS;
    
    public OS[] Targets { get; private init; }
    
    public static IAttribute Create(IReadOnlyList<object> parameters)
    {
        return new TargetOSAttribute
        {
            Targets = parameters.Cast<string>().ToArray().ExecuteOverAll(s => Utils.StringToOS(s)) // could use Select instead
        };
    }
}

public enum OS
{
    Linux,
    Windows,
    MacOS,
}