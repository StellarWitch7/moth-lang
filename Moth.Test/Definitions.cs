namespace Moth.Unit;

[TestClass]
public class Definitions
{
    [TestMethod]
    public void Class()
    {
        string expected = "%Thing = type { i32, i1, float }";
        string code = Utils.PrependNamespace("public class Thing { public int #i32; public bool #bool; private float #f32; }");
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetTypeByName("Thing").ToString());
    }

    [TestMethod]
    public void ObjectAccess()
    {
        string expectedType = "%Item = type { i32, ptr }";
        string expectedMain = "define i32 @main() {" +
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
        string code = Utils.PrependNamespace("public class Item " +
            $"{{ public Cost #i32; public Name #char*; {Utils.WrapInInit("return self;", "Item")} }} " +
            Utils.WrapInMainFunc("local item ?= #Item.init(); " +
                "item.Cost = 5; " +
                "return item.Cost;"));
        LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
        Assert.AreEqual(expectedType, module.GetTypeByName("Item").ToString());
        Assert.AreEqual(expectedMain, module.GetNamedFunction("main").ToString());
    }
}
