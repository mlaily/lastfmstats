namespace ApiModels

open System

type ScrobbleData = {
    Artist: string
    Album: string
    Timestamp: int64
    Track: string
    }


type IMainApi = {
    getResumeTimestamp: string -> Async<{| From: int64 |}>
}
