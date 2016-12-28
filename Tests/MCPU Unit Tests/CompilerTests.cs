using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;

using MCPU.Compiler;
using MCPU;

namespace MPCU.Testing
{
    [TestClass]
    public class CompilerTests
    {
        public static T Throws<T>(Action func)
            where T : Exception
        {
            try
            {
                func.Invoke();
            }
            catch (T ex)
            when (!(ex is UnitTestAssertException))
            {
                return ex;
            }

            throw new AssertFailedException($"An exception of type {typeof(T)} was expected, but not thrown.");
        }

        public static Instruction[] Compile(string code) => MCPUCompiler.Compile(code).AsA.Instructions;

        public static bool Contains(Instruction[] instr, OPCode opc) => instr?.Any(i => i.OPCode.Number == opc.Number) ?? false;


        [TestMethod]
        public void Test_01()
        {
            var ex = Throws<MCPUCompilerException>(() => Compile(@"
    syscall 0
"));
            Assert.IsTrue(ex.LineNr == 0);
        }
    }
}
