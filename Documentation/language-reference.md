# MCPU Assembly language reference

## Basics

The MCPU assembly language is a case-insensitive Intel-syntax-based language to control the MCPU processor.
A MCPU program (unless empty) must declare its main entry-point by using the following token:
```
	.main
```
Any functions used inside the program must be used _before_ the `.MAIN`-token, and any instruction (unless belonging to a defined function) must be placed _after_ the `.MAIN`-token.

Comments are indicated by a leading semicolon:
```
	instruction  ; comment
	; comment
```

### Instructions

A MCPU instruction consists of an OP code, followed by zero or more arguments, which can be comma- or space-separated:
```
    opcode
    opcode arg1
    opcode arg1 arg2 arg3 ...
    opcode arg1, arg2, arg3, ...
```
Most instructions which take two or more arguments use the first one as operation target and the following arguments as operation sources. This syntax derives from Intel-based x86-assembly, in which the target is denoted before the source.

One example could be:
```
	MOV [10] 315
```
Which moves (or rather copies) the constant value `315` to the address `10`.  
A list of all instructions can be found [here](./instruction-set.md).

### Instruction parameters

A MCPU instruction can accept up to 255 parameters due to the internal representation of instructions.  
A function call can only accept up to 254 arguments, as the first argument must be the function's name.
The MCPU assembly language generally differentiates between multiple types of parameters (or arguments) which can be passed to a function or OP code.

The first type are constant parameters, which can have the following form:
```
	315			; decimal integer
	0b100111011		; binary integer with a leading '0b'
	0o473			; octal integer with a leading '0o'
	0x13b			; hexadecimal integer with a leading '0x'
	13Bh			; hexadecimal integer with a trailing 'h'
	-315			; negative integer
	4.2			; floating point number
	4.2f			; floating point number with trailing literal 'f'
	-.42			; negative floating point number without a leading '0'
	4.2e+4			; floating point number with exponential notation
	null			; constant 0
	true			; constant 1
	false			; constant 0
	pi			; constant π = 3.14159265358979
	phi			; constant φ = 1.61803398874989
	e			; constant e = 2.71828182845905
	tau			; constant τ = 2π = 6.28318530717959
```

The second type are user-space zero-indexed memory addresses (or kernel-space with appropriate privileges), which each represent a 32-bit storage address:  
```
	[7]			; Address No.7 (Bytes: 7*4...7*4+3, meaning the bytes 28...31)
	[0x0080]		; Address No.128 (bytes 512...515)
	[0b10]			; Address No.2 (bytes 4...7)
	[0o15]			; Address No.13 (bytes 52...55)
	
	; the following two examples are INVALID
	[-55]			; negative address
	[4.2]			; floating-point address
```

The third type are memory pointers, meaning memory addresses which in turn point to other memory addresses:
```
	MOV [1] 7		; Copies the constant value '7' into address '1'
	
	[[1]]			; Pointer at address '1' which points to the memory address '7'
```
Imagine the following code:
```
	.MAIN			; Program start
	MOV [0] 1		; Copy the value '1' into address '0'
	MOV [[0]] 42		; Copy the value '42' into the address, to which '0' points
	INCR [0]		; Increase the value in '0' by 1
	MOV [[0]] 315		; Copy the value '315' into the address, to which now '0' points
```
After its execution, the memory map looks as follows:
```
 ADDRESS | VALUE
=========X=========
   0000	 |    2
   0001	 |    42
   0002	 |    315
   0003	 |    0
   0004	 |    0
   0005	 |    0
    :    :    :
```
Kernel addresses (and indirect kernel address) can be accessed using a leading 'k' (see [the privileged section](#privileged-instructions-and-operations) for more information)


The fourth type are functions and labels, which can be addressed using their name:
```
FUNC myfunc			; Declaration of function 'myfunc'
	...			; function logic
END FUNC			; End of function 'myfunc'

	.MAIN			; Program start
	CALL myfunc		; Call the function 'myfunc'
	JMP mylabel		; Jump to the label 'mylabel'
mylabel:			; Label definition
```
See the section [`Control flow`](#control-flow) for more details about the usage and functionality of functions and labels.

The fifth type are function parameters: When a function is called, the caller can pass parameters to the functions, which can be addressed using the dollar-sign '$' followed by the zero-indexed number of the function parameter. e.g:
```
FUNC func
	ADD [$0] [$1]		; Add the value at the address, to which the second parameter points
				; 	to the value of the address, to which the first parameter points
	MUL [$0] $2		; Multiply the value in the address, to which the first parameter points
				; 	with the value of the third parameter
END func

	.MAIN			; Program start
	MOV [1] 4		; Copy value '4' to address '1'
	MOV [3] 2		; Copy value '2' to address '3'
	CALL func 3 1 5 	; Execute the function with the parameters 3, 1 and 5 which is equivalent to:
				;	ADD [3] [1]
				;	MUL [3] 5
				; The result will be:
				; 	([3] + [1]) * 5 = (4 + 2) * 5 = 6 * 5 = 30
```

### Control flow

The program's control flow is determined by the instruction pointer, as it always points to the instruction which shall be executed during the next 'tick'. The control flow can therefore be changed by modifying the IP.
This can be achieved using function calls (`CALL`-instruction) or conditional and unconditional jumps.<br/>
_Note, that when executing with kernel privilege, a program can also change the control flow by writing to the 4-byte memory address `0x0002` -- or the byte addressses `0x0004` to `0x0007`. See [the introduction](./introduction.md) for more information._

A function can be defined using the `FUNC`-token, and called using the `CALL`-OP code. The closing `END FUNC`-token is also an implicit `RET`-instruction, however, one can prematurely exit a function using `RET`:
```
FUNC myfunction
    instruction
    instruction
    instruction
    RET
END FUNC

    ...
    CALL myfunc
    ...
```
Function parameters are passed after the function name and can be accessed from within the function using the dollar-prefix `$` (See more inside [the section about function parameters](#instruction-parameters)).

A jump label must have a unique name throughout the program and can be defined as follows:
```
label:
    instruction
```
It can be used in unconditional jump expressions, like `JMP`:
```
	; The order of instruction execution will be 0, 1, 4
	
	instr.0
	instr.1
	JMP label
	instr.2
	instr.3
label:
	instr.4
```
The instruction `JMPREL` moves the instruction pointer relatively from the current instruction. This means, that the instruction `JMPREL 1` would execute the next one as normal, `JMPREL 2` would _skip_ the next instruction and `JMPREL -1` would (re-)execute the previous one etc.

Conditional jump instructions like `JLE`, `JL`, `JGE`, `JG`, `JE`, `JNE`, `JZ`, `JNZ`, `JPOS` and `JNEG` are jumping to the given label, if the FLAGS-register (see [the introduction](./introduction.md)) meets the required criteria after a comparison (see [the instruction set](./instruction-set.md)).
The following example jumps to the label `label`, if the value inside the address `0x0042` is smaller than zero
```
	.MAIN			; Program start
	.....
	CMP [42]		; Compare the value stored inside address 42 (to zero)
	JNEG label		; If the value is negative, jump to label
	...			; Execute these instructions if the value is positive
label:
	...			; Execute these instructions if the value is negative
```

### I/O Operations

The instructions `IO`, `IN` and `OUT` are designed to communicate with I/O-ports:
`IO <port> <direction>` defines the `port`<sup>th</sup> I/O-port's direction; the direction `0` indicates an outgoing (write-only) port and `1` indicates an ingoing (read-only) port.
The instruction `IN <port> <dst>` reads the port's value into the address `dst` - `OUT <port> <src>` writes the given value `src` into the I/O-port `port`.
Only the lowest four bits of the value provided by the `OUT`-instruction will be passed to the port, as I/O-ports work with 4-bit integer values (all values between inclusive `0` and inclusive `15`).

Example code:
```
	.MAIN			; Program start
	IO 10 1			; Set the port 10 to 'in'
	IO 12 0			; Set the port 12 to 'out'
	IN 10 [3]		; Reads the value from port 10 into address 3
	ADD [3] 7		; Increments the value in address 3 by 7
	OUT 12 [3]		; Writes the new value of address 3 to the port 12
```

_For more information about the I/O-ports see [the introduction](./introduction.md)._

### Floating-point operations

Floating-point operations (float-operations) are operations, which partly use IEEE-754 single-precision floating-point 32Bit-decimal numbers instead of plain 32Bit integer numbers. This allows the user and processor to perform calculations with real and rational numbers as well as integer numbers.
Float arguments can be denoted as follows:
```
	42.0			; Floating-point number with a decimal point
	+42.0f			; Floating-point number with a 'f'-suffix and a sign
	42.	            	; Floating-point number without trailing decimal places
	.42			; Floating-point number without leading decimal places
	4.2e1			; Floating-point number in exponential representation
	-.42e+2			; Floating-point number in exponential representation with factor and exponent signs
	-4.2			; Floating-point number with a sign
```
During compile time, a floating-point argument will be converted to the integer, whoms bytes matches the IEEE byte representation of the floating-point number in question, e.g.:
```
	MOV [1] 42.0
	; will be translated to:
	MOV [1] 0x00002842
	; the actual representation of '42.0' is '0x42280000', but the byte order is reversed
	;	due to the endianess of the machine.
```
It is therefore _possible_ to pass a floating-point argument to any instruction, though it is not advisable as it can result in unexpected behaviour.
Floating-point operations, however, assume that their given argument(s) are floating-point numbers. It can therefore be also unpredictable to pass a regular integer argument to a floating-point operation:
```
	MOV [7] 42.0	; Value inside address 7 : 0x00002842
	FADD [7] 315	; The value '315' will be interpreted as the floating-point number
			;	'4.414090162623174E-43'. As this number is much to small, the
			;	operation's result will be still '42.0' and not 42 + 315 = 357
```
In order to convert a floating-point number to a integer number, one must use the instructions `FICAST` and `IFCAST`. The instruction `FICAST` stands for _'**F**loat to **I**nteger **Cast**'_, which converts a floating point number to an integer one. The reverse instruction is `IFCAST` which stands for _'**I**nteger to **F**loat **Cast**'_.
```
	MOV [5] 13	; Moves '13' to address 5
	IFCAST [4] [5]	; Converts '13' to '13.0' and stores the result into address 4
	FSUB [4] 1.0	; Subtracts '1.0' from '13.0' (in address 4)
	FICAST [6] [4]	; Casts '12.0' to '12' and stores the result into address 6
```

### Privileged instructions and operations

_**Do not use privileged instructions if you do not know what you are doing!**_

Privileged instructions, operations and addresses are objects/functions, which are not usually accessible for a 'regular' user. Using the kernel privilege one can use them like any other instruction/address/....

To claim kernel privilege use the tokens `.kernel` or `.user`:
```
	.MAIN		; Program start
	....		; Everything executed here is being executed with user privileges (default)
	.KERNEL		; Claim kernel privilege
	....		; Everything executed here is being executed with Kernel privileges
	.USER		; Return back to user privilege
	....		; Everything executed here is being executed with user privileges again
```

The following items require kernel privileges to be executed or used:

#### Kernel addresses

Kernel addresses are 4-byte addresses, which map to the 'real' byte-addresses used internally by the emulator. They could be compared to physical memory-addresses in real computers.
As user-space addresses start at the byte offset `0x0040`, any user-space address `a` can be translated to the kernel address `a + 16`.
Kernel addresses are prefixed with the letter `k` when used inside a program:
```
	MOV k[20] 42	 ; Moves the value '42' to the kernel address 20, which represent the
			 ;	user-space address 20 - 16 == 4
	MOV k[2] k[[20]] ; Copies the instruction pointer to the address, to which the kernel
			 ; 	address 20 is pointing (in this case address 42)
```
User-space addresses are limited by the memory's size - kernel addresses, however, are not. This means, that one can address I/O-ports, the instruction space, call stack or even the parent host system memory by passing addresses outside the user-space memory range:
```
	MOV [0] k[8]	; Copies the byte-representation of the first 4 I/O-ports to the user-
			;	space address 0
	MOV [1] k[-1]	; Copies the 4-byte block BEFORE the processor's memory to the user-space
			; 	address 1
```
_**WARNING:** reading or writing to the host's system memory using kernel-space addresses is possible, but not advisable as they can seriously harm the host's system_

See [the introduction](./introduction.md) for more information about the user-space memory and kernel address mapping.

#### Privileged instructions

The instructions `LEA`, `SYSCALL`, `ABK`, `RESET` and all push-operations are privileged instructions. The `ABK`-instruction is a shortcut for the expression `SYSCALL -1`, more information on which can be found [here](./instruction-set.md) or in the [section about IP and stack management](#ip-and-stack-management).
The `LEA`-instruction loads the effective source memory address into the target address:
```
	LEA [7] [42]	; Loads the value '0x006A' into the user-space address 7, as the user-
			;	address 0x002A (= 42) translates into the kernel address 0x006A
```
The `RESET`-instruction halts the processor and resets its memory, I/O-ports, flags, call stack and instruction space. The CPU-ID will not be reset.

#### System calls

A system-call (syscall) is a processor-exposed method, which can be called from within the MCPU assembly code to execute specific methods like debugging etc.
A list of all  defined syscalls can be found [here](./syscalls.md).

#### IP and Stack management

Using kernel privileges, the processor's instruction pointer can be modified using kernel addresses as follows:
```
	...
	CMP k[2] 30	; Compares the the instruction pointer with the constant 30
	JG mylabel	; If more than 30 instructions have been executed, jump to 'mylabel'
	MOV k[2] 42	; (Else) jump to the 42nd instruction
mylabel:
	HALT		; halts the processor
	...
```

The stack is usually only modified by the instructions `CALL` and `RET`, as they push or pop a call-frame onto or from the stack.
However, one can manipulate the stack using the OP codes `0x0040` to `0x0049`, as they provide basic stack functionality for 4-byte entries instead of entire call-frames.
The instruction 'SSWAP' swaps the two top-most values on the stack.
The instruction `PUSH <src>` pushes the 4-byte value `src` onto the call stack, while `PEEK <dst>` and `POP <dst>` peek and pop the top-most stack value into the given destination address.
The instructions `PUSHI`, `POPI` and `PEEKI` push/pop/peek the instruction pointer instead of user-defined values or addresses. Therefore, the instructions `POPI` and `PEEKI` can also be used to modify the instruction pointer's value.
The instructions `PUSHF`, `POPF` and `PEEKF` push/pop/peek the FLAGS-register instead of user values or addresses.
