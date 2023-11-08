namespace Moth.Unit;

[TestClass]
public class Operators
{
    [TestMethod]
    public void Addition()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 6" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 + 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Subtraction()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 - 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Multiplication()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 8" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 * 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Division()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 / 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Modulo()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 % 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void Exponential()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %pow = call float @llvm.powi.f32.i32(float 4.000000e+00, i32 2)" +
            "\n  %0 = fptosi float %pow to i32" +
            "\n  ret i32 %0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return 4 ^ 2;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void AddAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = add i32 %0, 2" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  %2 = load i32, ptr %val, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val += 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void SubAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = sub i32 %0, 2" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  %2 = load i32, ptr %val, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val -= 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void MulAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = mul i32 %0, 2" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  %2 = load i32, ptr %val, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val *= 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void DivAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = sdiv i32 %0, 2" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  %2 = load i32, ptr %val, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val /= 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void ModAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = srem i32 %0, 2" +
            "\n  store i32 %1, ptr %val, align 4" +
            "\n  %2 = load i32, ptr %val, align 4" +
            "\n  ret i32 %2" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val %= 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void ExpAssign()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  %val = alloca i32, align 4" +
            "\n  store i32 2, ptr %val, align 4" +
            "\n  %0 = load i32, ptr %val, align 4" +
            "\n  %1 = sitofp i32 %0 to float" +
            "\n  %pow = call float @llvm.powi.f32.i32(float %1, i32 2)" +
            "\n  %2 = fptosi float %pow to i32" +
            "\n  store i32 %2, ptr %val, align 4" +
            "\n  %3 = load i32, ptr %val, align 4" +
            "\n  ret i32 %3" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("local val #i32 = 2;" +
            "\n  val ^= 2;" +
            "\n  return val;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void AndFalseFalse()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return #i32 <- false and false;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void AndTrueFalse()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return #i32 <- true and false;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    //[TestMethod]
    //public void AndTrueTrue()
    //{
    //    var expected = "define i32 @main() {" +
    //        "\nentry:" +
    //        "\n  ret i32 1" +
    //        "\n}" +
    //        "\n";
    //    var code = Utils.BasicWrap("return #i32 <- true and true;");
    //    var module = Utils.FullCompile(code);
    //    Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    //}

    [TestMethod]
    public void AndFalseTrue()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return #i32 <- false and true;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    [TestMethod]
    public void OrFalseFalse()
    {
        var expected = "define i32 @main() {" +
            "\nentry:" +
            "\n  ret i32 0" +
            "\n}" +
            "\n";
        var code = Utils.BasicWrap("return #i32 <- false or false;");
        var module = Utils.FullCompile(code);
        Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    }

    //[TestMethod]
    //public void OrTrueFalse()
    //{
    //    var expected = "define i32 @main() {" +
    //        "\nentry:" +
    //        "\n  ret i32 1" +
    //        "\n}" +
    //        "\n";
    //    var code = Utils.BasicWrap("return #i32 <- true or false;");
    //    var module = Utils.FullCompile(code);
    //    Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    //}

    //[TestMethod]
    //public void OrTrueTrue()
    //{
    //    var expected = "define i32 @main() {" +
    //        "\nentry:" +
    //        "\n  ret i32 1" +
    //        "\n}" +
    //        "\n";
    //    var code = Utils.BasicWrap("return #i32 <- true or true;");
    //    var module = Utils.FullCompile(code);
    //    Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    //}

    //[TestMethod]
    //public void OrFalseTrue()
    //{
    //    var expected = "define i32 @main() {" +
    //        "\nentry:" +
    //        "\n  ret i32 1" +
    //        "\n}" +
    //        "\n";
    //    var code = Utils.BasicWrap("return #i32 <- false or true;");
    //    var module = Utils.FullCompile(code);
    //    Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
    //}
}