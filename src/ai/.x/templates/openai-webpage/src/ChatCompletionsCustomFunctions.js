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

exports.getCurrentDate = getCurrentDate;
exports.getCurrentDateSchema = getCurrentDateSchema;
