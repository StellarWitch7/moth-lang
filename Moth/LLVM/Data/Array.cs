using Moth.AST;
using Moth.AST.Node;

namespace Moth.LLVM.Data;

public class Array : Value
{
    public static Dictionary<Type, ArrStructDecl> ArrayTypes { get; } =
        new Dictionary<Type, ArrStructDecl>();

    public override ArrStructDecl Type { get; }
    public override LLVMValueRef LLVMValue { get; }

    public Array(LLVMCompiler compiler, Type elementType, Value[] elements)
        : base(compiler, null, null)
    {
        Type = ResolveType(compiler, elementType);
        LLVMValue = compiler.Builder.BuildAlloca(Type.LLVMType);

        var arrLLVMType = LLVMTypeRef.CreateArray(elementType.LLVMType, (uint)elements.Length);
        var arr = compiler.Builder.BuildStructGEP2(Type.LLVMType, LLVMValue, 0);
        var length = compiler.Builder.BuildStructGEP2(Type.LLVMType, LLVMValue, 1);

        LLVMValueRef values = compiler.Builder.BuildAlloca(arrLLVMType);
        compiler.Builder.BuildStore(
            LLVMValueRef.CreateConstArray(
                elementType.LLVMType,
                elements.ImplicitConvertAll(compiler, elementType).AsLLVMValues()
            ),
            values
        );
        compiler.Builder.BuildStore(compiler.Builder.BuildLoad2(arrLLVMType, values), arr);
        compiler.Builder.BuildStore(
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)elements.Length),
            length
        );
    }

    public static ArrStructDecl ResolveType(LLVMCompiler compiler, Type elementType)
    {
        if (Array.ArrayTypes.TryGetValue(elementType, out ArrStructDecl type))
        {
            // Keep empty
        }
        else
        {
            type = new ArrStructDecl(compiler, elementType);
            ArrayTypes.Add(elementType, type);
        }

        return type;
    }
}

public class ArrayIndexerFunction : DefinedFunction
{
    public ArrayIndexerFunction(
        LLVMCompiler compiler,
        ArrStructDecl internalArrayStruct,
        Type elementType
    )
        : base(
            compiler,
            internalArrayStruct,
            Reserved.Indexer,
            new FuncType(
                compiler,
                new RefType(compiler, elementType),
                new Type[] { new PtrType(compiler, internalArrayStruct), compiler.UInt32 },
                false
            ),
            new Parameter[0],
            PrivacyType.Pub,
            false,
            new Dictionary<string, IAttribute>()
        )
    {
        using LLVMBuilderRef builder = compiler.Module.Context.CreateBuilder();

        builder.PositionAtEnd(LLVMValue.AppendBasicBlock("entry"));

        var rawArray = builder.BuildLoad2(
            LLVMTypeRef.CreatePointer(elementType.LLVMType, 0),
            builder.BuildStructGEP2(internalArrayStruct.LLVMType, LLVMValue.Params[0], 0)
        );
        var result = builder.BuildInBoundsGEP2(
            elementType.LLVMType,
            rawArray,
            new LLVMValueRef[] { LLVMValue.Params[1] }
        );

        builder.BuildRet(result);
    }
}
