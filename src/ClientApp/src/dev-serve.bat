dotnet tool restore

rmdir /S /Q compiled

mkdir compiled

start /b dotnet dotnet-serve --directory . --open-browser --port 8080 --reverse-proxy /api/{**all}=http://localhost:5000 -h "Cache-Control: no-store, max-age=0"

REM watch CaddyFile
REM start /b caddy run -watch

"C:\Users\Melvyn\Desktop\Fable\src\Fable.Cli\bin\Debug\netcoreapp3.1\fable.exe" watch . --outDir compiled --sourceMaps --sourceMapsRoot "file:///"

REM ..\esbuild\esbuild.exe --outdir=dist --bundle --minify --target=es6 compiled/App.js
REM ..\esbuild\esbuild.exe --servedir=compiled --serve=localhost:8080