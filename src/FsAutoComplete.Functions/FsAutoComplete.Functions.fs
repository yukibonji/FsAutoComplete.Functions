module Functions

open System
open System.Web
open System.Net.Http
open Newtonsoft.Json
open Microsoft.FSharp.Compiler.SourceCodeServices

module JsonSerializer =

    open System
    open Microsoft.FSharp.Reflection
    open Newtonsoft.Json
    open Newtonsoft.Json.Converters
    open Microsoft.FSharp.Compiler
    open Microsoft.FSharp.Compiler.SourceCodeServices


    type private FSharpErrorSeverityConverter() =
      inherit JsonConverter()

      override x.CanConvert(t:System.Type) = t = typeof<FSharpErrorSeverity>

      override x.WriteJson(writer, value, serializer) =
        match value :?> FSharpErrorSeverity with
        | FSharpErrorSeverity.Error -> serializer.Serialize(writer, "Error")
        | FSharpErrorSeverity.Warning -> serializer.Serialize(writer, "Warning")

      override x.ReadJson(_reader, _t, _, _serializer) =
        raise (System.NotSupportedException())

      override x.CanRead = false
      override x.CanWrite = true

    type private RangeConverter() =
      inherit JsonConverter()

      override x.CanConvert(t:System.Type) = t = typeof<Range.range>

      override x.WriteJson(writer, value, _serializer) =
        let range = value :?> Range.range
        writer.WriteStartObject()
        writer.WritePropertyName("StartColumn")
        writer.WriteValue(range.StartColumn + 1)
        writer.WritePropertyName("StartLine")
        writer.WriteValue(range.StartLine)
        writer.WritePropertyName("EndColumn")
        writer.WriteValue(range.EndColumn + 1)
        writer.WritePropertyName("EndLine")
        writer.WriteValue(range.EndLine)
        writer.WriteEndObject()

      override x.ReadJson(_reader, _t, _, _serializer) =
        raise (System.NotSupportedException())

      override x.CanRead = false
      override x.CanWrite = true

    type OptionConverter() =
      inherit JsonConverter()

      override x.CanConvert(t) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

      override x.WriteJson(writer, value, serializer) =
        let value =
          if isNull value then null
          else
            let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
            fields.[0]
        serializer.Serialize(writer, value)

      override x.ReadJson(reader, t, existingValue, serializer) =
          let innerType = t.GetGenericArguments().[0]
          let innerType =
            if innerType.IsValueType then (typedefof<Nullable<_>>).MakeGenericType([|innerType|])
            else innerType
          let value = serializer.Deserialize(reader, innerType)
          let cases = FSharpType.GetUnionCases(t)
          if isNull value then FSharpValue.MakeUnion(cases.[0], [||])
          else FSharpValue.MakeUnion(cases.[1], [|value|])

    let private jsonConverters =
      [|
       FSharpErrorSeverityConverter() :> JsonConverter
       RangeConverter() :> JsonConverter
       OptionConverter() :> JsonConverter
      |]

    let internal writeJson(o: obj) = JsonConvert.SerializeObject(o, jsonConverters)

type Request = {
    kind : string
    line : int
    column : int
    content : string
}

open FsAutoComplete.Commands

let Run (req: HttpRequestMessage) =
    async {
        try
            let serialize x = async {
                let! x' = x
                return JsonSerializer.writeJson x'
            }

            let! requestBody = req.Content.ReadAsStringAsync() |> Async.AwaitTask
            let request = JsonConvert.DeserializeObject<Request> requestBody
            let! res =
                match request.kind with
                | "parse" -> parse request.content request.line request.column |> serialize
                | "declarations" -> declarations request.content request.line request.column |> serialize
                | "completion" -> completion request.content request.line request.column |> serialize
                | "tooltip" -> tooltip request.content request.line request.column |> serialize
                | "typesig" -> typesig request.content request.line request.column |> serialize
                | "symbolUse" -> symbolUse request.content request.line request.column |> serialize
                | "help" -> help request.content request.line request.column |> serialize
                | "findDeclarations" -> findDeclarations request.content request.line request.column |> serialize
                | "methods" -> methods request.content request.line request.column |> serialize
                | _ -> async.Return ""
            return req.CreateResponse(Net.HttpStatusCode.OK, res, "application/json")
        with
        | _ -> return req.CreateResponse(Net.HttpStatusCode.OK, "error", "application/json")
    } |> Async.StartAsTask
