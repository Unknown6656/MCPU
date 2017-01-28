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
    ¦---------------------------.---.---.---.-------.---.---.---.---¦
0f0 ¦        [ RESERVED ]       ¦CC.¦RET¦SYP¦ RESV. |SSZ¦GSZ¦LOF¦LSZ¦
    ¦---------------------------'---'---'---'-------'---'---'---'---+--- 1 KB
100 :                                                               :
    :                  global variables (.static)                   :
    :                                                               :
  s ¦---------------------------------------------------------------+--- 1 KB + GSZ (LOF[0])
    :                                                               :
    :                  local variables (.local)                     :
    :                                                               :
  n ¦---------------------------------------------------------------+--- ∑ LSZ (∀ calls)
    :///////////////////////////////////////////////////////////////:
    :///////////////////////////////////////////////////////////////:
    :///////////////////////////////////////////////////////////////:
  s ¦---------------------------------------------------------------+--- MEM_SZ - SSZ
    :                                                               :
    :                     string table (.text)                      :
    :                                                               :
    '---------------------------------------------------------------+--- MEM_SZ
```



(((TODO)))
