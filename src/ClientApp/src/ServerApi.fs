namespace LastFMStats.Client

open LastFMStats.Client.Util
open LastFMStats.Client.LastFmApi
open ApiModels
open Thoth.Json
open Fetch.Types
open Fetch
open FSharp.Control
open Fable.Core
open Fable.Remoting.Client

module ServerApi =

    let apiRoot = "http://localhost:5000/"

    let mainApi : IMainApi =
        Remoting.createApi ()
        |> Remoting.withBaseUrl apiRoot
        |> Remoting.withBinarySerialization
        |> Remoting.buildProxy<IMainApi>

    let loadAllScrobbleData userName =
        let rec loadAllScrobbleData' nextPageToken =
            asyncSeq {
                let requestOptions =
                    { PageToken = nextPageToken
                      PageSize = None }

                let! data = mainApi.getChartData (UserName userName) requestOptions

                if data.Timestamps.Length > 0 then
                    yield data

                if data.Timestamps.Length > 0 then
                    yield! loadAllScrobbleData' (Some data.NextPageToken)
            }

        loadAllScrobbleData' None
