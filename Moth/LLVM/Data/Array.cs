using Moth.AST.Node;
using System.Reflection.Metadata.Ecma335;

namespace Moth.LLVM.Data;

public class Array : Value
{
    public override ArrType Type { get; }
    public override LLVMValueRef LLVMValue { get; }

    private LLVMCompiler _compiler;

    public Array(LLVMCompiler compiler, Type elementType, Value[] elements)
        : base(null, null)
    {
        _compiler = compiler;
        Type = new ArrType(compiler, elementType); //TODO: replace with lazy type creation?
        LLVMValue = compiler.Builder.BuildAlloca(Type.BaseType.LLVMType);
        
        var arr = compiler.Builder.BuildStructGEP2(Type.BaseType.LLVMType, LLVMValue, 0);
        var length = compiler.Builder.BuildStructGEP2(Type.BaseType.LLVMType, LLVMValue, 1);
        compiler.Builder.BuildStore(LLVMValueRef.CreateConstArray(elementType.LLVMType, elements.AsLLVMValues()), arr);
        compiler.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)elements.Length), length);
    }
}

public class ArrayIndexerFunction : DefinedFunction
{
    public ArrayIndexerFunction(LLVMCompiler compiler, PrimitiveType internalArrayStruct, Type elementType)
        : base(compiler, internalArrayStruct, Reserved.Indexer, new FuncType(new PtrType(elementType), new Type[]
        {
            new PtrType(internalArrayStruct),
            Primitives.UInt32
        }, false), new Parameter[0], PrivacyType.Public, false, new Dictionary<string, IAttribute>())
    {
        using LLVMBuilderRef builder = compiler.Module.Context.CreateBuilder();
        builder.PositionAtEnd(LLVMValue.AppendBasicBlock("entry"));
        var rawArray = builder.BuildStructGEP2(internalArrayStruct.LLVMType, LLVMValue.Params[0], 0);
        var result = builder.BuildInBoundsGEP2(elementType.LLVMType, rawArray, new LLVMValueRef[]
        {
            builder.BuildIntCast(LLVMValue.Params[1], LLVMTypeRef.Int64)
        });
        builder.BuildRet(result);
    }
}