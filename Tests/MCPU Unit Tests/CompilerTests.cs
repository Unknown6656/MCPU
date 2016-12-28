using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;

using MCPU.Compiler;
using MCPU;

namespace MPCU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class CompilerTests
    {
        public const char ERROR_LOC = '�';


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

        public static MCPUCompilerException CompileExpectError(string code)
        {
            int ln = 1;

            ln = (from l in code.Split('\n')
                  let nr = ln++
                  where l.Contains(ERROR_LOC)
                  select nr).First();
            
            code = code.Replace(ERROR_LOC, ';');

            MCPUCompilerException ex = Throws<MCPUCompilerException>(delegate {
                throw MCPUCompiler.Compile(code).AsB;
            });

            IsTrue(ln == ex.LineNr);

            return ex;
        }

        public static Instruction[] Compile(string code) => MCPUCompiler.Compile(code).AsA.Instructions;

        public static bool Contains(Instruction[] instr, OPCode opc) => instr?.Any(i => i.OPCode.Number == opc.Number) ?? false;
        

        public CompilerTests() => MCPUCompiler.ResetLanguage();

        [TestMethod]
        public void Test_01()
        {
            var ex = CompileExpectError(@"
    mov [0] [0]   �### ERROR
");
            IsTrue(ex.Message == MCPUCompiler.GetString("INSTR_OUTSIDE_MAIN"));
        }

        [TestMethod]
        public void Test_02()
        {
            var ex = CompileExpectError(@"
    .main
    kernel 1   �### ERROR
");
            IsTrue(ex.Message == MCPUCompiler.GetString("DONT_USE_KERNEL"));
        }

        [TestMethod]
        public void Test_03()
        {
            var ex = CompileExpectError(@"
func f1
    mov [3] [1]
end func
end func   �### ERROR
");
            IsTrue(ex.Message == MCPUCompiler.GetString("MISSING_FUNC_DECL"));
        }
    }
}
