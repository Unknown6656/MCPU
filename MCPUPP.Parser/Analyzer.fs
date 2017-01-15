namespace MCPU.MCPUPP.Parser

open MCPU.MCPUPP.Parser.SyntaxTree
open System.Collections.Generic
open System

type SymbolTable(program) as self =
    inherit Dictionary<IdentifierRef, VariableDeclaration>(HashIdentity.Reference)

type internal SymbolScope(parent : SymbolScope option) =
    let mutable list = List.empty<VariableDeclaration>
    let IdentifierFromDecl = function
                             | ScalarDeclaration(_, i)
                             | PointerDeclaration(_, i)
                             | ArrayDeclaration(_, i) -> i
    let DeclaresIdentifier (i : IdentifierRef) decl = (IdentifierFromDecl decl) = i.Identifier
    member x.AddDeclaration decl =
        let ifd = IdentifierFromDecl
        if List.exists (fun x -> ifd x = ifd decl) list then
            raise (Errors.VariableAlreadyDefined (ifd decl))
        list <- decl::list
    member x.FindDeclaration idref =
        match List.tryFind (fun x -> DeclaresIdentifier idref x) list with
        | Some d -> d
        | None -> match parent with
                  | Some ss -> ss.FindDeclaration idref
                  | None -> raise(Errors.NameNotFound idref.Identifier)


module Analyzer =
    
    do
        ()