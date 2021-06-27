# LastFmStats

This repo holds the sources of [lastfmstats.200d.net](https://lastfmstats.200d.net/), a tool to graph [Last.fm](https://last.fm) scrobbles (A.K.A. music listenings) over time.

## Architecture overview

This is a client/server web application.

The frontend is written in F# and transpiled into javascript with [Fable](https://fable.io).  
It has two main responsibilities:
- fetch a user's tracks using the last.fm api and send them back to our own backend.
- query our backend to display a user's graph.

The backend is an ASP.NET Core application written in C#, with a mix of Entity Framework Core and Dapper hand tuned SQL for the data layer.

The DbContext and models are in a separate `RelationalSchema` project, so using the `dotnet ef` tool is possible. (I originally intended to write the backend in F# too, which is currently not supported by `dotnet ef`. After various experimentations I stuck with C# for the backend api because I'm more comfortable doing performance tuning in C# for now...)

The backend exposes an internal web api, and some Razor pages to pre-populate some server side data before the frontend takes over.

There are some shared F# DTOs between the frontend and backend.

The build scripts are either in bash or F#.

There is a C# BenchmarkDotNet project used to compare SQL INSERT performance of different methods (between EF Core, Dapper, and mostly hand written queries).  
[I wrote about INSERT performance in more details here.](https://zerowidthjoiner.net/2021/02/21/sqlite-bulk-insert-benchmarking-and-optimization)

Everything runs in docker.

## Technical notes

Node.js based tools are not required. [esbuild](https://esbuild.github.io/) is used to bundle the production javascript.

The F# build script compiles the frontend app and puts it in the `wwwroot` of the backend app, ready to be shipped. `build.cmd` and `dev-loop.cmd` can be used for prod and dev respectively (the dev script does not bundle the javascript files, and uses a watcher to recompile it immediately).

The frontend and backend use shared DTO types serialized to json. The frontend implementation relies on the browser's [`json()`](https://developer.mozilla.org/en-US/docs/Web/API/Body/json) method to interpret it, without further type checking.  
This is usually not recommended, as it can break easily if a .NET type is not translated the same way by the server and the client, but this is necessary for performance reasons: it's not unusual to have graphs made of 10MB of json, taking several seconds to be parsed.

There is a paging mechanism built-in when querying data to display a graph, but I disabled it (set to 1M points) because it resulted in unpleasant blinking when refreshing the graph, and waiting a few seconds seemed a better experience...

Since the database provider used is SQLite, a custom version of Dapper is required with this patch applied: https://github.com/DapperLib/Dapper/pull/720 (This repo references my fork as a git submodule)

There is a full F# Giraffe based backend implementation in the repo's history.
