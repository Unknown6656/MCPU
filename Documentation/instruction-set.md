# MCPU Instruction Set Reference Manual

The following table is a listing of all defined OP codes, their usage and description.<br/>
All instruction generally operate with 32-Bit singed integer values, addresses, pointers and parameters -- unless indicated with the icon ![𝔉][flt], where given values can be interpreted as [IEEE-754 32-Bit (aka 'single-precision') floating-point decimal number](https://en.wikipedia.org/wiki/Single-precision_floating-point_format). Unpredictable results can occur if floating-point numbers are used in regular instructions, as the floating-point value will interpreted as an integer number matching its bitwise IEEE-754 representation

OP codes marked with the icon ![︽][elv] require kernel privilege to execute. OP codes marked with the icon ![⥃][ip] perform IP (instruction pointer) manipulation, which can be used to change the control flow.


| Number | Name | Usage | Description |
|-------:|------|-------|-------------|
|`0000`|`NOP`|`NOP`| No Operation |
|`0001`|`HALT`|`HALT`| Halts the processor |
|![⥃][ip]`0002`|`JMP`|`JMP <label>`| Moves the instruction pointer to the given label |
|![⥃][ip]`0003`|`JMPREL`|`JMPREL <offset>`| Moves the instruction pointer by the given relative offset |
|![︽][elv]`0004`|`ABK`|`ABK`| Equivalent to `SYSCALL -1` |
|![︽][elv]`0005`|`SYSCALL`|`SYSCALL <number> [arguments]`| Executes a special system-internal function,<br/>which is a ssociated with the given syscall-<br/>number and  passes the given arguments to<br/>the called system-function.<br/>A list of syscalls can be found [here](./syscalls.md) |
|![⥃][ip]`0006`|`CALL`|`CALL <function> [arguments]`| Calls the (user-defined) function and passes the given<br/>function arguments |
|![⥃][ip]`0007`|`RET`|`RET`| Returns from the currently executing function<br/>to the function callee |
|`0008`|`COPY`|`COPY <src> <dst> <size>`| Copies `size` bytes from the address `src`<br/>to the address `dst` |
|`0009`|`CLEAR`|`CLEAR <addr> <size>`| Sets all addresses between `addr` and<br/>`addr + size` to zero |
|`000a`|`IO`|`IO <port> <1/0>`| Sets the `port` I/O-port's direction to `read`(1) or `write`(0) |
|`000b`|`IN`|`IN <port> <dst>`| Reads from the I/O-port `port` the current value into the address `dst` |
|`000c`|`OUT`|`OUT <port> <src>`| Writes the value `src` into the I/O-port `port` |
|`000d`|`CLEARFLAGS`|`CLEARFLAGS`| Resets the FLAGS-register to zero |
|`000e`|`SETFLAGS`|`SETFLAGS <src>`| Writes the value `src` into the FLAGS-register |
|`000f`|`GETFLAGS`|`GETFLAGS <dst>`| Reads the value from the FLAGS-register into the address `dst` |
|`0010`|`MOV`|`MOV <dst> <src>`| Copies the value `src` into the address `dst` |
|![︽][elv]`0011`|`LEA`|`LEA <dst> <src>`| Loads the effective (kernel) address from `src` into the address `dst` |
|`0012`|`ADD`|`ADD <a> <b>`| Adds the value `a` to `b` and stores the result into `a` |
|`0013`|`SUB`|`SUB <a> <b>`| Subtracts the value `a` from `b` and stores the result into `a` |
|`0014`|`MUL`|`MUL <a> <b>`| Multiplies the values `a` and `b` and stores the result into `a` |
|`0015`|`DIV`|`DIV <a> <b>`| Divides the value `a` by `b` and stores the result into `a` |
|`0016`|`MOD`|`MOD <a> <b>`| Calculates the remainder from `a/b` and stores the result into `a` |
|`0017`|`NEG`|`NEG <a>`| Negates the value `a` and stores the result into `a` |
|`0018`|`NOT`|`NOT <a>`| Bitwise inverts the value `a` and stores the result into `a` |
|`0019`|`OR`|`OR <a> <b>`| Calculates the bitwise OR-combination of `a` and `b` and stores the result into `a` |
|`001a`|`AND`|`AND <a> <b>`| Calculates the bitwise AND-combination of `a` and `b` and stores the result into `a` |
|`001b`|`XOR`|`XOR <a> <b>`| Calculates the bitwise XOR-combination of `a` and `b` and stores the result into `a` |
|`001c`|`NOR`|`NOR <a> <b>`| Calculates the bitwise NOR-combination of `a` and `b` and stores the result into `a` |
|`001d`|`NAND`|`NAND <a> <b>`| Calculates the bitwise NAND-combination of `a` and `b` and stores the result into `a` |
|`001e`|`NXOR`|`NXOR <a> <b>`| Calculates the bitwise NXOR-combination of `a` and `b` and stores the result into `a` |
|`001f`|`ABS`|`ABS <a>`| Calculates the mathematical absolute value of `a` and stores the result into `a` |
|`0020`|`BOOL`|`BOOL <a>`| Writes `0` into `a` if `a` is zero, otherwise writes `1` into `a` |
|`0021`|`POW`|`POW <a> <b>`| Calculates the value of `a` raised to the power of `b` and stores the result into `a` |
|`0022`|`SHR`|`SHR <a> <b>`| Bitwise (logically) shifts the value `a` to the right by `b` bits and stores the result into `a` |
|`0023`|`SHL`|`SHL <a> <b>`| Bitwise shifts the value `a` to the left by `b` bits and stores the result into `a` |
|`0024`|`ROR`|`ROR <a> <b>`| Bitwise rotates the value `a` to the right by `b` bits and stores the result into `a` |
|`0025`|`ROL`|`ROL <a> <b>`| Bitwise rotates the value `a` to the left by `b` bits and stores the result into `a` |
|`0026`|`FAC`|`FAC <a>`| Calculates the factorial of `a` and stores the result into `a` |
|`0027`|`INCR`|`INCR <a>`| Equivalent to `ADD a, 1` |
|`0028`|`DECR`|`DECR <a>`| Equivalent to `SUB a, 1` |
|`0029`|`CMP`|`CMP <a> [b]`| Compares the two given values `a` and `b` (or `0` and `a` if only one is given) and stores the comparison result into the FLAGS-register. The structure of the FLAGS-register can be found [here](./introduction.md) |
|![⥃][ip]`002a`|`JLE`|`JLE <label>`| Jumps to `label` if the first compared value is lower or equal to the second one |
|![⥃][ip]`002b`|`JL`|`JL <label>`| Jumps to `label` if the first compared value is smaller than the second one |
|![⥃][ip]`002c`|`JGE`|`JGE <label>`| Jumps to `label` if the first compared value is greater or equal to the second one |
|![⥃][ip]`002d`|`JG`|`JG <label>`| Jumps to `label` if the first compared value is greater than the second one |
|![⥃][ip]`002e`|`JE`|`JE <label>`| Jumps to `label` if the both compared values are equal |
|![⥃][ip]`002f`|`JNE`|`JNE <label>`| Jumps to `label` if the both compared values are not equal |
|![⥃][ip]`0030`|`JZ`|`JZ <label>`| Jumps to `label` if the compared value is zero (or if both are in case of a dual-value comparison) |
|![⥃][ip]`0031`|`JNZ`|`JNZ <label>`| Jumps to `label` if the compared value is not zero (or if both are in case of a dual-value comparison) |
|![⥃][ip]`0032`|`JNEG`|`JNEG <label>`| Jumps to `label` if the compared value is negative (or if both are in case of a dual-value comparison) |
|![⥃][ip]`0033`|`JPOS`|`JPOS <label>`| Jumps to `label` if the compared value is positive (or if both are in case of a dual-value comparison) |
|`0034`||| _&lt;UNASSIGNED&gt;_ |
|`0035`||| _&lt;UNASSIGNED&gt;_ |
|`0036`||| _&lt;UNASSIGNED&gt;_ |
|`0037`||| _&lt;UNASSIGNED&gt;_ |
|`0038`||| _&lt;UNASSIGNED&gt;_ |
|`0039`||| _&lt;UNASSIGNED&gt;_ |
|`003a`||| _&lt;UNASSIGNED&gt;_ |
|`003b`||| _&lt;UNASSIGNED&gt;_ |
|`003c`|`SWAP`|`SWAP <a> <b>`| Swaps the values of the addresses `a` and `b` |
|`003d`|`CPUID`|`CPUID <dst>`| Copies the CPU ID into the address `dst` |
|`003e`|`WAIT`|`WAIT <ms>`| Suspends the processor's execution for `ms` milliseconds |
|![︽][elv]`003f`|`RESET`|`RESET`| Halts the processor and resets it to its original (clean) state |
|![︽][elv]`0040`|`PUSH`|`PUSH <src>`| Pushes `src` onto the stack |
|![︽][elv]`0041`|`POP`|`POP <dst>`| Pops the stack's top-most value and stores it into `dst` |
|![︽][elv]`0042`|`PEEK`|`PEEK <dst>`| Peeks the stack's top-most value and stores it into `dst` |
|![︽][elv]`0043`|`SSWAP`|`SSWAP`| Swaps the two top-most elements on the processor's stack |
|![︽][elv]`0044`|`PUSHF`|`PUSHF`| Pushes the FLAGS-register onto the stack |
|![︽][elv]`0045`|`POPF`|`POPF`| Pops the stack's top-most value and stores it into the FLAGS-register |
|![︽][elv]`0046`|`PEEKF`|`PEEKF`| Peeks the stack's top-most value and stores it into the FLAGS-register |
|![︽][elv]`0047`|`PUSHI`|`PUSHI`| Pushes the instruction pointer onto the stack |
|![⥃][ip]![︽][elv]`0048`|`POPI`|`POPI`| Pops the stack's top-most value and stores it into the instruction pointer |
|![⥃][ip]![︽][elv]`0049`|`PEEKI`|`PEEKI`| Peeks the stack's top-most value and stores it into the instruction pointer |
|`004a`||| _&lt;UNASSIGNED&gt;_ |
|`004b`||| _&lt;UNASSIGNED&gt;_ |
|`004c`||| _&lt;UNASSIGNED&gt;_ |
|`004d`||| _&lt;UNASSIGNED&gt;_ |
|`004e`||| _&lt;UNASSIGNED&gt;_ |
|`004f`||| _&lt;UNASSIGNED&gt;_ |
|![𝔉][flt]`0050`|`FICAST`|`FICAST <dst> <src>`| Casts the float value at `src` to an integer and stores the result into `src` |
|![𝔉][flt]`0050`|`IFCAST`|`IFCAST <dst> <src>`| Casts the integer value at `src` to a float and stores the result into `src` |
|![𝔉][flt]`0051`|`FADD`|`FADD <a> <b>`| Adds `a` to `b` and stores the result into `a` |
|![𝔉][flt]`0052`|`FSUB`|`FSUB <a> <b>`| Subtracts `a` from `b` and stores the result into `a` |
|![𝔉][flt]`0053`|`FMUL`|`FMUL <a> <b>`| Multiplies `a` by `b` and stores the result into `a` |
|![𝔉][flt]`0054`|`FDIV`|`FDIV <a> <b>`| Divides `a` by `b` and stores the result into `a` |
|![𝔉][flt]`0055`|`FMOD`|`FMOD <a> <b>`| Calculates the remainder of `a` divided by `b` and stores the result into `a` |
|![𝔉][flt]`0056`|`FNEG`|`FNEG <a>`| Calculates the additive inverse of `a` (= `-a`) and stores the result into `a` |
|![𝔉][flt]`0057`|`FINV`|`FINV <a>`| Calculates the multiplicative inverse of `a` (= `1/a`) and stores the result into `a` |
|![𝔉][flt]`0058`|`FSQRT`|`FSQRT <a>`| Calculates the square root of `a` and stores the result into `a` |
|![𝔉][flt]`0059`|`FROOT`|`FROOT <a> <b>`| Calculates the `b`th root of `a` and stores the result into `a` |
|![𝔉][flt]`005a`|`FLOG`|`FLOG <a> <b>`| Calculates the logarithm of `a` to the base `b` and stores the result into `a` |
|![𝔉][flt]`005b`|`FLOGE`|`FLOGE <a>`| Calculates the natural logarithm of `a` and stores the result into `a` |
|![𝔉][flt]`005c`|`FEXP`|`FEXP <a>`| Calculates the exponential of `a` (= `e^a`) and stores the result into `a` |
|![𝔉][flt]`005d`|`FPOW`|`FPOW <a> <b>`| Calculates the value of `a` raised to the `b`th power and stores the result into `a` |
|![𝔉][flt]`005e`|`FFLOOR`|`FFLOOR <a>`| Rounds `a` down to the nearest number and stores the result into `a`  |
|![𝔉][flt]`005f`|`FCEIL`|`FCEIL <a>`| Rounds `a` up to the nearest number and stores the result into `a`  |
|![𝔉][flt]`0060`|`FROUND`|`FROUND <a>`| Rounds `a` to the nearest number and stores the result into `a` |
|![𝔉][flt]`0061`|`FMIN`|`FMIN <a> <b>`| Determines the smaller number of `a` and `b` and stores it into `a` |
|![𝔉][flt]`0062`|`FMAX`|`FMAX <a> <b>`| Determines the greater number of `a` and `b` and stores it into `a` |
|![𝔉][flt]`0063`|`FSIGN`|`FSIGN <a>`|  |
|![𝔉][flt]`0064`|`FSIN`|`FSIN <a>`| Calculates the sine of `a` and stores the result into `a` |
|![𝔉][flt]`0065`|`FCOS`|`FCOS <a>`| Calculates the cosine of `a` and stores the result into `a` |
|![𝔉][flt]`0066`|`FTAN`|`FTAN <a>`| Calculates the tangent of `a` and stores the result into `a` |
|![𝔉][flt]`0067`|`FSINH`|`FSINH <a>`| Calculates the hyperbolic sine of `a` and stores the result into `a` |
|![𝔉][flt]`0068`|`FCOSH`|`FCOSH <a>`| Calculates the hyperbolic cosine of `a` and stores the result into `a` |
|![𝔉][flt]`0069`|`FTANH`|`FTANH <a>`| Calculates the hyperbolic tangent of `a` and stores the result into `a` |
|![𝔉][flt]`006a`|`FASIN`|`FASIN <a>`| Calculates the inverse sine of `a` and stores the result into `a` |
|![𝔉][flt]`006b`|`FACOS`|`FACOS <a>`| Calculates the inverse cosine of `a` and stores the result into `a` |
|![𝔉][flt]`006c`|`FATAN`|`FATAN <a>`| Calculates the inverse tangent of `a` and stores the result into `a` |
|![𝔉][flt]`006d`|`FATAN2`|`FATAN2 <a> <b>`| Calculates the inverse tangent of `a/b` and stores the result into `a` |
|`0xffff`|`KERNEL`|`KERNEL <1/0>`| Enters (1) or exits (0) the privileged kernel execution mode |


[elv]: ./elevated.png "Elevated instruction"
[ip]: ./switch.png "Special IP handling"
[flt]: ./float.png "Floating-point arithmetic"
