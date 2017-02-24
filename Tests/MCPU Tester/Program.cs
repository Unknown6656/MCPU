using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using Piglet.Lexer;
using Piglet.Parser;

using MCPU.MCPUPP.Parser.SyntaxTree;
using MCPU.MCPUPP.Compiler;
using MCPU.MCPUPP.Parser;
using MCPU.MCPUPP.Tests;
using MCPU.Compiler;

namespace MCPU
{
    using IA = InstructionArgument;

    using static System.Console;
    using static ArgumentType;
    using static OPCodes;

    public unsafe class Program
    {
        public static void VisualizeError(LexerException err, string code)
        {
            int lnr = 1;

            foreach (string line in code.Split('\n'))
            {
                ForegroundColor = ConsoleColor.Gray;

                if (lnr == err.LineNumber)
                {
                    string bline = err.LineContents.TrimEnd();
                    string eline = line.Remove(0, err.LineContents.Length + 1).TrimStart();

                    ForegroundColor = ConsoleColor.Red;
                    Write("> ");
                    Write(bline);
                    BackgroundColor = ConsoleColor.DarkRed;
                    ForegroundColor = ConsoleColor.White;
                    Write(line.Substring(bline.Length, line.Length - (eline + bline).Length));
                    BackgroundColor = ConsoleColor.Black;
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(eline);
                }
                else
                    WriteLine($"  {line}");

                ++lnr;
            }
        }

        public static int Main(string[] args)
        {
            if (args.Contains("-inner"))
                return InnerMain(args);
            else // if (args.Contains("-unittests"))
                return UnitTestWrapper.Main(args);
        }

        private static int InnerMain(string[] args)
        {
            void err(Exception ex)
            {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            }
            Processor proc = new Processor(4096, 4096, -559038737);

            proc.OnError += (p, ex) => err(ex);

            const string code = @"
//int f1(int i)
//{
//    return 9 * i;
//}

void main(void)
{
    int lelz;
    float test;

    lelz = 315; //f1((5 - 3) / 2);
    test = sin(0.0);
    
    iprint(lelz);
    fprint(test);

    __asm ""syscall 5 74657374h"";
    __asm ""syscall 1"";
}
";
            MCPUPPCompiler comp = new MCPUPPCompiler(proc);
            Union<MCPUPPCompilerResult, MCPUPPCompilerException> res = comp.Compile(code);

            if (res.IsA)
            {
                MCPUPPCompilerResult mcpuppres = res.AsA;
                Instruction[] instr = mcpuppres.Instructions;

                WriteLine($"{mcpuppres.CompiledCode}\n\n{MCPUCompiler.Decompile(mcpuppres.Instructions)}\n");

                proc.Process(instr);

                // ConsoleExtensions.HexDump(proc.ToBytes());

            }
            else
                err(res.AsB);

            ReadKey(true);
            return 0;
        }
    }
}
