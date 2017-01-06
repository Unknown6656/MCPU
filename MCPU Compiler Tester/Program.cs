﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace MCPU
{
    using Compiler;
    
    public static class Program
    {
        public readonly static string[] PROGR = new string[] {
             /* 00 */ @"
FUNC decr1
    SUB [$0] 1
END FUNC

FUNC add42
    ADD [$0] 0x43
    CALL decr1 $0
END FUNC

.MAIN
    .KERNEL
    MOV [0] 0
    MOV [1] 0x315
    MOV [2] -0x43
    CALL add42 0
    CALL add42 1
    CALL add42 2
    SYSCALL 1
    .USER
", /* 01 */  @"
; this is a loop, which counts from 20 down to 0
func print
    .kernel
    syscall 2 [$0]
    .user
end func

    .main
    mov [1] 20
    call print 1
loop:
    decr [1]
    cmp [1]
    call print 1
    jz end
    jmp loop
end:
    halt
", /* 02 */ @"
    .main
    .kernel
    syscall 2 k[2]  ; print the IP
    add k[2] 2      ; skip the next instruction
    halt
    syscall 2 0xdeadbeef
", /* 03 */ @"
func fdebug
    syscall 3 $0
    syscall 2 $0
end func

    .main
    .kernel
    mov [0] 42.0
    call fdebug [0]
    fadd [0] 1.
    call fdebug [0]
    fmul [0] -.5
    call fdebug [0]
",

        };

        public static void Main(string[] args)
        {
            Processor proc = new Processor(16);
            Instruction[] res = MCPUCompiler.Compile(PROGR[3]);
            byte[] bytes = Instruction.SerializeMultiple(res);
            int line = 0;

            foreach (Instruction instr in res)
                WriteLine($"{line++:x8}: {instr}");

            WriteLine("\n\n\n" + MCPUCompiler.Decompile(res) + "\n");

            ConsoleExtensions.HexDump(bytes);

            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };
            proc.Process(res);
            
            ReadKey(true);
        }
    }
}