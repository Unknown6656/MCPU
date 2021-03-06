﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;
using System;

using static MCPU.OPCodes;

using PrecompilerData = System.ValueTuple<MCPU.Compiler.MCPUFunction[], MCPU.Compiler.MCPULabelMetadata[], System.ValueTuple<int, int>[], int[], int, string>;

namespace MCPU.Compiler
{
    /// <summary>
    /// Provides functions to parse and compile given code segments to MCPU-compatible instructions
    /// </summary>
    public static class MCPUCompiler
    {
        /// <summary>
        /// The character sequence, which indicates a not yet implemented code snippet/token/segment
        /// </summary>
        public const string TODO_TOKEN = "__TODO__";
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
        public const string FLOAT_CORE = @"((\+|\-|)([0-9]+\.[0-9]*|[0-9]*\.[0-9]+)(e(\+|\-|)[0-9]+)?[fd]?|pi|e|phi|tau|π|τ|φ|f_(min|max|(n|p)inf)|nan)";
        /// <summary>
        /// Integer matching pattern
        /// </summary>
        public const string INTEGER_CORE = @"(\-?(0x[0-9a-f]+|[0-9a-f]+h|[0-9]+|0o[01]+|0b[0-7]+)|true|false|null|line|i_(min|max))";
        /// <summary>
        /// Interrupt handler function name matching pattern
        /// </summary>
        public static readonly Regex INT_HANDLER_REGEX = new Regex(@"\bint_(?<int>[0-9a-f]{2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Address matching pattern
        /// </summary>
        public static readonly string ADDRESS_CORE = $@"(k?\[\$?(?<addr>{INTEGER_CORE})\]|k?\[\[\$?(?<ptr>{INTEGER_CORE})\]\])";
        /// <summary>
        /// Data assignment matching pattern
        /// </summary>
        public static readonly Regex DATA_REGEX = new Regex($@"(?<kernel>k)?\[(?<addr>{INTEGER_CORE})\]\s*\=\s*(?<value>{INTEGER_CORE})", RegexOptions.IgnoreCase);
        /// <summary>
        /// Argument core matching pattern
        /// </summary>
        public static readonly Regex ARGUMENT_CORE = new Regex($@"((?<float>{FLOAT_CORE})|{ADDRESS_CORE}|\$?(?<const>{INTEGER_CORE})|\<\s*(?<opref>[_a-z]*[a-z]+\w*)\s*\>|{NAME_REGEX_CORE})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        public static readonly Regex FUNC_REGEX = new Regex($@"((?<inline>\.inline)\s+|(?<int>interrupt)\s+|\b)func\s+{NAME_REGEX_CORE}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        /// <summary>
        /// Contains an enumeration of compiler constants
        /// </summary>
        public static readonly Dictionary<string, string> Constants = new Dictionary<string, string>
        {
            ["line"] = null,
            ["i_max"] = int.MaxValue.ToString(),
            ["i_min"] = int.MinValue.ToString(),
            ["nan"] = float.NaN.ToString(),
            ["f_max"] = float.MaxValue.ToString(),
            ["f_min"] = float.MinValue.ToString(),
            ["f_pinf"] = float.PositiveInfinity.ToString(),
            ["f_ninf"] = float.NegativeInfinity.ToString(),
            ["epsilon"] = float.Epsilon.ToString(),
            ["τ"] = FC_τ.ToString(),
            ["tau"] = FC_τ.ToString(),
            ["π"] = FC_π.ToString(),
            ["pi"] = FC_π.ToString(),
            ["φ"] = FC_φ.ToString(),
            ["phi"] = FC_φ.ToString(),
            ["e"] = FC_e.ToString(),
        };
        internal static readonly string[] __reserved = (from opc in CodesByID
                                                        where opc.Value.IsKeyword
                                                        select opc.Value.Token.ToLower()).Concat(new string[] { "func", "end", MAIN_FUNCTION_NAME, "line", "epsilon", "tau", "phi", "e", "pi", "i_max", "f_max", "i_min", "f_min", "f_pinf", "f_ninf", "nan", "interrupt", "data" }).ToArray();
        internal static readonly Dictionary<string, string> __defstrtable = new Dictionary<string, string>
        {
            ["JMP_INSIDE_FUNC"] = "A jump label may only be used inside a function or after the '.main'-token.",
            ["FUNC_ALREADY_EXISTS_SP"] = "A function called '{0}' does already exist.",
            ["LABEL_ALREADY_EXISTS_SP"] = "The label '{0}' does already exist on line {1}.",
            ["TOKEN_NOT_PARSED"] = "The token '.{0}' could not be parsed.",
            ["TOKEN_INSIDE_FUNC"] = "The token '.{0}' can only be used inside a function or after the '.main'-declaration.",
            ["FUNC_NOT_NESTED"] = "Functions cannot be nested. Please close the current function with an 'END FUNC'-token.",
            ["FUNC_AFTER_MAIN"] = "Functions cannot be declared after the '.main'-token.",
            ["FUNC_ALREADY_EXISTS"] = "The function '{0}' does already exist.",
            ["LABEL_ALREADY_EXISTS"] = "A label called '{0}' does already exist.",
            ["MISSING_FUNC_DECL"] = "A function declaration must precede an 'END FUNC'-token.",
            ["INSTR_OUTSIDE_FUNC"] = "An instruction may only be used inside a function or after the '.main'-token.",
            ["INSTR_NFOUND"] = "The instruction '{0}' could not be found.",
            ["DONT_USE_KERNEL"] = "The instruction 'kernel' should not be used directly. Use '.kernel' or '.user' instead.",
            ["ARGTYPE_NDET"] = "The type of the argument '{0}' could not be determined.",
            ["INVALID_ARG"] = "Invalid argument '{0}' given.",
            ["LABEL_FUNC_NFOUND"] = "The label or function '{0}' could not be found.",
            ["LINE_NPARSED"] = "The line '{0}' could not be parsed.",
            ["MAIN_TOKEN_MISSING"] = "The '.main'-token is missing.",
            ["INLINE_NYET_SUPP"] = "'.inline' not supported yet",
            ["FUNC_RESV_NAME"] = "The name '{0}' is reserved and can therefore not be used as function name.",
            ["LABEL_RESV_NAME"] = "The name '{0}' is reserved and can therefore not be used as label name.",
            ["NEED_MORE_ARGS"] = "The OP-code '{0}' requires at least '{1}' arguments.",
            ["COULDNT_TRANSLATE_EXEC"] = "The 'exec'-expression could not be translated.",
            ["INVALID_RET"] = "The 'ret'-instruction cannot be used after the '.main'-token due to a StackUnderflow during runtime. Consider the usage of 'halt'.",
            ["INVALID_MAIN_TOKEN"] = "The '.main'-token cannot be used inside a method or after an other '.main'-token.",
            ["TOKEN_NOT_ENOUGH_ARGS"] = "The token '.{0}' requires at least {1} argument(s).",
            ["TOKEN_UNKNOWN_SWITCH"] = "The switch '{0}' is either unknown or cannot be used using the '.enable'/'.disable'-token.",
            ["DONT_USE_INTERRUPT"] = "The instruction 'interrupt' should not be used directly. Use '.enable interrupt' or '.disable interrupt' instead.",
            ["DONT_USE_INTERRUPTTABLE"] = "The instruction 'interrupttable' should not be used directly. Declare interrupt handler functions to build the interrupt-table instead.",
            ["INVALID_INT_HANDLER_NAME"] = "The function name '{0}' is invalid for an interrupt handler routine. It should have the format 'int_xx' where 'xx' represents the hexadecimal interrupt code.",
            ["FUNC_RESV_NAME_INT"] = "The name '{0}' is reserved for interrupt handler functions and can therefore not be used as a regular function name.",
            ["DATA_INSIDE_FUNC"] = "The '.data'-section token cannot be used inside a function.",
            ["DATA_OUTSIDE_SECTION"] = "The data assignment expression should be used after a '.data'-section token.",
        };
        internal static Dictionary<string, string> __strtable;

        /// <summary>
        /// Sets or gets, whether compiler optimizations are enabled (NOP-optimization, empty statements, inlining etc.)
        /// </summary>
        public static bool OptimizationEnabled { set; get; } = false;
        /// <summary>
        /// Returns a list of all reserved MCPU keywords
        /// </summary>
        public static string[] ReservedKeywords => __reserved;


        static MCPUCompiler() => ResetLanguage();

        /// <summary>
        /// Resets the compiler's output language to the default one
        /// </summary>
        public static void ResetLanguage() => SetLanguage(__defstrtable);

        /// <summary>
        /// Sets the compiler's output language to the given one
        /// </summary>
        /// <param name="stringtable">Language represented by the given StringTable</param>
        public static void SetLanguage(Dictionary<string, string> stringtable)
        {
            if (stringtable != null)
                __strtable = stringtable;
        }

        /// <summary>
        /// Returns the language-specific string associated with the given key
        /// </summary>
        /// <param name="key">String key</param>
        /// <returns>String value</returns>
        public static string GetString(string key) => __strtable[key];

        /// <summary>
        /// Returns the formatted language-specific string associated with the given key
        /// </summary>
        /// <param name="key">String key</param>
        /// <returns>Formatted string value</returns>
        public static string GetString(string key, params object[] args) => string.Format(GetString(key), args);

        internal static unsafe PrecompilerData Precompile(params string[] lines)
        {
            List<(int, string, int)> unmapped = new List<(int, string, int)>();
            List<MCPULabelMetadata> labelmeta = new List<MCPULabelMetadata>();
            Dictionary<string, int> labels = new Dictionary<string, int>();
            Dictionary<int, int> init_data = new Dictionary<int, int>();
            List<MCPUFunction> functions = new List<MCPUFunction>();
            List<int> ignore = new List<int>();
            int is_func = 0, linenr = 0, id = 0;
            MCPUFunction curr_func = null;
            Match match;

            for (int l = lines.Length; linenr < l; ++linenr)
            {
                int ParseIntArg(string s)
                {
                    int intparse(string arg)
                    {
                        switch (arg)
                        {
                            case "i_max":
                                return int.MaxValue;
                            case "i_min":
                                return int.MinValue;
                            case "line":
                                return linenr + 1;
                            default:
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

                string line = lines[linenr];

                if (line.Contains(COMMENT_START))
                    line = line.Remove(line.IndexOf(COMMENT_START));

                line = line.Trim();

                if (line.Length == 0)
                {
                    ignore.Add(linenr);

                    continue; // we need this condition to be consistent with the original line numbers
                }

                if ((match = LABEL_REGEX.Match(line)).Success)
                    if (is_func == 0)
                        Error(GetString("JMP_INSIDE_FUNC"));
                    else
                    {
                        string name = match.Groups["name"].ToString().ToLower();
                        (int, string, int) um = unmapped.Find(_ => _.Item2 == name);
                        int tid = 0;

                        if (FindFirst(name) != null)
                            return Error(GetString("FUNC_ALREADY_EXISTS_SP", name));

                        if (um.Item2 != name)
                            if (labels.ContainsKey(name))
                                return Error(GetString("LABEL_ALREADY_EXISTS_SP", name, labels.First(_ => _.Key == name).Value + 1));
                            else
                                labels[name] = tid = ++id;
                        else
                        {
                            labels[name] = tid = um.Item3;

                            if (unmapped.Contains(um))
                                unmapped.Remove(um);
                        }

                        line = line.Remove(match.Index, match.Length);

                        labelmeta.Add(new MCPULabelMetadata { Name = name, DefinedLine = linenr, ParentFunction = curr_func });
                        curr_func.Instructions.Add((new MCPUJumpLabel(tid), linenr));
                    }

                if ((line = line.Trim()).Length > 0)
                {
                    if (line.StartsWith(".") && !line.ToLower().StartsWith(".inline"))
                    {
                        string[] args = line.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim()).ToArray();
                        bool inside_func = (is_func != 0) && (is_func != -2);

                        switch (line = args[0].Remove(0, 1))
                        {
                            case "main":
                                if (inside_func)
                                    return Error(GetString("INVALID_MAIN_TOKEN"));

                                curr_func = new MCPUFunction(MAIN_FUNCTION_NAME) { ID = 0, DefinedLine = linenr };
                                curr_func.Instructions.Add((NOP, linenr));
                                is_func = -1;

                                continue;
                            case "kernel" when inside_func:
                                curr_func.Instructions.Add(((KERNEL, new InstructionArgument[] { 1 }), linenr));

                                continue;
                            case "user" when inside_func:
                                curr_func.Instructions.Add(((KERNEL, new InstructionArgument[] { 0 }), linenr));

                                continue;
                            case "enable" when inside_func:
                            case "disable" when inside_func:
                                PrecompilerData? err = EnableDisable(line == "enable");

                                if (err == null)
                                    continue;
                                else
                                    return err.Value;
                            case "data":
                                if (is_func != 0)
                                    return Error(GetString("DATA_INSIDE_FUNC"));

                                is_func = -2;

                                continue;
                            default:
                                return Error(GetString(is_func != -1 ? "TOKEN_NOT_PARSED" : "TOKEN_INSIDE_FUNC", line));
                        }

                        PrecompilerData? EnableDisable(bool enable)
                        {
                            if (args.Length < 2)
                                return Error(GetString("TOKEN_NOT_ENOUGH_ARGS", enable ? "enable" : "disable", 1));

                            switch (args[1])
                            {
                                case "interrupt":
                                    curr_func.Instructions.Add(((INTERRUPT, new InstructionArgument[] { enable ? 1 : 0 }), linenr));

                                    return null;
                                // TODO : more options?
                                default:
                                    return Error(GetString("TOKEN_UNKNOWN_SWITCH", args[0] ?? ""));
                            }
                        }
                    }
                    else if ((match = FUNC_REGEX.Match(line)).Success)
                        if (is_func == 1)
                            return Error(GetString("FUNC_NOT_NESTED"));
                        else if (is_func == -1)
                            return Error(GetString("FUNC_AFTER_MAIN"));
                        else
                        {
                            bool inline = match.Groups["inline"]?.ToString()?.ToLower()?.Contains("inline") ?? false;
                            bool interrupt = match.Groups["int"]?.ToString()?.ToLower()?.Contains("interrupt") ?? false;
                            string name = match.Groups["name"].ToString().ToLower();
                            (int, string, int) um = unmapped.Find(_ => _.Item2 == name);
                            int tid;

                            if (name == MAIN_FUNCTION_NAME)
                                return Error(GetString("FUNC_RESV_NAME", name));
                            if (FindFirst(name) != null)
                                return Error(GetString("FUNC_ALREADY_EXISTS", name));

                            if ((um.Item2 != name) && labels.ContainsKey(name))
                                return Error(GetString("LABEL_ALREADY_EXISTS", name));

                            if (um.Item2 == name)
                            {
                                unmapped.Remove(um);
                                tid = um.Item3;
                            }
                            else
                                tid = ++id;

                            byte? intnr = null;

                            if (interrupt)
                                if ((match = INT_HANDLER_REGEX.Match(name)).Success)
                                    intnr = byte.Parse(match.Groups["int"].ToString(), NumberStyles.HexNumber);
                                else
                                    return Error(GetString("INVALID_INT_HANDLER_NAME", name));
                            else if (INT_HANDLER_REGEX.Match(name).Success)
                                return Error(GetString("FUNC_RESV_NAME_INT", name));

                            curr_func = new MCPUFunction(name, (NOP, linenr))
                            {
                                ID = tid,
                                IsInlined = inline,
                                DefinedLine = linenr,
                                InterruptNumber = intnr,
                            };
                            is_func = 1;

                            functions.Add(curr_func);
                        }
                    else if ((match = END_FUNC_REGEX.Match(line)).Success)
                        if ((is_func != 0) && (is_func != -2))
                        {
                            ignore.Add(linenr);

                            curr_func.Instructions.Add((is_func == -1 ? HALT : RET as OPCode, linenr));
                            curr_func = (from f in functions where f.Name == MAIN_FUNCTION_NAME select f).FirstOrDefault();
                            is_func = 0;
                        }
                        else
                            return Error(GetString("MISSING_FUNC_DECL"));
                    else if ((match = DATA_REGEX.Match(line)).Success)
                        if (is_func == -2)
                        {
                            int value = ParseIntArg(match.Groups["value"]?.ToString() ?? "");
                            int addr = ParseIntArg(match.Groups["addr"]?.ToString() ?? "");
                            int offs = (match.Groups["kernel"]?.ToString() ?? "").ToLower() == "k" ? 0 : Processor.MEM_OFFS;

                            init_data[addr + (offs / 4)] = value;
                        }
                        else
                            return Error(GetString("DATA_OUTSIDE_SECTION"));
                    else if ((match = INSTRUCTION_REGEX.Match(line)).Success)
                        if ((is_func == 0) && (is_func == -2))
                            return Error(GetString("INSTR_OUTSIDE_FUNC"));
                        else
                        {
                            List<InstructionArgument> args = new List<InstructionArgument>();
                            string token = match.Groups["name"].ToString().ToLower();

                            if (!CodesByToken.ContainsKey(token))
                                return Error(GetString("INSTR_NFOUND", token));

                            foreach (string arg in (line = line.Remove(match.Index, match.Length).Trim()).Split(' ', ','))
                                if ((arg ?? "").Trim().Length > 0)
                                    if ((match = ARGUMENT_CORE.Match(arg)).Success)
                                        try
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
                                                if (arg.Contains('[') || arg.Contains(']'))
                                                    return Error(GetString("INVALID_ARG", arg));

                                                iarg.Type = ArgumentType.Constant;
                                                iarg.Value = ParseFloatArg(val.ToLower());
                                            }
                                            else if (CheckGroup("const", out val))
                                            {
                                                iarg.Value = ParseIntArg(val);
                                                iarg.Type = ArgumentType.Constant;
                                            }
                                            else if (CheckGroup("opref", out val))
                                                try
                                                {
                                                    iarg.Type = ArgumentType.Constant;
                                                    iarg.Value = CodesByToken[val.ToLower().Trim()].Number;
                                                }
                                                catch
                                                {
                                                    return Error(GetString("COULDNT_TRANSLATE_EXEC"));
                                                }
                                            else if (CheckGroup("name", out val))
                                            {
                                                if (arg.Contains('[') || arg.Contains(']'))
                                                    return Error(GetString("INVALID_ARG", arg));

                                                Dictionary<string, int> dic = functions.ToDictionary(_ => _.Name.ToLower(), _ => _.ID);

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
                                                return Error(GetString("ARGTYPE_NDET", arg));

                                            if (arg.Contains('$'))
                                                iarg.Type |= ArgumentType.Parameter;

                                            args.Add(iarg);
                                        }
                                        catch
                                        {
                                            return Error(GetString("INVALID_ARG", arg));
                                        }
                                    else
                                        return Error(GetString("INVALID_ARG", arg));

                            OPCode opc = CodesByToken[token];

                            if (opc == INTERRUPT)
                                return Error(GetString("DONT_USE_INTERRUPT"));
                            if (opc == INTERRUPTTABLE)
                                return Error(GetString("DONT_USE_INTERRUPTTABLE"));
                            else if (opc == KERNEL)
                                return Error(GetString("DONT_USE_KERNEL"));

                            while ((opc == EXEC) && (args.Count > 0) && (args[0].Type == ArgumentType.Constant))
                                try
                                {
                                    opc = CodesByID[(ushort)args[0]];
                                    args.RemoveAt(0);
                                }
                                catch
                                {
                                    return Error(GetString("COULDNT_TRANSLATE_EXEC"));
                                }

                            if (opc.RequiredArguments > args.Count)
                                return Error(GetString("NEED_MORE_ARGS", opc.Token, opc.RequiredArguments));
                            else if ((opc == RET) && (curr_func.Name == MAIN_FUNCTION_NAME))
                                return Error(GetString("INVALID_RET"));

                            if (curr_func == null)
                                return Error(GetString("INSTR_OUTSIDE_FUNC"));

                            curr_func.Instructions.Add((new Instruction(opc, args.ToArray()), linenr));
                        }
                    else
                        return Error(GetString("LINE_NPARSED", line));
                }
            }

            functions.Add(curr_func);

            if (unmapped.Count > 0)
            {
                linenr = unmapped[0].Item1;

                return Error(GetString("LABEL_FUNC_NFOUND", unmapped[0].Item2));
            }
            else
                return (functions.ToArray(), labelmeta.ToArray(), (from kvp in init_data
                                                                   select (kvp.Key, kvp.Value)).ToArray(), ignore.ToArray(), -1, "");

            PrecompilerData Error(string message) => (null, null, null, null, linenr + 1, message);

            MCPUFunction FindFirst(string name) => (from f in functions where f.Name == name select f).FirstOrDefault();

            bool CheckGroup(string name, out string value) => (value = match.Groups[name].ToString().Trim()).Length > 0;

            unsafe int ParseFloatArg(string s)
            {
                switch (s)
                {
                    case "nan":
                        return (FloatIntUnion)float.NaN;
                    case "f_max":
                        return (FloatIntUnion)float.MaxValue;
                    case "f_min":
                        return (FloatIntUnion)float.MinValue;
                    case "f_pinf":
                        return (FloatIntUnion)float.PositiveInfinity;
                    case "f_ninf":
                        return (FloatIntUnion)float.NegativeInfinity;
                    case "epsilon":
                        return (FloatIntUnion)float.Epsilon;
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
                        return (FloatIntUnion)float.Parse(s//.Replace('.', ',')
                                                           .Replace('f', 'd')
                                                           .Replace("d", ""));
                }
            }
        }

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions and metadata, which can be executed by a MCPU processor or analyzed by an IDE
        /// </summary>
        /// <param name="code">MCPU assembly code</param>
        /// <exception cref="MCPUCompilerException">Possible compiler errors</exception>
        /// <returns>Compiled instructions and metadata</returns>
        public static MCPUCompilerResult CompileWithMetadata(string code) => CompileWithMetadata((code ?? "").Split('\n'));

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions and metadata, which can be executed by a MCPU processor or analyzed by an IDE
        /// </summary>
        /// <param name="lines">MCPU assembly code lines</param>
        /// <exception cref="MCPUCompilerException">Possible compiler errors</exception>
        /// <returns>Compiled instructions and metadata</returns>
        public static MCPUCompilerResult CompileWithMetadata(params string[] lines)
        {
            try
            {
                (MCPUFunction[] func, MCPULabelMetadata[] labels, (int, int)[] init_data, int[] ignored, int errln, string errmsg) = Precompile(lines);

                if ((errln != -1) || !string.IsNullOrEmpty(errmsg))
                    throw new MCPUCompilerException(errln, errmsg);

                MCPUFunction mainf = (from f in func where f?.Name == MAIN_FUNCTION_NAME select f).FirstOrDefault();

                if (mainf == null)
                    throw new MCPUCompilerException(0, GetString("MAIN_TOKEN_MISSING"));

                MCPUFunctionMetadata[] metadata = new MCPUFunctionMetadata[func.Length];
                Dictionary<byte, int> interrupttable = new Dictionary<byte, int>();
                List<(Instruction, int)> instr = new List<(Instruction, int)>();
                Dictionary<int, int> unused_isl = new Dictionary<int, int>();
                Dictionary<int, int> jumptable = new Dictionary<int, int>();
                List<int> rm = new List<int>();
                int linenr = 1, fnr = 0;

                instr.Add(((JMP, new InstructionArgument[] { (0, ArgumentType.Label) }), -1));

                foreach (MCPULabelMetadata l in labels)
                    if (__reserved.Contains(l.Name.ToLower()))
                        throw new MCPUCompilerException(l.DefinedLine + 1, GetString("LABEL_RESV_NAME", l.Name));

                foreach (MCPUFunction f in func.Where(f => f != mainf).Concat(new MCPUFunction[] { mainf }))
                    if (__reserved.Contains(f.Name.ToLower()) && (f != mainf))
                        throw new MCPUCompilerException(f.DefinedLine + 1, GetString("FUNC_RESV_NAME", f.Name));
                    else
                    {
                        unused_isl[f.ID] = f.DefinedLine;
                        jumptable[f.ID] = linenr + 1;
                        metadata[fnr++] = f;

                        if (f.InterruptNumber != null)
                            interrupttable[f.InterruptNumber.Value] = f.ID;

                        if (f.IsInlined)
                        {
                            bool caninline = (f.Instructions.Count <= 30) && f.Instructions.All(_ => (_.Item1.OPCode == RET) || !_.Item1.OPCode.SpecialIPHandling);

                            if (caninline && OptimizationEnabled)
                            {
                                // MAGIC GOES HERE

                                throw new NotImplementedException(GetString("INLINE_NYET_SUPP"));
                                continue;
                            }
                        }

                        bool canoptimize = false;
                        int tailopt = (from t in (f.Instructions as IEnumerable<(Instruction, int)>).Reverse()
                                       select t.Item1).TakeWhile(_ => f == mainf ? _ == HALT : _ == RET).Count();
                        int fdiff = f == mainf ? 0 : 1;

                        tailopt -= fdiff;

                        if ((tailopt > 0) && OptimizationEnabled)
                            for (int i = 0, l = f.Instructions.Last().Item2 - fdiff; i < tailopt; i++)
                                rm.Add(l - i);

                        foreach ((Instruction ins, int ol) in f.Instructions)
                            if (ins.OPCode is MCPUJumpLabel jmpl)
                            {
                                jumptable[jmpl.Value] = linenr;
                                unused_isl[jmpl.Value] = ol;
                                canoptimize = false;
                            }
                            else
                            {
                                if (canoptimize && OptimizationEnabled)
                                    rm.Add(ol);
                                else
                                {
                                    instr.Add((ins, ol));

                                    linenr++;
                                }

                                if ((ins.OPCode == RET) ||
                                    (ins.OPCode == HALT) ||
                                    (ins.OPCode == RESET))
                                    canoptimize = true;
                            }
                    }

                (Instruction, int)[] cmp_instr = instr.ToArray();

                for (int i = 0, l = cmp_instr.Length; i < l; i++)
                    for (int j = 0, k = cmp_instr[i].Item1.Arguments.Length; j < k; j++)
                        if (cmp_instr[i].Item1.Arguments[j].IsInstructionSpace)
                        {
                            int val = cmp_instr[i].Item1.Arguments[j].Value;

                            cmp_instr[i].Item1.Arguments[j].Value = jumptable[val];

                            unused_isl.Remove(val);
                        }

                (Instruction[] inst, int[] opt_lines, Dictionary<int, (int, bool)> offset_table) = Optimize(cmp_instr);

                foreach (byte b in interrupttable.Keys.ToArray())
                    if (offset_table.ContainsKey(interrupttable[b]))
                        interrupttable[b] -= offset_table[interrupttable[b]].Item1;

                List<Instruction> header = new List<Instruction>();

                header.Add((KERNEL, new InstructionArgument[] { 1 }));
                header.Add((INTERRUPTTABLE, (from kvp in interrupttable
                                             let fline = jumptable[kvp.Value]
                                             let nline = fline - (offset_table.ContainsKey(fline) ? offset_table[fline].Item1 : 0)
                                             where nline != 0 // prevent endless loops
                                             select (InstructionArgument)((kvp.Key << 24) | (nline & 0x00ffffff))).ToArray()));
                header.AddRange(from (int addr, int value) _ in init_data
                                select (Instruction)(MOV, new InstructionArgument[] { (_.addr, ArgumentType.KernelMode | ArgumentType.Address), (_.value, ArgumentType.Constant) }));
                header.Add((KERNEL, new InstructionArgument[] { 0 }));
                header[1] = (header[1].OPCode, (from a in header[1].Arguments
                                                let b = (uint)(a & 0xff000000)
                                                let c = a & 0x00ffffff
                                                select (InstructionArgument)(int)(b | (uint)(a + header.Count))).ToArray());

                foreach (byte b in interrupttable.Keys.ToArray())
                    interrupttable[b] += header.Count;

                inst = header.Concat(from i in inst
                                     select new Func<Instruction>(() =>
                                     {
                                         InstructionArgument[] args = i.Arguments;

                                         for (int j = 0; j < args.Length; j++)
                                             if (args[j].IsInstructionSpace)
                                                 args[j].Value += header.Count;

                                         return (i.OPCode, args);
                                     })()).ToArray();

                return (inst, (from o in opt_lines.Union(rm)
                                                  .Union(unused_isl.Values)
                                                  .Except(ignored)
                               where func.All(f => f.DefinedLine != o)
                               select o).ToArray(), metadata, labels, interrupttable);
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
        /// <returns>Compiler result</returns>
        public static Union<MCPUCompilerResult, MCPUCompilerException> Compile(string code) => Compile((code ?? "").Split('\n'));

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions, which can be executed by a MCPU processor
        /// </summary>
        /// <param name="lines">MCPU assembly code lines</param>
        /// <returns>Compiler result</returns>
        public static Union<MCPUCompilerResult, MCPUCompilerException> Compile(params string[] lines)
        {
            try
            {
                return CompileWithMetadata(lines);
            }
            catch (MCPUCompilerException ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Optimizes the given instruction list 
        /// </summary>
        /// <param name="instr">Instructions</param>
        /// <returns>Optimized instructions</returns>
        public static (Instruction[], int[], Dictionary<int, (int, bool)>) Optimize(params (Instruction, int)[] instr)
        {
            bool CanBeRemoved(Instruction i)
            {
                try
                {
                    bool In(params OPCode[] opc) => opc.Any(o => o == i);

                    if (i == EXEC)
                        return CanBeRemoved((OPCodes.CodesByID[(ushort)i[0].Value], i.Arguments.Skip(1).ToArray()));
                    else
                        return (i == NOP)
                            || (In(MOV, SWAP, OR, AND)                                          && i[0] == i[1])
                            || (In(JMPREL)                                                      && i[0] == (1, ArgumentType.Constant))
                            || (In(WAIT)                                                        && i[0] == (0, ArgumentType.Constant))
                            || (In(ADD, SUB, CLEAR, OR, XOR, FSUB, FADD, ROL, ROR, SHL, SHR)    && i[1] == (0, ArgumentType.Constant))
                            || (In(MUL, DIV)                                                    && i[1] == (1, ArgumentType.Constant))
                            || (In(AND, NXOR)                                                   && i[1] == (unchecked((int)0xffffffffu), ArgumentType.Constant))
                            || (In(FMUL, FDIV, FPOW, FROOT)                                     && i[1] == ((FloatIntUnion)1f, ArgumentType.Constant))
                            || (In(COPY)                                                        && i[2] == (0, ArgumentType.Constant));
                }
                catch
                {
                    return false;
                }
            }

            if (!OptimizationEnabled)
                return ((from i in instr select i.Item1).ToArray(), new int[0], new Dictionary<int, (int, bool)>());

            Dictionary<int, (int, bool)> offset_table = Enumerable.Range(0, instr.Length).ToDictionary(_ => _, _ => (0, false));
            List<Instruction> outp = new List<Instruction>();
            List<int> rm = new List<int>();
            int cnt = 0, line = 0;

            foreach ((Instruction i, int l) in instr)
            {
                bool rem = CanBeRemoved(i);

                if (rem)
                {
                    rm.Add(l);
                    ++cnt;
                }

                offset_table[line++] = (cnt, rem);
            }

            // two separate loops, or the look-ahead won't work
            for (int i = 0, l = instr.Length; i < l; i++)
                if (!offset_table[i].Item2)
                    outp.Add((instr[i].Item1.OPCode, (from arg in instr[i].Item1.Arguments
                                                      let na = arg.IsInstructionSpace ? (InstructionArgument)(arg.Value - offset_table[arg.Value].Item1, arg.Type) : arg
                                                      select na).ToArray()));

            return (outp.ToArray(), (from l in rm
                                     where l >= 0
                                     select l).ToArray(), offset_table);
        }

        /// <summary>
        /// Compiles the given MCPU assembly code to a list of instructions, which can be executed by a MCPU processor
        /// </summary>
        /// <param name="code">MCPU assembly code</param>
        /// <exception cref="MCPUCompilerException">Possible compiler errors</exception>
        /// <returns>Compiled instructions in byte format</returns>
        public static byte[] CompileToBinary(string code) => Compile(code).Match(res => Instruction.SerializeMultiple(res.Instructions), ex => null as byte[] ?? throw ex);

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

            int lblen = (int)Math.Ceiling(Math.Log(labels.Count + 1, 27));
            int lbcnt = 1;

            string nextlabel()
            {
                char[] str = new char[lblen];

                for (int i = lblen - 1; i >= 0; i--)
                {
                    int div = lbcnt / (int)Math.Pow(27, i);
                    int mod = div % (int)Math.Pow(27, i + 1);

                    str[i] = (char)(mod == 0 ? '\0' : '`' + mod);
                }

                ++lbcnt;

                return $"label_{new string(str).Replace("\0", "")}";
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
            string lineind() => $"line_{line:x4}:";

            sb.AppendLine($"main:{new string(' ', tab_wdh + 5)}.main");

            while (index < instructions.Length)
            {
                if (jump_table.ContainsKey(line))
                    sb.AppendLine($"{jump_table[line]}:"); // TODO : CORRECT LINE <-> INDEX OFFSET 
                else
                {
                    Instruction ins = instructions[index];

                    sb.Append(lineind())
                      .Append(new string(' ', tab_wdh));

                    if (ins.OPCode is Instructions.kernel)
                        sb.Append('.')
                          .AppendLine((ins.Arguments?[0] ?? 0) == 0 ? "user" : "kernel");
                    else if (ins.OPCode is Instructions.interrupt)
                        sb.AppendLine($".{((ins.Arguments?[0] ?? 0) == 0 ? "dis" : "en")}able interrupt");
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
    /// Represents public MCPU label metadata
    /// </summary>
    public struct MCPULabelMetadata
    {
        /// <summary>
        /// The label's name
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The function, in which the label has been defined
        /// </summary>
        public MCPUFunctionMetadata ParentFunction { set; get; }
        /// <summary>
        /// The (0-based) line, in which the label has been defined (The line number is one-based -- NOT zero-based)
        /// </summary>
        public int DefinedLine { set; get; }
    }

    /// <summary>
    /// Represents public MCPU function metadata
    /// </summary>
    public struct MCPUFunctionMetadata
    {
        /// <summary>
        /// The function's name
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// Sets or gets whether the function is inlined
        /// </summary>
        public bool Inlined { set; get; }
        /// <summary>
        /// Sets or gets the interrupt number, which the current function is the handler of (or null, if the function isn't an interrupt handler)
        /// </summary>
        public byte? InterruptHandler { set; get; }
        /// <summary>
        /// The (0-based) line, in which the function has been defined (The line number is one-based -- NOT zero-based)
        /// </summary>
        public int DefinedLine { set; get; }
    }

    /// <summary>
    /// Represents a successful MCPU compiler result
    /// </summary>
    public struct MCPUCompilerResult
    {
        /// <summary>
        /// The (1-based) line numbers, which can be optimized
        /// </summary>
        public int[] OptimizedLines { set; get; }
        /// <summary>
        /// The generated instructions
        /// </summary>
        public Instruction[] Instructions { set; get; }
        /// <summary>
        /// Reflected label information
        /// </summary>
        public MCPULabelMetadata[] Labels { set; get; }
        /// <summary>
        /// Reflected function information
        /// </summary>
        public MCPUFunctionMetadata[] Functions { set; get; }
        /// <summary>
        /// The interrupt handler table
        /// </summary>
        public Dictionary<byte, int> InterruptTable { set; get; }

        public static implicit operator (Instruction[], int[], MCPUFunctionMetadata[], MCPULabelMetadata[], Dictionary<byte, int>) (MCPUCompilerResult res) => (res.Instructions, res.OptimizedLines, res.Functions, res.Labels, res.InterruptTable);
        public static implicit operator MCPUCompilerResult((Instruction[], int[], MCPUFunctionMetadata[], MCPULabelMetadata[], Dictionary<byte, int>) _) => new MCPUCompilerResult { Instructions = _.Item1, OptimizedLines = _.Item2, Functions = _.Item3, Labels = _.Item4, InterruptTable = _.Item5 };
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
        /// The (0-based) line, in which the function has been defined
        /// </summary>
        public int DefinedLine { set; get; }
        /// <summary>
        /// The function's name
        /// </summary>
        public string Name { internal set; get; }
        /// <summary>
        /// Determines, whether the function is inlined during the compiling process
        /// </summary>
        public bool IsInlined { internal set; get; }
        /// <summary>
        /// Sets or gets the interrupt number, which the current function is the handler of (or null, if the function isn't an interrupt handler)
        /// </summary>
        public byte? InterruptNumber { internal set; get; }
        /// <summary>
        /// The function's instructions
        /// </summary>
        public List<(Instruction, int)> Instructions { get; }


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
        public MCPUFunction(string name, params (Instruction, int)[] instr)
            : this(name, instr as IEnumerable<(Instruction, int)>)
        {
        }

        /// <summary>
        /// Creates a new function
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="instr">Function instructions</param>
        public MCPUFunction(string name, IEnumerable<(Instruction, int)> instr)
        {
            Name = name;
            Instructions = instr?.ToList() ?? new List<(Instruction, int)>();
        }


        public static implicit operator MCPUFunctionMetadata(MCPUFunction func) => new MCPUFunctionMetadata
        {
            Name = func.Name,
            Inlined = func.IsInlined,
            DefinedLine = func.DefinedLine,
            InterruptHandler = func.InterruptNumber
        };
    }

    /// <summary>
    /// Represents a temporary instruction, which is only used during compile-time
    /// </summary>
    [OPCodeNumber(0xdead)]
    internal sealed class MCPUJumpLabel
        : OPCode
    {
        internal int Value { get; }

        internal MCPUJumpLabel(int val)
            : base(0, delegate { }) => Value = val;
    }
}
