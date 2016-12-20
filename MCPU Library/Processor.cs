// #define USE_INSTRUCTION_CACHE

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    /// <summary>
    /// A general MCPU processor event handler
    /// </summary>
    /// <param name="p">Processor instance</param>
    public delegate void ProcessEventHandler(Processor p);
    /// <summary>
    /// A general generic MCPU processor event handler
    /// </summary>
    /// <typeparam name="T">Generic argument type T</typeparam>
    /// <param name="p">Processor instance</param>
    /// <param name="args">Argument of type T</param>
    public delegate void ProcessEventHandler<T>(Processor p, T args);

    /// <summary>
    /// Represents the MSCPU-processor
    /// </summary>
    public unsafe sealed class Processor
        : IDisposable
    {
        #region FIELDS + CONSTANTS

        internal static readonly Dictionary<int, ProcessingDelegate> __syscalltable = new Dictionary<int, ProcessingDelegate> {
            { -1, delegate { /*  ABK INSTRUCTION  */ } },
            { 0, (p, _) => Console.WriteLine($"MCPU v. {Assembly.GetEntryAssembly().GetName().Version} created by Unknown6656") },
            { 1, (p, _) => ConsoleExtensions.HexDump(p.ToBytes()) },
            { 2, (p, _) => Console.WriteLine(string.Join(", ", from arg in _ select $"0x{p.TranslateConstant(arg):x8}")) },
        };
        
        public const int IP_OFFS = 0x04;
        public const int FLAG_OFFS = 0x08;
        public const int RESV_OFFS = 0x0a;
        public const int MEMS_OFFS = 0x10;
        public const int INSZ_OFFS = 0x14;
        public const int STACK_BASE_OFFS = 0x18;
        public const int STACK_PTR_OFFS = 0x1c;
        public const int IO_OFFS = 0x20;
        public const int IO_COUNT = 0x20;
        public const int MEM_OFFS = 0x40;
#if DEBUG
        public const int MAX_MEMSZ = 1024;
        public const int MAX_STACKSZ = 256;
#else
        public const int MAX_MEMSZ = 0x10000000; // 1GB of memory
        public const int MAX_STACKSZ = 0x400000; // 16MB of stack space
#endif
        internal bool disposed = false;
        internal byte* raw;

        #endregion
        #region EVENTS

        /// <summary>
        /// Raised after an instruction has been executed
        /// </summary>
        public event ProcessEventHandler<Instruction> InstructionExecuted;
        /// <summary>
        /// Rasied when a user-space memory access occures. The event is NOT raised, if the processor is running in elevated (kernel) mode
        /// </summary>
        public event ProcessEventHandler<int> UserspaceWriteAccess;
        /// <summary>
        /// Raised when the status flags are changed
        /// </summary>
        public event ProcessEventHandler<StatusFlags> StatusFlagsChanged;
        /// <summary>
        /// Raised when the information flags are changed
        /// </summary>
        public event ProcessEventHandler<InformationFlags> InformationFlagsChanged;
        /// <summary>
        /// Raised when the processor is halted
        /// </summary>
        public event ProcessEventHandler ProcessorHalted;
        /// <summary>
        /// Rasied when the processor is resetted to its original state
        /// </summary>
        public event ProcessEventHandler ProcessorReset;
        /// <summary>
        /// Raised if an exception occurs
        /// </summary>
        public event ProcessEventHandler<Exception> OnError;

        #endregion
        #region PROPERTIES

        /// <summary>
        /// Sets or gets whether the current processor is being executed using kernel privileges/kernel elevation
        /// </summary>
        public bool IsElevated
        {
            get => GetInformationFlag(InformationFlags.Elevated);
            internal set => SetInformationFlag(InformationFlags.Elevated, value);
        }

        /// <summary>
        /// Sets or gets whether the current processor is being executed using kernel privileges/kernel elevation
        /// </summary>
        public bool IsRunning
        {
            get => GetInformationFlag(InformationFlags.Running);
            internal set => SetInformationFlag(InformationFlags.Running, value);
        }

        /// <summary>
        /// The ID of the current MSCPU-processor
        /// </summary>
        public int CPUID
        {
            get => *((int*)raw);
            private set => *((int*)raw) = value;
        }

        /// <summary>
        /// The current instruction pointer
        /// </summary>
        public int IP
        {
            get => KernelSpace[IP_OFFS / 4];
            internal set => KernelSpace[IP_OFFS / 4] = value;
        }
        
        /// <summary>
        /// The userspace memory size (in 4-byte-blocks)
        /// </summary>
        public int Size
        {
            get => *((int*)(raw + MEMS_OFFS));
            private set => *((int*)(raw + MEMS_OFFS)) = value;
        }

        /// <summary>
        /// The instruction segment size (in bytes)
        /// </summary>
        public int InstructionSegmentSize
        {
            get => *((int*)(raw + INSZ_OFFS));
            private set => *((int*)(raw + INSZ_OFFS)) = value;
        }

        /// <summary>
        /// The total (kernelspace) memory size (in bytes)
        /// </summary>
        public int RawSize
        {
            get => KernelSpace[STACK_BASE_OFFS / 4];
            internal set => KernelSpace[STACK_BASE_OFFS / 4] = value;
        }

        /// <summary>
        /// The MCPU flag register
        /// </summary>
        public StatusFlags Flags
        {
            get => *((StatusFlags*)(raw + FLAG_OFFS));
            internal set
            {
                *((StatusFlags*)(raw + FLAG_OFFS)) = value;

                StatusFlagsChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// The MCPU processor information flags register
        /// </summary>
        public InformationFlags InformationFlags
        {
            get => *((InformationFlags*)(raw + RESV_OFFS));
            internal set
            {
                *((InformationFlags*)(raw + RESV_OFFS)) = value;

                InformationFlagsChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Sets or gets the 4-byte integer value stored inside the given userspace memory address 
        /// </summary>
        /// <param name="addr">Userspace memory address</param>
        /// <returns>Value</returns>
        public int this[int addr]
        {
            get => VerifyUserspaceAddr(addr, () => ((int*)(raw + MEM_OFFS))[addr]);
            set => VerifyUserspaceAddr(addr, () => {
                ((int*)(raw + MEM_OFFS))[addr] = value;

                UserspaceWriteAccess?.Invoke(this, addr);
            });
        }
        
        /// <summary>
        /// Accesses the I/O-ports of the current MCPU-processor
        /// </summary>
        public IOPorts IO { get; }

        /// <summary>
        /// Returns the current instruction
        /// </summary>
        public Instruction CurrentInstruction => (IP < 0) || (IP >= Instructions.Length) ? OPCodes.HALT : Instructions[IP];

        /// <summary>
        /// Returns a list of all instructions
        /// </summary>
        public Instruction[] Instructions
#if USE_INSTRUCTION_CACHE
            { set; get; }
#else
        {
            set
            {
                byte[] bytes = Instruction.SerializeMultiple(value ?? new Instruction[0]);
                int len = bytes.Length;
                int rsz = RawSize;

                InstructionSegmentSize = len;
                raw = (byte*)Marshal.ReAllocHGlobal((IntPtr)raw, (IntPtr)(rsz + len));

                for (int i = 0; i < len; i++)
                    raw[rsz + i] = bytes[i];
            }
            get
            {
                byte[] bytes = new byte[InstructionSegmentSize];

                for (int i = 0, l = bytes.Length, rsz = RawSize; i < l; i++)
                    bytes[i] = raw[rsz + i];

                return Instruction.DeserializeMultiple(bytes);
            }
        }
#endif

        /// <summary>
        /// The stack size (in 4-byte blocks)
        /// </summary>
        public int StackSize
        {
            get => KernelSpace[STACK_PTR_OFFS / 4];
            internal set => KernelSpace[STACK_PTR_OFFS / 4] = value;
        }

        /// <summary>
        /// The stack pointer address inside the kernelspace memory
        /// </summary>
        public int StackPointerAddress => (int)(StackPointer - KernelSpace);

        /// <summary>
        /// The stack base pointer address inside the kernelspace memory
        /// </summary>
        public int StackBaseAddress => (int)(StackBasePointer - KernelSpace);

        /// <summary>
        /// The stack base pointer, which points to the top-most 4-byte memory adress of the stack
        /// </summary>
        public int* StackBasePointer => KernelSpace + RawSize / 4;

        /// <summary>
        /// The stack pointer, which points to the bottom-most 4-byte memory adress of the stack
        /// </summary>
        public int* StackPointer => StackBasePointer - StackSize;
        
        /// <summary>
        /// Returns the byte-offset of the instruction region
        /// </summary>
        public int InstructionOffset => Size * 4 + MEM_OFFS;

        /// <summary>
        /// Returns a kernel memory pointer which points to the processor's kernel memory region
        /// </summary>
        public int* KernelSpace => (int*)raw;

        /// <summary>
        /// Returns a user memory pointer which points to the processor's user memory region
        /// </summary>
        public byte* UserSpace => raw + MEM_OFFS;

        #endregion
        #region METHODS

        /// <summary>
        /// Halts the processor
        /// </summary>
        public void Halt()
        {
            IsRunning = false;

            ProcessorHalted?.Invoke(this);

            // TODO
        }

        /// <summary>
        /// Suspends the processor for the given time interval
        /// </summary>
        /// <param name="ms">Time interval (in ms)</param>
        public void Sleep(int ms)
        {
            if (ms > 0)
            {
                Thread.Sleep(ms);


                // TODO

            }
        }

        /// <summary>
        /// Resets the processor into its original state
        /// </summary>
        public void Reset()
        {
            Halt();

            IP = 0;
            StackSize = 0;
            KernelSpace[3] = 0;
            Flags = StatusFlags.Empty;
            InformationFlags = InformationFlags.Empty;
            Instructions = new Instruction[0];
            
            for (int i = IO_OFFS, s = RawSize; i < s; i++)
                raw[i] = 0;

            ProcessorReset?.Invoke(this);
        }

        /// <summary>
        /// Moves the instruction pointer to the next instruction
        /// </summary>
        public void MoveNext() => MoveRelative(1);

        /// <summary>
        /// Moves the instruction pointer to the instruction which has the given relative offset
        /// </summary>
        public void MoveRelative(int offset) => MoveTo(IP + offset);

        /// <summary>
        /// Moves the instruction pointer to the given instruction index
        /// </summary>
        /// <param name="insndx">New instruction index</param>
        public void MoveTo(int insndx) =>
            IP = (insndx < 0) || (insndx >= Instructions.Length) ? throw new InvalidOperationException("The IP is out of range") : insndx;

        /// <summary>
        /// Processes the next instruction
        /// </summary>
#if DEBUG
        public
#else
        internal
#endif
        void ProcessNext()
        {
            Instruction ins = Instructions[IP];

            if ((ins != null) && (ins.GetType() != typeof(Instructions.halt)))
            {
                ins.Process(this);

                InstructionExecuted?.Invoke(this, ins);

                if (!ins.OPCode.SpecialIPHandling)
                    if (IP < Instructions.Length)
                        MoveNext();
                    else
                        Halt();
            }
            else
                Halt(); // TODO : ?
        }

        /// <summary>
        /// Executes the given bytes
        /// </summary>
        /// <param name="ins">Bytes to be executed</param>
        public void Process(byte[] bytes) => Process(Instruction.DeserializeMultiple(bytes));

        /// <summary>
        /// Executes the given bytes without previously resetting the processor
        /// </summary>
        /// <param name="ins">Bytes to be executed</param>
        public void ProcessWithoutReset(byte[] bytes) => ProcessWithoutReset(Instruction.DeserializeMultiple(bytes));

        /// <summary>
        /// Executes the given instructions
        /// </summary>
        /// <param name="ins">Instructions to be executed</param>
        public void Process(params Instruction[] ins)
        {
            Reset();
            ProcessWithoutReset(ins);
        }

        /// <summary>
        /// Executes the given instructions without previously resetting the processor
        /// </summary>
        /// <param name="ins">Instructions to be executed</param>
        public void ProcessWithoutReset(params Instruction[] ins)
        {
            Instructions = ins.Concat(new Instruction[] { OPCodes.HALT, OPCodes.HALT }).ToArray();
            IsRunning = true;

            Exception res;
            Task<Exception> t = new Task<Exception>(delegate {
                try
                {
                    while (IsRunning)
                        ProcessNext();

                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });
            t.Start();

            while (!(t.IsCanceled || t.IsCompleted || t.IsFaulted))
                Thread.Sleep(0);

            res = t.Result; // CANNOT USE AWAIT, AS IT IS AN UNSAFE CONTEXT

            if (res != null)
                OnError?.Invoke(this, res);

            Halt();
        }

        /// <summary>
        /// Pushes the given function call onto the callstack
        /// </summary>
        /// <param name="call">Function call</param>
        public void PushCall(FunctionCall call)
        {
            if (StackSize + call.Size > MAX_STACKSZ)
                throw new StackException("The callstack is not big enough to hold the given element (aka StackOverflowException).");

            foreach (int a in call.Arguments)
                Push(a);

            Push(call.Arguments.Length);
            Push((int)call.SavedFlags);
            Push(call.ReturnAddress);
        }

        /// <summary>
        /// Peeks the inner-most function call from the callstack and returns it
        /// </summary>
        /// <returns>Inner-most function call</returns>
        public FunctionCall PeekCall()
        {
            FunctionCall call = PopCall();

            PushCall(call);

            return call;
        }

        /// <summary>
        /// Pops the inner-most function call from the callstack and returns it
        /// </summary>
        /// <returns>Inner-most function call</returns>
        public FunctionCall PopCall()
        {
            FunctionCall call = new FunctionCall {
                ReturnAddress = Pop(),
                SavedFlags = (StatusFlags)Pop(),
                Arguments = new int[Pop()]
            };

            for (int l = call.Arguments.Length, i = l - 1; i >= 0; i--)
                call.Arguments[i] = Pop();

            return call;
        }

        /// <summary>
        /// Returns the byte-representation of the current processor's memory
        /// </summary>
        /// <returns>Memory byte representation</returns>
        public byte[] ToBytes()
        {
            int size = RawSize + InstructionSegmentSize;
            byte[] targ = new byte[size];

            fixed (byte* ptr = targ)
                for (int i = 0; i < size; i++)
                    ptr[i] = raw[i];

            return targ;
        }

        /// <summary>
        /// 'Translates' the given argument into a int*-pointer pointing to the (indirect) memory address or constant, to which the given argument is referring
        /// </summary>
        /// <param name="arg">Instruction argument</param>
        /// <returns>'Translated' address</returns>
        public int* TranslateAddress(InstructionArgument arg)
        {
            if (!arg.IsInstructionSpace)
            {
                int val = arg.Value;

                if (arg.Type.HasFlag(ArgumentType.Parameter))
                {
                    FunctionCall call = PeekCall();
                    int argc = call.Arguments.Length;

                    val = val < argc ? call.Arguments[val] : throw new ArgumentOutOfRangeException($"The current function call has not {val} arguments. Please provide an arument index between 0 and {argc}.");
                }

                if (arg.IsAddress)
                {
                    if (!arg.IsKernel)
                        val += MEM_OFFS;
                    else if (!IsElevated)
                        throw new MissingPrivilegeException();

                    return KernelSpace + (arg.Type.HasFlag(ArgumentType.Indirect) ? KernelSpace[val] : val);
                }
                else
                    return &val;
            }
            else throw new ArgumentException("The given argument must not be a function or a label.");
        }

        /// <summary>
        /// 'Translates' the given argument into a constant which is the value of the pointer, to which the given argument points
        /// </summary>
        /// <param name="arg">Instruction argument</param>
        /// <returns>'Translated' constant</returns>
        public int TranslateConstant(InstructionArgument arg) => *TranslateAddress(arg);

        /// <summary>
        /// 'Translates' the given argument into a float*-pointer pointing to the (indirect) memory address or constant, to which the given argument is referring
        /// </summary>
        /// <param name="arg">Instruction argument</param>
        /// <returns>'Translated' floating-point address</returns>
        public float* TranslateFloatAddress(InstructionArgument arg) => (float*)TranslateAddress(arg);

        /// <summary>
        /// 'Translates' the given argument into a floating-point constant which is the value of the pointer, to which the given argument points
        /// </summary>
        /// <param name="arg">Instruction argument</param>
        /// <returns>'Translated' floating-point constant</returns>
        public float TranslateFloatConstant(InstructionArgument arg) => *TranslateFloatAddress(arg);

        /// <summary>
        /// Pops an integer from the MCPU callstack (UNSAFE!)
        /// </summary>
        /// <returns>Popped integer</returns>
        public int Pop()
        {
            if (StackSize <= 0)
                throw new StackException("There is no element on the stack (aka StackUnderflowException).");
            else
            {
                int val = *StackPointer;

                --StackSize;

                return val;
            }
        }

        /// <summary>
        /// Peeks an integer from the MCPU callstack (UNSAFE!)
        /// </summary>
        /// <returns>Peeked integer</returns>
        public int Peek()
            => StackSize > 0 ? *StackPointer : throw new StackException("There is no element on the stack (aka StackUnderflowException).");

        /// <summary>
        /// Pushes the given integer onto the MCPU callstack (UNSAFE!)
        /// </summary>
        /// <param name="val">Integer value to be pushed</param>
        public void Push(int val)
        {
            if (StackSize < MAX_STACKSZ)
            {
                ++StackSize;

                *StackPointer = val;
            }
            else
                throw new InsufficientExecutionStackException("There is no element on the stack (aka StackUnderflowException).");
        }
        
        /// <summary>
        /// Translates the given int*-pointer to a user-space address and returns the address as integer
        /// </summary>
        /// <param name="addr">Address pointer</param>
        /// <returns>Address</returns>
        public int GetUserAddress(int* addr) => GetKernelAddress(addr) - MEM_OFFS;

        /// <summary>
        /// Translates the given int*-pointer to a kernel-space address and returns the address as integer
        /// </summary>
        /// <param name="addr">Address pointer</param>
        /// <returns>Address</returns>
        public int GetKernelAddress(int* addr) => (int)(addr - KernelSpace);

        internal int UserToKernel(int addr) => VerifyUserspaceAddr(addr, addr + MEM_OFFS);

        internal void VerifyUserspaceAddr(int addr) => VerifyUserspaceAddr<object>(addr, null);

        internal void VerifyUserspaceAddr(int addr, Action action) => VerifyUserspaceAddr(addr, () => {
            action();

            return null as object;
        });
        
        internal T VerifyUserspaceAddr<T>(int addr, T value) => VerifyUserspaceAddr(addr, () => value);

        internal T VerifyUserspaceAddr<T>(int addr, Func<T> action) =>
            (addr >= 0) && (addr < Size) ? action() : throw new IndexOutOfRangeException($"The given memory address is invalid. It must be a positive integer value between 0 and {Size}");

        internal bool GetInformationFlag(InformationFlags flag) => InformationFlags.HasFlag(flag);

        internal void SetInformationFlag(InformationFlags flag, bool value) => InformationFlags = (InformationFlags & ~flag) | (value ? flag : 0);

        #endregion
        #region .CTOR/.DTOR

        ~Processor() => Dispose();

        public void Dispose()
        {
            if (!disposed)
                Marshal.FreeHGlobal((IntPtr)raw);

            disposed = true;
        }

        /// <summary>
        /// Creates a new MCPU-processor instance with the maximum userspace memory size
        /// </summary>
        public Processor()
            : this(MAX_MEMSZ)
        {
        }

        /// <summary>
        /// Creates a new MCPU-processor instance with the given userspace-size (in 4-byte-blocks)
        /// </summary>
        /// <param name="size">Userspace size</param>
        public Processor(int size)
            : this(size, DateTime.Now.GetHashCode())
        {
        }

        /// <summary>
        /// Creates a new MCPU-processor instance with the given userspace-size (in 4-byte-blocks) and assigns the given CPUID to the processor
        /// </summary>
        /// <param name="size">Userspace size</param>
        /// <param name="cpuid">CPU ID</param>
        public Processor(int size, int cpuid)
        {
            if (size > MAX_MEMSZ)
                throw new OutOfMemoryException($"The (currently) maximum supported memory size are {MAX_MEMSZ * 4} bytes.");

            int raw_size = 4 * size + MEM_OFFS + MAX_STACKSZ * 4;

            raw = (byte*)Marshal.AllocHGlobal(raw_size);

            for (int i = 0; i < raw_size; i++)
                raw[i] = 0;

            IO = new IOPorts(raw);
            Size = size;
            CPUID = cpuid;
            StackSize = 0;
            RawSize = raw_size;
            Instructions = new Instruction[0];

            Contract.Assert(StackPointer == StackBasePointer);
        }

        #endregion
    }

    /// <summary>
    /// Represents an exception, which occures if a 'regular' user tries to perform kernel actions
    /// </summary>
    public class MissingPrivilegeException
        : InvalidOperationException
    {
        /// <summary>
        /// Represents an exception, which occures if a 'regular' user tries to perform kernel actions
        /// </summary>
        public MissingPrivilegeException()
            : base("The current task cannot be performed without kernel privileges.")
        {
        }

        /// <summary>
        /// Represents an exception, which occures if a 'regular' user tries to perform the given OP code
        /// </summary>
        /// <param name="opcode">OP code</param>
        public MissingPrivilegeException(OPCode opcode)
            : this(opcode.Token)
        {
        }

        /// <summary>
        /// Represents an exception, which occures if a 'regular' user tries to perform the given instruction
        /// </summary>
        /// <param name="ins">Instruction</param>
        public MissingPrivilegeException(Instruction ins)
            : this(ins.OPCode)
        {
        }

        /// <summary>
        /// Represents an exception, which occures if a 'regular' user tries to perform the action associated with the given token
        /// </summary>
        /// <param name="tokenname">Token name</param>
        public MissingPrivilegeException(string tokenname)
            : base($"The instruction or OP code '{tokenname}' could not be executed, as the current processor is not executed with kernel privileges")
        {
        }
    }

    /// <summary>
    /// Represents an exception, which occures while processing the MCPU-(call)stack
    /// </summary>
    public class StackException
        : Exception
    {
        /// <summary>
        /// Creates a new StackException with the given message
        /// </summary>
        /// <param name="message">Exception message</param>
        public StackException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Represents the I/O-port-range of a MCPU-processor
    /// </summary>
    public unsafe class IOPorts
    {
        internal IOPort* ports;
        

        /// <summary>
        /// Accesses the I/O-port at the given index
        /// </summary>
        /// <param name="port">I/O-port index</param>
        /// <returns>I/O-port</returns>
        public IOPort this[int port]
        {
            get => IsInRange(port) ? *(ports + port + Processor.IO_OFFS)
                                   : throw new IndexOutOfRangeException($"The I/O-port index must be a positive value between 0 and {Processor.IO_OFFS}.");
            set
            {
                IOPort old = this[port];

                if ((old.Direction == IODirection.In) && (value.Value != old.Value))
                    throw new InvalidOperationException($"The I/O-port no. {port} is set to read-only.");
                else
                    *(ports + port + Processor.IO_OFFS) = value;
            }
        }
        

        /// <summary>
        /// Sets the value of the given I/O-port to the given direction
        /// </summary>
        /// <param name="port">I/O-port index</param>
        /// <param name="value">New port value</param>
        public void SetValue(int port, byte value)
        {
            IOPort p = this[port];

            p.Value = value;

            this[port] = p;
        }

        /// <summary>
        /// Sets the direction of the given I/O-port to the given direction
        /// </summary>
        /// <param name="port">I/O-port index</param>
        /// <param name="direction">New port direction</param>
        public void SetDirection(int port, IODirection direction)
        {
            IOPort p = this[port];

            p.Direction = direction;

            this[port] = p;
        }

        internal bool IsInRange(int port) => (port >= 0) && (port < Processor.IO_COUNT);

        internal IOPorts(void* ptr) => ports = (IOPort*)ptr;
    }

    /// <summary>
    /// Represents an I/O-port
    /// </summary>
    [Serializable, NativeCppClass, StructLayout(LayoutKind.Sequential, Size = 1, Pack = 1)]
    public struct IOPort
    {
        internal byte raw;

        
        /// <summary>
        /// Sets or gets the I/O-port's value
        /// </summary>
        public byte Value
        {
            get => (byte)(raw & 0x0f);
            set => raw = Direction == IODirection.Out ? (byte)((raw & 0x80) | (value & 0x0f))
                                                      : throw new InvalidOperationException("The I/O-port is set to read-only.");
        }

        /// <summary>
        /// Sets or gets the I/O-port's direction
        /// </summary>
        public IODirection Direction
        {
            get => (IODirection)(raw >> 7);
            set => raw = (byte)((raw & 0x0f) | ((int)value << 7));
        }

        public static implicit operator (IODirection, byte) (IOPort port) => (port.Direction, port.Value);

        public static implicit operator IOPort((IODirection, byte) port) => new IOPort { Value = port.Item2, Direction = port.Item1 };
    }

    /// <summary>
    /// Represents an I/O-direction
    /// </summary>
    [Serializable]
    public enum IODirection
        : byte
    {
        /// <summary>
        /// The I/O-port is set to 'read' (in)
        /// </summary>
        In = 1,
        /// <summary>
        /// The I/O-port is set to 'write' (out)
        /// </summary>
        Out = 0,
    }

    /// <summary>
    /// MCPU status flags
    /// </summary>
    [Serializable, Flags]
    public enum StatusFlags
        : ushort
    {
        /// <summary>
        /// Indicates that the first comparison input is zero
        /// </summary>
        Zero1 = 0b1000_0000_0000_0000,
        /// <summary>
        /// Indicates that the second comparison input is zero
        /// </summary>
        Zero2 = 0b0100_0000_0000_0000,
        /// <summary>
        /// Indicates that the first comparison input is negative
        /// </summary>
        Sign1 = 0b0010_0000_0000_0000,
        /// <summary>
        /// Indicates that the second comparison input is negative
        /// </summary>
        Sign2 = 0b0001_0000_0000_0000,
        /// <summary>
        /// Indicates that both comparison inputs are equal
        /// </summary>
        Equal = 0b0000_1000_0000_0000,
        /// <summary>
        /// Indicates that the fist comarison input is smaller than the second input
        /// </summary>
        Lower = 0b0000_0100_0000_0000,
        /// <summary>
        /// Indicates that the fist comarison input is greater than the second input
        /// </summary>
        Greater = 0b0000_0010_0000_0000,
        /// <summary>
        /// Indicates that the comparison had only one input (meaning that the first input is zero, and the second one is the 'real' input)
        /// </summary>
        Unary = 0b0000_0001_0000_0000,


        /// <summary>
        /// Represents no flag
        /// </summary>
        Empty = 0b0000_0000_0000_0000,
    }
    
    /// <summary>
    /// MCPU processor information flags
    /// </summary>
    [Serializable, Flags]
    public enum InformationFlags
        : ushort
    {
        /// <summary>
        /// Indicates, that the processor is running with kernel privileges
        /// </summary>
        Elevated = 0b1000_0000_0000_0000,
        /// <summary>
        /// Indicates, that the processor is currently running
        /// </summary>
        Running = 0b0100_0000_0000_0000,
        /// <summary>
        /// Represents no flag
        /// </summary>
        Empty = 0b0000_0000_0000_0000,
    }
}
