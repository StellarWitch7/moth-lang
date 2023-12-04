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
        Assert.AreEqual(/*unknown*/);
    }

    [TestMethod]
    public void False()
    {
        string code = Utils.TypedFuncWrap("return false;", "bool");
        (var compiler, var engine) = Utils.FullCompile(code);
        var ret = Utils.RunFunction(compiler, engine);
        Assert.AreEqual("i1 0", ret);
    }

    [TestMethod]
    public void String()
    {
        throw new NotImplementedException();
    }

    [TestMethod]
    public void ScientificNotation()
    {
        string code = Utils.TypedFuncWrap("return 2e+5;", "i32");
        (var compiler, var engine) = Utils.FullCompile(code);
        var ret = Utils.RunFunction(compiler, engine);
        Assert.AreEqual("i32 200000", ret);
    }
}
