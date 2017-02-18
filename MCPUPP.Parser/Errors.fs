namespace MCPU.MCPUPP.Parser

open System.Collections.Generic
open System.Linq
open System

type CompilerException(message : string) =
    inherit Exception(message)

module Errors =
    let DefaultStrings : Dictionary<string, string> =
        Enumerable.ToDictionary([|
                                    "ERR_LEXER", "Lexer error: {0}."
                                    "ERR_PARSER", "Parser error: {0}."
                                    "VARIABLE_EXISTS", "The variable '{0}' has already been defined."
                                    "FUNCTION_EXISTS", "The function '{0}' has already been defined."
                                    "NOT_FOUND", "The variable or function '{0}' could not be found."
                                    "IVAL_CAST", "An instance of the type '{0}' cannot be converted to the type '{1}'."
                                    "IVAL_BREAK", "The break statement must be used inside a loop."
                                    "IVAL_INDEX", "An expression of the type '{0}' cannot be indexed."
                                    "IVAL_UOP", "The operator '{0}' cannot be applied to a value of the type '{1}'."
                                    "IVAL_BOP", "The operator '{0}' cannot be applied to arguments of the types '{1}' and '{2}'."
                                    "ARRAY_EXPECTED", "An array type has been expected as argument."
                                    "FUNC_EXPECTED_ARGC", "The function '{0}' expects {1} arguments."
                                    "IVAL_ARG", "Invalid argument №{0} for function '{1}' given: Expected a value of the type '{2}', but recived an argument of the type '{3}'."
                                    "MISSING_MAIN", "The program's entry-point function 'void main(void)' could not be found."
                                    "IVAL_MCPUASM", "Unable to parse the inline-MCPU assembly code."
                                    "IVAL_VARTYPE", "The type '{0}' cannot be used as variable type."
                                    "IVAL_PRE_BOP", "The binary operator '{0}' could not be pre-compiled."
                                |], (fun (k, _) -> k), (fun (_, v) -> v))
    let mutable (* BUUH ! *) internal LanguageStrings : Dictionary<string, string> = DefaultStrings

    let UpdateLanguage (lang) =
        if lang = null then
            LanguageStrings <- DefaultStrings
        else
            // TODO : verify that each index exists
            LanguageStrings <- lang
       
    let GetFormatString name = LanguageStrings.[name]
    let format s ([<ParamArray>]a) = String.Format(LanguageStrings.[s], a)
    let inline (==>) s a = format s a |> CompilerException

    let LexerError a = "ERR_LEXER" ==> [|box a|]
    let ParserError a = "ERR_PARSER" ==> [|box a|]
    let VariableAlreadyDefined a = "VARIABLE_EXISTS" ==> [|box a|]
    let FunctionAlreadyDefined a = "FUNCTION_EXISTS" ==> [|box a|]
    let NameNotFound a = "NOT_FOUND" ==> [|box a|]
    let InvalidConversion s t = "IVAL_CAST"  ==> [|box s; box t|]
    let InvalidBreak () = "IVAL_BREAK" ==> [||]
    let CannotIndex a = "IVAL_INDEX" ==> [|box a|]
    let CannotApplyUnaryOperator o t = "IVAL_UOP" ==> [|box o; box t|]
    let CannotApplyBinaryOperator o t1 t2 = "IVAL_BOP" ==> [|box o; box t1; box t2|]
    let ArrayExpected () = "ARRAY_EXPECTED" ==> [||]
    let InvalidArgumentCount f p = "FUNC_EXPECTED_ARGC" ==> [|box f; box p|]
    let InvalidArgument f i g e = "IVAL_ARG" ==> [|box i; box f; box e; box g|]
    let MissingEntryPoint () = "MISSING_MAIN" ==> [||]
    let UnableParseInlineAsm () = Piglet.Lexer.LexerException LanguageStrings.["IVAL_MCPUASM"]
    let InvalidVariableType t = "IVAL_VARTYPE" ==> [|box t|]
    let InvalidOperator o = "IVAL_PRE_BOP" ==> [|box o|]
