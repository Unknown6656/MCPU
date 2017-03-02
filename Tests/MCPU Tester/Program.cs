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
                InnerMain1(args);
            else if (args.Contains("-inner2"))
                InnerMain2(args);
            else // if (args.Contains("-unittests"))
                return UnitTestWrapper.Main(args);

            ReadKey(true);

            return 0;
        }

        private static void err(Exception ex)
        {
            ForegroundColor = ConsoleColor.Red;
            WriteLine("WELL FUGG :D");

            while (ex != null)
            {
                WriteLine($"{ex.Message}\n{ex.StackTrace}");

                ex = ex.InnerException;
            }

            ForegroundColor = ConsoleColor.White;
        }

        private static void InnerMain1(string[] args)
        {
            void onhalt(Processor _)
            {
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine("Processor is kill :/");
                ForegroundColor = ConsoleColor.White;
            }

            Processor proc = new Processor(4096, 4096, -559038737);
            int ind = 0;

            proc.ProcessorReset += onhalt;
            proc.ProcessorHalted += onhalt;
            proc.OnError += (p, ex) => err(ex);
            proc.InstructionExecuting += (p, ins) =>
            {
                WriteLine($"{p.IP:x8}:  {string.Join("", from i in Enumerable.Range(0, ind) select "¦   ")}{ins.ToShortString()}");

                if (ins == CALL)
                    ++ind;
                else if (ins == RET)
                    --ind;
            };

            const string code = @"
void main(void)
{
    int i;

    // i = iscan();
    i = 33;
    i *= 2;

    iprint(i);
}

//int f1(int i)
//{
//    return 9 * i;
//}
//
//void main(void)
//{
//    int lelz;
//    float test;
//
//    lelz = f1((5 - 3) / 2);
//    test = sin(0.0);
//
//    iprint(lelz);
//    fprint(test);
//
//    __asm ""syscall 5 74657374h"";
//    __asm ""syscall 1"";
//}
";
            MCPUPPCompiler comp = new MCPUPPCompiler(proc);
            Union<MCPUPPCompilerResult, MCPUPPCompilerException> res = comp.Compile(code);

            if (res.IsA)
            {
                MCPUPPCompilerResult mcpuppres = res.AsA;
                Instruction[] instr = mcpuppres.Instructions;

                WriteLine($"{mcpuppres.CompiledCode}\n{new string('=', 100)}\n{MCPUCompiler.Decompile(mcpuppres.Instructions)}\n{new string('=', 100)}");

                proc.Process(instr);

                // ConsoleExtensions.HexDump(proc.ToBytes());

            }
            else
                err(res.AsB);
        }

        private static void InnerMain2(string[] args)
        {
            const string code = @"
interrupt func int_88
    syscall 5 68656c6ch 6f207465h 7374h
end func

interrupt func int_01
    syscall 5 2a657870h 6c6f6465h 732ah
end func
    
    .data
    [0] = 420h
    k[0] = 88h
    [1] = 0
    
    .main
    .kernel
    .enable interrupt
    int k[0]
    div [0] [1]
";
            Union<MCPUCompilerResult, MCPUCompilerException> res = MCPUCompiler.Compile(code);

            if (res.IsA)
            {
                Instruction[] instr = res.AsA.Instructions;
                Processor proc = new Processor(1024, 1024, -559038737);

                WriteLine(MCPUCompiler.Decompile(instr));

                proc.OnError += (p, ex) => err(ex);
                proc.Process(instr);
            }
            else
                err(res.AsB);
        }
    }
}
