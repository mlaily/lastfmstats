// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

module.exports = {
    mode: "development",
    entry: "./src/App.fs.js",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        publicPath: "/",
        contentBase: "./public",
        port: 8080,
    },
// https://github.com/MangelMaxime/fulma-demo/blob/3d7ad93234364bc40701d5b288e8ed3de12522be/webpack.config.js#L115-L119
	module: {
		rules: [
			{
                test: /\.js$/,
                enforce: "pre",
                use: ["source-map-loader"],
            }
		]
	}
}
