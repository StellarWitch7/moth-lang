using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Class : CompilerData
{
    public string Name { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>();
    public Dictionary<string, Function> Methods { get; set; } = new Dictionary<string, Function>();
    public Dictionary<string, Field> StaticFields { get; set; } = new Dictionary<string, Field>();
    public Dictionary<string, Function> StaticMethods { get; set; } = new Dictionary<string, Function>();

    public Class(CompilerContext compiler, string name, LLVMTypeRef lLVMClass, PrivacyType privacy)
    {
        Name = name;
        LLVMType = lLVMClass;
        Privacy = privacy;
    }

    public void AddBuiltins(CompilerContext compiler)
    {
        // sizeof()
        {
            if (compiler.Classes.TryGetValue(Primitive.UnsignedInt64, out Class classOfReturnType))
            {
                var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int64, new LLVMTypeRef[0]);
                var func = new Function(compiler.Module.AddFunction("sizeof", funcType),
                    funcType, LLVMTypeRef.Int64, PrivacyType.Public, classOfReturnType, null, new List<Parameter>(), false);
                
                StaticMethods.Add("sizeof", func);
                func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
                compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
                compiler.Builder.BuildRet(LLVMType.SizeOf);
            }
            else
            {
                compiler.Warn($"Failed to create built-in static method \"sizeof() :u64\" within class"
                    + $" \"{Name}\" as its return type (\"{Primitive.UnsignedInt64}\") does not exist.");
            }
        }

        // alignof()
        {
            if (compiler.Classes.TryGetValue(Primitive.UnsignedInt64, out Class classOfReturnType))
            {
                var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int64, new LLVMTypeRef[0]);
                var func = new Function(compiler.Module.AddFunction("alignof", funcType),
                    funcType, LLVMTypeRef.Int64, PrivacyType.Public, classOfReturnType, null, new List<Parameter>(), false);

                StaticMethods.Add("alignof", func);
                func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
                compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);
                compiler.Builder.BuildRet(LLVMType.AlignOf);
            }
            else
            {
                compiler.Warn($"Failed to create built-in static method \"alignof() :u64\" within class"
                    + $" \"{Name}\" as its return type (\"{Primitive.UnsignedInt64}\") does not exist.");
            }
        }
    }
}
