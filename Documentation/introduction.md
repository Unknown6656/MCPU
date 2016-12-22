# MCPU Introduction

The MCPU is a 32-Bit little-endian processor, which basically consists of memory-mapped I/O-ports, registers and various dedicated memory segments.<br/>
The basic memory layout can be visualized as follows:
```
     0   1   2   3   4   5   6   7   8   9   a   b   c   d   e   f    BYTES
   .---------------.---------------.-------.-------.---------------.
00 ¦    CPU ID     ¦       IP      ¦ FLAGS ¦ INFO. ¦  MCPU Ticks   ¦ 0f
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

The CPU-ID is a uniquely assigned ID used to identify individual MCPU-instances. It can be accessed using the `CPUID`-instruction.

### Instruction Pointer (IP)

The instruction pointer is a 32-Bit register, which points to the currently executed instruction, which is stored inside the instruction space. One must note, that the IP does not point to a byte offset, but to the instruction index inside the instruction segment.

### StatusFlags-register (FLAGS)

The FLAGS-register is a 16-Bit register, which stores information about comparisons between values. It is used for conditional jumping, e.g. for calls/jumps which are dependant from certain memory values or function parameters.
The FLAGS-register is defined as follows:
```
         0   1   2   3   4   5   6   7   8   9   a   b   c   d   e   f    BITS
       .---.---.---.---.---.---.---.---.-------------------------------.
    00 ¦ . ¦ . ¦ . ¦ . ¦ . ¦ . ¦ . ¦ . ¦///////////////.///////////////¦ 0f
       '-|-'-|-'-|-'-|-'-|-'-|-'-|-'-|-'---------------|---------------'
         |   |   |   |   |   |   |   |                 |
 Zero 1 -'   |   |   |   |   |   |   |    (currently) Unused bits
 Zero 2 -----'   |   |   |   |   |   |
 Sign 1 ---------'   |   |   |   |   |
 Sign 2 -------------'   |   |   |   |
  Equal -----------------'   |   |   |
  Lower ---------------------'   |   |
Greater -------------------------'   '---- Unary comparison flag
```
The Flags `Zero 1`/`Zero 2` are `1`, if the first/second compared value were zero, respectively.
The Flags `Sign 1`/`Sign 2` are `0` if the first/second compared value are positive or zero and `1` if they were negative.
The Flag `Equal`, `Lower` and `Greater` are `1` if they fulfil their respective arithmetic description and `0` otherwise.
If the comparison was unary (even though a unary comparison `CMP a` is implemented as `CMP 0 a`), the 7<sup>th</sup> highest bit is set to `1` (or `0` otherwise).

### InformationFlags-register (INFO.) and MCPUTicks-register (TICKS)

The INFOFLAGS-register is a 16-Bit register, which stores 'global' processor information - meaning information, which will not be pushed/popped to/from the stack during calls.
```
         0   1   2   3   4   5   6   7   8   9   a   b   c   d   e   f    BITS
       .---.---.-------------------------------------------------------.
    00 ¦ . ¦ . ¦///////////////////////////.///////////////////////////¦ 0f
       '-|-'-|-'---------------------------|---------------------------'
         |   |                             |
   Elevated  |                (currently) Unused bits
        Running
```
The flag `Elevated` determines whether the processor is currently running with elevated (kernel) privilege or not (See [the language reference manual](./language-reference.md) for more information about elevated instructions and operations).
The flag `Running` indicates, that the processor is currently running. The processor's internal process-function is dependant from this particular flag, as it indicates whether the function shall continue processing or not.

The TICKS-register contains the number of complete processor cycles performed (fetching, analysing and processing an instruction).

### Size registers

The size registers `MEMS` (byte-offset 0x0010) and `INSTR_COUNT` (byte-offset 0x0014) contain the size of the user-space memory and instruction array, respectively.  
The `INSTR_COUNT`-register does contain the actual size of the byte-representation of the instruction array instead of the actual instruction count, as the name might indicate.
The MCPU-processor's total memory size can be calculated by adding the base pointer to the instruction-count-register.
The call-space offset can be determined by adding the memory offset (`0x0040`) to the `MEMS`-register.

### Stack and base pointer (SP and SBP/BP)

The stack base pointer (BP) contains the address of the bottom or base of the stack. This is the address `t0` in the top-most drawing of this document, as the stack grows from the top-most addresses to the bottom-most ones.
The stack pointer (SP) contains the size of the stack, which means that the top of stack is located at the address `t0 - size`.

Pushing an element onto the stack increases the SP, and writes the element at the new address `BASE_POINTER - SIZE`. Popping an element from the stack copies the element to the target address and decreases the SP by the element's size.

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
As each user-space memory value has the size of 32 Bits (or 4 bytes), the 4-byte address `n` inside the user-space represents the byte-offset `0x40 + 4 * n` inside the kernel-space, e.g.  
```
	MOV [10] 42
```
Moves the value '42' to the user-space memory address `10`, which is the kernel-address `0x4a` or `74`. The byte offset would be in this case `0x0128` or `296` for the least significant byte and `0x012B` or `299` for the most significant one.

The user-space memory has its size determined upon creation of the host processor. Its maximum size is (currently) 268.435.456 entries, which represent 1GB of user-space memory (Remember that each entry has a size of 4 bytes).

### Call space

The call-space is a memory-region, which resides after the user-space and is dedicated to store the current call stack.
If a program uses function calls, the return-address of the callee must be saved along with the FLAGS-register and the call parameters. To achieve this, the call-space represents a software-side stack of so-called call-frames.
Each call-frame can be visualized as follows:
```
     0   1   2   3   4   5   6   7   8   9   a   b                   n   BYTES
   .---------------.-------.-------.---------------.----- - - - - -----.
00 ¦ Return Addr.  ¦///////¦ FLAGS ¦ Param. Count  ¦    Parameters     ¦ nn
   '---------------'-------'-------'---------------'----- - - - - -----'
```
Where each parameter is a 4-byte value.
Before a call, the processor pushes all parameters in reversed order on the stack, followed by their count, the saved FLAGS-register (which will be reset to zero), 2 reserved bytes and the callee's return address.
After a `RET`-instruction, the processor pops the complete call frame from the stack and restores the FLAGS-register to its original state.

The maximum of the call-space size is (currently) 4.194.304 * 4 bytes, which represent 16MB of memory.

### Instruction space

The instruction space contains a serialized binary representation of the MCPU instructions, which are to be executed by the host processor. Each instruction is represented by the following structure:
```
     0   1   2   3               n   BYTES
   .-------.---.----- - - - - -----.
00 ¦OP Code¦ . ¦     Arguments     ¦ nn
   '-------'-|-'----- - - - - -----'
             |
 Argument count
```
Where each argument is stored as follows:
```
     0   1   2   3   4   BYTES
   .---.---------------.
00 ¦ . ¦     Value     ¦ 04
   '-|-'---------------'
     |
 Argument type
```
And the argument type as follows:
```
     0   1   2   3   4   5   6   7    BITS
   .---.---.-------.---.---.---.---.
00 ¦ . ¦ . ¦///////¦ . ¦ . ¦ . ¦ . ¦ 07
   '-|-'-|-'-------'-|-'-|-'-|-'-|-'
 Kernel  |           |   |   |   '-------- Address
  Mode   |           |   |   '------------ Indirect
         |           |   '---------------- Label
 Floating-point      '-------------------- Function parameter
   (planned)
```
After the execution of one instruction, the instruction pointer moves by one index, meaning `3 + n * 5` bytes -- unless the instruction in question is one which manipulates the value of the instruction pointer directly.
