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
      'process.env.OPENAI_API_KEY': JSON.stringify(process.env.OPENAI_API_KEY),
      'process.env.OPENAI_ORG_ID': JSON.stringify(process.env.OPENAI_ORG_ID),
      'process.env.AZURE_OPENAI_API_VERSION': JSON.stringify(process.env.AZURE_OPENAI_API_VERSION),
      'process.env.AZURE_OPENAI_ENDPOINT': JSON.stringify(process.env.AZURE_OPENAI_ENDPOINT),
      'process.env.AZURE_OPENAI_API_KEY': JSON.stringify(process.env.AZURE_OPENAI_API_KEY),
      'process.env.AZURE_OPENAI_CHAT_DEPLOYMENT': JSON.stringify(process.env.AZURE_OPENAI_CHAT_DEPLOYMENT),
    }),
  ],
};