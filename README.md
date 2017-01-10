# MCPU
This project is a microprocessor emulator, which comes with its own instruction set, assembly language and emulation environment.  
It also comes with a higher-level programming language, named MCPU++, which can be executed on this microprocessor.
The name **MCPU** stands for **M**inimalistic **C**entral **P**rocessing **U**nit, but can also be interpreted as **M**ine**c**raft **P**rocessing **U**nit, as the initial project was based on the idea to simulate a microprocessor inside the sandbox-game Minecraft.

#### Build status
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/master?svg=true&pendingText=master%20-%20pending&failingText=master%20-%20failed&passingText=master%20-%20passed" height="40"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/master)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/dev?svg=true&pendingText=dev%20-%20pending&failingText=dev%20-%20failed&passingText=dev%20-%20passed" height="40"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/dev)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/k9t9jqap2iemau3c/branch/mcpu%2B%2B?svg=true&pendingText=mcpu%2B%2B%20-%20pending&failingText=mcpu%2B%2B%20-%20failed&passingText=mcpu%2B%2B%20-%20passed" height="40"/>](https://ci.appveyor.com/project/Unknown6656/mcpu/branch/mcpu%2B%2B)
#### Testing status
[<img src="https://ci.appveyor.com/api/projects/status/fyvayfc9e82xh6eg/branch/master?svg=true&pendingText=master%20-%20pending&failingText=master%20-%20failed&passingText=master%20-%20passed" height="40"/>](https://ci.appveyor.com/project/Unknown6656/mcpu-he9wv/branch/master)<br/>
[<img src="https://ci.appveyor.com/api/projects/status/fyvayfc9e82xh6eg/branch/dev?svg=true&pendingText=dev%20-%20pending&failingText=dev%20-%20failed&passingText=dev%20-%20passed" height="40"/>](https://ci.appveyor.com/project/Unknown6656/mcpu-he9wv/branch/dev)

### Reference Manual

* [Introduction](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/introduction.md)
* [MCPU Instruction set](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/instruction-set.md)
* [MCPU Syscall table](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/syscalls.md)
* [MCPU Assembly Language reference](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/language-reference.md)
* [MCPU IDE Manual](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/ide.md)
* [MCPU++ Language reference](https://github.com/Unknown6656/MCPU/blob/documentation/Documentation/mcpu++.md)

### TODO-List

- [ ] IDE Documentation (!)
- [x] Floating-point arithmetic
- [x] Unit Tests
- [x] Push releases (used [name generator](http://www.codenamegenerator.com/))
- [x] Instruction serialization
- [x] Instruction-space management
- [x] Instruction optimization (NOP, empty statements etc.)
- [ ] Function inlining
- [x] Kernel-space memory addressing
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
