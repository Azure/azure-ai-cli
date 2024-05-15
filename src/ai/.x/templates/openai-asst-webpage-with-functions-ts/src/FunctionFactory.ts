class FunctionFactory {
  functions: { [name: string]: { schema: any; function: Function } };

  constructor() {
    this.functions = {};
  }

  addFunction(schema: any, fun: Function): void {
    this.functions[schema.name] = { schema: schema, function: fun };
  }

  getFunctionSchemas(): any[] {
    return Object.values(this.functions).map(value => value.schema);
  }

  getTools(): any[] {
    return Object.values(this.functions).map(value => {
      return {
        type: "function",
        function: value.schema
      };
    });
  }

  tryCallFunction(function_name: string, function_arguments: any): any {
    const function_info = this.functions[function_name];
    if (function_info === undefined) {
      return undefined;
    }

    var result = function_info.function(function_arguments);
    console.log(`assistant-function: ${function_name}(${function_arguments}) => ${result}`);

    return result;
  }
}

export { FunctionFactory };
