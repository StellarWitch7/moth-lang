﻿using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Moth.LLVM.Data;

public class Class : CompilerData
{
    public string Name { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>();
    public FuncDictionary Methods { get; set; } = new FuncDictionary();
    public Dictionary<string, Field> StaticFields { get; set; } = new Dictionary<string, Field>();
    public FuncDictionary StaticMethods { get; set; } = new FuncDictionary();
    public Dictionary<string, Constant> Constants { get; set; } = new Dictionary<string, Constant>();

    public Class(string name, LLVMTypeRef lLVMType, PrivacyType privacy)
    {
        Name = name;
        LLVMType = lLVMType;
        Privacy = privacy;
    }

    public void AddBuiltins(CompilerContext compiler)
    {
        // sizeof()
        {
            if (compiler.Classes.TryGetValue(Reserved.UnsignedInt64, out Class classOfReturnType))
            {
                var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int64, new LLVMTypeRef[0]);
                var func = new Function(Reserved.SizeOf, compiler.Module.AddFunction($"{Name}.{Reserved.SizeOf}", funcType),
                    funcType, LLVMTypeRef.Int64, classOfReturnType, PrivacyType.Public, null, new List<Parameter>(), false);
                
                StaticMethods.Add(new Signature(Reserved.SizeOf, new TypeRefNode[0]), func);
                func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
                compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

                if (Name == Reserved.Void)
                {
                    compiler.Builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0));
                }
                else
                {
                    compiler.Builder.BuildRet(LLVMType.SizeOf);
                }
            }
            else
            {
                compiler.Warn($"Failed to create built-in static method \"{Reserved.SizeOf}() :u64\" within class"
                    + $" \"{Name}\" as its return type (\"{Reserved.UnsignedInt64}\") does not exist.");
            }
        }

        // alignof()
        {
            if (compiler.Classes.TryGetValue(Reserved.UnsignedInt64, out Class classOfReturnType))
            {
                var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int64, new LLVMTypeRef[0]);
                var func = new Function(Reserved.AlignOf, compiler.Module.AddFunction($"{Name}.{Reserved.AlignOf}", funcType),
                    funcType, LLVMTypeRef.Int64, classOfReturnType, PrivacyType.Public, null, new List<Parameter>(), false);

                StaticMethods.Add(new Signature(Reserved.AlignOf, new TypeRefNode[0]), func);
                func.OpeningScope = new Scope(func.LLVMFunc.AppendBasicBlock("entry"));
                compiler.Builder.PositionAtEnd(func.OpeningScope.LLVMBlock);

                if (Name == Reserved.Void)
                {
                    compiler.Builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1));
                }
                else
                {
                    compiler.Builder.BuildRet(LLVMType.AlignOf);
                }
            }
            else
            {
                compiler.Warn($"Failed to create built-in static method \"{Reserved.AlignOf}() :u64\" within class"
                    + $" \"{Name}\" as its return type (\"{Reserved.UnsignedInt64}\") does not exist.");
            }
        }
    }
}
