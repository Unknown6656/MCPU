using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
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
using Piglet.Lexer;

using Program = Microsoft.FSharp.Collections.FSharpList<MCPU.MCPUPP.Parser.SyntaxTree.Declaration>;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using static ObjectDumper.Dumper;

    [TestClass]
    public class LexerTests
        : Commons
    {
        public LexerTests()
        {
            // THE FOLLOWING LINES FORCE TO INITIALIZE THE LAZY PARSER PROPERTY
            int j = Lexer.Parser.GetHashCode();

            AreEqual<int>(j, j);
        }

        internal static void ExpectParserFailure(string code) => Throws<ParseException>(() => Lexer.parse(code));

        internal static void ExpectLexerFailure(string code) => Throws<LexerException>(() => Lexer.parse(code));

        internal static void AreEqual(Program prog1, Program prog2)
        {
            bool innerequal(object obj1, object obj2)
            {
                try
                {
                    if (obj1 == obj2)
                        return true;
                    else if ((obj1 == null) ^ (obj2 == null))
                        return false;
                    else
                    {
                        Type t1 = obj1.GetType();
                        Type t2 = obj2.GetType();

                        if (t1 == t2)
                            if (obj1 is IEnumerable enum1)
                            {
                                IEnumerator enum2 = (obj2 as IEnumerable).GetEnumerator();

                                foreach (object v1 in enum1)
                                    if (!enum2.MoveNext())
                                        return false;
                                    else if (!innerequal(v1, enum2.Current))
                                        return false;

                                return true;
                            }
                            else if (obj1.Equals(obj2))
                                return true;
                            else if (obj1.GetHashCode() == obj2.GetHashCode())
                                return true;
                            else
                            {
                                TypedReference r1 = __makeref(obj1);
                                TypedReference r2 = __makeref(obj2);

                                FieldInfo[] f1 = t1.GetFields();
                                FieldInfo[] f2 = t2.GetFields();
                                PropertyInfo[] p1 = t1.GetProperties();
                                PropertyInfo[] p2 = t2.GetProperties();

                                if ((f1.Length == f2.Length) && (p1.Length == p2.Length))
                                {
                                    for (int i = 0; i < f1.Length; i++)
                                        if (!innerequal(f1[i].GetValueDirect(r1), f2[i].GetValueDirect(r2)))
                                            return false;

                                    for (int i = 0; i < p1.Length; i++)
                                        if (!innerequal(p1[i].GetValue(obj1), p2[i].GetValue(obj2)))
                                            return false;

                                    return true;
                                }
                            }
                    }
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                }

                return false;
            }

            try
            {
                AreEqual<Program>(prog1, prog2);
            }
            catch
            {
                IsTrue(innerequal(prog1, prog2));
            }
        }

        internal static void ValidateTest((string code, Program ast) data)
        {
            Program generated = Lexer.parse(data.code);

            try
            {
                AreEqual(generated, data.ast);
            }
            catch
            {
                Console.WriteLine();
                ConsoleExtensions.Diff(data.ast.ToDebugString(), generated.ToDebugString());

                throw;
            }
        }

        [TestInitialize]
        public override void Test_Init()
        {
        }

        [TestMethod]
        public void Test_01()
        {
            SYAstate s1 = ShuntingYardAlgorithm.parse("( 42 - 7 ) * 2");
            SYAstate s2 = ShuntingYardAlgorithm.ShuntingYard(s1);
            string[] expected = "42 7 - 2 *".Split();

            IsTrue(s2.Input.IsEmpty);
            IsTrue(s2.Stack.IsEmpty);

            int i = 0;

            foreach (string s in s2.Output.Reverse())
                IsTrue(s == expected[i++]);
        }

        [TestMethod]
        public void Test_02() => ValidateTest(UnitTests.Test01);

        [TestMethod]
        public void Test_03() => ValidateTest(UnitTests.Test02);

        [TestMethod]
        public void Test_04() => ValidateTest(UnitTests.Test03);

        [TestMethod]
        public void Test_05() => ValidateTest(UnitTests.Test04);

        [TestMethod]
        public void Test_06()
        {
            string code = @"
int a;

void main(void)
{
    int b;

    a = 42;
    b = 88;
    
    return a - b;
}
";
            var ast = Lexer.parse(code);
            string sym = ast.ToDebugString();

            // TODO
        }

        [TestMethod]
        public void Test_07() => ValidateTest(UnitTests.Test05);

        [TestMethod]
        public void Test_08() => ValidateTest(UnitTests.Test06);

        [TestMethod]
        public void Test_09() => ValidateTest(UnitTests.Test07);

        [TestMethod]
        public void Test_10() => ValidateTest(UnitTests.Test08);

        [TestMethod]
        public void Test_11() => ValidateTest(UnitTests.Test09);

        [TestMethod]
        public void Test_12() => ValidateTest(UnitTests.Test10);

        [TestMethod]
        public void Test_13() => ValidateTest(UnitTests.Test11);

        [TestMethod]
        public void Test_14() => ValidateTest(UnitTests.Test12);

        [TestMethod]
        public void Test_15() => ValidateTest(UnitTests.Test13);

        [TestMethod]
        public void Test_16() => ValidateTest(UnitTests.Test14);

        [TestMethod]
        public void Test_17() => ValidateTest(UnitTests.Test15);

        [TestMethod]
        public void Test_18() => ValidateTest(UnitTests.Test16);

        [TestMethod]
        public void Test_19() => ValidateTest(UnitTests.Test17);

        [TestMethod]
        public void Test_20() => ValidateTest(UnitTests.Test18);

        [TestMethod]
        public void Test_21() => ExpectParserFailure(@"
void main(void)
{
    42.0 /= 315;
}
");

        [TestMethod]
        public void Test_22() => ExpectParserFailure(@"
void main(void)
{
    delete 88;
}
");

        [TestMethod]
        public void Test_23() => Lexer.parse(@"
void main(void)
{
    __asm "" // hurr durr, comment in a string "";
}
");

        [TestMethod]
        public void Test_24() => ExpectLexerFailure(@"
void main(void)
{
    __asm "";
}
");

        [TestMethod]
        public void Test_25() => ExpectParserFailure(@"
void main(void)
{
    top[] kek;
}
");
    }
}
