module Utils

open System.Collections.Generic
open Fable.Core

/// A Dictionary of string * 'Value.
/// Translates to a JS object when compiled with Fable.
type Hash<'Value>() =
    let storage = new Dictionary<string,'Value>()
    [<Emit("$0[$1]{{=$2}}")>]
    member __.Item with get(key: string): 'Value = storage.[key]
                    and set(key: string) (value: 'Value): unit = storage.[key] <- value
    [<Emit("Object.entries($0)")>]
    member this.Entries() : (string * 'Value)[] =
        storage
        |> Seq.map (fun x -> x.Key, x.Value)
        |> Array.ofSeq
    [<Emit("Object.keys($0)")>]
    member this.Keys() : string[] = storage.Keys |> Array.ofSeq
    [<Emit("Object.values($0)")>]
    member this.Values() : 'Value[] = storage.Values |> Array.ofSeq
    [<Emit("$0.hasOwnProperty($1)")>]
    member this.HasKey(key: string) : bool = true
    [<Emit("for (var k in $0) if ($0.hasOwnProperty(k)) delete $0[k]")>]
    member this.Clear() = storage.Clear()