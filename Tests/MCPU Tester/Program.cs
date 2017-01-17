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
using MCPU.MCPUPP.Parser;
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

        public static void Main(string[] args)
        {
            var mcppp = @"
int i;

int bar(void)
{
    float* ptr;
}

void foo(int a)
{}

float topkek (int lulz)
{
    return 42.0;
}
".Trim();
            try
            {
                var ast = Lexer.parse(mcppp);
                string repr = Builder.ToString(ast);
            }
            catch (Exception ex)
            {
                if (ex is ParseException pex)
                    WriteLine($"{pex.FoundToken}\n{string.Join(", ", pex.ExpectedTokens)}");
                else if (ex is LexerException lex)
                {
                    VisualizeError(lex, mcppp);
                    ForegroundColor = ConsoleColor.White;
                }

                WriteLine();

                do
                {
                    print(ex);

                    ex = ex.InnerException;
                }
                while (ex != null);

                void print(Exception _)
                {
                    WriteLine(_.Message);
                    WriteLine(_.StackTrace);
                }
            }

            ReadKey(false);
            return;

            Processor proc = new Processor(64, 64, -559038737);

            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };

            var res = MCPUCompiler.Compile(@"
func rec
    wait $2
    incr [0]
    mov [[0]] [0]
    add [[0]] $0
    cmp [0] $1
    jge dump
    call rec $0 $1 $2
dump:
    syscall 1
    halt
end func
    
    .main
    .kernel
    syscall 1
    mov [0] 1
    call rec 42 20h 10
");
            var instr = res.AsA.Instructions;
            int line = 0;

            foreach (var i in instr)
                WriteLine($"{line++:d3}: {i}");
            
            proc.ProcessWithoutReset(instr);

            WriteLine($"SBP: {proc.StackBaseAddress:x8}");
            WriteLine($"SP:  {proc.StackPointerAddress:x8}");
            WriteLine($"SSZ: {proc.StackSize * 4}");
            
            ReadKey(true);
        }
    }
}
