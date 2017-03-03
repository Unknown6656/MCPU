using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

using MCPU.MCPUPP.Compiler;
using MCPU.MCPUPP.Parser;
using MCPU.Compiler;

using Program = Microsoft.FSharp.Collections.FSharpList<MCPU.MCPUPP.Parser.SyntaxTree.Declaration>;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using static MCPU.MCPUPP.Compiler.MCPUPPCompiler;
    using static MCPU.MCPUPP.Parser.Precompiler;
    using static MCPU.MCPUPP.Parser.Analyzer;

    [TestClass]
    public class PrecompilerTests
        : Commons
    {
        private Processor proc;
        private StreamWriter wr;
        private StringBuilder sb;


        public static (IMBuilder, IMProgram) Precompile(string source)
        {
            Program prog = Lexer.parse(source);
            AnalyzerResult res = Analyze(prog);
            IMBuilder builder = new IMBuilder(res);

            return (builder, builder.BuildClass(prog));
        }

        public static (IMVariable[] globals, IMMethod[] methods) GetResult(string source)
        {
            IMProgram res = Precompile(source).Item2;

            return (res.Fields.ToArray(), res.Methods.ToArray());
        }

        public string WrapMCPUCode(string inner, int locals = 0, int globals = 0) =>
            new StringBuilder().AppendLine(MCPUPPCompiler.GlobalHeader)
                               .AppendLine($"func {MCPUPPCompiler.MAIN_FUNCTION_NAME}")
                               .AppendLine(inner)
                               .AppendLine("end func")
                               .AppendLine(new MCPUPPCompiler(proc).GenerateFunctionCall(new MainFunctionCallInformation
                               {
                                   GlobalSize = globals,
                                   LocalSize = locals,
                               }))
                               .ToString();

        public void Execute(string inner, int locals = 0, int globals = 0) => proc.Process(MCPUCompiler.Compile(WrapMCPUCode(inner, locals, globals)).AsA.Instructions);

        [TestInitialize]
        public override void Test_Init()
        {
            Errors.UpdateLanguage<Dictionary<string, string>>(null); // RESET LANGUAGE TO DEFAULT

            sb = sb ?? new StringBuilder();
            sb.Clear();

            wr?.BaseStream?.Dispose();
            wr?.Dispose();
            wr = new StreamWriter(new MemoryStream());

            proc?.Halt();
            proc?.Dispose();
            proc = new Processor(4096, 4096, -559038737);
            proc.StandardOutput = wr;
            proc.OnError += (_, ex) => {
                throw ex;
            };
            proc.OnTextOutput += (_, text) => sb.Append(text);
        }

        [TestMethod]
        public void Test_01()
        {
            (IMVariable[] globals, IMMethod[] methods) = GetResult(@"
void main(void)
{
}

int global_i;
");
            AreEqual(globals.Length, 1);
            AreEqual(methods.Length, 1);
        }

        [TestMethod]
        public void Test_02()
        {
            (IMVariable[] globals, IMMethod[] methods) = GetResult(@"
int i;
int* p;
float[] a;

void main(void)
{
}
");
            AreEqual(globals.Length, 3);
            AreEqual(methods.Length, 1);

            IMVariable i = globals[0];
            IMVariable p = globals[1];
            IMVariable a = globals[2];

            AreEqual(i.Name, nameof(i));
            AreEqual(p.Name, nameof(p));
            AreEqual(a.Name, nameof(a));
            IsTrue(i.Type.Type.IsInt && i.Type.Cover.IsScalar);
            IsTrue(p.Type.Type.IsInt && p.Type.Cover.IsPointer);
            IsTrue(a.Type.Type.IsFloat && a.Type.Cover.IsArray);
        }

        [TestMethod]
        public void Test_03()
        {
            (IMVariable[] globals, IMMethod[] methods) = GetResult(@"
int f1 (float* ptr, int[]arr)
{
    return -1;
}

void main(void)
{
}
");
            AreEqual(globals.Length, 0);
            AreEqual(methods.Length, 2);

            IMMethod f1 = methods[0];
            IMVariable[] args = f1.Parameters.ToArray();

            AreEqual(f1.Name, nameof(f1));
            AreEqual(f1.Locals.Length, 0);
            AreEqual(args.Length, 2);
            AreEqual(args[0].Name, "ptr");
            IsTrue(args[0].Type.Type.IsFloat && args[0].Type.Cover.IsPointer);
            AreEqual(args[1].Name, "arr");
            IsTrue(args[1].Type.Type.IsInt && args[1].Type.Cover.IsArray);
            IsTrue(f1.ReturnType.IsInt);
        }

        [TestMethod]
        public void Test_04() => Execute(@"
    nop ; JUST TESTING, THAT NOTHING FAILS
");

        [TestMethod]
        public void Test_05()
        {
            Execute($@"
    call SY_PUSH 315h
    call SY_POPL 0
    call SY_PUSH 99
    call SY_PUSHL 
    decr [{F_SYP}]
    syscall 2 [[{F_SYP}]]
", 1);
            string @out = sb.ToString().Trim();

            AreEqual("0x00000315", @out);
        }
    }
}
