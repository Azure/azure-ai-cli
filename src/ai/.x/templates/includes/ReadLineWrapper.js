const rl = require('readline').createInterface({
  input: process.stdin,
  output: process.stdout
});

class ReadLineWrapper {

  constructor() {
    this.lineGenerator = this.getLines();
  }

  async* getLines() {
    for await (const line of rl) {
      yield line;
    }
  }

  async question(prompt) {
    process.stdout.write(prompt);
    const result = await this.lineGenerator.next();
    if(result.done) {
      this.close();
      return '';
    }
    return result.value;
  }

  close() {
    rl.close();
  }
}

const readline = new ReadLineWrapper();

module.exports = { readline: readline };
