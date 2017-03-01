# MCPU++ Reference

The MCPU++ programming language is a higher-level language, which will be translated into MCPU-instructions before execution. The MCPU++ syntax reference can be found [here](./mcpu++-syntax.md)<br/>
The MCPU++ operates completely inside the user-space memory segment (see [the introduction](./introduction.md) for more information and relies on the following user-space memory layout:
```
  4-BYTE-BLOCKS
      0   1   2   3   4   5   6   7   8   9   a   b   c   d   e   f
    .---------------------------------------------------------------+--- 0 KB
000 ¦                                                               ¦
    :                                       .-----------------------¦
    :                                       ¦///////////////////////¦
    ¦---------------------------------------'///////////////////////¦
    :///////////////////////////////////////////////////////////////:
    :////////////// temporary values (.temp-section) ///////////////:
    ¦///////////////////////////////////////////////////////////////¦
    ¦-----------------------.---.---.---.---.---.---.---.---.---.---¦
0f0 ¦      [ RESERVED ]     |AUX¦CC.¦RET¦SYP¦MSZ¦HSZ¦LAC¦GSZ¦LOF¦LSZ¦
    ¦-----------------------'---'---'---'---'---'---'---'---'---'---+--- 1 KB
100 :                                                               :
    :                  global variables (.static)                   :
    :                                                               :
    ¦---------------------------------------------------------------¦    <--------------------------------------.
    :///////////////////////////////////////////////////////////////:                                           |
    :///////////////////////////////////////////////////////////////:                                           |
    :///////////////////////////////////////////////////////////////:                                           |
  s ¦---------------------------------------------------------------+--- 1 KB + GSZ (LOF[0])      <-.           |
    :                  function arguments (.args)                   :                               |           |
    ¦---------------------------------------------------------------+--- LOF[i] + LAC[i]            | MCPU++    | MCPU++
    :                                                               :                               | CALL      | CALL
    :                   local variables (.local)                    :                               | FRAME     | SPACE
    :                                                               :                               |           |
  n ¦---------------------------------------------------------------+--- LOF[i] + LAC[i] + LSZ[i] <-'           |
    :///////////////////////////////////////////////////////////////:                                           |
    :///////////////////////////////////////////////////////////////:                                           |
    :///////////////////////////////////////////////////////////////:                                           |
    ¦---------------------------------------------------------------+--- ∑(LSZ[i] + LAC[i])  (∀ calls i)  <----'
    :///////////////////////////////////////////////////////////////:
    :///////////////////////////////////////////////////////////////:
    :///////////////////////////////////////////////////////////////:
    ¦---------------------------------------------------------------+--- 
    :                                                               :
    :                    heap allocations (.heap)                   :
    :                                                               :
    '---------------------------------------------------------------+--- MEM_SZ
```

| Field | Description |
|-------|-------------|
| `AUX` | Auxilliary counter field |
| `CC` | Call counter (temporary counter field) |
| `RET` | Return value |
| `SYP` | SYA stack pointer |
| `MSZ` | Memory size |
| `HSZ` | Heap object count |
| `LAC` | Local argument size/count (the size of '.local') |
| `GSZ` | Global variable size/count (the size of '.static') |
| `LOF` | Local variable offset (the offset of '.args') |
| `LSZ` | Local variable size/count (the size of '.local') |



(((TODO)))


Heap object:
```
    (((TODO)))

    .------------.----.
    |    DATA    |SIZE|
    '------------'----'
```