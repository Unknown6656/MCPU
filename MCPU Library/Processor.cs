// #define USE_INSTRUCTION_CACHE
#define WINDOWS

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace MCPU
{
    /// <summary>
    /// A general MCPU processor event handler
    /// </summary>
    /// <param name="p">Processor instance</param>
    public delegate void ProcessorEventHandler(Processor p);
    /// <summary>
    /// A general generic MCPU processor event handler
    /// </summary>
    /// <typeparam name="T">Generic argument type T</typeparam>
    /// <param name="p">Processor instance</param>
    /// <param name="args">Argument of type T</param>
    public delegate void ProcessorEventHandler<T>(Processor p, T args);


    /// <summary>
    /// Represents the MCPU-processor
    /// </summary>
    public unsafe sealed class Processor
        : IDisposable
    {
        #region FIELDS + CONSTANTS

        internal static readonly Dictionary<int, ProcessingDelegate> __syscalltable = new Dictionary<int, ProcessingDelegate>
        {
            [-1] = delegate { /* TODO : ABK INSTRUCTION */ },
            [0] = (p, _) => p.WriteLine($"MCPU v. {Assembly.GetEntryAssembly().GetName().Version} created by Unknown6656"),
            [1] = (p, _) => ConsoleExtensions.HexDump(p.ToBytes()),
            [2] = (p, _) => p.WriteLine(string.Join(", ", from arg in _ select $"0x{p.TranslateConstant(arg):x8}")),
            [3] = (p, _) => p.WriteLine(string.Join(", ", from arg in _ select p.TranslateFloatConstant(arg))),
            [4] = (p, _) => {
                OPCode.AssertAddress(0, _);

                *p.TranslateAddress(_[0]) = p.Ticks;
            },
            [5] = (p, _) => p.WriteLine(string.Join("", from arg in _
                                                        select new string(new char[] {
                                                            (char)((arg >> 24) & 'ÿ'),
                                                            (char)((arg >> 16) & 'ÿ'),
                                                            (char)((arg >> 8) & 'ÿ'),
                                                            (char)(arg & 'ÿ')
                                                        })).Replace("\0", "")),
            [6] = (p, _) => {
                string line = Console/* TODO : input stream */.ReadLine().Trim();
                int value;

                if (int.TryParse(line, out value))
                    *p.TranslateAddress(_[0]) = value;
            },
            [7] = (p, _) => {
                string line = Console/* TODO : input stream */.ReadLine().Trim();
                float value;

                if (float.TryParse(line, out value))
                    *p.TranslateFloatAddress(_[0]) = value;
            },
        };
        internal static readonly Dictionary<Type, byte> __interruptexceptiontable = new Dictionary<Type, byte>
        {
            [typeof(Exception)] = 0,
            [typeof(OverflowException)] = 1,
            [typeof(ArithmeticException)] = 1,
            [typeof(DivideByZeroException)] = 1,
            // [typeof(StackOverflowException)] = 2,
            [typeof(InsufficientExecutionStackException)] = 2,
            [typeof(StackException)] = 3,
            [typeof(IndexOutOfRangeException)] = 4,
            [typeof(MissingPrivilegeException)] = 5,
            [typeof(ArgumentException)] = 6,
            // more ?
        };
#if !WINDOWS
        private const string IVPEX_MSG = "The memory watcher unit requires a Win32-Environment with the corresponding API.";
#endif
        public const int IP_OFFS = 0x04;
        public const int FLAG_OFFS = 0x08;
        public const int RESV_OFFS = 0x0a;
        public const int TICK_OFFS = 0x0c;
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
        internal Dictionary<byte, int> interrupt_table = new Dictionary<byte, int>();
        internal Dictionary<int, (int Line, int Length)> function_table = new Dictionary<int, (int, int)>();
        internal TextWriter stdout = Console.Out;
        internal bool disposed = false;
        internal byte* raw;

        #endregion
        #region EVENTS

        /// <summary>
        /// Raised when the processor gets disposed
        /// </summary>
        public event ProcessorEventHandler OnDisposed;
        /// <summary>
        /// Raised when the processor outputs some text
        /// </summary>
        public event ProcessorEventHandler<string> OnTextOutput;
        /// <summary>
        /// Raised after an instruction has been executed
        /// </summary>
        public event ProcessorEventHandler<Instruction> InstructionExecuted;
        /// <summary>
        /// Raised before an instruction has been executed
        /// </summary>
        public event ProcessorEventHandler<Instruction> InstructionExecuting;
        /// <summary>
        /// Rasied when a user-space memory access occures. The event is NOT raised, if the processor is running in elevated (kernel) mode
        /// </summary>
        public event ProcessorEventHandler<int> UserspaceWriteAccess;
        /// <summary>
        /// Raised when the status flags are changed
        /// </summary>
        public event ProcessorEventHandler<StatusFlags> StatusFlagsChanged;
        /// <summary>
        /// Raised when the information flags are changed
        /// </summary>
        public event ProcessorEventHandler<InformationFlags> InformationFlagsChanged;
        /// <summary>
        /// Raised when the processor is halted
        /// </summary>
        public event ProcessorEventHandler ProcessorHalted;
        /// <summary>
        /// Rasied when the processor is resetted to its original state
        /// </summary>
        public event ProcessorEventHandler ProcessorReset;
        /// <summary>
        /// Raised if an exception occurs
        /// </summary>
        public event ProcessorEventHandler<MCPUProcessingException> OnError;
        /// <summary>
        /// Raised if the kernel memory's byte at the returned byte offset has changed
        /// </summary>
        public event ProcessorEventHandler<int> OnMemoryChanged
#if WINDOWS
            ;
#else
        {
            add => throw new InvalidProgramException(IVPEX_MSG);
            remove => throw new InvalidProgramException(IVPEX_MSG);
        }
#endif
        #endregion
        #region PROPERTIES

        /// <summary>
        /// Sets or gets the processor's debugging standard output stream
        /// </summary>
        public TextWriter StandardOutput
        {
            set => stdout = value ?? Console.Out;
            get => stdout;
        }

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
        /// The ID of the current MCPU-processor
        /// </summary>
        public int CPUID
        {
            get => *((int*)raw);
            private set => *((int*)raw) = value;
        }

        /// <summary>
        /// The number of executed processor ticks
        /// </summary>
        public int Ticks
        {
            get => KernelSpace[TICK_OFFS / 4];
            internal set => KernelSpace[TICK_OFFS / 4] = value;
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
        /// Sets or gets the processor's error handling policy
        /// </summary>
        public ErrorHandlingMode ErrorHandlingMode { set; get; } = ErrorHandlingMode.InterruptOrThrow;

        /// <summary>
        /// Sets or gets the 4-byte integer value stored inside the given userspace memory address 
        /// </summary>
        /// <param name="addr">Userspace memory address</param>
        /// <returns>Value</returns>
        public int this[int addr]
        {
            get => VerifyUserspaceAddr(addr, () => ((int*)UserSpace)[addr]);
            set => VerifyUserspaceAddr(addr, () =>
            {
                ((int*)UserSpace)[addr] = value;

                UserspaceWriteAccess?.Invoke(this, addr);
            });
        }

        /// <summary>
        /// Accesses the I/O-ports of the current MCPU-processor (from the processor's point of view)
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

        /// <summary>
        /// Returns the processor's call stack interpreted as `MCPU::FunctionCall`-instances
        /// </summary>
        public FunctionCall[] CallStack
        {
            get
            {
                List<FunctionCall> calls = new List<FunctionCall>();

                while (CanPeekCall())
                    calls.Add(PopCall());

                foreach (FunctionCall call in (calls as IEnumerable<FunctionCall>).Reverse())
                    PushCall(call);

                return calls.ToArray();
            }
        }

        /// <summary>
        /// Returns the generated interrupt table
        /// </summary>
        public ReadOnlyDictionary<byte, Instruction[]> InterruptTable
        {
            get
            {
                Dictionary<byte, Instruction[]> dic = new Dictionary<byte, Instruction[]>();
                Instruction[] instructions = Instructions;

                foreach (byte b in interrupt_table.Keys)
                {
                    List<Instruction> instr = new List<Instruction>();

                    for (int i = 0, func = interrupt_table[b], len = function_table.First(_ => _.Value.Line == func).Value.Length; i < len; i++)
                        instr.Add(instructions[func + i]);

                    dic[b] = instr.ToArray();
                }

                return new ReadOnlyDictionary<byte, Instruction[]>(dic);
            }
        }

        #endregion
        #region METHODS
#if WINDOWS
        [DllImport("kernel32.dll")]
        internal static extern uint GetWriteWatch(uint dwFlags, int* lpBaseAddress, uint* dwRegionSize, out IntPtr lpAddresses, ref UIntPtr lpdwCount, out uint lpdwGranularity);
#endif
        /// <summary>
        /// Executes the syscall function associated with the given syscall number
        /// </summary>
        /// <param name="num">Syscall number</param>
        /// <param name="args">Syscall arguments</param>
        public void Syscall(int num, params InstructionArgument[] args) => __syscalltable[num](this, args);

        /// <summary>
        /// Halts the processor
        /// </summary>
        public void Halt()
        {
            IsRunning = false;

            ProcessorHalted?.Invoke(this);

            // TODO : async halt
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

                // TODO : async sleep
            }
        }

        /// <summary>
        /// Resets the processor into its original state
        /// </summary>
        public void Reset()
        {
            Halt();

            IP = 0;
            Ticks = 0;
            StackSize = 0;
            KernelSpace[3] = 0;
            Flags = StatusFlags.Empty;
            InformationFlags = InformationFlags.Empty;
            Instructions = new Instruction[0];
            interrupt_table = new Dictionary<byte, int>();
            function_table = new Dictionary<int, (int, int)>();

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
        public void ProcessNext() => ProcessNext(Instructions[IP]);

        internal void ProcessNext(Instruction ins, bool update_ip = true)
        {
            try
            {
                if ((ins != null) && (ins.GetType() != typeof(Instructions.halt)))
                {
                    InstructionExecuting?.Invoke(this, ins);

                    ++Ticks;

                    ins.Process(this);

                    InstructionExecuted?.Invoke(this, ins);

                    if (!ins.OPCode.SpecialIPHandling)
                        if (IP < Instructions.Length)
                        {
                            if (update_ip)
                                MoveNext();
                        }
                        else
                            Halt();
                }
                else
                    Halt();
            }
            catch (Exception ex)
            {
                Type t = ex.GetType();
                bool handled = false;

                do
                    if (__interruptexceptiontable.ContainsKey(t))
                    {
                        handled = Interrupt(__interruptexceptiontable[t]);

                        break;
                    }
                while ((t = t.BaseType) != null);

                if (!(ErrorHandlingMode == ErrorHandlingMode.AlwaysThrow ? false :
                      ErrorHandlingMode == ErrorHandlingMode.AlwaysInterrupt ? true : handled))
                    if (ex is MCPUProcessingException mcpupex)
                        throw mcpupex;
                    else
                        throw new MCPUProcessingException(ex, ins);
            }
        }

        /// <summary>
        /// Executes the saved instructions
        /// </summary>
        public void Process() => Process(Instructions);

        /// <summary>
        /// Executes the given bytes
        /// </summary>
        /// <param name="ins">Bytes to be executed</param>
        public void Process(byte[] bytes) => Process(Instruction.DeserializeMultiple(bytes));

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
        /// Executes the saved instructions without previously resetting the processor
        /// </summary>
        public void ProcessWithoutReset() => ProcessWithoutReset(Instructions);

        /// <summary>
        /// Executes the given bytes without previously resetting the processor
        /// </summary>
        /// <param name="ins">Bytes to be executed</param>
        public void ProcessWithoutReset(byte[] bytes) => ProcessWithoutReset(Instruction.DeserializeMultiple(bytes));

        /// <summary>
        /// Executes the given instructions without previously resetting the processor
        /// </summary>
        /// <param name="ins">Instructions to be executed</param>
        public void ProcessWithoutReset(params Instruction[] ins)
        {
            Instructions = ins.Concat(new Instruction[] { OPCodes.HALT, OPCodes.HALT }).ToArray();
            IsRunning = true;

            MCPUProcessingException res;
            Task<MCPUProcessingException> t = new Task<MCPUProcessingException>(delegate
            {
                try
                {
                    while (IsRunning)
                        ProcessNext();

                    return null;
                }
                catch (MCPUProcessingException ex)
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
        /// Invokes the interrupt handler with the given interrupt code
        /// </summary>
        /// <param name="code">Interrupt code</param>
        public bool Interrupt(byte code)
        {
            if (InformationFlags.HasFlag(InformationFlags.InterruptEnable) &&
                !InformationFlags.HasFlag(InformationFlags.InterruptCurrent) &&
                (interrupt_table?.ContainsKey(code) ?? false))
            {
                SetInformationFlag(InformationFlags.InterruptCurrent, true);

                ProcessNext((OPCodes.CALL, new InstructionArgument[] { (interrupt_table[code], ArgumentType.Function) }), false);

                SetInformationFlag(InformationFlags.InterruptCurrent, false);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Pushes the given function call onto the callstack
        /// </summary>
        /// <param name="call">Function call</param>
        public void PushCall(FunctionCall? call)
        {
            if (call is FunctionCall c)
            {
                if (StackSize + c.Size > MAX_STACKSZ)
                    throw new StackException("The callstack is not big enough to hold the given element (aka StackOverflowException).");

                foreach (int a in c.Arguments)
                    Push(a);

                Push(c.Arguments.Length);
                Push((int)c.SavedFlags);
                Push(c.ReturnAddress);
            }
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
            FunctionCall call = new FunctionCall
            {
                ReturnAddress = Pop(),
                SavedFlags = (StatusFlags)Pop(),
                Arguments = new int[Pop()]
            };

            for (int l = call.Arguments.Length, i = l - 1; i >= 0; i--)
                call.Arguments[i] = Pop();

            return call;
        }

        /// <summary>
        /// Returns, whether a function call can be peeked (or popped) from the callstack
        /// </summary>
        /// <returns>Check result</returns>
        public bool CanPeekCall()
        {
            if (StackSize >= 3)
            {
                int ra = Pop();
                int sf = Pop();
                int sz = Pop();
                bool res = StackSize >= sz;

                Push(sz);
                Push(sf);
                Push(ra);

                return res;
            }
            else
                return false;
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

                if (arg.IsParameter)
                {
                    FunctionCall call = PeekCall();
                    int argc = call.Arguments.Length;

                    val = val < argc ? call.Arguments[val] : throw new ArgumentOutOfRangeException($"The current function call has not {val} arguments. Please provide an arument index between 0 and {argc}.");
                }

                if (arg.IsAddress)
                {
                    if (!arg.IsKernel)
                        val = UserToKernel(val);
                    else if (!IsElevated)
                        throw new MissingPrivilegeException();

                    if (arg.IsIndirect)
                    {
                        val = KernelSpace[val];

                        return KernelSpace + (arg.IsKernel ? val : UserToKernel(val));
                    }
                    else
                        return KernelSpace + val;
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
        /// Returns, whether an integer can be peeked (or popped) from the callstack (UNSAFE!)
        /// </summary>
        /// <returns>Check result</returns>
        public bool CanPeek() => StackSize > 0;

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

        /// <summary>
        /// Translates the given user-space address to the corresponding kernel-space address
        /// </summary>
        /// <param name="addr">User-space address</param>
        /// <returns>Kernel-space address</returns>
        public int UserToKernel(int addr) => VerifyUserspaceAddr(addr, addr + MEM_OFFS / 4);

        /// <summary>
        /// Sets the processor's given I/O-port to the given value externally, meaning that the processor is not aware of the changement
        /// </summary>
        /// <param name="port">I/O-port</param>
        /// <param name="value">New I/O value</param>
        public void SetIOExternally(int port, byte value)
        {
            byte* ptr = raw + IO_OFFS + port;

            *ptr &= 0xf0;
            *ptr |= (byte)(value & 0x0f);
        }

        internal void Write(string s)
        {
            StandardOutput?.Write(s);

            OnTextOutput?.Invoke(this, s);
        }

        internal void WriteLine(string s) => Write(s + '\n');

        internal void VerifyUserspaceAddr(int addr) => VerifyUserspaceAddr<object>(addr, null);

        internal void VerifyUserspaceAddr(int addr, Action action) => VerifyUserspaceAddr(addr, () =>
        {
            action();

            return null as object;
        });

        internal T VerifyUserspaceAddr<T>(int addr, T value) => VerifyUserspaceAddr(addr, () => value);

        internal T VerifyUserspaceAddr<T>(int addr, Func<T> action) =>
            (addr >= 0) && (addr < Size) ? action() : throw new IndexOutOfRangeException($"The given memory address {addr} (0x{addr:x8}) is invalid. It must be a positive integer value between 0 and {Size} (0x{Size:x8})");

        internal bool GetInformationFlag(InformationFlags flag) => InformationFlags.HasFlag(flag);

        internal void SetInformationFlag(InformationFlags flag, bool value) => InformationFlags = (InformationFlags & ~flag) | (value ? flag : 0);

        #endregion
        #region .CTOR/.DTOR

        ~Processor() => Dispose();

        public void Dispose()
        {
            if (!disposed)
            {
                Marshal.FreeHGlobal((IntPtr)raw);

                OnDisposed?.Invoke(this);
            }

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
            : this(size, MAX_STACKSZ, cpuid)
        {
        }

        /// <summary>
        /// Creates a new MCPU-processor instance with the given userspace-size (in 4-byte-blocks) and callstack size (in 4-byte-blocks) and assigns the given CPUID to the processor
        /// </summary>
        /// <param name="size">Userspace size</param>
        /// <param name="stacksize">Callstack size</param>
        /// <param name="cpuid">CPU ID</param>
        /// <exception cref="OutOfMemoryException">Thrown if the givn memory size is to big</exception>
        public Processor(int size, int stacksize, int cpuid)
        {
            if (size > MAX_MEMSZ)
                throw new OutOfMemoryException($"The (currently) maximum supported memory size are {MAX_MEMSZ * 4} bytes.");
            if (stacksize > MAX_STACKSZ)
                throw new OutOfMemoryException($"The (currently) maximum supported callstack size are {MAX_STACKSZ * 4} bytes.");

            int raw_size = (4 * size) + MEM_OFFS + (stacksize * 4);

            raw = (byte*)Marshal.AllocHGlobal(raw_size);

            for (int i = 0; i < raw_size; i++)
                raw[i] = 0;

            IO = new IOPorts(this);
            Ticks = 0;
            Size = size;
            CPUID = cpuid;
            StackSize = 0;
            RawSize = raw_size;
            Instructions = new Instruction[0];
            
            Contract.Assert(StackPointer == StackBasePointer);
        }

        #endregion

        /// <summary>
        /// Returns the byte-representation of the given processor's memory
        /// </summary>
        /// <param name="proc">Processor</param>
        /// <returns>Memory byte representation</returns>
        public static implicit operator byte[](Processor proc) => proc.ToBytes();
    }

    /// <summary>
    /// Represents an exception, which occures if a 'regular' user tries to perform kernel actions
    /// </summary>
    public sealed class MissingPrivilegeException
        : MCPUProcessingException
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
    public sealed class StackException
        : MCPUProcessingException
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
    /// Represents an exception, which occures while processing an faulty instruction
    /// </summary>
    [Serializable]
    public class MCPUProcessingException
        : InvalidOperationException
    {
        /// <summary>
        /// The faulty instruction
        /// </summary>
        public Instruction Instruction { get; }


        /// <summary>
        /// Creates a new MCPUProcessingException with the given message
        /// </summary>
        /// <param name="message">Exception message</param>
        public MCPUProcessingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new MCPUProcessingException with the given message and the given inner exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public MCPUProcessingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new MCPUProcessingException with the given inner exception and faulty instruction
        /// </summary>
        /// <param name="ex">Inner exception</param>
        /// <param name="ins">Faulty instruction</param>
        public MCPUProcessingException(Exception ex, Instruction ins)
            : base($"Invalid MCPU operation: {ex.Message}", ex) => Instruction = ins;
    }

    /// <summary>
    /// Represents the I/O-port-range of a MCPU-processor
    /// </summary>
    public unsafe class IOPorts
        : IEnumerable<IOPort>
    {
        private List<IOPort> plist;
        internal Processor proc;
        

        /// <summary>
        /// Accesses the I/O-port at the given index
        /// </summary>
        /// <param name="port">I/O-port index</param>
        /// <returns>I/O-port</returns>
        /// <exception cref="IndexOutOfRangeException">The I/O-port index is out of range</exception>
        /// <exception cref="InvalidOperationException">A value has been written to a read-only I/O-port</exception>
        public IOPort this[int port]
        {
            get => IsInRange(port) ? *((IOPort*)(proc.raw + Processor.IO_OFFS + port))
                                   : throw new IndexOutOfRangeException($"The I/O-port index must be a positive value between 0 and {Processor.IO_OFFS}.");
            set
            {
                IOPort old = this[port];

                if ((old.Direction == IODirection.In) && (value.Value != old.Value))
                    throw new InvalidOperationException($"The I/O-port no. {port} is set to read-only.");
                else
                    *((IOPort*)(proc.raw + Processor.IO_OFFS + port)) = value;
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

        /// <summary>
        /// Returns an enumerator that iterates through the port collection
        /// </summary>
        /// <returns>Generic enumerator</returns>
        public IEnumerator<IOPort> GetEnumerator()
        {
            for (int i = 0; i < Processor.IO_COUNT; i++)
                yield return this[i];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the port collection
        /// </summary>
        /// <returns>Non-generic enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal bool IsInRange(int port) => (port >= 0) && (port < Processor.IO_COUNT);

        internal IOPorts(Processor proc) => this.proc = proc;
    }

    /// <summary>
    /// Represents an I/O-port
    /// </summary>
    [Serializable, NativeCppClass, StructLayout(LayoutKind.Sequential, Size = 1, Pack = 0)]
    public struct IOPort
    {
        internal byte raw;


        /// <summary>
        /// Returns the raw value, which represents the current I/O-port
        /// </summary>
        public byte Raw => raw;

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

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance. </param>
        /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. </returns>
        public override bool Equals(object obj) => obj is IOPort other ? other == this : false;

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>The fully qualified type name.</returns>
        public override string ToString() => $"{raw:x2}: ({Value}, {Direction})";

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() => raw;

        public static bool operator ==(IOPort p1, IOPort p2) => p1.raw == p2.raw;

        public static bool operator !=(IOPort p1, IOPort p2) => !(p1 == p2);

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
        /// Indicates that the comparison is comparing two floating-point inputs instead of integer ones
        /// </summary>
        Float = 0b0000_0000_1000_0000,
        /// <summary>
        /// Indicates that the first comparison input is not a (floating-point) number
        /// </summary>
        NaN1 = 0b0000_0000_0100_0000,
        /// <summary>
        /// Indicates that the second comparison input is not a (floating-point) number
        /// </summary>
        NaN2 = 0b0000_0000_0010_0000,
        /// <summary>
        /// Indicates that the first comparison input is equal to infinity (positive or negative)
        /// </summary>
        Infinity1 = 0b0000_0000_0001_0000,
        /// <summary>
        /// Indicates that the second comparison input is equal to infinity (positive or negative)
        /// </summary>
        Infinity2 = 0b0000_0000_0000_1000,
        /// <summary>
        /// Indicates that the first comparison input is equal to negative infinity
        /// </summary>
        NegativeInfinity1 = Sign1 | Infinity1,
        /// <summary>
        /// Indicates that the second comparison input is equal to negative infinity
        /// </summary>
        NegativeInfinity2 = Sign2 | Infinity2,


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
        /// Indicates, that the interrupt handling is currently enabled
        /// </summary>
        InterruptEnable = 0b0010_0000_0000_0000,
        /// <summary>
        /// Indicates, that the processor is currently handling an interrupt
        /// </summary>
        InterruptCurrent = 0b0001_0000_0000_0000,
        /// <summary>
        /// Represents no flag
        /// </summary>
        Empty = 0b0000_0000_0000_0000,
    }

    /// <summary>
    /// Represents the possible MCPU processor error handling modes
    /// </summary>
    [Serializable]
    public enum ErrorHandlingMode
        : byte
    {
        /// <summary>
        /// An exception will only be thrown if no interrupt handler was found
        /// </summary>
        InterruptOrThrow = 0,
        /// <summary>
        /// An exception will always be thrown
        /// </summary>
        AlwaysThrow = 1,
        /// <summary>
        /// An exception will never be thrown
        /// </summary>
        AlwaysInterrupt = 2,
    }
}
