namespace Moth.Unit;

[TestClass]
public class Constants
{
    //[TestMethod]
    //public void True()
    //{
    //    var expected = "define i32 @main() {" +
    //        "\nentry:" +
    //        "\n  ret i32 1" +
    //        "\n}" +
    //        "\n";
    //    var code = Utils.BasicWrap("return #i32 <- true;");
    //    var module = Utils.FullCompile(code);
    //    Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    //}


    [TestMethod]
    public void False()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return #i32 <- false;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }
}
