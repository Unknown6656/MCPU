using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using MCPU.Compiler;

namespace MCPU.Testing
{
    using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;


    public abstract class Commons
    {
        public const char ERROR_LOC = '§';


        public Commons()
        {
            MCPUCompiler.ResetLanguage();
        }

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

            MCPUCompilerException ex = Throws<MCPUCompilerException>(delegate
            {
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

        public static MCPUCompilerResult Compile(string code) => MCPUCompiler.Compile(code).AsA;

        public static bool Contains(Instruction[] instr, OPCode opc) => instr?.Any(i => i.OPCode.Number == opc.Number) ?? false;

        [TestInitialize]
        public virtual void Test_Init()
        {
        }
    }
}
