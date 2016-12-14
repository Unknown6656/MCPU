using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCPU
{
    /// <summary>
    /// Represents the MSCPU-processor
    /// </summary>
    public unsafe sealed class Processor
        : IDisposable
    {
        #region FIELDS + CONSTANTS

        public const int EIP_OFFS = 0x04;
        public const int FLAG_OFFS = 0x08;
        public const int RESV_OFFS = 0x0a;
        public const int MEMS_OFFS = 0x10;
        public const int IO_OFFS = 0x20;
        public const int IO_COUNT = 0x20;
        public const int MEM_OFFS = 0x40;

        internal Instruction[] ins = null;
        internal bool disposed = false;
        internal byte* raw;

        #endregion
        #region PROPERTIES

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
            get => *((int*)(raw + EIP_OFFS));
            internal set => *((int*)(raw + EIP_OFFS)) = value;
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
        /// The MCPU flag register
        /// </summary>
        public Flags Flags
        {
            get => *((Flags*)(raw + MEMS_OFFS));
            private set => *((Flags*)(raw + MEMS_OFFS)) = value;
        }

        /// <summary>
        /// TODO --- Reserved
        /// </summary>
        internal ushort Reserved
        {
            get => *((ushort*)(raw + RESV_OFFS));
            private set => *((ushort*)(raw + RESV_OFFS)) = value;
        }

        /// <summary>
        /// Sets or gets the 4-byte integer value stored inside the given userspace memory address 
        /// </summary>
        /// <param name="addr">Userspace memory address</param>
        /// <returns>Value</returns>
        public int this[int addr]
        {
            get => VerifyUserspaceAddr(addr, () => ((int*)(raw + MEM_OFFS))[addr]);
            set => VerifyUserspaceAddr(addr, ((int*)(raw + MEM_OFFS))[addr] = value);
        }
        
        /// <summary>
        /// Accesses the I/O-ports of the current MCPU-processor
        /// </summary>
        public IOPorts IO { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public Instruction[] Instructions => null;

        /// <summary>
        /// Returns the byte-offset of the instruction region
        /// </summary>
        public int InstructionOffset => Size * 4 + MEM_OFFS;

        #endregion
        #region METHODS



        public void MoveNext()
        {

        }

        public void Process(Instruction ins)
        {
            if (ins != null)
            {

            }
        }

        internal void VerifyUserspaceAddr(int addr) => VerifyUserspaceAddr<object>(addr, null);

        internal T VerifyUserspaceAddr<T>(int addr, T value) => VerifyUserspaceAddr(addr, () => value);

        internal T VerifyUserspaceAddr<T>(int addr, Func<T> action) =>
            (addr >= 0) && (addr < Size) ? action() : throw new IndexOutOfRangeException($"The given memory address is invalid. It must be a positive integer value between 0 and {Size}");
        
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
            raw = (byte*)Marshal.AllocHGlobal(4 * size + MEM_OFFS);
            IO = new IOPorts(raw);
            Size = size;
            CPUID = cpuid;
        }

        #endregion
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
    public enum Flags
        : ushort
    {
    }
}
