{{if {_IMPORT_EXPORT_USING_ES6}}}
import { FunctionFactory } from "./FunctionFactory";
{{else}}
const { FunctionFactory } = require("./FunctionFactory");
{{endif}}
let factory = new FunctionFactory();

function getCurrentWeather(function_arguments) {
    const location = JSON.parse(function_arguments).location;
    return `The weather in ${location} is 72 degrees and sunny.`;
  };

const getCurrentWeatherSchema = {
  name: "get_current_weather",
  description: "Get the current weather in a given location",
  parameters: {
    type: "object",
    properties: {
      location: {
        type: "string",
        description: "The city and state, e.g. San Francisco, CA",
      },
      unit: {
        type: "string",
        enum: ["celsius", "fahrenheit"],
      },
    },
    required: ["location"],
  },
};

factory.addFunction(getCurrentWeatherSchema, getCurrentWeather);

function getCurrentDate() {
  const date = new Date();
  return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
}

const getCurrentDateSchema = {
  name: "get_current_date",
  description: "Get the current date",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getCurrentDateSchema, getCurrentDate);

function getCurrentTime() {
  const date = new Date();
  return `${date.getHours()}:${date.getMinutes()}:${date.getSeconds()}`;
}

const getCurrentTimeSchema = {
  name: "get_current_time",
  description: "Get the current time",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getCurrentTimeSchema, getCurrentTime);

{{if {_IMPORT_EXPORT_USING_ES6}}}
export { factory };
{{else}}
exports.factory = factory;
{{endif}}