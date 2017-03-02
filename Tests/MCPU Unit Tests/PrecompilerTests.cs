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

using MCPU.MCPUPP.Parser;

using Program = Microsoft.FSharp.Collections.FSharpList<MCPU.MCPUPP.Parser.SyntaxTree.Declaration>;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using static MCPU.MCPUPP.Parser.Precompiler;
    using static MCPU.MCPUPP.Parser.Analyzer;

    [TestClass]
    public class PrecompilerTests
        : Commons
    {
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

        [TestInitialize]
        public override void Test_Init() => Errors.UpdateLanguage<Dictionary<string, string>>(null); // RESET LANGUAGE TO DEFAULT

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
    }
}
