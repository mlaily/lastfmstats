module Utils

open System.Collections.Generic
open Fable.Core

/// A Dictionary of string * 'Value.
/// Translates to a JS object when compiled with Fable.
type Hash<'Value>(?initValue: IEnumerable<KeyValuePair<string, 'Value>>) =
    let storage =
        match initValue with
        | Some value -> new Dictionary<_,_>(value)
        | None -> new Dictionary<string,'Value>()
    new() = Hash<'Value>(?initValue = None)
    new(initValue: IEnumerable<KeyValuePair<string, 'Value>>) = Hash<'Value>(?initValue = Some initValue)

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
    interface IDictionary<string, 'Value> with
        member this.Add(key: string, value: 'Value): unit = storage.Add(key, value)
        member this.Add(item: KeyValuePair<string,'Value>): unit = (storage :> ICollection<KeyValuePair<string, 'Value>>).Add(item)
        member this.Clear(): unit = this.Clear()
        member this.Contains(item: KeyValuePair<string,'Value>): bool = (storage :> ICollection<KeyValuePair<string, 'Value>>).Contains(item)
        member this.ContainsKey(key: string): bool = storage.ContainsKey(key)
        member this.CopyTo(array: KeyValuePair<string,'Value> [], arrayIndex: int): unit = (storage :> ICollection<KeyValuePair<string, 'Value>>).CopyTo(array, arrayIndex)
        member this.Count: int = storage.Count
        member this.GetEnumerator(): IEnumerator<KeyValuePair<string,'Value>> = upcast storage.GetEnumerator()
        member this.GetEnumerator(): System.Collections.IEnumerator = upcast storage.GetEnumerator()
        member this.IsReadOnly: bool = (storage :> ICollection<KeyValuePair<string, 'Value>>).IsReadOnly
        member this.Item
            with get (key: string): 'Value = this.Item(key)
            and set (key: string) (v: 'Value): unit = this.Item(key) <- v
        member this.Keys: ICollection<string> = upcast storage.Keys
        member this.Remove(key: string): bool = storage.Remove(key)
        member this.Remove(item: KeyValuePair<string,'Value>): bool =  (storage :> ICollection<KeyValuePair<string, 'Value>>).Remove(item)
        member this.TryGetValue(key: string, value: byref<'Value>): bool = storage.TryGetValue(key, ref value)
        member this.Values: ICollection<'Value> = upcast storage.Values
