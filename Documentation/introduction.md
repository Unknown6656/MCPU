# MCPU Introduction

The MCPU is a 32-Bit little-endian processor, which basically consists of memory-mapped I/O-ports, registers and various dedicated memory segments.<br/>
The basic memory layout can be visualized as follows:
```
     0   1   2   3   4   5   6   7   8   9   a   b   c   d   e   f    BYTES
   .---------------.---------------.-------.-------.---------------.
00 ¦    CPU ID     ¦       IP      ¦ FLAGS ¦ INFO. ¦///////////////¦ 0f
   ¦---------------:---------------:-------'-------:---------------¦
10 ¦  Memory Size  ¦  Instr. Count ¦ Stack pointer ¦ Base pointer  ¦ 1f
   ¦---------------'---------------'---------------'---------------¦
20 ¦                                                               ¦ 2f
   ¦                           I/O-Ports                           ¦
30 ¦                                                               ¦ 3f
   ¦---------------------------------------------------------------¦
40 ¦                                                               ¦ 4f
   :                                                               :
   :                       Userspace memory                        :
   :                                                               :
n0 ¦                                                               ¦ nf
   ¦---------------------------------------------------------------¦
m0 ¦///////////////////////////////////////////////////////////////¦ mf
   :///////////////////////////////////////////////////////////////:
   :///////////////////////////////////////¦¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¦ rf
   ¦¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯'                       ¦
s0 ¦                           Call stack                          ¦ sf
   ¦---------------------------------------------------------------¦
t0 ¦                                                               ¦ tf
   ¦                      Instruction segment                      ¦
u0 ¦                                                               ¦ uf
   '---------------------------------------------------------------'
```
## Components
### CPUID-register

The CPU-ID is a uniquely assigned ID used to identify individual MCPU-instances.

### Instruction Pointer (IP)

The instruction pointer is a 32-Bit register, which points to the currently executed instruction, which is stored inside the instruction space. One must note, that the IP does not point to 

### StatusFlags-register (FLAGS)

(((TODO)))

### InformationFlags-register (INFO.)

(((TODO)))

### Size registers

(((TODO)))

### Stack and base pointer (SP and SBP/BP)

(((TODO)))

### I/O-Ports

The I/O-ports are memory-mapped ports, which can be used for basic input and output communication. The MCPU processor provides access to 32 I/O-ports which can be controlled using the instructions `IO`, `IN` and `OUT`. Each I/O-port is internally stored in a 8-Bit structure as follows:
```
     0   1   2   3   4   5   6   7    BITS
   .---.-----------.---------------.
00 ¦ . ¦/////./////¦     VALUE     ¦ 07
   '-|-'-----|-----'---------------'
     |       |
     |       Unused bits
     |
     Direction flag:
     	0 - Output (write)
     	1 - Input  (read)
```
A port can only read or write 4 bits at one time. If a larger value is pushed into the port, the value will be truncated to its last four bits.

### User-space memory

The user-space memory is the 'normally' accessible memory space, which starts at the byte offset `0x0040`.
As each user-space memory value has the size of 32 Bits (or 4 bytes), the 4-byte address `n` inside the user-space represents the byte-offset `0x40 + 4 * n` inside the kernel-space. The user-space memory has its size determined upon creation of the host processor. Its maximum size is (currently) 268.435.456 entries, which represent 1GB of user-space memory (Remember that each entry has a size of 4 bytes).

### Call space

The call-space is a memory-region, which resides after the user-space and is dedicated to store the current call stack.
If a program uses function calls, the return-address of the callee must be saved along with the FLAGS-register and the call parameters. To achieve this, the call-space represents a software-side stack of so-called call-frames.
Each call-frame can be visualized as follows:
```
     0   1   2   3   4   5   6   7   8   9   a   b                 n   BYTES
   .---------------.-------.-------.---------------.----- - - - - -----.
00 ¦ Return Addr.  ¦///////¦ FLAGS ¦ Param. Count  ¦    Parameters     ¦
   '---------------'-------'-------'---------------'----- - - - - -----'
```
Where each parameter is a 4-byte value.
Before a call, the processor pushes all parameters in reversed order on the stack, followed by their count, the saved FLAGS-register (which will be reset to zero), 2 reserved bytes and the callee's return address.
After a `RET`-instruction, the processor pops the complete call frame from the stack and restores the FLAGS-register to its original state.

The maximum of the call-space size is (currently) 4.194.304 * 4 bytes, which represent 16MB of memory.

### Instruction space

(((TODO)))





















