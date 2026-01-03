const path = require('path');
const glob = require('glob');
const TerserPlugin = require('terser-webpack-plugin');

// Dynamically create an entry object
// e.g., { 'db-core': './Client/db-core.ts', 'dsstatsDb': './Client/dsstatsDb.ts' }
const entries = glob.sync('./Client/**/*.ts', { ignore: './Client/tests/**' })
    .reduce((acc, item) => {
        const name = path.basename(item, '.ts');
        acc[name] = path.resolve(__dirname, item);
        return acc;
    }, {});

module.exports = {
    mode: 'production',
    entry: entries,
    output: {
        path: path.resolve(__dirname, 'wwwroot/js'),
        filename: '[name].js',
        library: {
            type: 'module',
        },
    },
    experiments: {
        outputModule: true,
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.ts', '.js'],
        alias: {
            // This alias is for resolving pako correctly, as seen in vite.config.ts
            'pako': path.resolve(__dirname, 'node_modules/pako/dist/pako.es5.js'),
        },
    },
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin({
            extractComments: false, // Do not extract comments to a separate file
            terserOptions: {
                format: {
                    comments: false, // Remove all comments
                },
            },
        })],
    },
};
