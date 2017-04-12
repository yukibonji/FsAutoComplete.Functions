module Functions

open System
open System.Web
open System.Net.Http
open Newtonsoft.Json
open Microsoft.FSharp.Compiler.SourceCodeServices



let file = """
let x = 1
let y = "z"
let z = x + y
"""

let Run (req: HttpRequestMessage) =

    async {
        // let c = FSharpChecker.Create()

        // let! res = FsAutoComplete.Commands.declarations file 0 0
        // let x =
        //     match res with
        //     | None -> ""
        //     | Some r -> JsonConvert.SerializeObject r

        return
            AppDomain.CurrentDomain.GetAssemblies()
            |> Array.map (fun n ->
                let x = n.GetName()
                x.FullName )
            |> Array.where (fun n -> n.StartsWith "F")
            |> req.CreateResponse

    } |> Async.StartAsTask
