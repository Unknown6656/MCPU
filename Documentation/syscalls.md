# MCPU System-internal (syscall) function specification

The following table is a listing of call system-internal function definitions, their associated number and their description.

| Number | Name      | Usage              | Description                              |
| ------ | --------- | ------------------ | ---------------------------------------- |
| `-1`   | `ABK`     | `SYSCALL -1`       | ABK instruction ([see here](https://github.com/Unknown6656/kit.edu/blob/master/CustomDefinitions/CustomDefinitions.pdf)). |
| `0`    | `INFO`    | `SYSCALL 0`        | Prints basic CPU information to the standard console output. |
| `1`    | `HEXDUMP` | `SYSCALL 1`        | Prints a complete hexadecimal dump of the processor's<br/>memory to the standard console output stream. |
| `2`    | `PRINT`   | `SYSCALL 2 [args]` | Prints the given values to the standard console output, e.g.<br/>Use `SYSCALL 2 [10] $3` to print the value at memory address<br/>`10`, followed by the value of the fourth function parameter. |
| `3`    | `FPRINT`  | `SYSCALL 3 [args]` | Prints the given floating-point values to the standard console<br/>output. |
| `4`    | `TICKS`   | `SYSCALL 4 <dst>`  | Copies the number of processor cycles (or ticks) to the given<br/>destination address `dst`. This is equivalent to `MOV <dst> k[3]` |
| `5`  | `TEXT` | `SYSCALL 5 [args]` | Prints all bytes inside the given arguments as extended-ASCII characters. The string does not need to be zero-terminated. |
| `6` | `SCAN` | `SYSCALL 6 <dst>` | Scans the next line from the standard console and parses it as signed 32Bit integer, which will be written into the address `dst`. If the input is invalid, the value of `dst` will not be overwritten. |
| `7` | `FSCAN` | `SYSCALL 7 <dst>` | Scans the next line from the standard console and parses it as signed 32Bit floating-point single-precision number, which will be written into the address `dst`. If the input is invalid, the value of `dst` will not be overwritten. |
