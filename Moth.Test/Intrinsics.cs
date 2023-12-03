// namespace Moth.Test;
//
// [TestClass]
// public class Intrinsics
// {
//     [TestMethod]
//     public void SizeOf()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i64, align 8" +
//             "\n  store i64 ptrtoint (ptr getelementptr (i32, ptr null, i32 1) to i64), ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i32.sizeof(); return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
//
//     [TestMethod]
//     public void AlignOf()
//     {
//         string expected = "define i32 @main() {" +
//             "\nentry:" +
//             "\n  %val = alloca i64, align 8" +
//             "\n  store i64 ptrtoint (ptr getelementptr ({ i1, i32 }, ptr null, i64 0, i32 1) to i64), ptr %val, align 4" +
//             "\n  ret i32 0" +
//             "\n}" +
//             "\n";
//         string code = Utils.BasicWrap("local val ?= #i32.alignof(); return 0;");
//         LLVMSharp.Interop.LLVMModuleRef module = Utils.FullCompile(code);
//         Assert.AreEqual(expected, module.GetNamedFunction("main").ToString());
//     }
// }
