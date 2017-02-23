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

using Piglet.Parser;

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
", "IVAL_CAST");

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

        [TestMethod, Skip] // skip, as this will be handled by the precompiler - not the analyzer
        public void Test_28() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm "" ]\¦[√№θσ ""; // random string
}
", "IVAL_MCPUASM");

        [TestMethod, Skip] // skip, as this will be handled by the precompiler - not the analyzer
        public void Test_29() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm ""func test"";
    __asm ""    nop"";
    __asm ""end func"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_30() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm "".kernel"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_31() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm "".user"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_32() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm "".main"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_33() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm ""ret"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_34() => ExpectAnalyzerFailure(@"
void main(void)
{
    __asm ""call main"";
}
", "IVAL_MCPUASM");

        [TestMethod]
        public void Test_35() => ExpectNoFailure(@"
void main(void)
{
    float f;
    int i;

    f += i;
    f -= 315;
    f *= 42.0;
    f /= i;
    f %= -1;
    i <<= i;
    i >>= i;
    i <<<= -2;
    i >>>= 42;
    i &= 315;
    i |= i;
    i %= 7;
    i ^= 41;
    f ^^= -2.5;
}
");

        [TestMethod]
        public void Test_36() => ExpectAnalyzerFailure(@"
void main(void)
{
    float f;
    int i;

    f &= i;
}
", "IVAL_CAST");

        [TestMethod]
        public void Test_37() => ExpectAnalyzerFailure(@"
void main(void)
{
    float f;

    f ^= -42.0;
}
", "IVAL_CAST");

        [TestMethod]
        public void Test_38() => ExpectAnalyzerFailure(@"
void main(void)
{
    float* f;
    float r;

    r = 42.0 ^^ f;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_39() => ExpectAnalyzerFailure(@"
void main(void)
{
    int[] arr;
    int res;

    res = arr >> 42;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_40() => ExpectAnalyzerFailure(@"
void main(void)
{
    int[] arr;
    int res;
    float y;

    res = arr <= y;
}
", "IVAL_BOP");

        [TestMethod]
        public void Test_41() => ExpectNoFailure(@"
void main(void)
{
    {
        int i;
        {
            float f;
        }
    }
    {
        int* i;
        float[] f;
    }
}
");

        [TestMethod]
        public void Test_42() => Throws<ParseException>(() => ExpectNoFailure(@"
___RANDOM_STRING__
θτ€φ¶σθ
"));

        [TestMethod]
        public void Test_43() => ExpectAnalyzerFailure(@"
void main(void)
{
    int i;

    delete i;
}
", "ARRAY_EXPECTED");

        [TestMethod]
        public void Test_44() => ExpectAnalyzerFailure(@"
void main(void)
{
    float* f;
    int sz;

    sz = f.length;
}
", "ARRAY_EXPECTED");

        [TestMethod]
        public void Test_45() => ExpectAnalyzerFailure(@"
void main(void)
{
    float i;
    int sz;

    sz = i.length;
}
", "ARRAY_EXPECTED");

        [TestMethod, Skip]
        public void Test_46() => ExpectAnalyzerFailure(@"
float i;

void main(void)
{
    int i;
}
", "VARIABLE_EXISTS");

        [TestMethod, Skip]
        public void Test_47() => ExpectAnalyzerFailure(@"
int[] i;

void main(void)
{
    int[] i;
}
", "VARIABLE_EXISTS");

        [TestMethod]
        public void Test_48() => ExpectAnalyzerFailure(@"
void main(void)
{
    void[] arr;
}
", "IVAL_VARTYPE");

        [TestMethod]
        public void Test_49() => ExpectAnalyzerFailure(@"
void main(void)
{
    void v;
}
", "IVAL_VARTYPE");

        [TestMethod]
        public void Test_50() => ExpectAnalyzerFailure(@"
void main(void)
{
    void* ptr;
}
", "IVAL_VARTYPE");

        [TestMethod]
        public void Test_51() => ExpectAnalyzerFailure(@"
void main(void kek)
{
}
", "IVAL_VARTYPE");

        /*
         * TO TEST:
         * 
            "ERR_LEXER"
            "ERR_PARSER"
            "ARRAY_EXPECTED"
         */
    }
}
