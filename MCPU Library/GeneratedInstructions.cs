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
        /// Represents the OP code "FAC" (0x00000026)
        /// </summary>
        public static fac FAC { get; }           = new fac();         
        /// <summary>
        /// Represents the OP code "INCR" (0x00000027)
        /// </summary>
        public static incr INCR { get; }         = new incr();        
        /// <summary>
        /// Represents the OP code "DECR" (0x00000028)
        /// </summary>
        public static decr DECR { get; }         = new decr();        
        /// <summary>
        /// Represents the OP code "CMP" (0x00000029)
        /// </summary>
        public static cmp CMP { get; }           = new cmp();         
        /// <summary>
        /// Represents the OP code "JLE" (0x0000002a)
        /// </summary>
        public static jle JLE { get; }           = new jle();         
        /// <summary>
        /// Represents the OP code "JL" (0x0000002b)
        /// </summary>
        public static jl JL { get; }             = new jl();          
        /// <summary>
        /// Represents the OP code "JGE" (0x0000002c)
        /// </summary>
        public static jge JGE { get; }           = new jge();         
        /// <summary>
        /// Represents the OP code "JG" (0x0000002d)
        /// </summary>
        public static jg JG { get; }             = new jg();          
        /// <summary>
        /// Represents the OP code "JE" (0x0000002e)
        /// </summary>
        public static je JE { get; }             = new je();          
        /// <summary>
        /// Represents the OP code "JNE" (0x0000002f)
        /// </summary>
        public static jne JNE { get; }           = new jne();         
        /// <summary>
        /// Represents the OP code "JZ" (0x00000030)
        /// </summary>
        public static jz JZ { get; }             = new jz();          
        /// <summary>
        /// Represents the OP code "JNZ" (0x00000031)
        /// </summary>
        public static jnz JNZ { get; }           = new jnz();         
        /// <summary>
        /// Represents the OP code "JNEG" (0x00000032)
        /// </summary>
        public static jneg JNEG { get; }         = new jneg();        
        /// <summary>
        /// Represents the OP code "JPOS" (0x00000033)
        /// </summary>
        public static jpos JPOS { get; }         = new jpos();        
        /// <summary>
        /// Represents the OP code "SWAP" (0x0000003c)
        /// </summary>
        public static swap SWAP { get; }         = new swap();        
        /// <summary>
        /// Represents the OP code "CPUID" (0x0000003d)
        /// </summary>
        public static cpuid CPUID { get; }       = new cpuid();       
        /// <summary>
        /// Represents the OP code "WAIT" (0x0000003e)
        /// </summary>
        public static wait WAIT { get; }         = new wait();        
        /// <summary>
        /// Represents the OP code "RESET" (0x0000003f)
        /// </summary>
        public static reset RESET { get; }       = new reset();       
        /// <summary>
        /// Represents the OP code "PUSH" (0x00000040)
        /// </summary>
        public static push PUSH { get; }         = new push();        
        /// <summary>
        /// Represents the OP code "POP" (0x00000041)
        /// </summary>
        public static pop POP { get; }           = new pop();         
        /// <summary>
        /// Represents the OP code "PEEK" (0x00000042)
        /// </summary>
        public static peek PEEK { get; }         = new peek();        
        /// <summary>
        /// Represents the OP code "SSWAP" (0x00000043)
        /// </summary>
        public static sswap SSWAP { get; }       = new sswap();       
        /// <summary>
        /// Represents the OP code "PUSHF" (0x00000044)
        /// </summary>
        public static pushf PUSHF { get; }       = new pushf();       
        /// <summary>
        /// Represents the OP code "POPF" (0x00000045)
        /// </summary>
        public static popf POPF { get; }         = new popf();        
        /// <summary>
        /// Represents the OP code "PEEKF" (0x00000046)
        /// </summary>
        public static peekf PEEKF { get; }       = new peekf();       
        /// <summary>
        /// Represents the OP code "PUSHI" (0x00000047)
        /// </summary>
        public static pushi PUSHI { get; }       = new pushi();       
        /// <summary>
        /// Represents the OP code "POPI" (0x00000048)
        /// </summary>
        public static popi POPI { get; }         = new popi();        
        /// <summary>
        /// Represents the OP code "PEEKI" (0x00000049)
        /// </summary>
        public static peeki PEEKI { get; }       = new peeki();       
        /// <summary>
        /// Represents the OP code "FICAST" (0x00000050)
        /// </summary>
        public static ficast FICAST { get; }     = new ficast();      
        /// <summary>
        /// Represents the OP code "IFCAST" (0x00000051)
        /// </summary>
        public static ifcast IFCAST { get; }     = new ifcast();      
        /// <summary>
        /// Represents the OP code "FADD" (0x00000052)
        /// </summary>
        public static fadd FADD { get; }         = new fadd();        
        /// <summary>
        /// Represents the OP code "FSUB" (0x00000053)
        /// </summary>
        public static fsub FSUB { get; }         = new fsub();        
        /// <summary>
        /// Represents the OP code "FMUL" (0x00000054)
        /// </summary>
        public static fmul FMUL { get; }         = new fmul();        
        /// <summary>
        /// Represents the OP code "FDIV" (0x00000055)
        /// </summary>
        public static fdiv FDIV { get; }         = new fdiv();        
        /// <summary>
        /// Represents the OP code "FMOD" (0x00000056)
        /// </summary>
        public static fmod FMOD { get; }         = new fmod();        
        /// <summary>
        /// Represents the OP code "FNEG" (0x00000057)
        /// </summary>
        public static fneg FNEG { get; }         = new fneg();        
        /// <summary>
        /// Represents the OP code "FINV" (0x00000058)
        /// </summary>
        public static finv FINV { get; }         = new finv();        
        /// <summary>
        /// Represents the OP code "FSQRT" (0x00000059)
        /// </summary>
        public static fsqrt FSQRT { get; }       = new fsqrt();       
        /// <summary>
        /// Represents the OP code "FROOT" (0x0000005a)
        /// </summary>
        public static froot FROOT { get; }       = new froot();       
        /// <summary>
        /// Represents the OP code "FLOG" (0x0000005b)
        /// </summary>
        public static flog FLOG { get; }         = new flog();        
        /// <summary>
        /// Represents the OP code "FLOGE" (0x0000005c)
        /// </summary>
        public static floge FLOGE { get; }       = new floge();       
        /// <summary>
        /// Represents the OP code "FEXP" (0x0000005d)
        /// </summary>
        public static fexp FEXP { get; }         = new fexp();        
        /// <summary>
        /// Represents the OP code "FPOW" (0x0000005e)
        /// </summary>
        public static fpow FPOW { get; }         = new fpow();        
        /// <summary>
        /// Represents the OP code "FFLOOR" (0x0000005f)
        /// </summary>
        public static ffloor FFLOOR { get; }     = new ffloor();      
        /// <summary>
        /// Represents the OP code "FCEIL" (0x00000060)
        /// </summary>
        public static fceil FCEIL { get; }       = new fceil();       
        /// <summary>
        /// Represents the OP code "FROUND" (0x00000061)
        /// </summary>
        public static fround FROUND { get; }     = new fround();      
        /// <summary>
        /// Represents the OP code "FMIN" (0x00000062)
        /// </summary>
        public static fmin FMIN { get; }         = new fmin();        
        /// <summary>
        /// Represents the OP code "FMAX" (0x00000063)
        /// </summary>
        public static fmax FMAX { get; }         = new fmax();        
        /// <summary>
        /// Represents the OP code "FSIGN" (0x00000064)
        /// </summary>
        public static fsign FSIGN { get; }       = new fsign();       
        /// <summary>
        /// Represents the OP code "FSIN" (0x00000065)
        /// </summary>
        public static fsin FSIN { get; }         = new fsin();        
        /// <summary>
        /// Represents the OP code "FCOS" (0x00000066)
        /// </summary>
        public static fcos FCOS { get; }         = new fcos();        
        /// <summary>
        /// Represents the OP code "FTAN" (0x00000067)
        /// </summary>
        public static ftan FTAN { get; }         = new ftan();        
        /// <summary>
        /// Represents the OP code "FSINH" (0x00000068)
        /// </summary>
        public static fsinh FSINH { get; }       = new fsinh();       
        /// <summary>
        /// Represents the OP code "FCOSH" (0x00000069)
        /// </summary>
        public static fcosh FCOSH { get; }       = new fcosh();       
        /// <summary>
        /// Represents the OP code "FTANH" (0x0000006a)
        /// </summary>
        public static ftanh FTANH { get; }       = new ftanh();       
        /// <summary>
        /// Represents the OP code "FASIN" (0x0000006b)
        /// </summary>
        public static fasin FASIN { get; }       = new fasin();       
        /// <summary>
        /// Represents the OP code "FACOS" (0x0000006c)
        /// </summary>
        public static facos FACOS { get; }       = new facos();       
        /// <summary>
        /// Represents the OP code "FATAN" (0x0000006d)
        /// </summary>
        public static fatan FATAN { get; }       = new fatan();       
        /// <summary>
        /// Represents the OP code "FATAN2" (0x0000006e)
        /// </summary>
        public static fatan2 FATAN2 { get; }     = new fatan2();      
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
            { (ushort)38, FAC },   // [mcpu.corelib] MCPU.Instructions.fac
            { (ushort)39, INCR },   // [mcpu.corelib] MCPU.Instructions.incr
            { (ushort)40, DECR },   // [mcpu.corelib] MCPU.Instructions.decr
            { (ushort)41, CMP },   // [mcpu.corelib] MCPU.Instructions.cmp
            { (ushort)42, JLE },   // [mcpu.corelib] MCPU.Instructions.jle
            { (ushort)43, JL },   // [mcpu.corelib] MCPU.Instructions.jl
            { (ushort)44, JGE },   // [mcpu.corelib] MCPU.Instructions.jge
            { (ushort)45, JG },   // [mcpu.corelib] MCPU.Instructions.jg
            { (ushort)46, JE },   // [mcpu.corelib] MCPU.Instructions.je
            { (ushort)47, JNE },   // [mcpu.corelib] MCPU.Instructions.jne
            { (ushort)48, JZ },   // [mcpu.corelib] MCPU.Instructions.jz
            { (ushort)49, JNZ },   // [mcpu.corelib] MCPU.Instructions.jnz
            { (ushort)50, JNEG },   // [mcpu.corelib] MCPU.Instructions.jneg
            { (ushort)51, JPOS },   // [mcpu.corelib] MCPU.Instructions.jpos
            { (ushort)60, SWAP },   // [mcpu.corelib] MCPU.Instructions.swap
            { (ushort)61, CPUID },   // [mcpu.corelib] MCPU.Instructions.cpuid
            { (ushort)62, WAIT },   // [mcpu.corelib] MCPU.Instructions.wait
            { (ushort)63, RESET },   // [mcpu.corelib] MCPU.Instructions.reset
            { (ushort)64, PUSH },   // [mcpu.corelib] MCPU.Instructions.push
            { (ushort)65, POP },   // [mcpu.corelib] MCPU.Instructions.pop
            { (ushort)66, PEEK },   // [mcpu.corelib] MCPU.Instructions.peek
            { (ushort)67, SSWAP },   // [mcpu.corelib] MCPU.Instructions.sswap
            { (ushort)68, PUSHF },   // [mcpu.corelib] MCPU.Instructions.pushf
            { (ushort)69, POPF },   // [mcpu.corelib] MCPU.Instructions.popf
            { (ushort)70, PEEKF },   // [mcpu.corelib] MCPU.Instructions.peekf
            { (ushort)71, PUSHI },   // [mcpu.corelib] MCPU.Instructions.pushi
            { (ushort)72, POPI },   // [mcpu.corelib] MCPU.Instructions.popi
            { (ushort)73, PEEKI },   // [mcpu.corelib] MCPU.Instructions.peeki
            { (ushort)80, FICAST },   // [mcpu.corelib] MCPU.Instructions.ficast
            { (ushort)81, IFCAST },   // [mcpu.corelib] MCPU.Instructions.ifcast
            { (ushort)82, FADD },   // [mcpu.corelib] MCPU.Instructions.fadd
            { (ushort)83, FSUB },   // [mcpu.corelib] MCPU.Instructions.fsub
            { (ushort)84, FMUL },   // [mcpu.corelib] MCPU.Instructions.fmul
            { (ushort)85, FDIV },   // [mcpu.corelib] MCPU.Instructions.fdiv
            { (ushort)86, FMOD },   // [mcpu.corelib] MCPU.Instructions.fmod
            { (ushort)87, FNEG },   // [mcpu.corelib] MCPU.Instructions.fneg
            { (ushort)88, FINV },   // [mcpu.corelib] MCPU.Instructions.finv
            { (ushort)89, FSQRT },   // [mcpu.corelib] MCPU.Instructions.fsqrt
            { (ushort)90, FROOT },   // [mcpu.corelib] MCPU.Instructions.froot
            { (ushort)91, FLOG },   // [mcpu.corelib] MCPU.Instructions.flog
            { (ushort)92, FLOGE },   // [mcpu.corelib] MCPU.Instructions.floge
            { (ushort)93, FEXP },   // [mcpu.corelib] MCPU.Instructions.fexp
            { (ushort)94, FPOW },   // [mcpu.corelib] MCPU.Instructions.fpow
            { (ushort)95, FFLOOR },   // [mcpu.corelib] MCPU.Instructions.ffloor
            { (ushort)96, FCEIL },   // [mcpu.corelib] MCPU.Instructions.fceil
            { (ushort)97, FROUND },   // [mcpu.corelib] MCPU.Instructions.fround
            { (ushort)98, FMIN },   // [mcpu.corelib] MCPU.Instructions.fmin
            { (ushort)99, FMAX },   // [mcpu.corelib] MCPU.Instructions.fmax
            { (ushort)100, FSIGN },   // [mcpu.corelib] MCPU.Instructions.fsign
            { (ushort)101, FSIN },   // [mcpu.corelib] MCPU.Instructions.fsin
            { (ushort)102, FCOS },   // [mcpu.corelib] MCPU.Instructions.fcos
            { (ushort)103, FTAN },   // [mcpu.corelib] MCPU.Instructions.ftan
            { (ushort)104, FSINH },   // [mcpu.corelib] MCPU.Instructions.fsinh
            { (ushort)105, FCOSH },   // [mcpu.corelib] MCPU.Instructions.fcosh
            { (ushort)106, FTANH },   // [mcpu.corelib] MCPU.Instructions.ftanh
            { (ushort)107, FASIN },   // [mcpu.corelib] MCPU.Instructions.fasin
            { (ushort)108, FACOS },   // [mcpu.corelib] MCPU.Instructions.facos
            { (ushort)109, FATAN },   // [mcpu.corelib] MCPU.Instructions.fatan
            { (ushort)110, FATAN2 },   // [mcpu.corelib] MCPU.Instructions.fatan2
            { (ushort)65535, KERNEL },   // [mcpu.corelib] MCPU.Instructions.kernel
        };
	}
}
