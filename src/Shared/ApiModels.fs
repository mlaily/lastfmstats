module ApiModels

open Utils

type FlatScrobble = {
    Artist: string
    Album: string
    Track: string
    Timestamp: int64
}

type UserName = UserName of string

type GetResumeTimestampResponse = {
    ResumeFrom: int64
}

type InsertScrobblesResponse = {
    NewUsers: int
    NewArtists: int
    NewAlbums: int
    NewTracks: int
    NewScrobbles: int
}

type GetChartRequestOptions = {
    PageToken: int64 option
    PageSize: int option
}

type ChartTrace = {
    Timestamps: string[]
    Texts: string[]
}
type GetChartDataResponse = {
    Data: Hash<ChartTrace>
    // Paging properties:
    NextPageToken: int64
    TotalCount: int
}

type IMainApi = {
    getResumeTimestamp: UserName -> Async<GetResumeTimestampResponse>
    insertScrobbles: UserName -> FlatScrobble[] -> Async<Result<InsertScrobblesResponse, string>>
    getChartData: UserName -> GetChartRequestOptions -> Async<GetChartDataResponse>
}
