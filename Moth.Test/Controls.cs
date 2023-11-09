namespace Moth.Unit;

[TestClass]
public class Controls
{
    [TestMethod]
    public void If()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  br i1 true, label %then, label %else" +
            "\n" +
            "\nthen:                                             ; preds = %entry" +
            "\n  ret i32 4" +
            "\n" +
            "\nelse:                                             ; preds = %entry" +
            "\n  ret i32 2" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("if 4 > 2 { return 4; } else { return 2; }");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void InlineIf()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %result = alloca i32, align 4" +
            "\n  br i1 true, label %then, label %else" +
            "\n" +
            "\nthen:                                             ; preds = %entry" +
            "\n  store i32 4, ptr %result, align 4" +
            "\n  br label %continue" +
            "\n" +
            "\nelse:                                             ; preds = %entry" +
            "\n  store i32 2, ptr %result, align 4" +
            "\n  br label %continue" +
            "\n" +
            "\ncontinue:                                         ; preds = %else, %then" +
            "\n  %0 = load i32, ptr %result, align 4" +
            "\n  ret i32 %0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("return if 4 > 2 then 4 else 2;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void While()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  br label %loop" +
            "\n" +
            "\nloop:                                             ; preds = %entry" +
            "\n  br i1 true, label %then, label %continue" +
            "\n" +
            "\nthen:                                             ; preds = %loop" +
            "\n  ret i32 4" +
            "\n" +
            "\ncontinue:                                         ; preds = %loop" +
            "\n  ret i32 2" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("while 4 > 2 { return 4; } return 2;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Return()
    {
        string expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        string code = Utils.BasicWrap("return 0;");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }
}
