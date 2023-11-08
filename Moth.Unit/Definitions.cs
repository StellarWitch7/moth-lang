namespace Moth.Unit;

[TestClass]
public class Definitions
{
    [TestMethod]
    public void ObjectAccess()
    {
        var expectedType = "%Item = type { i32, ptr }";
        var expectedMain = "define i32 @main() {" +
            "\nentry:" +
            "\n  %init = call %Item @Item.init()" +
            "\n  %item = alloca %Item, align 8" +
            "\n  store %Item %init, ptr %item, align 8" +
            "\n  %0 = load %Item, ptr %item, align 8" +
            "\n  %Cost = getelementptr inbounds %Item, ptr %item, i32 0, i32 0" +
            "\n  store i32 5, ptr %Cost, align 4" +
            "\n  %1 = load %Item, ptr %item, align 8" +
            "\n  %Cost1 = getelementptr inbounds %Item, ptr %item, i32 0, i32 0" +
            "\n  %2 = load i32, ptr %Cost1, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.PrependNamespace("public class Item " +
            $"{{ public Cost #i32; public Name #char*; {Utils.WrapInInit("return self;", "Item")} }} " +
            Utils.WrapInMainFunc("local item ?= #Item.init(); " +
                "item.Cost = 5; " +
                "return item.Cost;"));
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expectedType, module.GetTypeByName("Item").ToString());
        Assert.AreEqual(expectedMain, module.GetNamedFunction("main").ToString());
    }
}
