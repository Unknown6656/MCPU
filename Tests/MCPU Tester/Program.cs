using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using MCPU.Compiler;

namespace MCPU
{
    using IA = InstructionArgument;

    using static System.Console;
    using static ArgumentType;
    using static OPCodes;

    public unsafe class Program
    {
        public static void Main(string[] args)
        {
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
