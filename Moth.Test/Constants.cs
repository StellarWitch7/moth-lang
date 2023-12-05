using Moth.LLVM;
using LLVMSharp.Interop;

namespace Moth.Test;

[TestClass]
public class Constants
{
    [TestMethod]
    public void True()
    {
        string code = Utils.TypedFuncWrap("return true;", "bool");
        (var compiler, var engine) = Utils.FullCompile(code);
        var ret = Utils.RunFunction(compiler, engine);
        
        unsafe
        {
            Assert.AreEqual((ulong)1, LLVMSharp.Interop.LLVM.GenericValueToInt(ret, 0));
        }
    }

    [TestMethod]
    public void False()
    {
        string code = Utils.TypedFuncWrap("return false;", "bool");
        (var compiler, var engine) = Utils.FullCompile(code);
        var ret = Utils.RunFunction(compiler, engine);
        
        unsafe
        {
            Assert.AreEqual((ulong)0, LLVMSharp.Interop.LLVM.GenericValueToInt(ret, 0));
        }
    }

    [TestMethod]
    public void String()
    {
        throw new NotImplementedException();
    }

    [TestMethod]
    public void ScientificNotation()
    {
        throw new NotImplementedException();
    }
}
