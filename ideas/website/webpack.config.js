// webpack.config.js
const path = require('path');

module.exports = {
  entry: './src/script.js', // Adjust the path based on your project structure
  output: {
    filename: 'bundle.js',
    path: path.resolve(__dirname, 'dist'),
  },
};
