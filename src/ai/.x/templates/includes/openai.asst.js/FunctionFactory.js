{{if {_IMPORT_EXPORT_USING_ES6}}}
export class FunctionFactory {
{{else}}
class FunctionFactory {
{{endif}}
  constructor() {
    this.functions = {};
  }

  addFunction(schema, fun) {
    this.functions[schema.name] = { schema: schema, function: fun };
  }

  getFunctionSchemas() {
    return Object.values(this.functions).map(value => value.schema);
  }

  getTools() {
    return Object.values(this.functions).map(value => {
      return {
        type: "function",
        function: value.schema
      };
    });
  }

  tryCallFunction(function_name, function_arguments) {
    const function_info = this.functions[function_name];
    if (function_info === undefined) {
      return undefined;
    }

    var result = function_info.function(function_arguments);
    {{if {_IS_BROWSER_TEMPLATE}}}
    console.log(`assistant-function: ${function_name}(${function_arguments}) => ${result}`);
    {{else}}
    process.stdout.write(`\rassistant-function: ${function_name}(${function_arguments}) => ${result}\n`);
    process.stdout.write('\nAssistant: ');
    {{endif}}

    return result;
  }
}
{{if !{_IMPORT_EXPORT_USING_ES6}}}

exports.FunctionFactory = FunctionFactory;
{{endif}}