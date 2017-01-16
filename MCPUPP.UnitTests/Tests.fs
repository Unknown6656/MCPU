namespace MCPU.MCPUPP.Tests

open MCPU.MCPUPP.Parser.SyntaxTree
open MCPU.MCPUPP.Parser

module UnitTests =
    let Test01 =
        !~<("", List.empty<Declaration>)
    let Test02 =
        !~<("int a;", [
            GlobalVarDecl(
                ScalarDeclaration(
                    Int,
                    "a"
                )
            )
        ])
    let Test03 =
        !~<("void main(void) { }", [
            FunctionDeclaration(
                Unit,
                "main",
                [|
                |],
                (
                    [
                    ],
                    [
                    ]
                )
            )
        ])
    let Test04 =
        !~<("int test(float[] a, int* b, int c) { }", [
            FunctionDeclaration(
                Int,
                "test",
                [|
                    ArrayDeclaration(Float, "a")
                    PointerDeclaration(Int, "b")
                    ScalarDeclaration(Int, "c")
                |],
                (
                    [
                    ],
                    [
                    ]
                )
            )
        ])

    do
        ()