﻿using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;

// Autogenerated  2017-02-21 08:08:16:881217   (UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna

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
        public static nop NOP { get; }                       = new nop();
        /// <summary>
        /// Represents the OP code "HALT" (0x00000001)
        /// </summary>
        public static halt HALT { get; }                     = new halt();
        /// <summary>
        /// Represents the OP code "JMP" (0x00000002)
        /// </summary>
        public static jmp JMP { get; }                       = new jmp();
        /// <summary>
        /// Represents the OP code "JMPREL" (0x00000003)
        /// </summary>
        public static jmprel JMPREL { get; }                 = new jmprel();
        /// <summary>
        /// Represents the OP code "ABK" (0x00000004)
        /// </summary>
        public static abk ABK { get; }                       = new abk();
        /// <summary>
        /// Represents the OP code "SYSCALL" (0x00000005)
        /// </summary>
        public static syscall SYSCALL { get; }               = new syscall();
        /// <summary>
        /// Represents the OP code "CALL" (0x00000006)
        /// </summary>
        public static call CALL { get; }                     = new call();
        /// <summary>
        /// Represents the OP code "RET" (0x00000007)
        /// </summary>
        public static ret RET { get; }                       = new ret();
        /// <summary>
        /// Represents the OP code "COPY" (0x00000008)
        /// </summary>
        public static copy COPY { get; }                     = new copy();
        /// <summary>
        /// Represents the OP code "CLEAR" (0x00000009)
        /// </summary>
        public static clear CLEAR { get; }                   = new clear();
        /// <summary>
        /// Represents the OP code "IO" (0x0000000a)
        /// </summary>
        public static io IO { get; }                         = new io();
        /// <summary>
        /// Represents the OP code "IN" (0x0000000b)
        /// </summary>
        public static @in IN { get; }                        = new @in();
        /// <summary>
        /// Represents the OP code "OUT" (0x0000000c)
        /// </summary>
        public static @out OUT { get; }                      = new @out();
        /// <summary>
        /// Represents the OP code "CLEARFLAGS" (0x0000000d)
        /// </summary>
        public static clearflags CLEARFLAGS { get; }         = new clearflags();
        /// <summary>
        /// Represents the OP code "SETFLAGS" (0x0000000e)
        /// </summary>
        public static setflags SETFLAGS { get; }             = new setflags();
        /// <summary>
        /// Represents the OP code "GETFLAGS" (0x0000000f)
        /// </summary>
        public static getflags GETFLAGS { get; }             = new getflags();
        /// <summary>
        /// Represents the OP code "MOV" (0x00000010)
        /// </summary>
        public static mov MOV { get; }                       = new mov();
        /// <summary>
        /// Represents the OP code "LEA" (0x00000011)
        /// </summary>
        public static lea LEA { get; }                       = new lea();
        /// <summary>
        /// Represents the OP code "ADD" (0x00000012)
        /// </summary>
        public static add ADD { get; }                       = new add();
        /// <summary>
        /// Represents the OP code "SUB" (0x00000013)
        /// </summary>
        public static sub SUB { get; }                       = new sub();
        /// <summary>
        /// Represents the OP code "MUL" (0x00000014)
        /// </summary>
        public static mul MUL { get; }                       = new mul();
        /// <summary>
        /// Represents the OP code "DIV" (0x00000015)
        /// </summary>
        public static div DIV { get; }                       = new div();
        /// <summary>
        /// Represents the OP code "MOD" (0x00000016)
        /// </summary>
        public static mod MOD { get; }                       = new mod();
        /// <summary>
        /// Represents the OP code "NEG" (0x00000017)
        /// </summary>
        public static neg NEG { get; }                       = new neg();
        /// <summary>
        /// Represents the OP code "NOT" (0x00000018)
        /// </summary>
        public static not NOT { get; }                       = new not();
        /// <summary>
        /// Represents the OP code "OR" (0x00000019)
        /// </summary>
        public static or OR { get; }                         = new or();
        /// <summary>
        /// Represents the OP code "AND" (0x0000001a)
        /// </summary>
        public static and AND { get; }                       = new and();
        /// <summary>
        /// Represents the OP code "XOR" (0x0000001b)
        /// </summary>
        public static xor XOR { get; }                       = new xor();
        /// <summary>
        /// Represents the OP code "NOR" (0x0000001c)
        /// </summary>
        public static nor NOR { get; }                       = new nor();
        /// <summary>
        /// Represents the OP code "NAND" (0x0000001d)
        /// </summary>
        public static nand NAND { get; }                     = new nand();
        /// <summary>
        /// Represents the OP code "NXOR" (0x0000001e)
        /// </summary>
        public static nxor NXOR { get; }                     = new nxor();
        /// <summary>
        /// Represents the OP code "ABS" (0x0000001f)
        /// </summary>
        public static abs ABS { get; }                       = new abs();
        /// <summary>
        /// Represents the OP code "BOOL" (0x00000020)
        /// </summary>
        public static @bool BOOL { get; }                    = new @bool();
        /// <summary>
        /// Represents the OP code "POW" (0x00000021)
        /// </summary>
        public static pow POW { get; }                       = new pow();
        /// <summary>
        /// Represents the OP code "SHR" (0x00000022)
        /// </summary>
        public static shr SHR { get; }                       = new shr();
        /// <summary>
        /// Represents the OP code "SHL" (0x00000023)
        /// </summary>
        public static shl SHL { get; }                       = new shl();
        /// <summary>
        /// Represents the OP code "ROR" (0x00000024)
        /// </summary>
        public static ror ROR { get; }                       = new ror();
        /// <summary>
        /// Represents the OP code "ROL" (0x00000025)
        /// </summary>
        public static rol ROL { get; }                       = new rol();
        /// <summary>
        /// Represents the OP code "FAC" (0x00000026)
        /// </summary>
        public static fac FAC { get; }                       = new fac();
        /// <summary>
        /// Represents the OP code "INCR" (0x00000027)
        /// </summary>
        public static incr INCR { get; }                     = new incr();
        /// <summary>
        /// Represents the OP code "DECR" (0x00000028)
        /// </summary>
        public static decr DECR { get; }                     = new decr();
        /// <summary>
        /// Represents the OP code "CMP" (0x00000029)
        /// </summary>
        public static cmp CMP { get; }                       = new cmp();
        /// <summary>
        /// Represents the OP code "JLE" (0x0000002a)
        /// </summary>
        public static jle JLE { get; }                       = new jle();
        /// <summary>
        /// Represents the OP code "JL" (0x0000002b)
        /// </summary>
        public static jl JL { get; }                         = new jl();
        /// <summary>
        /// Represents the OP code "JGE" (0x0000002c)
        /// </summary>
        public static jge JGE { get; }                       = new jge();
        /// <summary>
        /// Represents the OP code "JG" (0x0000002d)
        /// </summary>
        public static jg JG { get; }                         = new jg();
        /// <summary>
        /// Represents the OP code "JE" (0x0000002e)
        /// </summary>
        public static je JE { get; }                         = new je();
        /// <summary>
        /// Represents the OP code "JNE" (0x0000002f)
        /// </summary>
        public static jne JNE { get; }                       = new jne();
        /// <summary>
        /// Represents the OP code "JZ" (0x00000030)
        /// </summary>
        public static jz JZ { get; }                         = new jz();
        /// <summary>
        /// Represents the OP code "JNZ" (0x00000031)
        /// </summary>
        public static jnz JNZ { get; }                       = new jnz();
        /// <summary>
        /// Represents the OP code "JNEG" (0x00000032)
        /// </summary>
        public static jneg JNEG { get; }                     = new jneg();
        /// <summary>
        /// Represents the OP code "JPOS" (0x00000033)
        /// </summary>
        public static jpos JPOS { get; }                     = new jpos();
        /// <summary>
        /// Represents the OP code "INT" (0x0000003b)
        /// </summary>
        public static @int INT { get; }                      = new @int();
        /// <summary>
        /// Represents the OP code "SWAP" (0x0000003c)
        /// </summary>
        public static swap SWAP { get; }                     = new swap();
        /// <summary>
        /// Represents the OP code "CPUID" (0x0000003d)
        /// </summary>
        public static cpuid CPUID { get; }                   = new cpuid();
        /// <summary>
        /// Represents the OP code "WAIT" (0x0000003e)
        /// </summary>
        public static wait WAIT { get; }                     = new wait();
        /// <summary>
        /// Represents the OP code "RESET" (0x0000003f)
        /// </summary>
        public static reset RESET { get; }                   = new reset();
        /// <summary>
        /// Represents the OP code "PUSH" (0x00000040)
        /// </summary>
        public static push PUSH { get; }                     = new push();
        /// <summary>
        /// Represents the OP code "POP" (0x00000041)
        /// </summary>
        public static pop POP { get; }                       = new pop();
        /// <summary>
        /// Represents the OP code "PEEK" (0x00000042)
        /// </summary>
        public static peek PEEK { get; }                     = new peek();
        /// <summary>
        /// Represents the OP code "SSWAP" (0x00000043)
        /// </summary>
        public static sswap SSWAP { get; }                   = new sswap();
        /// <summary>
        /// Represents the OP code "PUSHF" (0x00000044)
        /// </summary>
        public static pushf PUSHF { get; }                   = new pushf();
        /// <summary>
        /// Represents the OP code "POPF" (0x00000045)
        /// </summary>
        public static popf POPF { get; }                     = new popf();
        /// <summary>
        /// Represents the OP code "PEEKF" (0x00000046)
        /// </summary>
        public static peekf PEEKF { get; }                   = new peekf();
        /// <summary>
        /// Represents the OP code "PUSHI" (0x00000047)
        /// </summary>
        public static pushi PUSHI { get; }                   = new pushi();
        /// <summary>
        /// Represents the OP code "POPI" (0x00000048)
        /// </summary>
        public static popi POPI { get; }                     = new popi();
        /// <summary>
        /// Represents the OP code "PEEKI" (0x00000049)
        /// </summary>
        public static peeki PEEKI { get; }                   = new peeki();
        /// <summary>
        /// Represents the OP code "FICAST" (0x00000050)
        /// </summary>
        public static ficast FICAST { get; }                 = new ficast();
        /// <summary>
        /// Represents the OP code "IFCAST" (0x00000051)
        /// </summary>
        public static ifcast IFCAST { get; }                 = new ifcast();
        /// <summary>
        /// Represents the OP code "FADD" (0x00000052)
        /// </summary>
        public static fadd FADD { get; }                     = new fadd();
        /// <summary>
        /// Represents the OP code "FSUB" (0x00000053)
        /// </summary>
        public static fsub FSUB { get; }                     = new fsub();
        /// <summary>
        /// Represents the OP code "FMUL" (0x00000054)
        /// </summary>
        public static fmul FMUL { get; }                     = new fmul();
        /// <summary>
        /// Represents the OP code "FDIV" (0x00000055)
        /// </summary>
        public static fdiv FDIV { get; }                     = new fdiv();
        /// <summary>
        /// Represents the OP code "FMOD" (0x00000056)
        /// </summary>
        public static fmod FMOD { get; }                     = new fmod();
        /// <summary>
        /// Represents the OP code "FNEG" (0x00000057)
        /// </summary>
        public static fneg FNEG { get; }                     = new fneg();
        /// <summary>
        /// Represents the OP code "FINV" (0x00000058)
        /// </summary>
        public static finv FINV { get; }                     = new finv();
        /// <summary>
        /// Represents the OP code "FSQRT" (0x00000059)
        /// </summary>
        public static fsqrt FSQRT { get; }                   = new fsqrt();
        /// <summary>
        /// Represents the OP code "FROOT" (0x0000005a)
        /// </summary>
        public static froot FROOT { get; }                   = new froot();
        /// <summary>
        /// Represents the OP code "FLOG" (0x0000005b)
        /// </summary>
        public static flog FLOG { get; }                     = new flog();
        /// <summary>
        /// Represents the OP code "FLOGE" (0x0000005c)
        /// </summary>
        public static floge FLOGE { get; }                   = new floge();
        /// <summary>
        /// Represents the OP code "FEXP" (0x0000005d)
        /// </summary>
        public static fexp FEXP { get; }                     = new fexp();
        /// <summary>
        /// Represents the OP code "FPOW" (0x0000005e)
        /// </summary>
        public static fpow FPOW { get; }                     = new fpow();
        /// <summary>
        /// Represents the OP code "FFLOOR" (0x0000005f)
        /// </summary>
        public static ffloor FFLOOR { get; }                 = new ffloor();
        /// <summary>
        /// Represents the OP code "FCEIL" (0x00000060)
        /// </summary>
        public static fceil FCEIL { get; }                   = new fceil();
        /// <summary>
        /// Represents the OP code "FROUND" (0x00000061)
        /// </summary>
        public static fround FROUND { get; }                 = new fround();
        /// <summary>
        /// Represents the OP code "FMIN" (0x00000062)
        /// </summary>
        public static fmin FMIN { get; }                     = new fmin();
        /// <summary>
        /// Represents the OP code "FMAX" (0x00000063)
        /// </summary>
        public static fmax FMAX { get; }                     = new fmax();
        /// <summary>
        /// Represents the OP code "FSIGN" (0x00000064)
        /// </summary>
        public static fsign FSIGN { get; }                   = new fsign();
        /// <summary>
        /// Represents the OP code "FSIN" (0x00000065)
        /// </summary>
        public static fsin FSIN { get; }                     = new fsin();
        /// <summary>
        /// Represents the OP code "FCOS" (0x00000066)
        /// </summary>
        public static fcos FCOS { get; }                     = new fcos();
        /// <summary>
        /// Represents the OP code "FTAN" (0x00000067)
        /// </summary>
        public static ftan FTAN { get; }                     = new ftan();
        /// <summary>
        /// Represents the OP code "FSINH" (0x00000068)
        /// </summary>
        public static fsinh FSINH { get; }                   = new fsinh();
        /// <summary>
        /// Represents the OP code "FCOSH" (0x00000069)
        /// </summary>
        public static fcosh FCOSH { get; }                   = new fcosh();
        /// <summary>
        /// Represents the OP code "FTANH" (0x0000006a)
        /// </summary>
        public static ftanh FTANH { get; }                   = new ftanh();
        /// <summary>
        /// Represents the OP code "FASIN" (0x0000006b)
        /// </summary>
        public static fasin FASIN { get; }                   = new fasin();
        /// <summary>
        /// Represents the OP code "FACOS" (0x0000006c)
        /// </summary>
        public static facos FACOS { get; }                   = new facos();
        /// <summary>
        /// Represents the OP code "FATAN" (0x0000006d)
        /// </summary>
        public static fatan FATAN { get; }                   = new fatan();
        /// <summary>
        /// Represents the OP code "FATAN2" (0x0000006e)
        /// </summary>
        public static fatan2 FATAN2 { get; }                 = new fatan2();
        /// <summary>
        /// Represents the OP code "FCMP" (0x0000006f)
        /// </summary>
        public static fcmp FCMP { get; }                     = new fcmp();
        /// <summary>
        /// Represents the OP code "JNAN" (0x00000070)
        /// </summary>
        public static jnan JNAN { get; }                     = new jnan();
        /// <summary>
        /// Represents the OP code "JNNAN" (0x00000071)
        /// </summary>
        public static jnnan JNNAN { get; }                   = new jnnan();
        /// <summary>
        /// Represents the OP code "JINF" (0x00000072)
        /// </summary>
        public static jinf JINF { get; }                     = new jinf();
        /// <summary>
        /// Represents the OP code "JPINF" (0x00000073)
        /// </summary>
        public static jpinf JPINF { get; }                   = new jpinf();
        /// <summary>
        /// Represents the OP code "JNINF" (0x00000074)
        /// </summary>
        public static jninf JNINF { get; }                   = new jninf();
        /// <summary>
        /// Represents the OP code "INTERRUPT" (0x0000fffc)
        /// </summary>
        public static interrupt INTERRUPT { get; }           = new interrupt();
        /// <summary>
        /// Represents the OP code "INTERRUPTTABLE" (0x0000fffd)
        /// </summary>
        public static interrupttable INTERRUPTTABLE { get; } = new interrupttable();
        /// <summary>
        /// Represents the OP code "EXEC" (0x0000fffe)
        /// </summary>
        public static exec EXEC { get; }                     = new exec();
        /// <summary>
        /// Represents the OP code "KERNEL" (0x0000ffff)
        /// </summary>
        public static kernel KERNEL { get; }                 = new kernel();


        /// <summary>
        /// A collection of all opcodes mapped to their code number
        /// </summary>
        public static Dictionary<ushort, OPCode> CodesByID { get; } = new Dictionary<ushort, OPCode>() {
            [0]     = NOP,
            [1]     = HALT,
            [2]     = JMP,
            [3]     = JMPREL,
            [4]     = ABK,
            [5]     = SYSCALL,
            [6]     = CALL,
            [7]     = RET,
            [8]     = COPY,
            [9]     = CLEAR,
            [10]    = IO,
            [11]    = IN,
            [12]    = OUT,
            [13]    = CLEARFLAGS,
            [14]    = SETFLAGS,
            [15]    = GETFLAGS,
            [16]    = MOV,
            [17]    = LEA,
            [18]    = ADD,
            [19]    = SUB,
            [20]    = MUL,
            [21]    = DIV,
            [22]    = MOD,
            [23]    = NEG,
            [24]    = NOT,
            [25]    = OR,
            [26]    = AND,
            [27]    = XOR,
            [28]    = NOR,
            [29]    = NAND,
            [30]    = NXOR,
            [31]    = ABS,
            [32]    = BOOL,
            [33]    = POW,
            [34]    = SHR,
            [35]    = SHL,
            [36]    = ROR,
            [37]    = ROL,
            [38]    = FAC,
            [39]    = INCR,
            [40]    = DECR,
            [41]    = CMP,
            [42]    = JLE,
            [43]    = JL,
            [44]    = JGE,
            [45]    = JG,
            [46]    = JE,
            [47]    = JNE,
            [48]    = JZ,
            [49]    = JNZ,
            [50]    = JNEG,
            [51]    = JPOS,
            [59]    = INT,
            [60]    = SWAP,
            [61]    = CPUID,
            [62]    = WAIT,
            [63]    = RESET,
            [64]    = PUSH,
            [65]    = POP,
            [66]    = PEEK,
            [67]    = SSWAP,
            [68]    = PUSHF,
            [69]    = POPF,
            [70]    = PEEKF,
            [71]    = PUSHI,
            [72]    = POPI,
            [73]    = PEEKI,
            [80]    = FICAST,
            [81]    = IFCAST,
            [82]    = FADD,
            [83]    = FSUB,
            [84]    = FMUL,
            [85]    = FDIV,
            [86]    = FMOD,
            [87]    = FNEG,
            [88]    = FINV,
            [89]    = FSQRT,
            [90]    = FROOT,
            [91]    = FLOG,
            [92]    = FLOGE,
            [93]    = FEXP,
            [94]    = FPOW,
            [95]    = FFLOOR,
            [96]    = FCEIL,
            [97]    = FROUND,
            [98]    = FMIN,
            [99]    = FMAX,
            [100]   = FSIGN,
            [101]   = FSIN,
            [102]   = FCOS,
            [103]   = FTAN,
            [104]   = FSINH,
            [105]   = FCOSH,
            [106]   = FTANH,
            [107]   = FASIN,
            [108]   = FACOS,
            [109]   = FATAN,
            [110]   = FATAN2,
            [111]   = FCMP,
            [112]   = JNAN,
            [113]   = JNNAN,
            [114]   = JINF,
            [115]   = JPINF,
            [116]   = JNINF,
            [65532] = INTERRUPT,
            [65533] = INTERRUPTTABLE,
            [65534] = EXEC,
            [65535] = KERNEL,
        };
	
        /// <summary>
        /// A collection of all opcodes mapped to their token string
        /// </summary>
        public static Dictionary<string, OPCode> CodesByToken { get; } = new Dictionary<string, OPCode>() {
            ["nop"]            = NOP,
            ["halt"]           = HALT,
            ["jmp"]            = JMP,
            ["jmprel"]         = JMPREL,
            ["abk"]            = ABK,
            ["syscall"]        = SYSCALL,
            ["call"]           = CALL,
            ["ret"]            = RET,
            ["copy"]           = COPY,
            ["clear"]          = CLEAR,
            ["io"]             = IO,
            ["in"]             = IN,
            ["out"]            = OUT,
            ["clearflags"]     = CLEARFLAGS,
            ["setflags"]       = SETFLAGS,
            ["getflags"]       = GETFLAGS,
            ["mov"]            = MOV,
            ["lea"]            = LEA,
            ["add"]            = ADD,
            ["sub"]            = SUB,
            ["mul"]            = MUL,
            ["div"]            = DIV,
            ["mod"]            = MOD,
            ["neg"]            = NEG,
            ["not"]            = NOT,
            ["or"]             = OR,
            ["and"]            = AND,
            ["xor"]            = XOR,
            ["nor"]            = NOR,
            ["nand"]           = NAND,
            ["nxor"]           = NXOR,
            ["abs"]            = ABS,
            ["bool"]           = BOOL,
            ["pow"]            = POW,
            ["shr"]            = SHR,
            ["shl"]            = SHL,
            ["ror"]            = ROR,
            ["rol"]            = ROL,
            ["fac"]            = FAC,
            ["incr"]           = INCR,
            ["decr"]           = DECR,
            ["cmp"]            = CMP,
            ["jle"]            = JLE,
            ["jl"]             = JL,
            ["jge"]            = JGE,
            ["jg"]             = JG,
            ["je"]             = JE,
            ["jne"]            = JNE,
            ["jz"]             = JZ,
            ["jnz"]            = JNZ,
            ["jneg"]           = JNEG,
            ["jpos"]           = JPOS,
            ["int"]            = INT,
            ["swap"]           = SWAP,
            ["cpuid"]          = CPUID,
            ["wait"]           = WAIT,
            ["reset"]          = RESET,
            ["push"]           = PUSH,
            ["pop"]            = POP,
            ["peek"]           = PEEK,
            ["sswap"]          = SSWAP,
            ["pushf"]          = PUSHF,
            ["popf"]           = POPF,
            ["peekf"]          = PEEKF,
            ["pushi"]          = PUSHI,
            ["popi"]           = POPI,
            ["peeki"]          = PEEKI,
            ["ficast"]         = FICAST,
            ["ifcast"]         = IFCAST,
            ["fadd"]           = FADD,
            ["fsub"]           = FSUB,
            ["fmul"]           = FMUL,
            ["fdiv"]           = FDIV,
            ["fmod"]           = FMOD,
            ["fneg"]           = FNEG,
            ["finv"]           = FINV,
            ["fsqrt"]          = FSQRT,
            ["froot"]          = FROOT,
            ["flog"]           = FLOG,
            ["floge"]          = FLOGE,
            ["fexp"]           = FEXP,
            ["fpow"]           = FPOW,
            ["ffloor"]         = FFLOOR,
            ["fceil"]          = FCEIL,
            ["fround"]         = FROUND,
            ["fmin"]           = FMIN,
            ["fmax"]           = FMAX,
            ["fsign"]          = FSIGN,
            ["fsin"]           = FSIN,
            ["fcos"]           = FCOS,
            ["ftan"]           = FTAN,
            ["fsinh"]          = FSINH,
            ["fcosh"]          = FCOSH,
            ["ftanh"]          = FTANH,
            ["fasin"]          = FASIN,
            ["facos"]          = FACOS,
            ["fatan"]          = FATAN,
            ["fatan2"]         = FATAN2,
            ["fcmp"]           = FCMP,
            ["jnan"]           = JNAN,
            ["jnnan"]          = JNNAN,
            ["jinf"]           = JINF,
            ["jpinf"]          = JPINF,
            ["jninf"]          = JNINF,
            ["interrupt"]      = INTERRUPT,
            ["interrupttable"] = INTERRUPTTABLE,
            ["exec"]           = EXEC,
            ["kernel"]         = KERNEL,
        };
	}
}
