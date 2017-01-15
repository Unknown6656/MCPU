namespace MCPU.MCPUPP.Parser

type CompilerException(message : string) =
    inherit System.Exception(message)

module Errors =
    let LexerError a = CompilerException(sprintf "Lexer error: %s" a)
    let ParserError a = CompilerException(sprintf "Parser error: %s" a)
    let VariableAlreadyDefined a = CompilerException(sprintf "The variable %s has already been defined" a)
    let NameNotFound a = CompilerException(sprintf "The variable or function %s could not be found" a)
