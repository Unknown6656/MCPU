﻿using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

using MCPU.Compiler;
using MCPU;

namespace MCPU.MCPUPP.Compiler
{
    /// <summary>
    /// Provides functions to parse and compile given MCPU++ code segments to MCPU-compatible instructions or MCPU assembly code
    /// </summary>
    public unsafe class MCPUPPCompiler
        : IDisposable
    {
        /// <summary>
        /// The MCPU++ function name prefix
        /// </summary>
        public const string FUNCTION_PREFIX = "function__";
        /// <summary>
        /// The name of the main (entry-point) MCPU++ function
        /// </summary>
        public const string MAIN_FUNCTION_NAME = FUNCTION_PREFIX + "mcpuppmain";
        /// <summary>
        /// The address of the MCPU++ field 'CC'
        /// </summary>
        public const int F_CC = 0xf7;
        /// <summary>
        /// The address of the MCPU++ field 'RET'
        /// </summary>
        public const int F_RET = 0xf8;
        /// <summary>
        /// The address of the MCPU++ field 'SYP'
        /// </summary>
        public const int F_SYP = 0xf9;
        /// <summary>
        /// The address of the MCPU++ field 'SSZ'
        /// </summary>
        public const int F_SSZ = 0xfc;
        /// <summary>
        /// The address of the MCPU++ field 'GSZ'
        /// </summary>
        public const int F_GSZ = 0xfd;
        /// <summary>
        /// The address of the MCPU++ field 'LOF'
        /// </summary>
        public const int F_LOF = 0xfe;
        /// <summary>
        /// The address of the MCPU++ field 'LSZ'
        /// </summary>
        public const int F_LSZ = 0xff;
        /// <summary>
        /// The size of the temporary value section (.temp)
        /// </summary>
        public const int TMP_SZ = 0x100;
        /// <summary>
        /// The processor's minimum userspace size (in 4-byte-blocks)
        /// </summary>
        public const int MIN_PROC_SZ = 0x200; // 2 KB

        private int idc = 0;
        
        /// <summary>
        /// The MCPU Processor, on which the current compiler instance relies
        /// </summary>
        public Processor Processor { private set; get; }
        /// <summary>
        /// Returns the global precompiled MCPU++ code file heaeder
        /// </summary>
        public static string GlobalHeader => $@"
func MOVDO                  ; mov [[dst]+offs] [src] <==> call MOVDO dst offs src
    push [{F_CC}]
    mov [{F_CC}] [$0]
    add [{F_CC}] $1
    mov [[{F_CC}]] $2
    pop [{F_CC}]
end func

func MOVSO                  ; mov [dst] [[src]+offs] <==> call MOVDO dst src offs
    mov [$0] $1
    add [$0] $2
    mov [$0] [[$0]]
end func

func MOVO                   ; mov [[dst]+offs₁] [[src]+offs₂] <==> call MOVDO dst offs₁ src offs₂
    push [{F_CC - 1}]
    push [{F_CC}]
    mov [{F_CC - 1}] $0
    add [{F_CC - 1}] $1
    mov [{F_CC}] $2
    add [{F_CC}] $3
    mov [[{F_CC - 1}]] [[{F_CC}]]
    pop [{F_CC}]
    pop [{F_CC - 1}]
end func

func SY_PUSH
    mov [[{F_SYP}]] $0
    incr [{F_SYP}]
end func

func SY_PUSHL
    call MOVSO {F_CC} {F_LOF} $0
    call SY_PUSH [[{F_CC}]]
end func

func SY_PUSHG
    mov [{F_CC}] {TMP_SZ}
    add [{F_CC}] $0
    call SY_PUSH [[{F_CC}]]
end func

func SY_POP
    decr [{F_SYP}]
    mov [$0] [[{F_SYP}]]
end func

func SY_POPL
    call MOVSO {F_CC} {F_LOF} $0
    call SY_POP [{F_CC}]
end func

func SY_POPG
    mov [{F_CC}] {TMP_SZ}
    add [{F_CC}] $0
    call SY_POP [{F_CC}]
end func
";

        /// <summary>
        /// Destructs the current MCPU++ Compiler instance
        /// </summary>
        ~MCPUPPCompiler() => Dispose();

        /// <summary>
        /// Disposes the current compiler instance and relases all resources
        /// </summary>
        public void Dispose() => Processor = null;

        /// <summary>
        /// Creates a new MCPU++ Compiler instance based on the given processor
        /// </summary>
        /// <param name="p">MCPU Processor</param>
        public MCPUPPCompiler(Processor p)
        {
            if (p.Size <= MIN_PROC_SZ)
                throw new MCPUPPCompilerException($"The minimum processor userspace memory segment size must be {MIN_PROC_SZ * 4} bytes, to compile and execute a MCPU++-application");

            Processor = p;
            Processor.OnDisposed += _ => Dispose();
        }

        /// <summary>
        /// Returns a unique ID, which can be used for labels etc.
        /// </summary>
        /// <returns>Unique ID</returns>
        public string GetUniqueID() => $"{new Guid():N}_{idc++:x}";

        /// <summary>
        /// Formats the given code snippet's indentation
        /// </summary>
        /// <param name="code">Code snippet</param>
        /// <returns>Formatted code</returns>
        public static string FormatMCPUCode(string code) => string.Join("\n", FormatMCPUCode(code.Split('\n', '\r')));

        /// <summary>
        /// Formats the given code snippet line's indentation
        /// </summary>
        /// <param name="lines">Code lines</param>
        /// <returns>Formatted code lines</returns>
        public static string[] FormatMCPUCode(params string[] lines) => FormatMCPUCode(lines as IEnumerable<string>);

        /// <summary>
        /// Formats the given code snippet line's indentation
        /// </summary>
        /// <param name="lines">Code lines</param>
        /// <returns>Formatted code lines</returns>
        public static string[] FormatMCPUCode(IEnumerable<string> lines) => (from string l in lines
                                                                             let tl = l.Trim()
                                                                             where tl.Length > 0
                                                                             let cnt = tl.Contains(MCPUCompiler.COMMENT_START) ? tl.Remove(tl.IndexOf(MCPUCompiler.COMMENT_START)).Trim() : tl
                                                                             select (cnt.EndsWith(":") ? "" : "    ") + tl).ToArray();

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function header
        /// </summary>
        /// <param name="funcname">The function name</param>
        /// <returns>Function header</returns>
        public string InnerFunctionHeader(string funcname) => $@"
func {funcname}
    clear [[{F_LOF}]] [{F_LSZ}]
";

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function footer
        /// </summary>
        /// <param name="returnval">The function's return argument/value</param>
        /// <returns>Function footer</returns>
        public string InnerFunctionFooter(InstructionArgument returnval) => $@"
    mov [{F_RET}] {returnval.ToShortString()}
end func
";

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function call
        /// </summary>
        /// <param name="nfo">Callee information</param>
        /// <param name="argv">Function arguments</param>
        /// <returns>Function call code</returns>
        public string GenerateFunctionCall(Union<MCPUPPFunctionCallInformation, MCPUPPMainFunctionCallInformation> nfo, params InstructionArgument[] argv)
        {
            if (nfo.IsA)
            {
                string args = string.Join(" ", from arg in argv select arg.ToShortString());
                string call_id = $"_call_{GetUniqueID()}_{nfo.AsA.Name}";
                string lab_bef = "before" + call_id;
                string lab_aft = "after" + call_id;

                return $@"
    pushf
    mov [{F_CC}] 0
{lab_bef}:
    push [[{F_CC}]]
    incr [{F_CC}]
    cmp [{F_CC}] [{F_SYP}]
    jle {lab_bef}
    push [{F_SYP}]
    push [{F_LOF}]
    push [{F_LSZ}]
    add [{F_LOF}] [{F_LSZ}]
    mov [{F_LSZ}] {nfo.AsA.LocalSize}
    clear [0] {F_SYP + 1}
    call {nfo.AsA.Name} {args}
    pop [{F_LSZ}]
    pop [{F_LOF}]
    pop [{F_SYP}]
    mov [{F_CC}] [{F_SYP}]
{lab_aft}:
    pop [[{F_SYP}]]
    decr [{F_SYP}]
    cmp [{F_SYP}]
    jpos {lab_aft}
";
            }
            else
                return $@"
    .main
    .kernel
    clear [0] {TMP_SZ}
    mov [{F_LOF}] {TMP_SZ}
    mov [{F_LSZ}] {nfo.AsB.LocalSize}
    mov [{F_GSZ}] {nfo.AsB.GlobalSize}
    mov [{F_LOF}] {TMP_SZ}
    add [{F_LOF}] [{F_GSZ}]
    call {MAIN_FUNCTION_NAME}
    halt
";
        }
    }

    [Serializable]
    /// <summary>
    /// Represents a MCPU++ compiler error
    /// </summary>
    public sealed class MCPUPPCompilerException
        : Exception
    {
        /// <summary>
        /// The error message
        /// </summary>
        public new string Message => base.Message;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="msg">Compiler error message</param>
        public MCPUPPCompilerException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// Represents a basic MCPU++ function call information structure
    /// </summary>
    [Serializable]
    public struct MCPUPPFunctionCallInformation
    {
        /// <summary>
        /// The called function's name
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The called function's local size
        /// </summary>
        public int LocalSize { set; get; }
    }

    /// <summary>
    /// Represents a basic MCPU++ main function call information structure
    /// </summary>
    [Serializable]
    public struct MCPUPPMainFunctionCallInformation
    {
        /// <summary>
        /// The global variable section size
        /// </summary>
        public int GlobalSize { set; get; }
        /// <summary>
        /// The main function's local size
        /// </summary>
        public int LocalSize { set; get; }
    }
}
