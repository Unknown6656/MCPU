using System;
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
             @"
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
", @"
; this is a loop, which counts from 20 down to 0
.main
    .kernel
    mov [1] 20
    syscall 2 [1]
loop:
    decr [1]
    cmp [1]
    syscall 2 [1]
    jz end
    jmp loop
end:
    .user
    halt
", @"
.main
    .kernel
    syscall 2 k[2]  ; print the IP
    add k[2] 2      ; skip the next instruction
    halt
    syscall 2 0xdeadbeef
",
        };

        public static void Main(string[] args)
        {
            Processor proc = new Processor(16);
            Instruction[] res = MCPUCompiler.Compile(PROGR[1]);
            byte[] bytes = Instruction.SerializeMultiple(res);

            WriteLine(string.Join("\n", res as object[]));
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
