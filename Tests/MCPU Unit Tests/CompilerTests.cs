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
        public const char ERROR_LOC = '§';


        public static bool ApproximateFormatStringEqual(string str, string format)
        {
            str = str ?? "";
            format = format ?? "";

            if (str == format)
                return true;
            else
            {
                string[] ln1 = str.Split(' ', '\n', '\r', '\t');
                string[] ln2 = format.Split(' ', '\n', '\r', '\t');
                int len = Math.Min(ln1.Length, ln2.Length);
                int cnt = 0;

                for (int i = 0; i < len; i++)
                    cnt += ln1[i] == ln2[i] ? 0 : 1;

                cnt *= 2;
                cnt -= format.Replace("{{", "").Replace("}}", "").Count(f => f == '{' || f == '}');

                return (cnt / (double)len) < .9;
            }
        }

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

            try
            {
                ln = (from l in code.Split('\n')
                      let nr = ln++
                      where l.Contains(ERROR_LOC)
                      select nr).First();
            }
            catch
            {
                ln = -1;
            }
            
            code = code.Replace(ERROR_LOC, ';');

            MCPUCompilerException ex = Throws<MCPUCompilerException>(delegate {
                throw MCPUCompiler.Compile(code).AsB;
            });

            IsTrue(ln == ex.LineNr);

            return ex;
        }

        public static void CompileExpectError(string code, string message)
        {
            MCPUCompilerException ex = CompileExpectError(code);

            IsTrue(ApproximateFormatStringEqual(ex.Message, message));
        }

        public static Instruction[] Compile(string code) => MCPUCompiler.Compile(code).AsA.Instructions;

        public static bool Contains(Instruction[] instr, OPCode opc) => instr?.Any(i => i.OPCode.Number == opc.Number) ?? false;
        

        public CompilerTests() => MCPUCompiler.ResetLanguage();

        [TestMethod]
        public void Test_01() => CompileExpectError(@"
    mov [0] [0]   §### ERROR
", MCPUCompiler.GetString("INSTR_OUTSIDE_MAIN"));

        [TestMethod]
        public void Test_02() => CompileExpectError(@"
    .main
    kernel 1   §### ERROR
", MCPUCompiler.GetString("DONT_USE_KERNEL"));

        [TestMethod]
        public void Test_03() => CompileExpectError(@"
func f1
    mov [3] [1]
end func
end func   §### ERROR
", MCPUCompiler.GetString("MISSING_FUNC_DECL"));

        [TestMethod]
        public void Test_04() => CompileExpectError(@"
func f1
    jmp label
label:
    add [6], 8
label:   §### ERROR
end func
", MCPUCompiler.GetString("LABEL_ALREADY_EXISTS_SP"));

        [TestMethod]
        public void Test_05() => CompileExpectError(@"
func myfunc
    NOP
end func

    .main
myfunc:   §### ERROR
    HALT
", MCPUCompiler.GetString("FUNC_ALREADY_EXISTS_SP"));

        [TestMethod]
        public void Test_06() => CompileExpectError(@"
func f1
    NOP
func f2   §### ERROR
    MOV [0] 42
end func
end func
", MCPUCompiler.GetString("FUNC_NOT_NESTED"));

        [TestMethod]
        public void Test_07() => CompileExpectError(@"
    .main
    NOP

func myfunc   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("FUNC_AFTER_MAIN"));

        [TestMethod]
        public void Test_08() => CompileExpectError(@"
func f1
    NOP
end func
func f1   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("FUNC_ALREADY_EXISTS"));

        [TestMethod]
        public void Test_09() => CompileExpectError(@"
func f1
label:
    NOP
end func

func label   §### ERROR
    MOV [0] 42
end func
", MCPUCompiler.GetString("LABEL_ALREADY_EXISTS"));

        [TestMethod]
        public void Test_10() => CompileExpectError(@"
    .main
    COMPLETEY_RANDOM_STRING [42] 315   §### ERROR
", MCPUCompiler.GetString("INSTR_NFOUND"));

        [TestMethod]
        public void Test_11() => CompileExpectError(@"
    .main
    MOV [3] ()   §### ERROR
", MCPUCompiler.GetString("INVALID_ARG"));

        [TestMethod]
        public void Test_12() => CompileExpectError(@"
    .main
    //   §### ERROR
", MCPUCompiler.GetString("LINE_NPARSED"));

        [TestMethod]
        public void Test_13() => CompileExpectError(@"
    .random   §### ERROR
", MCPUCompiler.GetString("TOKEN_NOT_PARSED"));

        [TestMethod]
        public void Test_14() => CompileExpectError(@"
.inline func myfunc   ;### ERROR <AFTER PRECOMPILATION>
    NOP
end func

    .main
", MCPUCompiler.GetString("INLINE_NYET_SUPP"));
    }
}
