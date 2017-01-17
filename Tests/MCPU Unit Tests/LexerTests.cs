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

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using static ObjectDumper.Dumper;

    [TestClass]
    public class LexerTests
        : Commons
    {
        internal static void ValidateTest((string code, FSharpList<Declaration> ast) data)
        {
            FSharpList<Declaration> generated = Lexer.parse(data.code);

            AreEqual(generated, data.ast);
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
            string sym = ast.Dump(2);

            Debugger.Break();// TODO
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
    }
}
