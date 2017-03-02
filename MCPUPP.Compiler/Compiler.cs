using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Reflection;
using Microsoft.FSharp.Core;

using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using MCPU.MCPUPP.Parser.SyntaxTree;
using MCPU.MCPUPP.Parser;
using MCPU.Compiler;
using MCPU;

using Program = Microsoft.FSharp.Collections.FSharpList<MCPU.MCPUPP.Parser.SyntaxTree.Declaration>;

using static MCPU.MCPUPP.Parser.Precompiler.IMInstruction;
using static MCPU.MCPUPP.Parser.Precompiler;
using static MCPU.MCPUPP.Parser.Analyzer;

//////////////////////////////////////////////////// IMPORTANT : POINTER ADDRESS ARE KERNEL ADRESSES !!! ////////////////////////////////////////////////////

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
        /// The address of the MCPU++ field 'AUX'
        /// </summary>
        public const int F_AUX = 0xf6;
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
        /// The address of the MCPU++ field 'MSZ'
        /// </summary>
        public const int F_MSZ = 0xfa;
        /// <summary>
        /// The address of the MCPU++ field 'HSZ'
        /// </summary>
        public const int F_HSZ = 0xfb;
        /// <summary>
        /// The address of the MCPU++ field 'LAC'
        /// </summary>
        public const int F_LAC = 0xfc;
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

        private static readonly Random rand = new Random((int)(DateTime.Now.Ticks & 0xffffffff));
        private static int idc = 0;

        /// <summary>
        /// The MCPU Processor, on which the current compiler instance relies
        /// </summary>
        public Processor Processor { private set; get; }
        /// <summary>
        /// Returns the global precompiled MCPU++ code file heaeder
        /// </summary>
        public static string GlobalHeader
        {
            get
            {
                string[] lbl = new string[10];

                for (int i = 0; i < lbl.Length; i++)
                    lbl[i] = GetUniqueID();

                return $@"
interrupt func int_00       ; general interrupt method
    syscall 5 67656e65h 72616c20h 6572726fh 72000000h
end func

func MOVDO                  ; mov [[dst]+offs] [src] <==> call MOVDO dst offs src
    mov [{F_CC}] [$0]
    add [{F_CC}] $1
    mov [[{F_CC}]] $2
end func

func MOVSO                  ; mov [dst] [[src]+offs] <==> call MOVDO dst src offs
    mov [$0] $1
    add [$0] $2
    mov [$0] [[$0]]
end func

func MOVO                   ; mov [[dst]+offs₁] [[src]+offs₂] <==> call MOVDO dst offs₁ src offs₂
    call SY_PUSH [{F_AUX}]
    mov [{F_AUX}] $0
    add [{F_AUX}] $1
    mov [{F_CC}] $2
    add [{F_CC}] $3
    mov [[{F_AUX}]] [[{F_CC}]]
    call SY_POP {F_AUX}
end func

func SY_PUSH                ; push $0 onto the SYA-stack
    mov [[{F_SYP}]] $0
    incr [{F_SYP}]
end func

func SY_PUSHL               ; push local № $0 onto the SYA-stack
    mov [{F_CC}] [{F_LAC}]
    add [{F_CC}] [{F_LOF}]
    call MOVSO {F_CC} {F_CC} $0
    call SY_PUSH [[{F_CC}]]
end func

func SY_PUSHG               ; push global № $0 onto the SYA-stack
    mov [{F_CC}] {TMP_SZ}
    add [{F_CC}] $0
    call SY_PUSH [[{F_CC}]]
end func

func SY_PUSHA               ; push argument № $0 onto the SYA-stack
    mov [{F_CC}] {F_LOF}
    add [{F_CC}] $0
    call SY_PUSH [[{F_CC}]]
end func

func SY_POP                 ; pop from SYA-stack into [$0]
    decr [{F_SYP}]
    mov [$0] [[{F_SYP}]]
end func

func SY_POPL                ; pop from SYA-stack into local № $0
    mov [{F_CC}] [{F_LAC}]
    add [{F_CC}] [{F_LOF}]
    call MOVSO {F_CC} {F_CC} $0
    call SY_POP [{F_CC}]
end func

func SY_POPG                ; pop from SYA-stack into global № $0
    mov [{F_CC}] {TMP_SZ}
    add [{F_CC}] $0
    call SY_POP [{F_CC}]
end func

func SY_POPA                ; pop from SYA-stack into argument № $0
    mov [{F_CC}] {F_LOF}
    add [{F_CC}] $0
    call SY_POP [{F_CC}]
end func

func SY_PEEK                ; peek from SYA-stack into $0
    mov [$0] [{F_SYP}]
    decr [$0]
    mov [$0] [[$0]]
end func

func SY_EXEC_1              ; takes the top-most element, executes <$0> and pushes the result back
    call SY_POP {F_CC}
    exec $0 [{F_CC}]
    call SY_PUSH [{F_CC}]
end func

func SY_EXEC_2              ; takes the two top-most elements, executes <$0> and pushes the result back  «WARING: THE VALUE [AUX] WILL BE LOST»
    call SY_POP {F_CC}
    call SY_POP {F_AUX}
    exec $0 [{F_CC}] [{F_AUX}]
    call SY_PUSH [{F_CC}]
end func

func H_GETADDR              ; moves the address of object №$0 to the address [$1]
    call SY_PUSH [{F_AUX}]
    call SY_PUSH [{F_AUX - 1}]
    mov [{F_AUX - 1}] 0
    mov [{F_CC}] [{F_MSZ}]
    decr [{F_CC}]
    mov [{F_AUX}] $0
{lbl[0]}:
    cmp [{F_AUX}]
    jz {lbl[1]}
    add [{F_AUX - 1}] [[{F_CC}]]
    sub [{F_CC}] [[{F_CC}]]
    decr [{F_CC}]
    jmp {lbl[0]}
{lbl[1]}:
    add [{F_CC}] [[{F_CC}]]
    mov [$1] [{F_CC}]
    call SY_POP {F_AUX - 1}
    call SY_POP {F_AUX}
end func

func H_GETSIZE              ; moves the size of object №$0 to the address [$1]
    call SY_PUSH [{F_AUX}]
    call SY_PUSH [{F_AUX - 1}]
    mov [{F_AUX - 1}] 0
    mov [{F_CC}] [{F_MSZ}]
    decr [{F_CC}]
    mov [{F_AUX}] $0
{lbl[2]}:
    cmp [{F_AUX}]
    jz {lbl[3]}
    add [{F_AUX - 1}] [[{F_CC}]]
    sub [{F_CC}] [[{F_CC}]]
    decr [{F_CC}]
    jmp {lbl[2]}
{lbl[3]}:
    mov [$1] [[{F_CC}]]
    call SY_POP {F_AUX - 1}
    call SY_POP {F_AUX}
end func

func H_ALLOC                ; allocates an object with the size $0 and writes its ID to [$1] and its address to [$2]
    call SY_PUSH [{F_AUX}]
    call SY_PUSH [{F_AUX - 1}]
    mov [{F_AUX - 1}] 0
    mov [{F_CC}] [{F_MSZ}]
    decr [{F_CC}]
    mov [{F_AUX}] [{F_HSZ}]
{lbl[4]}:
    cmp [{F_AUX}]
    jz {lbl[5]}
    add [{F_AUX - 1}] [[{F_CC}]]
    sub [{F_CC}] [[{F_CC}]]
    decr [{F_CC}]
    jmp {lbl[4]}
{lbl[5]}:
    ; [AUX] <- 0
    ; [CC.] <- &object
    ; [AUX-1] <- heap size (in 4-byte-blocks)
    mov [[{F_CC}]] $0
    decr [{F_CC}] $0
    clear [{F_CC}] $0
    mov [$2] [{F_CC}]
    mov [$1] [{F_HSZ}]
    incr [{F_HSZ}]
    call SY_POP {F_AUX - 1}
    call SY_POP {F_AUX}
end func

func H_DELETE               ; deletes the object with the ID №$0
    
end func
";
            }
        }
        /// <summary>
        /// Returns a dictionary of predefined functions and their associated MCPU code
        /// </summary>
        public static ReadOnlyDictionary<string, string> PredefinedFunctions { get; }


        static MCPUPPCompiler() => PredefinedFunctions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["iprint"] = $@"
    call SY_POP {F_CC}
    syscall 2 [{F_CC}]
",
            ["fprint"] = $@"
    call SY_POP {F_CC}
    syscall 3 [{F_CC}]
",
            ["iscan"] = $@"
    syscall 6 [{F_CC}]
    call SY_PUSH {F_CC}
",
            ["fscan"] = $@"
    syscall 7 [{F_CC}]
    call SY_PUSH {F_CC}
",
            ["sin"] = exec_sya1(OPCodes.FSIN),
            ["cos"] = exec_sya1(OPCodes.FCOS),
            ["tan"] = exec_sya1(OPCodes.FTAN),
            ["asin"] = exec_sya1(OPCodes.FASIN),
            ["acos"] = exec_sya1(OPCodes.FACOS),
            ["atan"] = exec_sya1(OPCodes.FATAN),
            ["atan2"] = exec_sya2(OPCodes.FATAN2),
            ["sinh"] = exec_sya1(OPCodes.FSINH),
            ["cosh"] = exec_sya1(OPCodes.FCOSH),
            ["tanh"] = exec_sya1(OPCodes.FTANH),
            ["halt"] = "halt",
            ["cpuid"] = $@"
    cpuid [{F_CC}]
    call SY_PUSH [{F_CC}]
",
            ["wait"] = $@"
    call SY_POP {F_CC}
    wait {F_CC}
",
            ["io"] = $@"
    push [{F_CC - 1}]
    call SY_POP {F_CC - 1}
    call SY_POP {F_CC}
    io [{F_CC}] [{F_CC - 1}]
    pop [{F_CC - 1}]
",
            ["in"] = $@"
    call SY_POP {F_CC}
    in [{F_CC}] [{F_CC}]
    call SY_PUSH [{F_CC}]
",
            ["out"] = $@"
    push [{F_CC - 1}]
    call SY_POP {F_CC - 1}
    call SY_POP {F_CC}
    out [{F_CC}] [{F_CC - 1}]
    pop [{F_CC - 1}]
",
            ["log"] = exec_sya1(OPCodes.FLOG),
            ["ln"] = exec_sya1(OPCodes.FLOGE),
            ["exp"] = exec_sya1(OPCodes.FEXP),
            ["round"] = exec_sya1(OPCodes.FROUND),
            ["ceil"] = exec_sya1(OPCodes.FCEIL),
            ["floor"] = exec_sya1(OPCodes.FFLOOR),
        });

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
        /// Destructs the current MCPU++ Compiler instance
        /// </summary>
        ~MCPUPPCompiler() => Dispose();

        /// <summary>
        /// Disposes the current compiler instance and relases all resources
        /// </summary>
        public void Dispose() => Processor = null;

        /// <summary>
        /// Returns a unique ID, which can be used for labels etc.
        /// </summary>
        /// <returns>Unique ID</returns>
        public static string GetUniqueID()
        {
            byte[] bytes = new byte[8];

            rand.NextBytes(bytes);

             return $"id_{string.Join("", from b in bytes select b.ToString("x2"))}_{idc++:x}";
        }

        /// <summary>
        /// Returns the unique label ID associated with the given intermediate label
        /// </summary>
        /// <param name="label">Intermediate label</param>
        /// <returns>Unique label ID</returns>
        public static string GetUniqueID(int label) => $"label_{label:x8}";

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
                                                                             select (MCPUCompiler.LABEL_REGEX.IsMatch(cnt) ||
                                                                                     MCPUCompiler.FUNC_REGEX.IsMatch(cnt) ||
                                                                                     MCPUCompiler.END_FUNC_REGEX.IsMatch(cnt) ? "" : "    ") + tl + (MCPUCompiler.END_FUNC_REGEX.IsMatch(cnt) ? "\n" : "")).ToArray();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prog"></param>
        /// <returns></returns>
        public Union<MCPUPPCompilerResult, MCPUPPCompilerException> Compile(string source)
        {
            try
            {
                Program prog = Lexer.parse(source);
                AnalyzerResult res = Analyze(prog);

                IMBuilder builder = new IMBuilder(res);
                IMProgram preproc = builder.BuildClass(prog);

                string mcpu = $"{GlobalHeader}\n\n{GenerateInstructions(prog, res, builder, preproc)}";

                mcpu = FormatMCPUCode(mcpu);

                Instruction[] instr = MCPUCompiler.Compile(mcpu).AsA.Instructions;

                return new MCPUPPCompilerResult
                {
                    Instructions = instr,
                    CompiledCode = mcpu,
                    SourceCode = source,
                    UserFunctions = (from m in preproc.Methods
                                     select new FunctionSignature
                                     {
                                         Name = m.Name,
                                         ReturnType = m.ReturnType.Convert(),
                                         Parameters = (from p in m.Parameters
                                                       select (p.Type.Convert(), p.Name)).ToArray(),
                                     }).ToArray()
                };
            }
            catch (Exception ex)
            {
                return ex is MCPUPPCompilerException mex ? mex : new MCPUPPCompilerException(ex.Message);
            }
        }

        /// <summary>
        /// Returns the MCPU++ internal function name of the given intermediate method
        /// </summary>
        /// <param name="m">Intermediate method</param>
        /// <returns>MCPU++ internal function name</returns>
        public string FunctionName(IMMethod m) => m.Name == EntryPointName ? MAIN_FUNCTION_NAME : $"{FUNCTION_PREFIX}{m.Name}";

        internal string GenerateInstructions(Program prog, AnalyzerResult res, IMBuilder builder, IMProgram preproc)
        {
            Dictionary<string, int> globals = new Dictionary<string, int>();
            StringBuilder sb = new StringBuilder();

            IMMethod[] funcs = preproc.Methods.ToArray();
            IMMethod entrypoint = funcs.First(_ => _.Name == EntryPointName);

            foreach (IMVariable field in preproc.Fields)
                globals[field.Name] = globals.Count;

            foreach (IMMethod m in funcs)
            {
                string footer = $"{InnerFunctionFooter(out string endlbl).TrimEnd()}     ; end of method '{m.Name}'";

                sb.AppendLine($"func {FunctionName(m)}      ; {m.Signature}");

                foreach (IMInstruction instr in m.Body)
                    sb.AppendLine(GenerateInstruction(instr, (m, endlbl), globals));

                sb.AppendLine(footer);
            }

            sb.Append(GenerateFunctionCall(new MainFunctionCallInformation
            {
                GlobalSize = preproc.Fields.Length,
                LocalSize = entrypoint.Locals.Length,
            }));

            return sb.ToString();
        }

        internal string GenerateInstruction(IMInstruction instr, (IMMethod func, string endlbl) func, Dictionary<string, int> globals)
        {
            switch (instr.Tag)
            {
                case Tags.Ret:
                    {
                        string lbl = GetUniqueID();

                        return $@"
    cmp [{F_SYP}]
    jnz {lbl}
    mov [{F_RET}] 0
    jmp {func.endlbl}
{lbl}:
    call SY_POP {F_RET}
    jmp {func.endlbl}";
                    }
                case Tags.Halt: return "halt";
                case Tags.Nop: return "nop";
                case Tags.Dup: return $@"
    call SY_PEEK {F_CC}
    call SY_PUSH [{F_CC}]
";
                case Tags.Add: return exec_sya2(OPCodes.ADD);
                case Tags.Sub: return exec_sya2(OPCodes.SUB);
                case Tags.Mul: return exec_sya2(OPCodes.MUL);
                case Tags.Div: return exec_sya2(OPCodes.DIV);
                case Tags.Mod: return exec_sya2(OPCodes.MOD);
                case Tags.Shr: return exec_sya2(OPCodes.SHR);
                case Tags.Shl: return exec_sya2(OPCodes.SHL);
                case Tags.Ror: return exec_sya2(OPCodes.ROR);
                case Tags.Rol: return exec_sya2(OPCodes.ROL);
                case Tags.And: return exec_sya2(OPCodes.AND);
                case Tags.Or: return exec_sya2(OPCodes.OR);
                case Tags.Xor: return exec_sya2(OPCodes.XOR);
                case Tags.Pow: return exec_sya2(OPCodes.POW);
                case Tags.Not: return exec_sya1(OPCodes.NOT);
                case Tags.Neg: return exec_sya1(OPCodes.NEG);
                case Tags.Bool: return exec_sya1(OPCodes.BOOL);
                case Tags.Pop: return $"decr [{F_SYP}]";
                case Tags.Cmp: return exec_sya2(OPCodes.CMP);
                case Tags.FCmp: return exec_sya2(OPCodes.FCMP);
                case Tags.FIcast: return exec_sya1(OPCodes.FICAST);
                case Tags.IFcast: return exec_sya1(OPCodes.IFCAST);
                default:
                    switch (instr)
                    {
                        case Call call:
                            if (PredefinedFunctions.ContainsKey(call.Item1))
                                return PredefinedFunctions[call.Item1];
                            else
                            {
                                int argc = call.Item2;

                                return GenerateFunctionCall(new FunctionCallInformation
                                {
                                    Name = $"{FUNCTION_PREFIX}{call.Item1}",
                                    LocalSize = 0, // TODO
                                    ParameterSize = 0, // TODO
                                });
                            }
                        case Inline inline:
                            return inline.Item;
                        case Ldcf cf:
                            return $"call SY_PUSH {cf.Item}";
                        case Ldci ci:
                            return $"call SY_PUSH {ci.Item}";
                        case Ldarg arg:
                            return $"call SY_PUSHA {arg.Item}";
                        case Starg arg:
                            return $"call SY_POPA {arg.Item}";
                        case Ldloc loc:
                            return $"call SY_PUSHL {loc.Item}";
                        case Stloc loc:
                            return $"call SY_POPL {loc.Item}";
                        case Ldfld fld:
                            return $"call SY_PUSHG {globals[fld.Item.Name]}";
                        case Stfld fld:
                            return $"call SY_POPG {globals[fld.Item.Name]}";
                        case Label lbl:
                            return $"{GetUniqueID(lbl.Item)}:";
                        case Jmp jmp:
                            return $"jmp {GetUniqueID(jmp.Item)}";
                        case Jz jz:
                            return $"jz {GetUniqueID(jz.Item)}";
                        case Jnz jnz:
                             return $"jnz {GetUniqueID(jnz.Item)}";
                        case Ldaddrarg arg:
                            return $@"
    mov [{F_CC}] {arg.Item}
    add [{F_CC}] [{F_LOF}]
    call SY_PUSH [{F_CC}]
";
                        case Ldaddrfld fld:
                            return $@"
    mov [{F_CC}] {TMP_SZ}
    add [{F_CC}] {globals[fld.Item.Name]}
    call SY_PUSH [{F_CC}]
";
                        case Ldaddrloc loc:
                            return $@"
    mov [{F_CC}] {F_LOF}
    add [{F_CC}] {F_LAC}
    call MOVSO {F_CC} {F_CC} {loc.Item}
    call SY_PUSH [{F_CC}]
";
                        default:
                            return $"; TODO: {instr}";
                    }
            }

            /* TODO:
             | Malloc of VariableType
             | Delete
             | Ldlen
             | Ldptra
             | Stptra
             | Ldptrv
             | Stptrv
             | Ldelem of VariableType
             | Stelem of VariableType
             | Ldptr of VariableType
             | Stptr of VariableType
             */
        }

        internal static string exec_sya1(OPCode opc) => $"call SY_EXEC_1 {opc.Number:x4}h       ; '{opc.Token}'-instruction";

        internal static string exec_sya2(OPCode opc) => $"call SY_EXEC_2 {opc.Number:x4}h       ; '{opc.Token}'-instruction";

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function header
        /// </summary>
        /// <param name="funcname">The function name</param>
        /// <returns>Function header</returns>
        public string InnerFunctionHeader(string funcname) => $@"
func {funcname}
    mov [{F_CC}] [{F_LOF}]
    add [{F_CC}] [{F_LAC}]
    clear [[{F_CC}]] [{F_LSZ}]
";

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function footer
        /// </summary>
        /// <param name="endlbl">The label name, which represents the end of the current function</param>
        /// <param name="returnval">The function's return argument/value</param>
        /// <returns>Function footer</returns>
        public string InnerFunctionFooter(out string endlbl, InstructionArgument? returnval = null) => $@"
{endlbl = GetUniqueID()}:
    mov [{F_RET}] {(returnval ?? new InstructionArgument { Type = ArgumentType.Address, Value = F_RET }).ToShortString()}
end func
";

        /// <summary>
        /// Generates the precompiled MCPU++ code for a function call
        /// </summary>
        /// <param name="nfo">Callee information</param>
        /// <param name="argv">Function arguments</param>
        /// <returns>Function call code</returns>
        public string GenerateFunctionCall(Union<FunctionCallInformation, MainFunctionCallInformation> nfo, params InstructionArgument[] argv)
        {
            if (nfo.IsA)
            {
                string args = string.Join(" ", from arg in argv select arg.ToShortString());
                string call_id = $"_call_{GetUniqueID()}_{nfo.AsA.Name}";
                string lab_bef = "before" + call_id;
                string lab_aft = "after" + call_id;

                return $@"
    pushf
    push [{F_AUX}]
    mov [{F_CC}] 0
{lab_bef}:
    push [[{F_CC}]]
    incr [{F_CC}]
    cmp [{F_CC}] [{F_SYP}]
    jle {lab_bef}
    push [{F_SYP}]
    push [{F_LAC}]
    push [{F_LOF}]
    push [{F_LSZ}]
    add [{F_LOF}] [{F_LAC}]
    add [{F_LOF}] [{F_LSZ}]
    mov [{F_LSZ}] {nfo.AsA.LocalSize}
    mov [{F_LAC}] {nfo.AsA.ParameterSize}
    clear [0] {F_SYP + 1}
    call {nfo.AsA.Name} {args}
    pop [{F_LSZ}]
    pop [{F_LOF}]
    pop [{F_LAC}]
    pop [{F_SYP}]
    mov [{F_CC}] [{F_SYP}]
{lab_aft}:
    pop [[{F_SYP}]]
    decr [{F_SYP}]
    cmp [{F_SYP}]
    jpos {lab_aft}
    pop [{F_AUX}]
    popf
";
            }
            else
                return $@"
    .main
    .kernel
    .enable interrupt
    clear [0] {TMP_SZ}
    mov [{F_LOF}] {TMP_SZ}
    mov [{F_LSZ}] {nfo.AsB.LocalSize}
    mov [{F_GSZ}] {nfo.AsB.GlobalSize}
    mov [{F_LOF}] {TMP_SZ}
    add [{F_LOF}] [{F_GSZ}]
    call {MAIN_FUNCTION_NAME}
    .disable interrupt
    .user
    halt
";
        }
    }

    /// <summary>
    /// Contains a number of extension methods concerning the MCPU++ Abstract Syntax Tree (AST)
    /// </summary>
    public static class SyntaxTreeExtensions
    {
        internal static readonly Type ITupleType = typeof(Tuple).Assembly.GetType("System.ITuple");


        internal static VariableType Convert(this SymbolVariableType svt) =>
            (svt.IsArray ? VariableType.Array : svt.IsScalar ? VariableType.Scalar : VariableType.Pointer) | svt.Type.Convert();

        internal static VariableType Convert(this Parser.SyntaxTree.VariableType svt) =>
            svt.IsInt ? VariableType.Int : svt.IsFloat ? VariableType.Float : VariableType.Void;

        internal static dynamic GenerateTuple(object val)
        {
            if (val == null)
                return null;

            List<object> values = new List<object>();
            Type t = val.GetType();
            PropertyInfo prop = t.GetProperty("Item");
            int i = 1;

            if (prop == null)
                while (true)
                {
                    prop = t.GetProperty($"Item{i}");

                    if (prop != null)
                    {
                        values.Add(prop.GetValue(val));

                        ++i;
                    }
                    else
                        break;
                }
            else
                return prop.GetValue(val);

            MethodInfo meth = (from m in typeof(Tuple).GetMethods()
                               where m.Name == nameof(Tuple.Create)
                               where m.IsGenericMethodDefinition
                               let gen = m.GetParameters()
                               where gen.Length == i - 1
                               select m).FirstOrDefault();

            return meth?.MakeGenericMethod((from v in values
                                            select v?.GetType() ?? typeof(object)).ToArray())
                       ?.Invoke(null, values.ToArray());
        }

        // this is a C#-port from the F#-function:
        // https://github.com/Unknown6656/MCPU/blob/ae1240f405a09b56e6f37fd1fc5575b32e55bd3b/MCPUPP.Parser/SyntaxTree.fs#L239..L257
        internal static string tstr(object val, int indent, bool padstart = true, Type overridetype = null)
        {
            string tab = new string(' ', 4 * indent);

            string inner()
            {
                if (val == null)
                    return "(null)";
                else if (val is string str)
                    return $"\"{str}\"";

                Type type = val.GetType();
                string tstring = (overridetype ?? type).Name;

                try
                {
                    if (type?.GetGenericTypeDefinition() == typeof(FSharpOption<>))
                        return tstr(type.GetProperty("Value").GetValue(val), 0);
                }
                catch { }

                if (FSharpType.IsTuple(type))
                    return prints("(", FSharpValue.GetTupleFields(val), ")");
                else if (ITupleType.IsAssignableFrom(type))
                {
                    PropertyInfo prop = ITupleType.GetProperty("Size", BindingFlags.Instance | BindingFlags.NonPublic);
                    int sz = (int)prop.GetValue(val, new object[0]);

                    return prints("(", from i in Enumerable.Range(1, sz) select ITupleType.GetProperty($"Item{i}").GetValue(val), ")");
                }
                else if (val is ValueTuple vtuple)
                {
                    throw null; // TODO
                }
                else if (val is IEnumerable @enum)
                    return prints("[", @enum, "]");
                else
                    switch (val)
                    {
                        case Statement _:
                        case Expression _:
                        case Declaration _:
                        case ExpressionStatement.Expression _:
                            return tstr(GenerateTuple(val), indent, false, type);
                        default:
                            return $"{tstring} : {val}";
                    }

                string printl(IEnumerable l) => string.Join(",\n", from object e in l select tstr(e, indent + 1));
                string prints(string p, IEnumerable l, string s) => $"{tstring} : {p}\n{printl(l)}\n{tab}{s}";
            }

            return (padstart ? tab : "") + inner();
        }

        /// <summary>
        /// Returns the debugging-string representation of the MCPU++ program represented by the given AST
        /// </summary>
        /// <param name="prog">MCPU++ program AST</param>
        /// <returns>String representation</returns>
        public static string ToDebugString(this Program prog) => tstr(prog, 0);

        /// <summary>
        /// Returns the debugging-string representation of the given MCPU++ program analyzer result
        /// </summary>
        /// <param name="res">MCPU++ program analyzer result</param>
        /// <returns>String representation</returns>
        public static string ToDebugString(this AnalyzerResult res) => tstr(res, 0);
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
    /// Represents the result of a successful MCPU++ code compilation
    /// </summary>
    [Serializable]
    public sealed class MCPUPPCompilerResult
    {
        /// <summary>
        /// Retruns the original MCPU++ source code
        /// </summary>
        public string SourceCode { internal set; get; }
        /// <summary>
        /// Returns the assembled MCPU code
        /// </summary>
        public string CompiledCode { internal set; get; }
        /// <summary>
        /// Returns the compiled instructions
        /// </summary>
        public Instruction[] Instructions { internal set; get; }
        /// <summary>
        /// Returns a list of user-defined function signatures
        /// </summary>
        public FunctionSignature[] UserFunctions { internal set; get; }
    }

    /// <summary>
    /// Represents a MCPU++ function signature
    /// </summary>
    [Serializable]
    public struct FunctionSignature
    {
        /// <summary>
        /// The function's name
        /// </summary>
        public string Name;
        /// <summary>
        /// The function's return type
        /// </summary>
        public VariableType ReturnType;
        /// <summary>
        /// The function's parameters
        /// </summary>
        public (VariableType Type, string Name)[] Parameters;
    }

    /// <summary>
    /// Represents a basic MCPU++ function call information structure
    /// </summary>
    [Serializable]
    public struct FunctionCallInformation
    {
        /// <summary>
        /// The called function's name
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The called function's local size
        /// </summary>
        public int LocalSize { set; get; }
        /// <summary>
        /// The called function's parameter size
        /// </summary>
        public int ParameterSize { set; get; }
    }

    /// <summary>
    /// Represents a basic MCPU++ main function call information structure
    /// </summary>
    [Serializable]
    public struct MainFunctionCallInformation
    {
        /// <summary>
        /// The global variable section size
        /// </summary>
        public int GlobalSize { set; get; }
        /// <summary>
        /// The main function's local size
        /// </summary>
        public int LocalSize { set; get; }


        public static implicit operator FunctionCallInformation(MainFunctionCallInformation main) => new FunctionCallInformation
        {
            ParameterSize = 0,
            LocalSize = main.LocalSize,
            Name = MCPUPPCompiler.MAIN_FUNCTION_NAME
        };
    }

    /// <summary>
    /// Represents an internal MCPU++ variable reference
    /// </summary>
    [Serializable]
    public struct Variable
    {
        /// <summary>
        /// The variable's name (as declared in the MCPU++ code)
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The variable type
        /// </summary>
        public VariableType Type { set; get; }
        /// <summary>
        /// Returns, whether the current variable is a floating-point variable
        /// </summary>
        public bool IsFloat => Type.HasFlag(VariableType.Float);
        /// <summary>
        /// Returns, whether the current variable is an array
        /// </summary>
        public bool IsArray => Type.HasFlag(VariableType.Array);
        /// <summary>
        /// Returns, whether the current variable is a pointer
        /// </summary>
        public bool IsPointer => Type.HasFlag(VariableType.Pointer);
        /// <summary>
        /// The variable's raw size (in 4-byte blocks)
        /// </summary>
        public int Size { set; get; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, Flags]
    public enum VariableType
        : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Int = 0x01,
        /// <summary>
        /// 
        /// </summary>
        Float = 0x02,
        /// <summary>
        /// 
        /// </summary>
        Void = 0x04,
        /// <summary>
        /// 
        /// </summary>
        Scalar = 0x00,
        /// <summary>
        /// 
        /// </summary>
        Pointer = 0x40,
        /// <summary>
        /// 
        /// </summary>
        Array = 0x80,
        /// <summary>
        /// 
        /// </summary>
        Undefined = Pointer | Array,
    }
}
