namespace testing

open MCPU
open MCPU.Compiler

module FSTests =
    [<EntryPoint>]
    let main argv =
        let code = @""
        let res = MCPUCompiler.Compile code
        
        0
