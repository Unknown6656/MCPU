# MCPU Instruction Set Reference Manual

The following table is a listing of all defined OP codes, their usage and description.

OP codes marked with the icon (((TODO))) require kernel privilege to execute. OP codes marked with the icon (((TODO))) perform IP (instruction pointer) manipulation, which can be used to change the control flow.


| Number | Name | Usage | Description |
|:------:|------|-------|-------------|
|`0x0000`|`NOP`|`NOP`| No Operation |
|`0x0001`|`HALT`|`HALT`| Halts the processor |
|`0x0002`|`JMP`|`JMP <label>`| Moves the instruction pointer to the given label |
|`0x0003`|`JMPREL`|`JMPREL <offset>`| Moves the instruction pointer by the given relative offset |
|`0x0004`|`ABK`|`ABK`| Equivalent to `SYSCALL -1` |
|`0x0005`|`SYSCALL`|`SYSCALL <number> [arguments]`| Executes a special system-internal function,<br/>which is a ssociated with the given syscall-number and <br/>passes the given arguments to the called system-function<br/>A list of syscalls can be found [here](https://github.com/Unknown6656/MCPU/blob/master/Documentation/syscalls.md) |
|`0x0006`|`CALL`|`CALL <function> [arguments]`| Calls the (user-defined) function and passes the given<br/>function arguments |
|`0x0007`|`RET`|`RET`| Returns from the currently executing function to the function callee |
|`0x0008`|`COPY`|`COPY <src> <dst> <size>`| Copies `size` bytes from the address `src` to the address `dst` |
|`0x0009`|`CLEAR`|`CLEAR <addr> <size>`| Sets all addresses between `addr` and `addr + size` to zero |
|`0x000a`|`IO`|`IO <port> <1/0>`| Sets the `port` I/O-port's direction to `read`(1) or `write`(0) |
|`0x000b`|`IN`|`IN <port> <dst>`| Reads from the I/O-port `port` the current value into the address `dst` |
|`0x000c`|`OUT`|`OUT <port> <src>`| Writes the value `src` into the I/O-port `port` |
|`0x000d`|`CLEARFLAGS`|`CLEARFLAGS`| Resets the FLAGS-register to zero |
|`0x000e`|`SETFLAGS`|`SETFLAGS <src>`| Writes the value `src` into the FLAGS-register |
|`0x000f`|`GETFLAGS`|`GETFLAGS <dst>`| Reads the value from the FLAGS-register into the address `dst` |
|`0x0010`|`MOV`|`MOV <dst> <src>`| Copies the value `src` into the address `dst` |
|`0x0011`|`LEA`|`LEA <dst> <src>`| Loads the effective (kernel) address from `src` into the address `dst` |
|`0x0012`|`ADD`|`ADD <a> <b>`| Adds the value `a` to `b` and stores the result into the address `a` |
|`0x0013`|`SUB`|`SUB <a> <b>`| (((TODO))) |
|`0x0014`|`MUL`|`MUL <a> <b>`| (((TODO))) |
|`0x0015`|`DIV`|`DIV <a> <b>`| (((TODO))) |
|`0x0016`|`MOD`|`MOD <a> <b>`| (((TODO))) |
|`0x0017`|`NEG`|`NEG <a>`| (((TODO))) |
|`0x0018`|`NOT`|`NOT <a>`| (((TODO))) |
|`0x0019`|`OR`|`OR <a> <b>`| (((TODO))) |
|`0x001a`|`AND`|`AND <a> <b>`| (((TODO))) |
|`0x001b`|`XOR`|`XOR <a> <b>`| (((TODO))) |
|`0x001c`|`NOR`|`NOR <a> <b>`| (((TODO))) |
|`0x001d`|`NAND`|`NAND <a> <b>`| (((TODO))) |
|`0x001e`|`NXOR`|`NXOR <a> <b>`| (((TODO))) |
|`0x001f`|`ABS`|`ABS <a>`| (((TODO))) |
|`0xffff`|`KERNEL`|`KERNEL <1/0>`| Enters (1) or exits (0) the privileged kernel execution mode |