namespace Moth.Unit;

[TestClass]
public class Constants
{
    [TestMethod]
    public void True()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i1, align 1" +
            "\n  store i1 true, ptr %val, align 1" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val ?= true; return 0;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void False()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i1, align 1" +
            "\n  store i1 false, ptr %val, align 1" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val ?= false; return 0;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void String()
    {
        var expectedMain = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca ptr, align 8" +
            "\n  store ptr @0, ptr %val, align 8" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var expectedGlobal = "@0 = global [7 x i8] c\"hello\\0A\\00\"";
        var code = Utils.BasicWrap("local val ?= \"hello\\n\"; return 0;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expectedGlobal, module.FirstGlobal.ToString());
        Assert.AreEqual(expectedMain, module.GetNamedFunction("main").ToString());
    }
}
