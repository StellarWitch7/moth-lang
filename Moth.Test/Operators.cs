// namespace Moth.Test;
//
// [TestClass]
// public class Operators
// {
//     [TestMethod]
//     public void AddInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 6" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 + 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void SubInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 - 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void MulInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 8" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 * 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void DivInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 / 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ModInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 % 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ExpInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %pow = call float @llvm.powi.f32.i32(float 4.000000e+00, i32 2)" +
//             "\n  %0 = fptosi float %pow to i32" +
//             "\n  ret i32 %0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return 4 ^ 2;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AddAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = add i32 %0, 2" +
//             "\n  store i32 %1, ptr %val, align 4" +
//             "\n  %2 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val += 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void SubAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = sub i32 %0, 2" +
//             "\n  store i32 %1, ptr %val, align 4" +
//             "\n  %2 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val -= 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void MulAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = mul i32 %0, 2" +
//             "\n  store i32 %1, ptr %val, align 4" +
//             "\n  %2 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val *= 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void DivAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = sdiv i32 %0, 2" +
//             "\n  store i32 %1, ptr %val, align 4" +
//             "\n  %2 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val /= 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ModAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = srem i32 %0, 2" +
//             "\n  store i32 %1, ptr %val, align 4" +
//             "\n  %2 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val %= 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ExpAssignInt()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 2, ptr %val, align 4" +
//             "\n  %0 = load i32, ptr %val, align 4" +
//             "\n  %1 = sitofp i32 %0 to float" +
//             "\n  %pow = call float @llvm.powi.f32.i32(float %1, i32 2)" +
//             "\n  %2 = fptosi float %pow to i32" +
//             "\n  store i32 %2, ptr %val, align 4" +
//             "\n  %3 = load i32, ptr %val, align 4" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #i32 = 2;" +
//             "\n  val ^= 2;" +
//             "\n  return val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AndFalseFalse()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 false, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= false and false; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AndTrueFalse()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 false, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= true and false; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AndTrueTrue()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= true and true; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AndFalseTrue()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 false, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= false and true; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void OrFalseFalse()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 false, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= false or false; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void OrTrueFalse()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= true or false; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void OrTrueTrue()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= true or true; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void OrFalseTrue()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= false or true; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Casti32u32()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 -6, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #u32 <- -6; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Castu1i32()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 1, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i32 <- true; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Casti32u1()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #bool <- 2; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Casti32i64()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i64, align 8" +
//             "\n  store i64 7, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i64 <- 7; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Casti32f32()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 7.000000e+00, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #f32 <- 7; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Casti32f64()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca double, align 8" +
//             "\n  store double 7.000000e+00, ptr %val, align 8" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #f64 <- 7; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Castf32f64()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca double, align 8" +
//             "\n  store double 7.000000e+00, ptr %val, align 8" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #f64 <- 7.0; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Castf32i32()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 7, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i32 <- 7.0; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Castf32u32()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i32, align 4" +
//             "\n  store i32 7, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #u32 <- 7.0; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Castf32i64()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i64, align 8" +
//             "\n  store i64 7, ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i64 <- 7.0; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AddFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 6" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 + 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void SubFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 - 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void MulFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 8" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 * 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void DivFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 / 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ModFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 % 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ExpFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %pow = call float @llvm.pow.f32(float 4.000000e+00, float 2.000000e+00)" +
//             "\n  %0 = fptosi float %pow to i32" +
//             "\n  ret i32 %0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("return #i32 <- 4.0 ^ 2.0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AddAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %1 = fadd float %0, 2.000000e+00" +
//             "\n  store float %1, ptr %val, align 4" +
//             "\n  %2 = load float, ptr %val, align 4" +
//             "\n  %3 = fptosi float %2 to i32" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val += 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void SubAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %1 = fsub float %0, 2.000000e+00" +
//             "\n  store float %1, ptr %val, align 4" +
//             "\n  %2 = load float, ptr %val, align 4" +
//             "\n  %3 = fptosi float %2 to i32" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val -= 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void MulAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %1 = fmul float %0, 2.000000e+00" +
//             "\n  store float %1, ptr %val, align 4" +
//             "\n  %2 = load float, ptr %val, align 4" +
//             "\n  %3 = fptosi float %2 to i32" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val *= 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void DivAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %1 = fdiv float %0, 2.000000e+00" +
//             "\n  store float %1, ptr %val, align 4" +
//             "\n  %2 = load float, ptr %val, align 4" +
//             "\n  %3 = fptosi float %2 to i32" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val /= 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ModAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %1 = frem float %0, 2.000000e+00" +
//             "\n  store float %1, ptr %val, align 4" +
//             "\n  %2 = load float, ptr %val, align 4" +
//             "\n  %3 = fptosi float %2 to i32" +
//             "\n  ret i32 %3" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val %= 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void ExpAssignFloat()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca float, align 4" +
//             "\n  store float 2.000000e+00, ptr %val, align 4" +
//             "\n  %0 = load float, ptr %val, align 4" +
//             "\n  %pow = call float @llvm.pow.f32(float %0, float 2.000000e+00)" +
//             "\n  store float %pow, ptr %val, align 4" +
//             "\n  %1 = load float, ptr %val, align 4" +
//             "\n  %2 = fptosi float %1 to i32" +
//             "\n  ret i32 %2" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #f32 = 2.0;" +
//             "\n  val ^= 2.0;" +
//             "\n  return #i32 <- val;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void Equal()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 false, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #bool = 4 == 2; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void NotEqual()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i1, align 1" +
//             "\n  store i1 true, ptr %val, align 1" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val #bool = 4 != 2; return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
// }