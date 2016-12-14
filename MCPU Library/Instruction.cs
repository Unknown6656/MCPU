using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCPU
{
    /// <summary>
    /// Represents an OP code
    /// </summary>
    public abstract class OPCode
    {
        /// <summary>
        /// The OP code token
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// Returns the argument count
        /// </summary>
        public int RequiredArguments { get; }

        /// <summary>
        /// Returns the OP code's number associated with the current instance
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="args"></param>
        public abstract void Process(Processor p, int[] args);
        
        internal void __process(Processor p, params int[] arguments)
        {
            if ((RequiredArguments > 0) && (arguments.Length < RequiredArguments))
                throw new ArgumentException($"The intruction {Token} requires at least {RequiredArguments} arguments.", nameof(arguments));
            else
                Process(p ?? throw new ArgumentNullException("The processor must not be null."), RequiredArguments < 1 ? new int[0] : arguments.Take(RequiredArguments).ToArray());
        }

        /// <summary>
        /// Creates a new OP code
        /// </summary>
        /// <param name="argc">Argument count</param>
        public OPCode(int argc)
        {
            Type t = GetType();

            Token = t.Name.ToUpper();
            RequiredArguments = argc;

            if (t != typeof(OPCode))
            {
                OPCodeNumberAttribute attr = (from v in t.GetCustomAttributes(true)
                                              where v is OPCodeNumberAttribute
                                              select v as OPCodeNumberAttribute).FirstOrDefault();

                Number = attr?.Number ?? throw new InvalidProgramException($"The OP-code {Token} must define an number using the {typeof(OPCodeNumberAttribute).FullName}");
            }
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
    }

    /// <summary>
    /// Defines the OP code's internal number
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited = true), Serializable]
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
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public enum ArgumentType
        : byte
    {
        /// <summary>
        /// Represents a constant argument
        /// </summary>
        Constant = 0b0000,
        /// <summary>
        /// Represents an memory address
        /// </summary>
        Address = 0b0001,
        IndirectAddress = 0b0011,
        Label = 0b0100,
        Function = 0b0110,
    }
}
