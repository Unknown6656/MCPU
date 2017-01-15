module MCPU.MCPUPP.Parser.Analyzer

open MCPU.MCPUPP.Parser.SyntaxTree
open System.Collections.Generic
open System

type SymbolScope(parent : SymbolScope option) =
    let mutable list = List.empty<VariableDeclaration>
    let IdentifierFromDecl = function
                             | ScalarDeclaration(_, i)
                             | PointerDeclaration(_, i)
                             | ArrayDeclaration(_, i) -> i
    let DeclaresIdentifier (i : IdentifierRef) decl = (IdentifierFromDecl decl) = i.Identifier
    member x.AddDeclaration decl =
        let ifd = IdentifierFromDecl
        if List.exists (fun x -> ifd x = ifd decl) list then
            Errors.VariableAlreadyDefined (ifd decl)
        list <- decl::list
    member x.FindDeclaration idref =
        match List.tryFind (fun x -> DeclaresIdentifier idref x) list with
        | Some d -> d
        | None -> match parent with
                  | Some ss -> ss.FindDeclaration idref
                  | None -> Errors.NameNotFound idref.Identifier

type SymbolScopeStack() =
    let stack = Stack<SymbolScope>()
    do
        stack.Push(SymbolScope None)
    
    member x.CurrentScope = stack.Peek()
    member x.Pop() = stack.Pop() |> ignore
    member x.Push() = stack.Push((SymbolScope << Some) x.CurrentScope)
    member x.AddDeclaration decl = x.CurrentScope.AddDeclaration decl
    
type VariableCoverType =
    | Scalar
    | Array
    | Pointer

type SymbolVariableType =
    {
        Type  : VariableType;
        Cover : VariableCoverType;
    }
    member x.IsInt = x.Type = Int
    member x.IsUnit = x.Type = Unit
    member x.IsFloat = x.Type = Float
    member x.IsArray = x.Cover = Array
    member x.IsScalar = x.Cover = Scalar
    member x.IsPointer = x.Cover = Pointer
    override x.ToString() =
        x.Type.ToString() + (match x.Cover with
                             | Pointer -> "*"
                             | Array -> "[]"
                             | Scalar -> "")
        
let ScalarType t = { Type = t; Cover = Scalar }

let TypeOfDeclaration = function
                        | ScalarDeclaration (t, _) -> { Type = t; Cover = Scalar }
                        | ArrayDeclaration (t, _)  -> { Type = t; Cover = Array }
                        | PointerDeclaration (t, _)  -> { Type = t; Cover = Pointer }

type SymbolTable(program) as self =
    inherit Dictionary<IdentifierRef, VariableDeclaration>(HashIdentity.Reference)

    let WhileStatementStack = Stack<WhileStatement>()
    let SymbolScopeStack = SymbolScopeStack()
    let rec ScanDeclaration = function
                              | GlobalVarDecl x -> SymbolScopeStack.AddDeclaration x
                              | FunctionDeclaration x -> ScanFunctionDeclaration x
    and ScanFunctionDeclaration (rettype, _, param, blockstat) =
        let rec ScanBlockStatement (locdecl, stat) =
            SymbolScopeStack.Push()
            List.iter (SymbolScopeStack.AddDeclaration) locdecl
            List.iter ScanStatement stat
            SymbolScopeStack.Pop()
            |> ignore
        and ScanStatement = function
                            | ExpressionStatement es -> match es with
                                                        | Expression e -> ScanExpression e
                                                        | Nop -> ()
                            | BlockStatement b -> ScanBlockStatement b
                            | IfStatement (e, s1, Some s2) ->
                                ScanExpression e
                                ScanStatement s1
                                ScanStatement s2
                            | IfStatement (e, s, None) ->
                                ScanExpression e
                                ScanStatement s
                            | WhileStatement (e, s) ->
                                WhileStatementStack.Push (e, s)
                                ScanExpression e
                                ScanStatement s
                                WhileStatementStack.Pop()
                                |> ignore
                            | ReturnStatement (Some e) -> ScanExpression e
                            | ReturnStatement None ->
                                if rettype <> Unit then
                                    Errors.InvalidConversion (rettype.ToString()) Builder.UnitString
                            | BreakStatement ->
                                if WhileStatementStack.Count = 0 then
                                    Errors.InvalidBreak
                            |_ -> ()
        and AddIdentifierMapping idref =
            let decl = SymbolScopeStack.CurrentScope.FindDeclaration idref
            self.Add(idref, decl)
        and ScanExpression = function
                             | ScalarAssignmentExpression (i, e)
                             | ArrayIdentifierExpression (i, e)
                             | PointerAssignmentExpression (i, e)
                             | PointerValueAssignmentExpression (i, e) ->
                                 AddIdentifierMapping i
                                 ScanExpression e
                             | IdentifierExpression i
                             | PointerValueIdentifierExpression i
                             | PointerAddressIdentifierExpression i -> AddIdentifierMapping i
                             | ArrayAssignmentExpression (i, e1, e2) ->
                                 AddIdentifierMapping i
                                 ScanExpression e1
                                 ScanExpression e2
                             | FunctionCallExpression (_, args) ->
                                 args
                                 |> List.iter ScanExpression
                             | LiteralExpression _ -> ()
                             | UnaryExpression (_, e)
                             | ArrayAllocationExpression (_, e) ->
                                 ScanExpression e
                             |_ -> ()
        
        SymbolScopeStack.Push()
        param
        |> Array.iter SymbolScopeStack.AddDeclaration
        ScanBlockStatement blockstat
        SymbolScopeStack.Pop()
        |> ignore
    do
        program
        |> List.iter ScanDeclaration

    member x.GetIdentifierType idref = TypeOfDeclaration self.[idref]
   
type FunctionTableEntry =
    {
        ReturnType     : VariableType;
        ParameterTypes : SymbolVariableType list;
    }
     
type PredefinedFunction = string * VariableType * SymbolVariableType list

// BUILD IN METHODS
let PredefinedFunctions : PredefinedFunction list = [
        ("iprint", Unit, [ { Type = Int; Cover = Scalar; } ])
        ("fprint", Unit, [ { Type = Float; Cover = Scalar; } ])
        ("iscan", Int, [])
        ("fscan", Float, [])
        ("halt", Unit, [])
        ("wait", Unit, [ { Type = Int; Cover = Scalar; } ])

        // TODO: add sine, cosine, log etc.
    ]

type FunctionTable(program) as self =
    inherit Dictionary<Identifier, FunctionTableEntry>()
    
    let rec ScanDeclaration = function
                              | GlobalVarDecl _ -> ()
                              | FunctionDeclaration (t, i, p, _) ->
                                  if self.ContainsKey i then
                                      Errors.FunctionAlreadyDefined i
                                  self.Add(i, {
                                      ReturnType = t
                                      ParameterTypes = p
                                                       |> Array.toList
                                                       |> List.map TypeOfDeclaration
                                  })
    do
        List.iter (fun (n, r, p) -> self.Add(n, { ReturnType = r; ParameterTypes = p })) PredefinedFunctions
        List.iter ScanDeclaration program

type ExpressionTypeDictionary(program, ftable : FunctionTable, stable : SymbolTable) as self =
    inherit Dictionary<Expression, VariableType>(HashIdentity.Reference)
    
    let rec ScanDeclaration = function
                              | FunctionDeclaration x -> ScanFunctionDeclaration x
                              | _ -> ()
    and ScanFunctionDeclaration (rettype, _, _, blockstat) =
        let rec ScanBlockStatement (_, stats) =
            List.iter ScanStatement stats
        and ScanStatement = function
                            | ExpressionStatement es -> match es with
                                                        | Expression e -> ScanExpression e
                                                                          |> ignore
                                                        | Nop -> ()
                            | BlockStatement b -> ScanBlockStatement b
                            | IfStatement (e, s1, Some s2) ->
                                ScanExpression e |> ignore
                                ScanStatement s1
                                ScanStatement s2
                            | IfStatement (e, s, None)
                            | WhileStatement (e, s) ->
                                ScanExpression e |> ignore
                                ScanStatement s
                            | ReturnStatement (Some e) ->
                                let type' = ScanExpression e
                                if type' <> ScalarType rettype then
                                    Errors.InvalidConversion type' rettype
                            |_ -> ()
        and ScanExpression expr =
            let CheckTypes s t = if s <> t then Errors.InvalidConversion s t
            let CheckIndexType e = CheckTypes (ScanExpression e) (ScalarType Int)
            let ExpressionType =
                 let ttransform (i, e) = (stable.GetIdentifierType i, ScanExpression e)
                 match expr with
                 | ScalarAssignmentExpression (i, e) ->
                     let i, e = ttransform(i, e)
                     CheckTypes i e
                     i
                 | PointerAssignmentExpression (i, e) ->
                     let i, e = ttransform(i, e)
                     // TODO !!!!!!
                     ()
                 | PointerValueAssignmentExpression (i, e) ->
                     let i, e = ttransform(i, e)

                     if not i.IsPointer then
                         Errors.CannotIndex (i.ToString())
                     elif not e.IsScalar then
                         Errors.InvalidConversion e i
                     else
                         CheckTypes i e
                     ScalarType i.Type
                 | ArrayAssignmentExpression (i, e1, e2) ->
                     CheckIndexType e1
                     let e2 = ScanExpression e2
                     let i = stable.GetIdentifierType i

                     if not i.IsArray then
                         Errors.CannotIndex (i.ToString())
                     elif not e2.IsScalar then
                         Errors.InvalidConversion e2 i
                     else
                         CheckTypes i e2
                     ScalarType i.Type
                 | BinaryExpression (e1, op, e2) ->
                     let e1 = ScanExpression e1
                     let e2 = ScanExpression e2
                     let fail = Errors.CannotApplyBinaryOperator op e1 e2
                     
                     if e1.IsArray <> e2.IsArray then
                        fail
                     else match op with
                          | Or | And | Xor ->
                              if (e1 = ScalarType Int) && (e2 = ScalarType Int) then
                                  ScalarType Int
                              else fail
                          | Equal | NotEqual ->
                              if e1 <> e2 then fail
                          
                              ScalarType Int
                          | LessEqual | Less | GreaterEqual | Greater -> ()
                          | Add | Subtract | Multiply | Divide | Modulus | Power ->
                              e1
             ()