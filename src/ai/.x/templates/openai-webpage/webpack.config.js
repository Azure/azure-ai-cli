const path = require('path');
const webpack = require('webpack');
const Dotenv = require('dotenv-webpack');

module.exports = {
  entry: './src/script.js',
  output: {
    filename: 'main.js',
    path: path.resolve(__dirname, 'dist'),
  },
  plugins: [
    new Dotenv(),
    new webpack.DefinePlugin({
      'process.env.ENDPOINT': JSON.stringify(process.env.ENDPOINT),
      'process.env.AZURE_API_KEY': JSON.stringify(process.env.AZURE_API_KEY),
      'process.env.DEPLOYMENT_NAME': JSON.stringify(process.env.DEPLOYMENT_NAME),
      'process.env.SYSTEM_PROMPT': JSON.stringify(process.env.SYSTEM_PROMPT),
    }),
  ],
};