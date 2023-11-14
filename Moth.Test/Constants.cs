﻿namespace Moth.Unit;

[TestClass]
public class Constants
{
    [TestMethod]
    public void True()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i1, align 1" +
            "\n  store i1 true, ptr %val, align 1" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("local val ?= true; return 0;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void False()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i1, align 1" +
            "\n  store i1 false, ptr %val, align 1" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("local val ?= false; return 0;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void String()
    {
        string expectedGlobal = "@litstr = global [7 x i8] c\"hello\\0A\\00\"";
        string expectedMain = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca ptr, align 8" +
            "\n  store ptr @litstr, ptr %val, align 8" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("local val ?= \"hello\\n\"; return 0;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expectedGlobal, module.FirstGlobal.ToString());
        Assert.AreEqual(expectedMain, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void ScientificNotation()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %pow = call float @llvm.powi.f32.i32(float 1.000000e+01, i32 5)" +
            "\n  %0 = fptosi float %pow to i32" +
            "\n  %1 = mul i32 2, %0" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("local val ?= 2e+5; return 0;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }
}