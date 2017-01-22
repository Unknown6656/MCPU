﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        public static void ExpectNoFailure(string code) => Analyzer.Analyze(Lexer.parse(code));

        public static void ExpectAnalyzerFailure(string code, string errname)
        {
            string errmsg = Throws<Exception>(() => ExpectNoFailure(code)).Message;
            string errexp = Errors.GetFormatString(errname);
            bool comp = ApproximateFormatStringEqual(errmsg, errexp);

            if (!comp)
            {
                Console.WriteLine();
                ConsoleExtensions.Diff(errexp, errmsg);
            }

            IsTrue(comp);
        }

        [TestInitialize]
        public override void Test_Init() => Errors.UpdateLanguage<Dictionary<string, string>>(null); // RESET LANGUAGE TO DEFAULT

        [TestMethod]
        public void Test_01() => ExpectNoFailure(@"
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

        [TestMethod]
        public void Test_06() => ExpectAnalyzerFailure(@"
void main(void)
{
    int i;

    i = 42.0;
}
", "IVAL_CAST");

        [TestMethod]
        public void Test_07() => ExpectAnalyzerFailure(@"
void main(void)
{
    int* i;

    i = new float[6];
}
", "IVAL_CAST");

        [TestMethod]
        public void Test_08() => ExpectAnalyzerFailure(@"
void main(void)
{
    float[] arr;

    arr = new float[42.0];
}
", "IVAL_CAST");

        [TestMethod]
        public void Test_09() => ExpectAnalyzerFailure(@"
void main(void)
{
    if (true)
    {
        break;
    }
}
", "IVAL_BREAK");

        [TestMethod]
        public void Test_10() => ExpectAnalyzerFailure(@"
void main(void)
{
    int j;

    j[6] = 315;
}
", "IVAL_INDEX");

        [TestMethod]
        public void Test_11() => ExpectAnalyzerFailure(@"
void main(void)
{
    int* ptr;

    ptr[6] = 315;
}
", "IVAL_INDEX");

        [TestMethod]
        public void Test_12() => ExpectAnalyzerFailure(@"
int main(void)
{
    int* val;

    return val >> 42.0;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_13() => ExpectAnalyzerFailure(@"
int main(void)
{
    int i;
    float f;

    i = 315;
    f = -42.0;

    return i & f;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_14() => ExpectAnalyzerFailure(@"
int main(void)
{
    int[] arr;

    return 44 + arr;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_15() => ExpectAnalyzerFailure(@"
int main(void)
{
    int[] arr;

    return -arr;
}
", "IVAL_UOP");

        [TestMethod]
        public void Test_16() => ExpectAnalyzerFailure(@"
int main(void)
{
    int* ptr;

    return +ptr;
}
", "IVAL_UOP");

        [TestMethod]
        public void Test_17() => ExpectAnalyzerFailure(@"
int main(void)
{
    {
        int i;

        i = 42;
    }

    return i;
}
", "NOT_FOUND");

        [TestMethod]
        public void Test_18() => ExpectNoFailure(@"
int foo()
{
    int i;
    i = -1;
    return i;
}

int main(void)
{
    int i;

    i = 42;
            
    while (true)
        if (i > 7)
            break;
        else
            i = i / 5;

    return i;
}
");

        [TestMethod]
        public void Test_19() => ExpectNoFailure(@"
void main(void)
{
    int i;

    i += 9;
    i *= (i >>>= 4);
    i %= (i -= 315);
}
");

        [TestMethod]
        public void Test_20() => ExpectAnalyzerFailure(@"
float main(void)
{
    float f;

    return f[315];
}
", "IVAL_INDEX");

        [TestMethod]
        public void Test_21() => ExpectAnalyzerFailure(@"
void main(void)
{
    int* ptr;

    ptr[5] = 8;
}
", "IVAL_INDEX");

        [TestMethod]
        public void Test_22() => ExpectAnalyzerFailure(@"
int ptr;

void main(void)
{
    delete ptr;
}
", "ARRAY_EXPECTED");

        [TestMethod]
        public void Test_23() => ExpectAnalyzerFailure(@"
void func1(int i) { }

void main(void)
{
    func1();
}
", "FUNC_EXPECTED_ARGC");

        [TestMethod]
        public void Test_24() => ExpectAnalyzerFailure(@"
void func1(int i) { }

void main(void)
{
    func1(315, 42);
}
", "FUNC_EXPECTED_ARGC");

        [TestMethod]
        public void Test_25() => ExpectNoFailure(@"
void func1(int i) { }

void main(void)
{
    func1(-88 << 7);
}
");

        [TestMethod]
        public void Test_26() => ExpectAnalyzerFailure(@"
void func1(int i) { }

void main(void)
{
    func1(42.0);
}
", "IVAL_ARG");

        [TestMethod]
        public void Test_27() => ExpectAnalyzerFailure(@"
void not_a_main_function(void)
{
}
", "MISSING_MAIN");


        /*
         * TO TEST:

            assignment operators
                
            "ERR_LEXER"
            "ERR_PARSER"
            
            
            "ARRAY_EXPECTED"
            "IVAL_MCPUASM"
         */

    }
}
