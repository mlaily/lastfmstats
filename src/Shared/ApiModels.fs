module ApiModels

type GenericError = {
    Error: string
}

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

type GetChartDataResponse = {
    Timestamps: string[]
    Colors: string[]
    Texts: string[]
    // Paging properties:
    NextPageToken: int64
    TotalCount: int
}

type IMainApi = {
    getResumeTimestamp: UserName -> Async<GetResumeTimestampResponse>
    insertScrobbles: UserName -> FlatScrobble[] -> Async<Result<InsertScrobblesResponse, string>>
    getChartData: UserName -> GetChartRequestOptions -> Async<GetChartDataResponse>
}
