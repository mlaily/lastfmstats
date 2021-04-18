﻿module ApiModels

// Types defined here are shared between the server and the client.
// Since we currently use native js deserialization with casting on the client side (zero overhead),
// it's important to use only simple or compatible types, to avoid nasty surprises.
//
// For example, int64 cannot be used transparently this way (see https://fable.io/docs/dotnet/numbers.html)
// and will lead to runtime errors when Fable uses them as its internal int64 representation but the value is really
// just a js Number coming straight from json.
// A simple solution is to use float (double in C#) instead, which translate directly to js Number...

type GenericError = {
    Error: string
}

type FlatScrobble = {
    Artist: string
    Album: string
    Track: string
    Timestamp: float // int64
}

type UserName = UserName of string

type GetResumeTimestampResponse = {
    ResumeFrom: float // int64
}

type InsertScrobblesResponse = {
    NewUsers: int
    NewArtists: int
    NewAlbums: int
    NewTracks: int
    NewScrobbles: int
}

type GetChartRequestOptions = {
    PageToken: float option // int64
    PageSize: int option
}

type GetChartDataResponse = {
    Timestamps: string[]
    Colors: string[]
    Texts: string[]
    // Paging properties:
    NextPageToken: float // int64
    TotalCount: int
}

type IMainApi = {
    getResumeTimestamp: UserName -> Async<GetResumeTimestampResponse>
    insertScrobbles: UserName -> FlatScrobble[] -> Async<Result<InsertScrobblesResponse, string>>
    getChartData: UserName -> GetChartRequestOptions -> Async<GetChartDataResponse>
}
