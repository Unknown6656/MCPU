using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    public delegate void ProcessingDelegate(Processor p, params InstructionArgument[] args);

    /// <summary>
    /// Represents an OP code
    /// </summary>
    public abstract class OPCode
    {
        /// <summary>
        /// Processes the current instruction on the given processor with the given arguments
        /// </summary>
        /// <param name="p">Processor</param>
        /// <param name="args">Instruction arguments</param>
        public ProcessingDelegate Process { get; }
        /// <summary>
        /// Returns whether the OP code requires kernel privilege elevation
        /// </summary>
        public bool RequiresEleveation { get; }
        /// <summary>
        /// Returns whether the OP code performs special instruction pointer (IP) handling
        /// </summary>
        public bool SpecialIPHandling { get; }
        /// <summary>
        /// Returns the argument count
        /// </summary>
        public int RequiredArguments { get; }
        /// <summary>
        /// The OP code token
        /// </summary>
        public string Token { get; }
        /// <summary>
        /// Returns the OP code's number associated with the current instance
        /// </summary>
        public int Number { get; }


        internal void __process(Processor p, params InstructionArgument[] arguments)
        {
            if ((RequiredArguments > 0) && (arguments.Length < RequiredArguments))
                throw new ArgumentException($"The intruction {Token} requires at least {RequiredArguments} arguments.", nameof(arguments));
            else if (!p.IsElevated && RequiresEleveation)
                throw new MissingPrivilegeException();
            else
                Process(p ?? throw new ArgumentNullException("The processor must not be null."), RequiredArguments < 1 ? new InstructionArgument[0] : arguments.Take(RequiredArguments).ToArray());
        }

        /// <summary>
        /// Creates a new OP code
        /// </summary>
        /// <param name="argc">Argument count</param>
        public OPCode(int argc, ProcessingDelegate del)
        {
            Type t = GetType();

            Token = t.Name.ToUpper();
            RequiredArguments = argc;
            Process = del;

            if (t != typeof(OPCode))
            {
                OPCodeNumberAttribute attr = (from v in t.GetCustomAttributes(true)
                                              where v is OPCodeNumberAttribute
                                              select v as OPCodeNumberAttribute).FirstOrDefault();

                Number = attr?.Number ?? throw new InvalidProgramException($"The OP-code {Token} must define an number using the {typeof(OPCodeNumberAttribute).FullName}");
                RequiresEleveation = (from v in t.GetCustomAttributes(true) where v is RequiresPrivilegeAttribute select true).FirstOrDefault();
                SpecialIPHandling = (from v in t.GetCustomAttributes(true) where v is SpecialIPHandlingAttribute select true).FirstOrDefault();
            }
        }

        #region ASSERTIONS

        internal static ArgumentException _assertexcp(int argn, string reason) => new ArgumentException($"The given argument №{argn + 1} is invalid for the current call, {reason}");

        /// <summary>
        /// Asserts that the argument at the given index is a constant 
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertConstant(int argn, InstructionArgument[] argv)
            => argv[argn].IsConstant ? true : throw _assertexcp(argn, "as it must be a constant argument.");

        /// <summary>
        /// Asserts that the argument at the given index is a not constant 
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertNotConstant(int argn, InstructionArgument[] argv)
            => argv[argn].IsConstant ? throw _assertexcp(argn, "as it must not be a constant argument.") : true;

        /// <summary>
        /// Asserts that the argument at the given index is an address
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertAddress(int argn, InstructionArgument[] argv)
            => argv[argn].IsAddress ? true : throw _assertexcp(argn, "as it must be an address.");

        /// <summary>
        /// Asserts that the argument at the given index is not an address
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertNotAddress(int argn, InstructionArgument[] argv)
            => argv[argn].IsAddress ? throw _assertexcp(argn, "as it must not be an address.") : true;

        /// <summary>
        /// Asserts that the argument at the given index is a jump label or function
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertInstructionSpace(int argn, InstructionArgument[] argv)
            => argv[argn].IsInstructionSpace ? true : throw _assertexcp(argn, "as it must be a jump label or a function.");

        /// <summary>
        /// Asserts that the argument at the given index is not a jump label or function
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertNotInstructionSpace(int argn, InstructionArgument[] argv)
            => argv[argn].IsInstructionSpace ? throw _assertexcp(argn, "as it must not be a jump label or a function.") : true;

        /// <summary>
        /// Asserts that the argument at the given index is a label
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertLabel(int argn, InstructionArgument[] argv)
            => argv[argn].Type == ArgumentType.Label ? true : throw _assertexcp(argn, "as it must be a jump label.");

        /// <summary>
        /// Asserts that the argument at the given index is a function
        /// </summary>
        /// <param name="argn">Argument index</param>
        /// <param name="argv">Argument vector</param>
        /// <returns>Assertion result</returns>
        public static bool AssertFunction(int argn, InstructionArgument[] argv)
            => argv[argn].Type == ArgumentType.Function ? true : throw _assertexcp(argn, "as it must be a function.");

        #endregion
    }

    /// <summary>
    /// Represents an OP code which performs a binary arithmetic function
    /// </summary>
    public abstract unsafe class ArithmeticUnaryOPCode
        : OPCode
    {
        /// <summary>
        /// Creates a new OP code using the given unary arithmetic function
        /// </summary>
        /// <param name="func">Unary arithmetic function</param>
        public ArithmeticUnaryOPCode(Func<int, int> func)
            : base(1, (p, _) => {
                AssertNotInstructionSpace(0, _);

                *p.TranslateAddress(_[0]) = func(*p.TranslateAddress(_[0]));
            })
        {
        }
    }

    /// <summary>
    /// Represents an OP code which performs an unary arithmetic function
    /// </summary>
    public abstract unsafe class ArithmeticBinaryOPCode
        : OPCode
    {
        /// <summary>
        /// Creates a new OP code using the given binary arithmetic function
        /// </summary>
        /// <param name="func">Binary arithmetic function</param>
        public ArithmeticBinaryOPCode(Func<int, int, int> func)
            : base(2, (p, _) => {
                AssertNotInstructionSpace(0, _);
                AssertNotInstructionSpace(1, _);
                
                *p.TranslateAddress(_[0]) = func(*p.TranslateAddress(_[0]), *p.TranslateAddress(_[1]));
            })
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Instruction
    {
        /// <summary>
        /// The instruction's OP code
        /// </summary>
        public OPCode OPCode { get; }
        /// <summary>
        /// The instruction arguments
        /// </summary>
        public InstructionArgument[] Arguments { get; }

        /// <summary>
        /// Processes the current instruction on the given processor
        /// </summary>
        /// <param name="p">Processor</param>
        public void Process(Processor p) => OPCode.__process(p, Arguments);

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="opcode">OP code</param>
        public Instruction(OPCode opcode)
            : this(opcode, null)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="opcode">OP code</param>
        /// <param name="args">Arguments</param>
        public Instruction(OPCode opcode, params InstructionArgument[] args)
        {
            OPCode = opcode;
            Arguments = args ?? new InstructionArgument[0];
        }

        /// <summary>
        /// Creates a new instruction based on the given OP code and arguments
        /// </summary>
        /// <param name="opcode">OP code</param>
        /// <param name="args">Arguments</param>
        /// <returns>Instruction</returns>
        public static Instruction Create(int opcode, params InstructionArgument[] args) => Create(OPCodes.Codes[opcode], args);

        /// <summary>
        /// Creates a new instruction based on the given OP code and arguments
        /// </summary>
        /// <param name="opcode">OP code</param>
        /// <param name="args">Arguments</param>
        /// <returns>Instruction</returns>
        public static Instruction Create(OPCode opcode, params InstructionArgument[] args) => (opcode, args);

        public static implicit operator Instruction(OPCode opc) => new Instruction(opc);

        public static implicit operator Instruction((int, IEnumerable<InstructionArgument>) ins) => new Instruction(OPCodes.Codes[ins.Item1], ins.Item2?.ToArray());

        public static implicit operator Instruction((OPCode, IEnumerable<InstructionArgument>) ins) => new Instruction(ins.Item1, ins.Item2?.ToArray());

        public static implicit operator (OPCode, InstructionArgument[]) (Instruction ins) => (ins.OPCode, ins.Arguments);

        public static implicit operator (int, InstructionArgument[]) (Instruction ins) => (ins.OPCode.Number, ins.Arguments);
    }

    /// <summary>
    /// Defines the OP code's internal number
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true), Serializable]
    public class OPCodeNumberAttribute
        : Attribute
    {
        /// <summary>
        /// The OP code's number
        /// </summary>
        public ushort Number { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="number">The OP code's number</param>
        public OPCodeNumberAttribute(ushort number) => Number = number;
    }

    /// <summary>
    /// Indicates that the targeting OP code requires kernel privileges to be executed
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequiresPrivilegeAttribute
        : Attribute
    {
    }

    /// <summary>
    /// Indicates that the targeting OP code handles the instruction pointer (IP) specially
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SpecialIPHandlingAttribute
        : Attribute
    {
    }

    /// <summary>
    /// Represents a MCPU function call
    /// </summary>
    [Serializable]
    public struct FunctionCall
    {
        /// <summary>
        /// The call's return address
        /// </summary>
        public int ReturnAddress { get; internal set; }
        /// <summary>
        /// The call's arguments
        /// </summary>
        public int[] Arguments { get; internal set; }
        /// <summary>
        /// Returns the unmanaged size of the current function call
        /// </summary>
        public int Size => 2 + Arguments.Length;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="ret">Return address</param>
        /// <param name="args">Call arguments</param>
        public FunctionCall(int ret, params int[] args)
        {
            Arguments = args;
            ReturnAddress = ret;
        }
        /// <summary>
        /// Returns the string representation of the current function call
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString() => $"ret: {ReturnAddress:x}, args: ({string.Join(", ", Arguments)})";


        public static implicit operator int[] (FunctionCall call) => new int[] { call.ReturnAddress, call.Arguments.Length }.Concat(call.Arguments).ToArray();

        public static implicit operator FunctionCall(int[] raw) => new FunctionCall { ReturnAddress = raw[0], Arguments = raw.Skip(2).Take(raw[1]).ToArray() };
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct InstructionArgument
    {
        /// <summary>
        /// The argument value
        /// </summary>
        public int Value { set; get; }
        /// <summary>
        /// The argument type
        /// </summary>
        public ArgumentType Type { set; get; }
        /// <summary>
        /// The parameter-invariant arguemnt type
        /// </summary>
        public ArgumentType ParameterInvariantType => Type & ~ArgumentType.Parameter;
        /// <summary>
        /// The kernel-invariant arguemnt type
        /// </summary>
        public ArgumentType KernelInvariantType => Type & ~ArgumentType.KernelMode;
        /// <summary>
        /// Returns, whether the argument shall be processed inside kernel-space
        /// </summary>
        public bool IsKernel => Type.HasFlag(ArgumentType.KernelMode);
        /// <summary>
        /// Returns, whether the argument is an address
        /// </summary>
        public bool IsAddress => Type.HasFlag(ArgumentType.Address);
        /// <summary>
        /// Returns, whether the argument is a constant
        /// </summary>
        public bool IsConstant => (KernelInvariantType & KernelInvariantType) == ArgumentType.Constant;
        /// <summary>
        /// Returns, whether the argument resides inside the instruction segment (meaning it is a label or function)
        /// </summary>
        public bool IsInstructionSpace => Type.HasFlag(ArgumentType.Label);
        /// <summary>
        /// Returns the string representation of the current instruction argument
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString() => $"({Type}: {Value})";


        public static implicit operator int(InstructionArgument arg) => arg.Value;

        public static implicit operator InstructionArgument(int val) => new InstructionArgument { Value = val, Type = ArgumentType.Constant };

        public static implicit operator (int, ArgumentType) (InstructionArgument arg) => (arg.Value, arg.Type);

        public static implicit operator InstructionArgument((int, ArgumentType) val) => new InstructionArgument { Value = val.Item1, Type = val.Item2 };
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, Flags]
    public enum ArgumentType
        : byte
    {
        /// <summary>
        /// Represents a constant argument
        /// </summary>
        Constant = 0b0000_0000,
        /// <summary>
        /// Represents a function call parameter
        /// </summary>
        Parameter = 0b0000_1000,
        /// <summary>
        /// Represents an memory address
        /// </summary>
        Address = 0b0000_0001,
        /// <summary>
        /// Represents some kind of indirect value (to be combined with other argument types)
        /// </summary>
        Indirect = 0b0000_0010,
        /// <summary>
        /// Represents a jump label
        /// </summary>
        Label = 0b0000_0100,
        /// <summary>
        /// Uses the kernel-space addresses instead of user-space addresses
        /// </summary>
        KernelMode = 0b1000_0000,
        /// <summary>
        /// Represents an indirect memory address
        /// </summary>
        IndirectAddress = Indirect | Address,
        /// <summary>
        /// Represents a function call parameter address
        /// </summary>
        ParameterAddres = Parameter | Address,
        /// <summary>
        /// Represents an indirect function call parameter address
        /// </summary>
        IndirectParameterAddress = Indirect | ParameterAddres,
        /// <summary>
        /// Represents a function
        /// </summary>
        Function = Indirect | Label,
    }
}
