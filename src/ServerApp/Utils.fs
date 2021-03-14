module Utils

open System

let normalizeEmpty value =
    if String.IsNullOrWhiteSpace(value) then ""
    else value.Trim()

let normalizeUserName userName =
    let safeValue = normalizeEmpty userName
    safeValue.ToLowerInvariant()
