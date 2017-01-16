namespace MCPU.MCPUPP.Parser

type CompilerException(message : string) =
    inherit System.Exception(message)

module Errors =
    let (!!) x = CompilerException x
                 |> raise
    let LexerError a = !!(sprintf "Lexer error: %s." a)
    let ParserError a = !!(sprintf "Parser error: %s." a)
    let VariableAlreadyDefined a = !!(sprintf "The variable %s has already been defined." a)
    let FunctionAlreadyDefined a = !!(sprintf "The function %s has already been defined." a)
    let NameNotFound a = !!(sprintf "The variable or function %s could not be found." a)
    let InvalidConversion s t = !!(sprintf "An instance of the type %s cannot be converted to the type %s." <| s.ToString() <| t.ToString())
    let InvalidBreak = ignore !!"The break statement must be used inside a loop."
    let CannotIndex a = !!(sprintf "An expression of the type %s cannot be indexed." a)
    let CannotApplyUnaryOperator o t = !!(sprintf "The operator %s cannot be applied to a value of the type %s." <| o.ToString() <| t.ToString())
    let CannotApplyBinaryOperator o t1 t2 = !!(sprintf "The operator %s cannot be applied to arguments of the types %s and %s." <| o.ToString() <| t1.ToString() <| t2.ToString())
    let ArrayExpected = ignore !!"An array type has been expected as argument"
    let InvalidArgumentCount f p = !!(sprintf "The function %s expects %d arguments" f p)
    let InvalidArgument f i g e = !!(sprintf "Invalid argument №%d for function %s given: Expected a value of the type %s, but recived an argument of the type %s." i f <| e.ToString() <| g.ToString())
    let MissingEntryPoint = ignore !!"The program's entry-point function 'void main()' could not be found"
