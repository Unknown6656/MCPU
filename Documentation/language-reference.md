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
	315				; decimal integer
	0b100111011		; binary integer with a leading '0b'
	0o473			; octal integer with a leading '0o'
	0x13b			; hexadecimal integer with a leading '0x'
	13Bh			; hexadecimal integer with a trailing 'h'
	-315			; negative integer
	4.2				; floating point number
	4.2f			; floating point number with trailing literal 'f'
	-.42			; negative floating point number without a leading '0'
	4.2e+4			; floating point number with exponential notation
	null			; constant 0
	true			; constant 1
	false			; constant 0
```

The second type are user-space zero-indexed memory addresses (or kernel-space with appropriate privileges), which each represent a 32-bit storage address:  
```
	[7]				; Address No.7 (Bytes: 7*4...7*4+3, meaning the bytes 28...31)
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
	MOV [[0]] 42	; Copy the value '42' into the address, to which '0' points
	INCR [0]		; Increase the value in '0' by 1
	MOV [[0]] 315	; Copy the value '315' into the address, to which now '0' points
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
	...				; function logic
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
	ADD [$0] [$1]	; Add the value at the address, to which the second parameter points
					; 	to the value of the address, to which the first parameter points
	MUL [$0] $2		; Multiply the value in the address, to which the first parameter points
					; 	with the value of the third parameter
END func

	.MAIN			; Program start
	MOV [1] 4		; Copy value '4' to address '1'
	MOV [3] 2		; Copy value '2' to address '3'
	CALL func 3 1 5 ; Execute the function with the parameters 3, 1 and 5 which is equivalent to:
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
	...				; Execute these instructions if the value is positive
label:
	...				; Execute these instructions if the value is negative
```

### I/O Operations

(((TODO)))

### Floating-point operations

(((TODO)))

### Privileged instructions and operations

(((TODO)))
