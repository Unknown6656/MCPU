using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        internal const string FLOAT_CORE = @"";

        internal const string INTEGER_CORE = @"(\-?(0x[0-9a-f]+|[0-9a-f]+h|[0-9]+|0b[01]+)|true|false|null)";

        internal static readonly Regex ARGUMENT_CORE = new Regex($@"(\$?(\[(?<addr>{INTEGER_CORE})\]|\[\[(?<ptr>{INTEGER_CORE})\]\]|(?<const>{INTEGER_CORE}))|{NAME_REGEX_CORE})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex INSTRUCTION_REGEX = new Regex($@"^\s*\b{NAME_REGEX_CORE}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Case-insensitive name pattern
        /// </summary>
        public const string NAME_REGEX_CORE = @"(?<name>[_a-z]*[a-z]+\w*)";
        /// <summary>
        /// Matches jump lables
        /// </summary>
        public static readonly Regex LABEL_REGEX = new Regex($@"(\b{NAME_REGEX_CORE}\b\:)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Matches function declarations
        /// </summary>
        public static readonly Regex FUNC_REGEX = new Regex($@"\bfunc\s+{NAME_REGEX_CORE}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Matches function endings
        /// </summary>
        public static readonly Regex END_FUNC_REGEX = new Regex(@"\bend\s+func\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        internal static IEnumerable<string> PreprocessLines(IEnumerable<string> lines) =>
            from line in lines
            let ln = new Func<string>(() => line.Contains(COMMENT_START) ? line.Remove(0, line.IndexOf(COMMENT_START) + COMMENT_START.Length)
                                                                         : line)().Trim()
            where ln.Length > 0
            select ln;

        public static (MCPUFunction[], int, string) Precompile(string code)
        {
            string[] lines = PreprocessLines((code ?? "").Split('\n', '\r')).ToArray();
            Dictionary<string, int> labels = new Dictionary<string, int>();
            List<MCPUFunction> functions = new List<MCPUFunction>();
            MCPUFunction curr_func = new MCPUFunction(MAIN_FUNCTION_NAME);
            bool is_func = false;
            int linenr = 0;
            Match match;

            bool CheckGroup(string name) => match.Groups[name].ToString().Trim().Length > 0;
            (MCPUFunction[], int, string) Error(string message) => (null, linenr, message);

            functions.Add(curr_func);

            for (int l = lines.Length; linenr < l; ++linenr)
            {
                string line = lines[linenr];
                
                if ((match = LABEL_REGEX.Match(line)).Success)
                {
                    labels[match.Groups["name"].ToString()] = linenr;
                    line = line.Remove(match.Index, match.Length);
                }

                if ((line = line.Trim()).Length > 0)
                {
                    if ((match = FUNC_REGEX.Match(line)).Success)
                        if (is_func)
                            return Error("Functions cannot be nested. Please close the current function with an 'END FUNC'-token.");
                        else
                            functions.Add(curr_func = new MCPUFunction(match.Groups["name"].ToString(), OPCodes.NOP) { DefinedLine = linenr });
                    else if ((match = END_FUNC_REGEX.Match(line)).Success)
                        if (is_func)
                        {
                            curr_func.Instructions.Add(OPCodes.RET);
                            curr_func = (from f in functions where f.Name == MAIN_FUNCTION_NAME select f).First();
                        }
                        else
                            return Error("A function declaration must precede an 'END FUNC'-token.");
                    else if ((match = INSTRUCTION_REGEX.Match(line)).Success)
                    {
                        List<InstructionArgument> args = new List<InstructionArgument>();
                        string token = match.Groups["name"].ToString().ToLower();

                        if (!OPCodes.CodesByToken.ContainsKey(token))
                            return Error($"The instruction '{token}' could not be found.");

                        foreach (string arg in (line = line.Remove(match.Index, match.Length).Trim()).Split(' ', ','))
                            if ((arg ?? "").Trim().Length > 0)
                                if ((match = ARGUMENT_CORE.Match(line)).Success)
                                    if (CheckGroup("ptr"))
                                    {

                                    }
                                    else if (CheckGroup("addr"))
                                    {

                                    }
                                    else if (CheckGroup("const"))
                                    {

                                    }
                                    else if (CheckGroup("name"))
                                    {

                                    }
                                    else if (CheckGroup("float"))
                                    {

                                    }
                                    else
                                        return Error($"The type of the argument '{arg}' could not be determined.");
                                else
                                    return Error($"Invalid argument '{arg}' given.");

                        curr_func.Instructions.Add(new Instruction(OPCodes.CodesByToken[token], args.ToArray()));
                    }
                    else
                        return Error($"The line '{line}' could not be parsed.");
                }
                else
                    curr_func.Instructions.Add(OPCodes.NOP);
            }

            return (functions.ToArray(), -1, "");
        }
    }

    /// <summary>
    /// Represents a MCPU function definition
    /// </summary>
    public class MCPUFunction
    {
        /// <summary>
        /// The line, in which the function has been defined
        /// </summary>
        public int DefinedLine { set; get; }
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

    /*
     * FUNC myfunc
     *     ...
     * END FUNC //implicit RET
     * 
     * 
     * 
     * LABEL:
     *     INS ARG1, ARG2, ...
     * 
     * 
     *     
     * constant     0
     *              0x0
     *              0b0
     * address      [0]
     * ind.addr.    [[0]]
     * parameter    $0
     * p.addr.      [$0]
     * p.ind.addr.  [[$0]]
     * 
     */
}
