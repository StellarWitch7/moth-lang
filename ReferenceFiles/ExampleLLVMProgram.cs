using LLVMSharp.Interop;

namespace LanguageParserV2;

public static class ExampleLLVMProgram
{
	public record struct Params
	{
		public int A, B;
	}
	
	public static unsafe void Main()
	{
		LLVMContextRef context = LLVMContextRef.Global;
		LLVMModuleRef module = context.CreateModuleWithName("test");
		
		// Create a type called Params
		var paramsT = context.CreateNamedStruct("Params");
		paramsT.StructSetBody(new []{ LLVMTypeRef.Int32, LLVMTypeRef.Int32 }, false);
		
		// Create a function with signature: (Params, int) -> Params
		LLVMTypeRef funcType = LLVMTypeRef.CreateFunction(paramsT, new[]
		{
			paramsT,
			LLVMTypeRef.Int32,
		});
		LLVMValueRef func = module.AddFunction("something", funcType);
		
		// Builders are used to generate IR statements
		LLVMBuilderRef builder = context.CreateBuilder();
		
		// Think of blocks as something similar to scopes, but a bit more low level
		LLVMBasicBlockRef block = context.AppendBasicBlock(func, "");
		builder.PositionAtEnd(block);

		// Params result = <parameter 0>
		var result = builder.BuildAlloca(paramsT, "result");
		builder.BuildStore(func.Params[0], result);
		
		// Load result.A and result.B
		var a = builder.BuildLoad2(LLVMTypeRef.Int32, builder.BuildStructGEP2(paramsT, result, 0));
		var b = builder.BuildLoad2(LLVMTypeRef.Int32, builder.BuildStructGEP2(paramsT, result, 1));
		
		// result.A >= result.B
		var cond = builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, a, b);

		// Create various blocks in preparation for a conditional branch
 		var then = context.AppendBasicBlock(func, "then");
		var @else = context.AppendBasicBlock(func, "else");
		var @continue = context.AppendBasicBlock(func, "continue");
		
		// if result.A >= result.B { ... } else { ... } ...
		builder.BuildCondBr(cond, then, @else);
		
		// then
		{
			builder.PositionAtEnd(then);
			
			// result.A = result.A + <parameter 1>
			var newA = builder.BuildAdd(a, func.Params[1]);
			builder.BuildStore(newA, builder.BuildStructGEP2(paramsT, result, 0));
			
			// result.B = result.B + <parameter 1>
			var newB = builder.BuildAdd(b, func.Params[1]);
			builder.BuildStore(newB, builder.BuildStructGEP2(paramsT, result, 1));
			
			// goto continue
			builder.BuildBr(@continue);
		}

		// else
		{
			builder.PositionAtEnd(@else);
			
			// result.A = result.A - <parameter 1>
			var newA = builder.BuildSub(a, func.Params[1]);
			builder.BuildStore(newA, builder.BuildStructGEP2(paramsT, result, 0));
			
			// result.B = result.B - <parameter 1>
			var newB = builder.BuildSub(b, func.Params[1]);
			builder.BuildStore(newB, builder.BuildStructGEP2(paramsT, result, 1));
			
			// goto continue
			builder.BuildBr(@continue);
		}
		
		// continue
		{
			builder.PositionAtEnd(@continue);
			var resultVal = builder.BuildLoad2(paramsT, result);
			builder.BuildRet(resultVal);
		}

		module.Dump();
		module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);

		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmParser();
		LLVM.InitializeX86AsmPrinter();
		
		LLVMExecutionEngineRef jit = module.CreateExecutionEngine();
		var something = (delegate*<Params, int, Params>) jit.GetFunctionAddress("something");
		
		// For some reason passing structs directly from C# to LLVM doesn't work as expected.
		// Pointers (or refs) to structs work though, so you may try with those.
		Console.WriteLine(something(new Params { A = 42, B = 0 }, 64));
	}
}

