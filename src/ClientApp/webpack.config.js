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
    externals: {
        'plotly.js': 'Plotly'
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
	},
    resolve: {
        alias: {
            // Use a smaller bundle since we only need scatter2d
            // 'plotly.js/dist/plotly': path.join(__dirname, 'node_modules/plotly.js/dist/plotly-gl2d.min.js')
        }
    }
}
