namespace Myriad.Core
open Microsoft.FSharp.Compiler.Ast
open FsAst

module Create =
    let createMap (parent: LongIdent) (field: SynField)  =
        let field = field.ToRcd
        let fieldName = match field.Id with None -> failwith "no field name" | Some f -> f 

        let recordType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let varName = "x"
        let pattern =
            let name = LongIdentWithDots.Create([fieldName.idText])
            let arg =
                let named = SynPatRcd.CreateNamed(Ident.Create varName, SynPatRcd.CreateWild )
                SynPatRcd.CreateTyped(named, recordType)
                |> SynPatRcd.CreateParen

            SynPatRcd.CreateLongIdent(name, [arg])

        let expr =
            let ident = LongIdentWithDots.Create [ yield varName; yield fieldName.idText]
            SynExpr.CreateLongIdent(false, ident, None)

        let valData =
            let argInfo = SynArgInfo.CreateIdString "x"
            let valInfo = SynValInfo.SynValInfo([[argInfo]], SynArgInfo.Empty)
            SynValData.SynValData(None, valInfo, None)

        SynModuleDecl.CreateLet [{SynBindingRcd.Let with
                                    Pattern = pattern
                                    Expr = expr
                                    ValData = valData }]

    let createCreate (parent: LongIdent) (fields: SynFields) =
        let varIdent = LongIdentWithDots.CreateString "create"

        let recordType =
            LongIdentWithDots.Create (parent |> List.map (fun i -> i.idText))
            |> SynType.CreateLongIdent

        let pattern =
            let arguments =
                fields |> List.map (fun f ->let field = f.ToRcd
                                            let name = SynPatRcd.CreateNamed(field.Id.Value, SynPatRcd.CreateWild)
                                            SynPatRcd.CreateTyped(name, field.Type) |> SynPatRcd.CreateParen )

            SynPatRcd.CreateLongIdent(varIdent, arguments)

        let expr = 
            let fields =
                fields
                |> List.map (fun f ->   let field = f.ToRcd
                                        let fieldIdent = match field.Id with None -> failwith "no field name" | Some f -> f 
                                        let name = LongIdentWithDots.Create([fieldIdent.idText])
                                        let ident = SynExpr.CreateIdent fieldIdent
                                        RecordFieldName(name, true), Some ident, None)

            let newRecord = SynExpr.Record(None, None, fields, Microsoft.FSharp.Compiler.Range.range.Zero )
            SynExpr.CreateTyped(newRecord, recordType)

        let returnTypeInfo = SynBindingReturnInfoRcd.Create recordType
        SynModuleDecl.CreateLet [{SynBindingRcd.Let with Pattern = pattern; Expr = expr; ReturnInfo = Some returnTypeInfo }]

    let createRecordModule (data: {| namespaceId: LongIdent; recordId: LongIdent; recordFields : SynFields|}) =

        let openParent = SynModuleDecl.CreateOpen (LongIdentWithDots.Create (data.namespaceId |> List.map (fun ident -> ident.idText)))

        let fieldMaps = data.recordFields |> List.map (createMap data.recordId)
        let create = createCreate data.recordId data.recordFields
        let declarations = [
            yield openParent
            yield!fieldMaps
            yield create ]

        let info = SynComponentInfoRcd.Create data.recordId
        SynModuleDecl.CreateNestedModule(info, declarations)