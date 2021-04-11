dotnet tool restore

rmdir /S /Q build

mkdir build

start /b dotnet dotnet-serve --directory build --open-browser --port 8080 --reverse-proxy /api/{**all}=http://localhost:5000

REM watch CaddyFile
REM start /b caddy run -watch

"C:\Users\Melvyn\Desktop\Fable\src\Fable.Cli\bin\Debug\netcoreapp3.1\fable.exe" watch src --outDir build --sourceMaps --sourceMapsRoot "file:///" --runWatch copy src\index.html build

REM esbuild\esbuild.exe --outdir=dist --bundle --minify --target=es6 build/App.js
REM esbuild\esbuild.exe --servedir=build --serve=localhost:8080