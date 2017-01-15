namespace MCPU.MCPUPP.Tests

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU.MCPUPP.Parser

module UnitTests =
    let Test01 =
        !~<("int a;", [
            GlobalVarDecl(
                ScalarDeclaration(
                    Int,
                    "a"
                )
            )
        ])

    do
        ()