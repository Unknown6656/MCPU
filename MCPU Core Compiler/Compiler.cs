﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;
using System;

namespace MCPU.Compiler
{
    /// <summary>
    /// Provides functions to parse and compile given code segments to MCPU-compatible instructions
    /// </summary>
    public static class MCPUCompiler
    {
        /// <summary>
        /// The character sequence which marks the start of a code comment
        /// </summary>
        public const string COMMENT_START = ";";
        /// <summary>
        /// The name of the main (entry-point) function
        /// </summary>
        public const string MAIN_FUNCTION_NAME = "____main";
        /// <summary>
        /// Floating-point matching pattern
        /// </summary>
        public const string FLOAT_CORE = @"((\+|\-|)([0-9]+\.[0-9]*|[0-9]*\.[0-9]+)(e(\+|\-|)[0-9]+)?[fd]?|pi|e|phi|tau|π|τ)";
        /// <summary>
        /// Integer matching pattern
        /// </summary>
        public const string INTEGER_CORE = @"(\-?(0x[0-9a-f]+|[0-9a-f]+h|[0-9]+|0o[01]+|0b[0-7]+)|true|false|null)";
        /// <summary>
        /// Argument core matching pattern
        /// </summary>
        internal static readonly Regex ARGUMENT_CORE = new Regex($@"((?<float>{FLOAT_CORE})|k?\[\$?(?<addr>{INTEGER_CORE})\]|k?\[\[\$?(?<ptr>{INTEGER_CORE})\]\]|\$?(?<const>{INTEGER_CORE})|{NAME_REGEX_CORE})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Instruction matching pattern
        /// </summary>
        public static readonly Regex INSTRUCTION_REGEX = new Regex($@"^\s*\b{NAME_REGEX_CORE}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Case-insensitive name pattern
        /// </summary>
        public const string NAME_REGEX_CORE = @"(?<name>[_a-z]*[a-z]+\w*)";
        /// <summary>
        /// Matches jump labels
        /// </summary>
        public static readonly Regex LABEL_REGEX = new Regex($@"(\b{NAME_REGEX_CORE}\b\:)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Matches function declarations
        /// </summary>
        public static readonly Regex FUNC_REGEX = new Regex($@"\b(inline\s+)?func\s+{NAME_REGEX_CORE}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Matches function endings
        /// </summary>
        public static readonly Regex END_FUNC_REGEX = new Regex(@"\bend\s+func\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Represents the IEEE-754 representation of the mathematical constant π
        /// </summary>
        public static readonly int FC_π = (FloatIntUnion)Math.PI;
        /// <summary>
        /// Represents the IEEE-754 representation of the mathematical constant τ
        /// </summary>
        public static readonly int FC_τ = (FloatIntUnion)(Math.PI * 2);
        /// <summary>
        /// Represents the IEEE-754 representation of the mathematical constant e
        /// </summary>
        public static readonly int FC_e = (FloatIntUnion)Math.E;
        /// <summary>
        /// Represents the IEEE-754 representation of the mathematical constant φ
        /// </summary>
        public static readonly int FC_φ = (FloatIntUnion)1.61803398874989;


        internal static IEnumerable<string> PreprocessLines(IEnumerable<string> lines) =>
            from line in lines
            let ln = new Func<string>(() => line.Contains(COMMENT_START) ? line.Remove(line.IndexOf(COMMENT_START)) : line)().Trim()
            where ln.Length > 0
            select ln;

        internal static unsafe (MCPUFunction[], int, string) Precompile(string code)
        {
            string[] lines = PreprocessLines((code ?? "").Split('\n', '\r')).ToArray();
            List<(int, string, int)> unmapped = new List<(int, string, int)>();
            Dictionary<string, int> labels = new Dictionary<string, int>();
            List<MCPUFunction> functions = new List<MCPUFunction>();
            MCPUFunction curr_func = null;
            int is_func = 0;
            int linenr = 0;
            Match match;
            int id = 0;

            (MCPUFunction[], int, string) Error(string message) => (null, linenr + 1, message);
            MCPUFunction FindFirst(string name) => (from f in functions where f.Name == name select f).FirstOrDefault();
            bool CheckGroup(string name, out string value) => (value = match.Groups[name].ToString().Trim()).Length > 0;
            unsafe int ParseFloatArg(string s)
            {
                switch (s)
                {
                    case "τ":
                    case "tau":
                        return FC_τ;
                    case "π":
                    case "pi":
                        return FC_π;
                    case "φ":
                    case "phi":
                        return FC_φ;
                    case "e":
                        return FC_e;
                    default:
                        float f = float.Parse(s.Replace('.', ',')
                                               .Replace('f', 'd')
                                               .Replace("d", ""));
                        return *((int*)&f);
                }

            }
            int ParseIntArg(string s)
            {
                int intparse(string arg)
                {
                    if (arg.EndsWith("h"))
                        return int.Parse(arg.Remove(arg.Length - 1), NumberStyles.HexNumber);
                    else if (arg.StartsWith("0x"))
                        return int.Parse(arg.Remove(0, 2), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier);
                    else if (arg.StartsWith("0b"))
                        return Convert.ToInt32(arg.Remove(0, 2), 2);
                    else if (arg.StartsWith("0o"))
                        return Convert.ToInt32(arg.Remove(0, 2), 8);
                    else if ((arg == "null") & (arg == "false"))
                        return 0;
                    else if (arg == "true")
                        return 1;
                    else
                        return int.Parse(arg);
                }

                bool isneg = false;

                s = s.ToLower().Trim();

                if (s.StartsWith("+"))
                    s = s.Remove(0, 1);
                else if (s.StartsWith("-"))
                {
                    isneg = true;
                    s = s.Remove(0, 1);
                }

                int val = intparse(s.Trim());

                return isneg ? -val : val;
            }


            for (int l = lines.Length; linenr < l; ++linenr)
            {
                string line = lines[linenr];
                
                if ((match = LABEL_REGEX.Match(line)).Success)
                    if (is_func == 0)
                        Error("An jump label may only be used inside a function or after the '.main'-token.");
                    else
                    {
                        string name = match.Groups["name"].ToString().ToLower();
                        (int, string, int) um = unmapped.FirstOrDefault(_ => _.Item2 == name);

                        if (FindFirst(name) != null)
                            return Error($"A function called '{name}' does already exist.");

                        if (um.Item2 != name)
                            if (labels.ContainsKey(name))
                                return Error($"The label '{name}' does already exist.");
                            else
                                labels[name] = ++id;
                        else
                        {
                            labels[name] = um.Item3;
                            unmapped.Remove(um);
                        }

                        line = line.Remove(match.Index, match.Length);

                        curr_func.Instructions.Add(new MCPUJumpLabel(id));
                    }

                if ((line = line.Trim()).Length > 0)
                {
                    if (line.StartsWith("."))
                        switch (line = line.Remove(0, 1).Trim().ToLower())
                        {
                            case "main":
                                curr_func = new MCPUFunction(MAIN_FUNCTION_NAME) { ID = 0 };
                                curr_func.Instructions.Add(OPCodes.NOP);
                                is_func = -1;

                                continue;
                            case "kernel" when (is_func != 0):
                                curr_func.Instructions.Add((OPCodes.KERNEL, new InstructionArgument[] { 1 }));

                                continue;
                            case "user" when (is_func != 0):
                                curr_func.Instructions.Add((OPCodes.KERNEL, new InstructionArgument[] { 0 }));

                                continue;
                            default:
                                return Error(is_func != -1 ? $"The token '.{line}' could not be parsed."
                                                           : $"The token '.{line}' can only be used inside a function or after the '.main'-declaration.");
                        }
                    else if ((match = FUNC_REGEX.Match(line)).Success)
                        if (is_func == 1)
                            return Error("Functions cannot be nested. Please close the current function with an 'END FUNC'-token.");
                        else if (is_func == -1)
                            return Error("Functions cannot be declared after the '.main'-token.");
                        else
                        {
                            string name = match.Groups["name"].ToString().ToLower();

                            if (labels.ContainsKey(name))
                                return Error($"A label called '{name}' does already exist.");
                            else if (FindFirst(name) != null)
                                return Error($"The function '{name}' does already exist.");

                            curr_func = new MCPUFunction(name, OPCodes.NOP) { ID = ++id };
                            is_func = 1;

                            functions.Add(curr_func);
                        }
                    else if ((match = END_FUNC_REGEX.Match(line)).Success)
                        if (is_func != 0)
                        {
                            curr_func.Instructions.Add(is_func == -1 ? OPCodes.HALT : OPCodes.RET as OPCode);
                            curr_func = (from f in functions where f.Name == MAIN_FUNCTION_NAME select f).FirstOrDefault();
                            is_func = 0;
                        }
                        else
                            return Error("A function declaration must precede an 'END FUNC'-token.");
                    else if ((match = INSTRUCTION_REGEX.Match(line)).Success)
                        if (is_func == 0)
                            Error("An instruction may only be used inside a function or after the '.main'-token.");
                        else
                        {
                            List<InstructionArgument> args = new List<InstructionArgument>();
                            string token = match.Groups["name"].ToString().ToLower();

                            if (!OPCodes.CodesByToken.ContainsKey(token))
                                return Error($"The instruction '{token}' could not be found.");
                            else if (token == "kernel")
                                return Error($"The instruction 'KERNEL' should not be used directly. Use '.kernel' or '.user' instead.");

                            foreach (string arg in (line = line.Remove(match.Index, match.Length).Trim()).Split(' ', ','))
                                if ((arg ?? "").Trim().Length > 0)
                                    if ((match = ARGUMENT_CORE.Match(arg)).Success)
                                    {
                                        InstructionArgument iarg = new InstructionArgument();
                                        string val;
                                        
                                        if (CheckGroup("ptr", out val))
                                        {
                                            iarg.Value = ParseIntArg(val);
                                            iarg.Type = ArgumentType.IndirectAddress;

                                            if (arg.StartsWith("k"))
                                                iarg.Type |= ArgumentType.KernelMode;
                                        }
                                        else if (CheckGroup("addr", out val))
                                        {
                                            iarg.Value = ParseIntArg(val);
                                            iarg.Type = ArgumentType.Address;

                                            if (arg.StartsWith("k"))
                                                iarg.Type |= ArgumentType.KernelMode;
                                        }
                                        else if (CheckGroup("float", out val))
                                        {
                                            iarg.Type = ArgumentType.Constant;
                                            iarg.Value = ParseFloatArg(val.ToLower());
                                        }
                                        else if (CheckGroup("const", out val))
                                        {
                                            iarg.Value = ParseIntArg(val);
                                            iarg.Type = ArgumentType.Constant;
                                        }
                                        else if (CheckGroup("name", out val))
                                        {
                                            var dic = functions.ToDictionary(_ => _.Name.ToLower(), _ => _.ID);

                                            val = val.ToLower();

                                            if (dic.ContainsKey(val))
                                            {
                                                iarg.Value = dic[val];
                                                iarg.Type = ArgumentType.Function;
                                            }
                                            else
                                            {
                                                iarg.Type = ArgumentType.Label;

                                                if (labels.ContainsKey(val))
                                                    iarg.Value = labels[val];
                                                else
                                                {
                                                    iarg.Value = labels[val] = ++id;
                                                    unmapped.Add((linenr, val, id));
                                                }
                                            }
                                        }
                                        else
                                            return Error($"The type of the argument '{arg}' could not be determined.");

                                        if (arg.Contains('$'))
                                            iarg.Type |= ArgumentType.Parameter;

                                        args.Add(iarg);
                                    }
                                    else
                                        return Error($"Invalid argument '{arg}' given.");

                            curr_func.Instructions.Add(new Instruction(OPCodes.CodesByToken[token], args.ToArray()));
                        }
                    else
                        return Error($"The line '{line}' could not be parsed.");
                }
            }

            functions.Add(curr_func);

            if (unmapped.Count > 0)
            {
                linenr = unmapped.First().Item1;

                return Error($"The label or function '{unmapped.First().Item2}' could not be found.");
            }
            else
                return (functions.ToArray(), -1, "");
        }

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions, which can be executed by a MCPU processor
        /// </summary>
        /// <param name="code">MCPU assembly code</param>
        /// <exception cref="MCPUCompilerException">Possible compiler errors</exception>
        /// <returns>Compiled instructions</returns>
        public static Instruction[] Compile(string code)
        {
            try
            {
                (MCPUFunction[] func, int errln, string errmsg) = Precompile(code);

                if ((errln != -1) || !string.IsNullOrEmpty(errmsg))
                    throw new MCPUCompilerException(errln, errmsg);

                MCPUFunction mainf = (from f in func where f.Name == MAIN_FUNCTION_NAME select f).First();
                Dictionary<int, int> jumptable = new Dictionary<int, int>();
                List<Instruction> instr = new List<Instruction>();
                int linenr = 1;

                instr.Add((OPCodes.JMP, new InstructionArgument[] { (0, ArgumentType.Label) }));

                foreach (MCPUFunction f in func.Where(f => f != mainf).Concat(new MCPUFunction[] { mainf }))
                {
                    jumptable[f.ID] = linenr;
                    
                    foreach (Instruction ins in f.Instructions)
                        if (ins.OPCode is MCPUJumpLabel)
                            jumptable[(ins.OPCode as MCPUJumpLabel).Value] = linenr;
                        else
                        {
                            instr.Add(ins);

                            linenr++;
                        }
                }

                Instruction[] cmp_instr = instr.ToArray();

                for (int i = 0, l = cmp_instr.Length; i < l; i++)
                    for (int j = 0, k = cmp_instr[i].Arguments.Length; j < k; j++)
                        if (cmp_instr[i].Arguments[j].IsInstructionSpace)
                            cmp_instr[i].Arguments[j].Value = jumptable[cmp_instr[i].Arguments[j].Value];
                
                return cmp_instr;
            }
            catch (Exception ex)
            when (!(ex is MCPUCompilerException))
            {
                throw new MCPUCompilerException(-1, $"{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions, which can be executed by a MCPU processor
        /// </summary>
        /// <param name="code">MCPU assembly code</param>
        /// <exception cref="MCPUCompilerException">Possible compiler errors</exception>
        /// <returns>Compiled instructions in byte format</returns>
        public static byte[] CompileToBinary(string code) => Instruction.SerializeMultiple(Compile(code));

        /// <summary>
        /// Decompiles the given instructions into readable MCPU assembly code
        /// </summary>
        /// <param name="instructions">Instructions</param>
        /// <returns>MCPU assembly code</returns>
        public static string Decompile(params Instruction[] instructions)
        {
            Dictionary<int, int> linetransl = new Dictionary<int, int>();
            List<Instruction> instr = new List<Instruction>();
            List<int> labels = new List<int>();
            int line = 0;
            int offs = 0;

            foreach (Instruction i in instructions)
            {
                if (i.OPCode is Instructions.nop)
                    --offs;
                else
                    instr.Add(i);

                linetransl.Add(line++, offs);
            }

            line = 0;
            instructions = (from i in instr
                            select new Instruction(i.OPCode, (from arg in i.Arguments
                                                              let ll = line++
                                                              select new Func<InstructionArgument>(delegate
                                                              {
                                                                  if (arg.IsInstructionSpace)
                                                                  {
                                                                      int nv = linetransl[arg] + arg;
                                                                      
                                                                      labels.Add(nv);

                                                                      return (nv, arg.Type);
                                                                  }
                                                                  else
                                                                      return arg;
                                                              })()).ToArray())).ToArray();

            int lblen = (int)Math.Ceiling(Math.Log(labels.Count, 26));
            int lbcnt = 0;

            string nextlabel()
            {
                char[] str = new char[lblen];

                for (int i = lblen - 1; i >= 0; i--)
                {
                    int div = lbcnt / (int)Math.Pow(26, i);
                    int mod = div % (int)Math.Pow(26, i + 1);
                    
                    str[i] = (char)('a' + mod);
                }

                ++lbcnt;

                return $"label_{new string(str)}";
            }

            Dictionary<int, string> jump_table = labels.Distinct().ToDictionary(_ => _, _ => nextlabel());
            StringBuilder sb = new StringBuilder();
            const int tab_wdh = 4;
            int index = 0;

            line = 0;

            string tostr(InstructionArgument arg, bool wasaddr = false)
            {
                string ret = "";

                if (arg.IsInstructionSpace)
                    return jump_table[arg];
                else if (arg.IsKernel)
                    ret = "k";

                arg.Type = arg.KernelInvariantType;

                if (arg.IsAddress)
                    return ret + $"[{tostr((arg.Value, arg.Type & ~ArgumentType.Address), true)}]";
                else if (arg.IsIndirect)
                    return ret + $"[{tostr((arg.Value, arg.Type & ~ArgumentType.Indirect), true)}]";
                else if (arg.IsParameter)
                    ret += '$';

                return ret + (wasaddr ? "0x" + arg.Value.ToString("x8") : arg.Value.ToString());
            }

            sb.AppendLine($"{new string(' ', tab_wdh)}.main");

            while (index < instructions.Length)
            {
                if (jump_table.ContainsKey(line))
                    sb.AppendLine($"{jump_table[line]}:"); // TODO : CORRECT LINE <-> INDEX OFFSET 
                else
                {
                    Instruction ins = instructions[index];

                    sb.Append(new string(' ', tab_wdh));

                    if (ins.OPCode is Instructions.kernel)
                        sb.Append('.')
                          .AppendLine((ins.Arguments?[0] ?? 0) == 0 ? "user" : "kernel");
                    else
                    {
                        sb.Append(ins.OPCode.Token);

                        foreach (InstructionArgument arg in ins.Arguments)
                            sb.Append(' ')
                              .Append(tostr(arg));

                        sb.AppendLine();
                    }

                    ++index;
                }

                ++line;
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Decompiles the given instruction byte array into readable MCPU assembly code
        /// </summary>
        /// <param name="instructions">Instruction byte array</param>
        /// <returns>MCPU assembly code</returns>
        public static string Decompile(byte[] instructions) => Decompile(Instruction.DeserializeMultiple(instructions));
    }

    /// <summary>
    /// Represents a MCPU compiler error
    /// </summary>
    public sealed class MCPUCompilerException
        : Exception
    {
        /// <summary>
        /// The line number, on which the error occured
        /// </summary>
        public int LineNr { get; }

        /// <summary>
        /// The error message
        /// </summary>
        public new string Message => base.Message;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="line">Error line number</param>
        /// <param name="msg">Compiler error message</param>
        public MCPUCompilerException(int line, string msg)
            : base(msg) => LineNr = line;
    }

    /// <summary>
    /// Represents a MCPU function definition
    /// </summary>
    public class MCPUFunction
    {
        /// <summary>
        /// The internal ID assigned to each function
        /// </summary>
        public int ID { set; get; }
        /// <summary>
        /// The function's name
        /// </summary>
        public string Name { internal set; get; }
        /// <summary>
        /// The function's instructions
        /// </summary>
        public List<Instruction> Instructions { get; }


        /// <summary>
        /// Creates a new function
        /// </summary>
        /// <param name="name">Function name</param>
        public MCPUFunction(string name)
            : this (name, null)
        {
        }

        /// <summary>
        /// Creates a new function
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="instr">Function instructions</param>
        public MCPUFunction(string name, params Instruction[] instr)
            : this(name, instr as IEnumerable<Instruction>)
        {
        }

        /// <summary>
        /// Creates a new function
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="instr">Function instructions</param>
        public MCPUFunction(string name, IEnumerable<Instruction> instr)
        {
            Name = name;
            Instructions = instr?.ToList() ?? new List<Instruction>();
        }
    }
    
    /// <summary>
    /// Represents a temporary instruction, which is only used during compile-time
    /// </summary>
    [OPCodeNumber(0xfffe)]
    internal sealed class MCPUJumpLabel
        : OPCode
    {
        internal int Value { get; }

        internal MCPUJumpLabel(int val)
            : base(0, delegate { }) => Value = val;
    }
}