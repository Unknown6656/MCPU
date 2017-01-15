namespace MCPU.MCPUPP.Tests

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU.MCPUPP.Parser

module UnitTests =
    let intline (<==>) code ast =
        let ast' = Lexer.parse code
        if ast' <> ast then failwith "Expected an other AST"
        ()
    
    let Test_01 = "" <==> Program()

    do
        ()