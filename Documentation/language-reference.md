# MCPU Assembly language reference

A MCPU instruction consists of an OP code, followed by zero or more arguments, which are comma-separated:
```
    opcode
    opcode arg1
    opcode arg1, arg2, arg3, ...
```
A jump label must have a unique (case-insensitive) name throughout the program and can be defined as follows:
```
label:
    instruction
```
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

