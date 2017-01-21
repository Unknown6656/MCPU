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

using MCPU.MCPUPP.Parser.SyntaxTree;
using MCPU.MCPUPP.Compiler;
using MCPU.MCPUPP.Parser;
using MCPU.MCPUPP.Tests;
using MCPU.Compiler;

using Program = Microsoft.FSharp.Collections.FSharpList<MCPU.MCPUPP.Parser.SyntaxTree.Declaration>;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using static ObjectDumper.Dumper;

    [TestClass]
    public class AnalyzerTests
        : Commons
    {
        public AnalyzerTests()
        {
            // THE FOLLOWING LINES FORCE TO INITIALIZE THE LAZY PARSER PROPERTY
            int j = Lexer.Parser.GetHashCode();

            AreEqual<int>(j, j);
        }

        public static void ExpectAnalyzerFailure(string code, string errname)
        {
            string errmsg = Throws<Exception>(() =>
            {
                Program prog = Lexer.parse(code);

                Analyzer.Analyze(prog);
            }).Message;
            string errexp = Errors.GetFormatString(errname);

            IsTrue(ApproximateFormatStringEqual(errmsg, errexp));
        }

        [TestInitialize]
        public override void Test_Init() => Errors.UpdateLanguage<Dictionary<string, string>>(null); // RESET LANGUAGE TO DEFAULT

        [TestMethod]
        public void Test_01()
        {
            Program prog = Lexer.parse(@"
void main(void)
{
    float[] arr;

    arr = new float[42];
    arr[7] = fscan();
    arr[8] = fscan();

    fprint(arr[7] ^^ arr[8]);

    delete arr;
}
");
            Analyzer.AnalyzerResult res = Analyzer.Analyze(prog);
        }

        [TestMethod]
        public void Test_02() => ExpectAnalyzerFailure(@"
void main(void)
{
    int i;
    float i;
}
", "VARIABLE_EXISTS");

        [TestMethod]
        public void Test_03() => ExpectAnalyzerFailure(@"
int func1(void)
{  
    return 2;
}

void func1(int arg)
{
}
", "FUNCTION_EXISTS");

        [TestMethod]
        public void Test_04() => ExpectAnalyzerFailure(@"
int main(void)
{
    return 4 << i;
}
", "NOT_FOUND");

        [TestMethod]
        public void Test_05() => ExpectAnalyzerFailure(@"
int main(void)
{
    return myfunc(315);
}
", "NOT_FOUND");


        /*
         * TO TEST:

            "ERR_LEXER"
            "ERR_PARSER"
            "NOT_FOUND"
            "IVAL_CAST"
            "IVAL_BREAK"
            "IVAL_INDEX"
            "IVAL_UOP"
            "IVAL_BOP"
            "ARRAY_EXPECTED"
            "FUNC_EXPECTED_ARGC"
            "ARRAY_EXPECTED"
            "IVAL_ARG"
            "MISSING_MAIN"
            "IVAL_MCPUASM"
         */

    }
}
