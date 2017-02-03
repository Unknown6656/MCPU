# MCPU
This project is a microprocessor emulator, which comes with its own instruction set, assembly language and emulation environment.  
It also comes with a higher-level programming language, named MCPU++, which can be executed on this microprocessor.
The name **MCPU** stands for **M**inimalistic **C**entral **P**rocessing **U**nit, but can also be interpreted as **M**ine**c**raft **P**rocessing **U**nit, as the initial project was based on the idea to simulate a microprocessor inside the sandbox-game Minecraft.

#### Build status (Windows 64Bit, VS 2017 RC)
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/master?svg=true&pendingText=master%20-%20pending&failingText=master%20-%20failed&passingText=master%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/master)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/dev?svg=true&pendingText=dev%20-%20pending&failingText=dev%20-%20failed&passingText=dev%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/dev)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/mcpu%2B%2B?svg=true&pendingText=mcpu%2B%2B%20-%20pending&failingText=mcpu%2B%2B%20-%20failed&passingText=mcpu%2B%2B%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/mcpu%2B%2B)<br/>
[<img src="https://img.shields.io/github/release/Unknown6656/MCPU.svg" height="30"/>](https://github.com/Unknown6656/MCPU/releases)

#### Testing status (Windows 64Bit)
[<img src="https://ci.appveyor.com/api/projects/status/fyvayfc9e82xh6eg/branch/master?svg=true&pendingText=master%20-%20pending&failingText=master%20-%20failed&passingText=master%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu-he9wv/branch/master)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/fyvayfc9e82xh6eg/branch/dev?svg=true&pendingText=dev%20-%20pending&failingText=dev%20-%20failed&passingText=dev%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu-he9wv/branch/dev)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/fyvayfc9e82xh6eg/branch/mcpu%2B%2B?svg=true&pendingText=mcpu%2B%2B%20-%20pending&failingText=mcpu%2B%2B%20-%20failed&passingText=mcpu%2B%2B%20-%20passed" height="30"/>](https://ci.appveyor.com/project/Unknown6656/mcpu-he9wv/branch/mcpu%2B%2B)

#### Stats
[<img src="https://img.shields.io/issuestats/i/github/Unknown6656/MCPU.svg" height="30"/>](https://github.com/Unknown6656/MCPU/issues)<br/>
[<img src="https://img.shields.io/issuestats/p/github/Unknown6656/MCPU.svg" height="30"/>](https://github.com/Unknown6656/MCPU/pulls)<br/>
[<img src="https://img.shields.io/github/downloads/Unknown6656/MCPU/total.svg" height="30"/><br/>](https://github.com/Unknown6656/MCPU/releases)

### Reference Manual

* [Introduction](https://github.com/Unknown6656/MCPU/tree/master/Documentation/introduction.md)
* [MCPU Instruction set](https://github.com/Unknown6656/MCPU/tree/master/Documentation/instruction-set.md)
* [MCPU Syscall table](https://github.com/Unknown6656/MCPU/tree/mastern/Documentation/syscalls.md)
* [MCPU Assembly Language reference](https://github.com/Unknown6656/MCPU/tree/master/Documentation/language-reference.md)
* [MCPU IDE Manual](https://github.com/Unknown6656/MCPU/tree/master/Documentation/ide.md)
* [MCPU++ Language reference](https://github.com/Unknown6656/MCPU/tree/master/Documentation/mcpu++.md)

### TODO-List

- [ ] IDE Documentation (!) **(currently in progress)**
- [ ] MCPU++ Documentation (!) **(currently in progress)**
- [ ] Function inlining
- [ ] Asynchronous processor execution
- [ ] Virtual Device support
- [ ] String processing and support
- [ ] IDE/Editor **(currently in progress)**
    - [x] compiler
    - [x] syntax highlighting
    - [X] multi-language support **(currently in progress)**
        - See issue #14
    - [x] auto-completition **(Nearly finished)**
    - [x] debugger **(currently in progress)**
    - [ ] refractoring
    - [ ] MCPU++ editor
