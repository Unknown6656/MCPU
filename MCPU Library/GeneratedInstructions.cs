using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
	using Instructions;

    /// <summary>
    /// Contains all known OP codes
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode, Serializable, GeneratedCode("Visual Studio T4 Template Generator", "15.0")]
	public static class OPCodes
	{
        /// <summary>
        /// Represents the OP code "NOP" (0x00000000)
        /// </summary>
        public static nop NOP { get; }           = new nop();         
        /// <summary>
        /// Represents the OP code "HALT" (0x00000001)
        /// </summary>
        public static halt HALT { get; }         = new halt();        
        /// <summary>
        /// Represents the OP code "JMP" (0x00000002)
        /// </summary>
        public static jmp JMP { get; }           = new jmp();         
        /// <summary>
        /// Represents the OP code "JMPREL" (0x00000003)
        /// </summary>
        public static jmprel JMPREL { get; }     = new jmprel();      
        /// <summary>
        /// Represents the OP code "ABK" (0x00000004)
        /// </summary>
        public static abk ABK { get; }           = new abk();         
        /// <summary>
        /// Represents the OP code "SYSCALL" (0x00000005)
        /// </summary>
        public static syscall SYSCALL { get; }   = new syscall();     
        /// <summary>
        /// Represents the OP code "CALL" (0x00000006)
        /// </summary>
        public static call CALL { get; }         = new call();        
        /// <summary>
        /// Represents the OP code "RET" (0x00000007)
        /// </summary>
        public static ret RET { get; }           = new ret();         
        /// <summary>
        /// Represents the OP code "COPY" (0x00000008)
        /// </summary>
        public static copy COPY { get; }         = new copy();        
        /// <summary>
        /// Represents the OP code "CLEAR" (0x00000009)
        /// </summary>
        public static clear CLEAR { get; }       = new clear();       
        /// <summary>
        /// Represents the OP code "IO" (0x0000000a)
        /// </summary>
        public static io IO { get; }             = new io();          
        /// <summary>
        /// Represents the OP code "IN" (0x0000000b)
        /// </summary>
        public static @in IN { get; }            = new @in();         
        /// <summary>
        /// Represents the OP code "OUT" (0x0000000c)
        /// </summary>
        public static @out OUT { get; }          = new @out();        
        /// <summary>
        /// Represents the OP code "CLEARFLAGS" (0x0000000d)
        /// </summary>
        public static clearflags CLEARFLAGS { get; } = new clearflags();
        /// <summary>
        /// Represents the OP code "SETFLAGS" (0x0000000e)
        /// </summary>
        public static setflags SETFLAGS { get; } = new setflags();    
        /// <summary>
        /// Represents the OP code "GETFLAGS" (0x0000000f)
        /// </summary>
        public static getflags GETFLAGS { get; } = new getflags();    
        /// <summary>
        /// Represents the OP code "MOV" (0x00000010)
        /// </summary>
        public static mov MOV { get; }           = new mov();         
        /// <summary>
        /// Represents the OP code "LEA" (0x00000011)
        /// </summary>
        public static lea LEA { get; }           = new lea();         
        /// <summary>
        /// Represents the OP code "ADD" (0x00000012)
        /// </summary>
        public static add ADD { get; }           = new add();         
        /// <summary>
        /// Represents the OP code "SUB" (0x00000013)
        /// </summary>
        public static sub SUB { get; }           = new sub();         
        /// <summary>
        /// Represents the OP code "MUL" (0x00000014)
        /// </summary>
        public static mul MUL { get; }           = new mul();         
        /// <summary>
        /// Represents the OP code "DIV" (0x00000015)
        /// </summary>
        public static div DIV { get; }           = new div();         
        /// <summary>
        /// Represents the OP code "MOD" (0x00000016)
        /// </summary>
        public static mod MOD { get; }           = new mod();         
        /// <summary>
        /// Represents the OP code "NEG" (0x00000017)
        /// </summary>
        public static neg NEG { get; }           = new neg();         
        /// <summary>
        /// Represents the OP code "NOT" (0x00000018)
        /// </summary>
        public static not NOT { get; }           = new not();         
        /// <summary>
        /// Represents the OP code "OR" (0x00000019)
        /// </summary>
        public static or OR { get; }             = new or();          
        /// <summary>
        /// Represents the OP code "AND" (0x0000001a)
        /// </summary>
        public static and AND { get; }           = new and();         
        /// <summary>
        /// Represents the OP code "XOR" (0x0000001b)
        /// </summary>
        public static xor XOR { get; }           = new xor();         
        /// <summary>
        /// Represents the OP code "NOR" (0x0000001c)
        /// </summary>
        public static nor NOR { get; }           = new nor();         
        /// <summary>
        /// Represents the OP code "NAND" (0x0000001d)
        /// </summary>
        public static nand NAND { get; }         = new nand();        
        /// <summary>
        /// Represents the OP code "NXOR" (0x0000001e)
        /// </summary>
        public static nxor NXOR { get; }         = new nxor();        
        /// <summary>
        /// Represents the OP code "ABS" (0x0000001f)
        /// </summary>
        public static abs ABS { get; }           = new abs();         
        /// <summary>
        /// Represents the OP code "BOOL" (0x00000020)
        /// </summary>
        public static @bool BOOL { get; }        = new @bool();       
        /// <summary>
        /// Represents the OP code "POW" (0x00000021)
        /// </summary>
        public static pow POW { get; }           = new pow();         
        /// <summary>
        /// Represents the OP code "SHR" (0x00000022)
        /// </summary>
        public static shr SHR { get; }           = new shr();         
        /// <summary>
        /// Represents the OP code "SHL" (0x00000023)
        /// </summary>
        public static shl SHL { get; }           = new shl();         
        /// <summary>
        /// Represents the OP code "ROR" (0x00000024)
        /// </summary>
        public static ror ROR { get; }           = new ror();         
        /// <summary>
        /// Represents the OP code "ROL" (0x00000025)
        /// </summary>
        public static rol ROL { get; }           = new rol();         
        /// <summary>
        /// Represents the OP code "CMP" (0x00000026)
        /// </summary>
        public static cmp CMP { get; }           = new cmp();         
        /// <summary>
        /// Represents the OP code "JLE" (0x00000027)
        /// </summary>
        public static jle JLE { get; }           = new jle();         
        /// <summary>
        /// Represents the OP code "JL" (0x00000028)
        /// </summary>
        public static jl JL { get; }             = new jl();          
        /// <summary>
        /// Represents the OP code "JGE" (0x00000029)
        /// </summary>
        public static jge JGE { get; }           = new jge();         
        /// <summary>
        /// Represents the OP code "JG" (0x0000002a)
        /// </summary>
        public static jg JG { get; }             = new jg();          
        /// <summary>
        /// Represents the OP code "JE" (0x0000002b)
        /// </summary>
        public static je JE { get; }             = new je();          
        /// <summary>
        /// Represents the OP code "JNE" (0x0000002c)
        /// </summary>
        public static jne JNE { get; }           = new jne();         
        /// <summary>
        /// Represents the OP code "JZ" (0x0000002d)
        /// </summary>
        public static jz JZ { get; }             = new jz();          
        /// <summary>
        /// Represents the OP code "JNZ" (0x0000002e)
        /// </summary>
        public static jnz JNZ { get; }           = new jnz();         
        /// <summary>
        /// Represents the OP code "JNEG" (0x0000002f)
        /// </summary>
        public static jneg JNEG { get; }         = new jneg();        
        /// <summary>
        /// Represents the OP code "JPOS" (0x00000030)
        /// </summary>
        public static jpos JPOS { get; }         = new jpos();        
        /// <summary>
        /// Represents the OP code "FAC" (0x00000031)
        /// </summary>
        public static fac FAC { get; }           = new fac();         
        /// <summary>
        /// Represents the OP code "INCR" (0x00000032)
        /// </summary>
        public static incr INCR { get; }         = new incr();        
        /// <summary>
        /// Represents the OP code "DECR" (0x00000033)
        /// </summary>
        public static decr DECR { get; }         = new decr();        
        /// <summary>
        /// Represents the OP code "SWAP" (0x0000003d)
        /// </summary>
        public static swap SWAP { get; }         = new swap();        
        /// <summary>
        /// Represents the OP code "CPUID" (0x0000003e)
        /// </summary>
        public static cpuid CPUID { get; }       = new cpuid();       
        /// <summary>
        /// Represents the OP code "WAIT" (0x0000003f)
        /// </summary>
        public static wait WAIT { get; }         = new wait();        
        /// <summary>
        /// Represents the OP code "PUSH" (0x00000040)
        /// </summary>
        public static push PUSH { get; }         = new push();        
        /// <summary>
        /// Represents the OP code "POP" (0x00000041)
        /// </summary>
        public static pop POP { get; }           = new pop();         
        /// <summary>
        /// Represents the OP code "PUSHF" (0x00000042)
        /// </summary>
        public static pushf PUSHF { get; }       = new pushf();       
        /// <summary>
        /// Represents the OP code "POPF" (0x00000043)
        /// </summary>
        public static popf POPF { get; }         = new popf();        
        /// <summary>
        /// Represents the OP code "PUSHI" (0x00000044)
        /// </summary>
        public static pushi PUSHI { get; }       = new pushi();       
        /// <summary>
        /// Represents the OP code "POPI" (0x00000045)
        /// </summary>
        public static popi POPI { get; }         = new popi();        
        /// <summary>
        /// Represents the OP code "KERNEL" (0x0000ffff)
        /// </summary>
        public static kernel KERNEL { get; }     = new kernel();      


        /// <summary>
        /// A collection of all opcodes mapped to their code number
        /// </summary>
        public static Dictionary<ushort, OPCode> Codes { get; } = new Dictionary<ushort, OPCode>() {
            { (ushort)0, NOP },   // [mcpu.corelib] MCPU.Instructions.nop
            { (ushort)1, HALT },   // [mcpu.corelib] MCPU.Instructions.halt
            { (ushort)2, JMP },   // [mcpu.corelib] MCPU.Instructions.jmp
            { (ushort)3, JMPREL },   // [mcpu.corelib] MCPU.Instructions.jmprel
            { (ushort)4, ABK },   // [mcpu.corelib] MCPU.Instructions.abk
            { (ushort)5, SYSCALL },   // [mcpu.corelib] MCPU.Instructions.syscall
            { (ushort)6, CALL },   // [mcpu.corelib] MCPU.Instructions.call
            { (ushort)7, RET },   // [mcpu.corelib] MCPU.Instructions.ret
            { (ushort)8, COPY },   // [mcpu.corelib] MCPU.Instructions.copy
            { (ushort)9, CLEAR },   // [mcpu.corelib] MCPU.Instructions.clear
            { (ushort)10, IO },   // [mcpu.corelib] MCPU.Instructions.io
            { (ushort)11, IN },   // [mcpu.corelib] MCPU.Instructions.in
            { (ushort)12, OUT },   // [mcpu.corelib] MCPU.Instructions.out
            { (ushort)13, CLEARFLAGS },   // [mcpu.corelib] MCPU.Instructions.clearflags
            { (ushort)14, SETFLAGS },   // [mcpu.corelib] MCPU.Instructions.setflags
            { (ushort)15, GETFLAGS },   // [mcpu.corelib] MCPU.Instructions.getflags
            { (ushort)16, MOV },   // [mcpu.corelib] MCPU.Instructions.mov
            { (ushort)17, LEA },   // [mcpu.corelib] MCPU.Instructions.lea
            { (ushort)18, ADD },   // [mcpu.corelib] MCPU.Instructions.add
            { (ushort)19, SUB },   // [mcpu.corelib] MCPU.Instructions.sub
            { (ushort)20, MUL },   // [mcpu.corelib] MCPU.Instructions.mul
            { (ushort)21, DIV },   // [mcpu.corelib] MCPU.Instructions.div
            { (ushort)22, MOD },   // [mcpu.corelib] MCPU.Instructions.mod
            { (ushort)23, NEG },   // [mcpu.corelib] MCPU.Instructions.neg
            { (ushort)24, NOT },   // [mcpu.corelib] MCPU.Instructions.not
            { (ushort)25, OR },   // [mcpu.corelib] MCPU.Instructions.or
            { (ushort)26, AND },   // [mcpu.corelib] MCPU.Instructions.and
            { (ushort)27, XOR },   // [mcpu.corelib] MCPU.Instructions.xor
            { (ushort)28, NOR },   // [mcpu.corelib] MCPU.Instructions.nor
            { (ushort)29, NAND },   // [mcpu.corelib] MCPU.Instructions.nand
            { (ushort)30, NXOR },   // [mcpu.corelib] MCPU.Instructions.nxor
            { (ushort)31, ABS },   // [mcpu.corelib] MCPU.Instructions.abs
            { (ushort)32, BOOL },   // [mcpu.corelib] MCPU.Instructions.bool
            { (ushort)33, POW },   // [mcpu.corelib] MCPU.Instructions.pow
            { (ushort)34, SHR },   // [mcpu.corelib] MCPU.Instructions.shr
            { (ushort)35, SHL },   // [mcpu.corelib] MCPU.Instructions.shl
            { (ushort)36, ROR },   // [mcpu.corelib] MCPU.Instructions.ror
            { (ushort)37, ROL },   // [mcpu.corelib] MCPU.Instructions.rol
            { (ushort)38, CMP },   // [mcpu.corelib] MCPU.Instructions.cmp
            { (ushort)39, JLE },   // [mcpu.corelib] MCPU.Instructions.jle
            { (ushort)40, JL },   // [mcpu.corelib] MCPU.Instructions.jl
            { (ushort)41, JGE },   // [mcpu.corelib] MCPU.Instructions.jge
            { (ushort)42, JG },   // [mcpu.corelib] MCPU.Instructions.jg
            { (ushort)43, JE },   // [mcpu.corelib] MCPU.Instructions.je
            { (ushort)44, JNE },   // [mcpu.corelib] MCPU.Instructions.jne
            { (ushort)45, JZ },   // [mcpu.corelib] MCPU.Instructions.jz
            { (ushort)46, JNZ },   // [mcpu.corelib] MCPU.Instructions.jnz
            { (ushort)47, JNEG },   // [mcpu.corelib] MCPU.Instructions.jneg
            { (ushort)48, JPOS },   // [mcpu.corelib] MCPU.Instructions.jpos
            { (ushort)49, FAC },   // [mcpu.corelib] MCPU.Instructions.fac
            { (ushort)50, INCR },   // [mcpu.corelib] MCPU.Instructions.incr
            { (ushort)51, DECR },   // [mcpu.corelib] MCPU.Instructions.decr
            { (ushort)61, SWAP },   // [mcpu.corelib] MCPU.Instructions.swap
            { (ushort)62, CPUID },   // [mcpu.corelib] MCPU.Instructions.cpuid
            { (ushort)63, WAIT },   // [mcpu.corelib] MCPU.Instructions.wait
            { (ushort)64, PUSH },   // [mcpu.corelib] MCPU.Instructions.push
            { (ushort)65, POP },   // [mcpu.corelib] MCPU.Instructions.pop
            { (ushort)66, PUSHF },   // [mcpu.corelib] MCPU.Instructions.pushf
            { (ushort)67, POPF },   // [mcpu.corelib] MCPU.Instructions.popf
            { (ushort)68, PUSHI },   // [mcpu.corelib] MCPU.Instructions.pushi
            { (ushort)69, POPI },   // [mcpu.corelib] MCPU.Instructions.popi
            { (ushort)65535, KERNEL },   // [mcpu.corelib] MCPU.Instructions.kernel
        };
	}
}
