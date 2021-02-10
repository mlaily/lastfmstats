namespace ApiModels

open System

type ScrobbleData = {
    Artist: string
    Album: string
    Timestamp: int64
    Track: string
    }