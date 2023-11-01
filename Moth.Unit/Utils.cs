using LLVMSharp.Interop;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Unit;

internal class Utils
{
    public static LLVMModuleRef FullCompile(string code)
    {
        var tokens = Tokenizer.Tokenize(code);
        var context = new ParseContext(tokens);
        var ast = TokenParser.ProcessScript(context);
        var compiler = new CompilerContext("fullcomp");
        LLVMCompiler.Compile(compiler, new ScriptAST[] { ast });
        return compiler.Module;
    }
}
