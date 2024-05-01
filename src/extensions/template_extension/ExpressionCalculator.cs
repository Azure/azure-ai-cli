//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class CalcException : Exception
    {
        public CalcException(string message, int position) : base(message)
        {
            Position = position;
        }
        public int Position { get; }
    }

    public class ExpressionCalculator
    {
        public enum TokenType
        {
            Identifier,
            Number,
            String,
            Bool,
            Equal,
            Plus,
            Minus,
            Times,
            Divide,
            Mod,
            Div,
            LogicalAnd,
            LogicalOr,
            LogicalNot,
            BitwiseAnd,
            BitwiseOr,
            BitwiseNot,
            Power,
            OpenParen,
            CloseParen,
            Comma,
            Eos,
        }
        public enum Radix
        {
            Bin = 2,
            Oct = 8,
            Dec = 10,
            Hex = 16
        }

        public delegate dynamic FunctionDelegate();

        public class Function
        {
            public Function(string name, FunctionDelegate function)
            {
                Name = name;
                Delegate = function;
            }

            public string Name { get; set; }
            public FunctionDelegate Delegate { get; set; }
        }

        public class Constant
        {
            public Constant(string name, dynamic value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; set; }
            public dynamic Value { get; set; }
        }

        public class Variable
        {
            public Variable(string name, dynamic value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; set; }
            public dynamic Value { get; set; } // It could be double, string, or bool
        }

        public ExpressionCalculator()
        {
            _functions = new List<Function>();
            _constants = new List<Constant>();
            _variables = new List<Variable>();

            _constantE = new Constant("E", 2.71828182845905);
            _constantPI = new Constant("PI", 3.14159265358979);
            _constantNAN = new Constant("NAN", double.NaN);
            AddConstant(_constantE);
            AddConstant(_constantPI);
            AddConstant(_constantNAN);

            // math functions
            AddFunction(new Function("ABS", Abs));
            AddFunction(new Function("ACOS", Acos));
            AddFunction(new Function("ASIN", Asin));
            AddFunction(new Function("ATAN", Atan));
            AddFunction(new Function("ATAN2", Atan2));
            AddFunction(new Function("CEIL", Ceil));
            AddFunction(new Function("COS", Cos));
            AddFunction(new Function("COSH", Cosh));
            AddFunction(new Function("EXP", Exp));
            AddFunction(new Function("FLOOR", Floor));
            AddFunction(new Function("LOG", Log));
            AddFunction(new Function("LOG10", Log10));
            AddFunction(new Function("SIN", Sin));
            AddFunction(new Function("SINH", Sinh));
            AddFunction(new Function("SQRT", Sqrt));
            AddFunction(new Function("TAN", Tan));
            AddFunction(new Function("TANH", Tanh));
            AddFunction(new Function("TRUNCATE", Truncate));
            AddFunction(new Function("MAX", Max));
            AddFunction(new Function("MIN", Min));
            
            // string functions
            AddFunction(new Function("TOLOWER", Tolower));
            AddFunction(new Function("TOUPPER", Toupper));
            AddFunction(new Function("CONTAINS", Contains));
            AddFunction(new Function("STARTSWITH", StartsWith));
            AddFunction(new Function("ENDSWITH", EndsWith));
            AddFunction(new Function("ISEMPTY", IsEmpty));
        }

        public dynamic Evaluate(string str)
        {
            _expression = str;
            _position = _nextPosition = 0;

            SkipWhiteSpace();
            NextToken();

            var value = Statement();
            if (_tokenType != TokenType.Eos)
            {
                throw UnexpectedCharacterCalcException(_position);
            }

            return value;
        }

        public void AddFunction(Function function)
        {
            function.Name = function.Name.ToUpper();
            _functions.Add(function);
        }

        public void AddConstant(Constant constant)
        {
            constant.Name = constant.Name.ToUpper();
            _constants.Add(constant);
        }

        public void AddVariable(Variable variable)
        {
            variable.Name = variable.Name.ToUpper();
            _variables.Add(variable);
        }

        public dynamic Statement()
        {
            var isAssignment = false;
            string? variableName = null;

            if (_tokenType == TokenType.Identifier && FunctionFromString(_token) == null && ConstantFromString(_token) == null)
            {
                variableName = _token;
                NextToken();
                if (_tokenType == TokenType.Equal)
                {
                    NextToken();
                    isAssignment = true;
                }
            }

            if (!isAssignment)
            {
                _position = _nextPosition = 0;
                SkipWhiteSpace();
                NextToken();
            }

            var value = BitExpression();
            if (isAssignment)
            {
                var variable = VariableFromString(variableName!);
                if (variable == null)
                {
                    variable = new Variable(variableName!, value);
                    AddVariable(variable);
                }
                variable.Value = value;
            }
            return value;
        }

        private dynamic BitExpression()
        {
            var value = BitTerm();
            while (true)
            {
                if (_tokenType == TokenType.LogicalOr)
                {
                    NextToken();
                    var value2 = BitTerm();
                    if (!(value is bool) || !(value2 is bool))
                    {
                        throw new CalcException("Expected boolean", _position);
                    }
                    value = (bool)value || (bool)value2;
                }
                else if (_tokenType == TokenType.BitwiseOr)
                {
                    NextToken();
                    var value2 = BitTerm();
                    if (value >= long.MinValue && value <= long.MaxValue)
                    {
                        var l1 = (long)value;
                        if (value2 >= long.MinValue && value2 <= long.MaxValue)
                        {
                            var l2 = (long)value2;
                            value = (double)(l1 | l2);
                        }
                        else
                        {
                            throw new CalcException("Result to large. Bitwise operation conversion impossible.", _position);
                        }
                    }
                    else
                    {
                        throw new CalcException("Result to large. Bitwise operation impossible.", _position);
                    }
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private dynamic BitTerm()
        {
            var value = BitFactor();
            while (true)
            {
                if (_tokenType == TokenType.LogicalAnd)
                {
                    NextToken();
                    var value2 = BitFactor();
                    if (!(value is bool) || !(value2 is bool))
                    {
                        throw new CalcException("Expected boolean", _position);
                    }
                    value = (bool)value && (bool)value2;
                }
                else if (_tokenType == TokenType.BitwiseAnd)
                {
                    NextToken();
                    var value2 = BitFactor();
                    if (value >= long.MinValue && value <= long.MaxValue)
                    {
                        var l1 = (long)value;
                        if (value2 >= long.MinValue && value2 <= long.MaxValue)
                        {
                            var l2 = (long)value2;
                            value = (double)(l1 & l2);
                        }
                        else
                        {
                            throw new CalcException("Result to large. Bitwise operation conversion impossible.", _position);
                        }
                    }
                    else
                    {
                        throw new CalcException("Result to large. Bitwise operation impossible.", _position);
                    }
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private dynamic BitFactor()
        {
            var value = Expression();
            while (true)
            {
                if (_tokenType == TokenType.LogicalAnd)
                {
                    NextToken();
                    var value2 = Expression();
                    if (!(value is bool) || !(value2 is bool))
                    {
                        throw new CalcException("Expected boolean", _position);
                    }
                    value = (bool)value && (bool)value2;
                }
                else if (_tokenType == TokenType.BitwiseAnd)
                {
                    NextToken();
                    var value2 = Expression();
                    if (value >= long.MinValue && value <= long.MaxValue)
                    {
                        var l1 = (long)value;
                        if (value2 >= long.MinValue && value2 <= long.MaxValue)
                        {
                            var l2 = (long)value2;
                            value = (double)(l1 & l2);
                        }
                        else
                        {
                            throw new CalcException("Result to large. Bitwise operation conversion impossible.", _position);
                        }
                    }
                    else
                    {
                        throw new CalcException("Result to large. Bitwise operation impossible.", _position);
                    }
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private dynamic Expression()
        {
            var value = Term();
            while (true)
            {
                if (_tokenType == TokenType.Plus)
                {
                    NextToken();
                    var value2 = Term();
                    var precision = value == 0 || value2 == 0 ? 1 : Math.Pow(10, Math.Max(Math.Ceiling(Math.Log10(Math.Abs(value))), Math.Ceiling(Math.Log10(Math.Abs(value2)))) - 15);
                    value = Round(value + value2, precision);
                }
                else if (_tokenType == TokenType.Minus)
                {
                    NextToken();
                    var value2 = Term();
                    var precision = value == 0 || value2 == 0 ? 1 : Math.Pow(10, Math.Max(Math.Ceiling(Math.Log10(Math.Abs(value))), Math.Ceiling(Math.Log10(Math.Abs(value2)))) - 15);
                    value = Round(value - value2, precision);
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private dynamic Term()
        {
            var value = Unary();
            while (true)
            {
                if (_tokenType == TokenType.Times)
                {
                    NextToken();
                    value = Exact(value * Unary());
                }
                else if (_tokenType == TokenType.Divide)
                {
                    NextToken();
                    value = Exact(value / Unary());
                }
                else if (_tokenType == TokenType.Mod)
                {
                    NextToken();
                    value = Exact(value % Unary());
                }
                else if (_tokenType == TokenType.Div)
                {
                    NextToken();
                    value = Exact(Math.Floor(value / Unary()));
                }
                else
                {
                    break;
                }
            }
            return value;
        }

        private dynamic Unary()
        {
            var negate = false;
            var bitwiseNot = false;
            var logicalNot = false;
            if (_tokenType == TokenType.Minus)
            {
                NextToken();
                negate = true;
            }
            else if (_tokenType == TokenType.Plus)
            {
                NextToken();
                negate = false;
            }
            else if (_tokenType == TokenType.BitwiseNot)
            {
                NextToken();
                bitwiseNot = true;
            }
            else if (_tokenType == TokenType.LogicalNot)
            {
                NextToken();
                logicalNot = true;
            }
            var value = Exponent();
            if (negate)
            {
                value = value * -1.0;
            }
            if (logicalNot)
            {
                if (!(value is bool))
                {
                    throw new CalcException("Expected boolean", _position);
                }
                value = !(bool)value;
            }
            if (bitwiseNot)
            {
                if (value >= double.MinValue && value <= double.MaxValue)
                {
                    long l = (long)value;
                    l = ~l;
                    value = (double)l;
                }
                else
                {
                    throw new CalcException("Result to large. Bitwise operation impossible.", _position);
                }
            }
            return value;
        }

        private dynamic Exponent()
        {
            var value = Factor();
            if (_tokenType == TokenType.Power)
            {
                NextToken();
                value = Exact(Math.Pow(value, Unary()));
            }
            return value;
        }

        private dynamic Factor()
        {
            dynamic value;
            if (_tokenType == TokenType.Number)
            {
                value = double.Parse(_token);
                NextToken();
            }
            else if (_tokenType == TokenType.String)
            {
                value = _token;
                NextToken();
            }
            else if (_tokenType == TokenType.Bool)
            {
                value = bool.Parse(_token);
                NextToken();
            }
            else if (_tokenType == TokenType.Identifier)
            {
                Function? function;
                Constant? constant;
                Variable? variable;
                if ((function = FunctionFromString(_token)) != null)
                {
                    NextToken();
                    value = function.Delegate();
                    if (value is double)
                    {
                        value = Exact(value);
                    }
                }
                else if ((constant = ConstantFromString(_token)) != null)
                {
                    NextToken();
                    value = constant.Value;
                    if (value is double)
                    {
                        value = Exact(value);
                    }
                }
                else if ((variable = VariableFromString(_token)) != null)
                {
                    NextToken();
                    value = variable.Value;
                    if (value is double)
                    {
                        value = Exact(value);
                    }
                }
                else
                {
                    throw new CalcException("Undefined symbol", _position);
                }
            }
            else if (_tokenType == TokenType.OpenParen)
            {
                NextToken();
                value = Expression();
                if (_tokenType != TokenType.CloseParen)
                {
                    throw new CalcException("Expected close parenthesis", _position);
                }
                NextToken();
            }
            else
            {
                throw new CalcException("Expected number, function, or constant", _position);
            }
            return value;
        }

        private void NextToken()
        {
            _position = _nextPosition;
            if (_nextPosition >= _expression.Length)
            {
                _tokenType = TokenType.Eos;
            }
            else
            {
                if (char.IsLetter(_expression[_nextPosition]))
                {
                    while (_nextPosition < _expression.Length &&
                           (char.IsLetter(_expression[_nextPosition]) ||
                            char.IsDigit(_expression[_nextPosition]) ||
                            _expression[_nextPosition] == '_'))
                    {
                        _nextPosition++;
                    }

                    _token = _expression.Substring(_position, _nextPosition - _position);
                    _tokenType = _token.ToUpper() switch
                    {
                        "MOD" => TokenType.Mod,
                        "DIV" => TokenType.Div,
                        "TRUE" => TokenType.Bool,
                        "FALSE" => TokenType.Bool,
                        _ => TokenType.Identifier,
                    };

                    SkipWhiteSpace();
                }
                else if (char.IsDigit(_expression[_position]) || _expression[_position] == '.')
                {
                    while (_nextPosition < _expression.Length && (char.IsDigit(_expression[_nextPosition]) || _expression[_nextPosition] == '.'))
                    {
                        _nextPosition++;
                    }

                    _token = _expression.Substring(_position, _nextPosition - _position);
                    _tokenType = TokenType.Number;
                    SkipWhiteSpace();
                }
                else if (_expression[_position] == '"')
                {
                    _nextPosition++;
                    while (_nextPosition < _expression.Length && _expression[_nextPosition] != '"')
                    {
                        _nextPosition++;
                    }
                    if (_nextPosition >= _expression.Length)
                    {
                        throw new CalcException("Expected closing quote", _position);
                    }

                    _token = _expression.Substring(_position + 1, _nextPosition - _position - 1);
                    _nextPosition++;
                    _tokenType = TokenType.String;
                    SkipWhiteSpace();
                }
                else if (_expression.Substring(_nextPosition).StartsWith("&&"))
                {
                    _tokenType = TokenType.LogicalAnd;
                    _token = "&&";
                    _nextPosition += 2;
                    SkipWhiteSpace();
                }
                else if (_expression.Substring(_nextPosition).StartsWith("||"))
                {
                    _tokenType = TokenType.LogicalOr;
                    _token = "||";
                    _nextPosition += 2;
                    SkipWhiteSpace();
                }
                else
                {
                    switch (_expression[_nextPosition])
                    {
                        case '=':
                            _tokenType = TokenType.Equal;
                            break;
                        case '+':
                            _tokenType = TokenType.Plus;
                            break;
                        case '-':
                            _tokenType = TokenType.Minus;
                            break;
                        case '*':
                            if (_nextPosition + 1 < _expression.Length &&
                                _expression[_nextPosition + 1] == '*')
                            {
                                _tokenType = TokenType.Power;
                                _nextPosition++;
                            }
                            else
                            {
                                _tokenType = TokenType.Times;
                            }
                            break;
                        case '/':
                            _tokenType = TokenType.Divide;
                            break;
                        case '(':
                            _tokenType = TokenType.OpenParen;
                            break;
                        case ')':
                            _tokenType = TokenType.CloseParen;
                            break;
                        case ',':
                            _tokenType = TokenType.Comma;
                            break;
                        case '!':
                            _tokenType = TokenType.LogicalNot;
                            break;
                        case '~':
                            _tokenType = TokenType.BitwiseNot;
                            break;
                        case '&':
                            _tokenType = TokenType.BitwiseAnd;
                            break;
                        case '^':
                            _tokenType = TokenType.Power;
                            break;
                        case '|':
                            _tokenType = TokenType.BitwiseOr;
                            break;
                        default:
                            throw UnexpectedCharacterCalcException(_nextPosition);
                    }
                    _token = _expression.Substring(_position, _nextPosition - _position);
                    _nextPosition++;
                    SkipWhiteSpace();
                }
            }
        }

        private CalcException UnexpectedCharacterCalcException(int pos)
        {
            var charAsInt = (int)_expression[pos];
            var hexOfChar = (charAsInt).ToString("X");
            var charAsString = _expression.Substring(pos, 1);
            var ex = new CalcException(charAsInt < 127
                ? $"Unexpected character at position {pos} ('{charAsString}', 0x{hexOfChar}); see: \n{_expression}."
                : $"Unexpected character at position {pos} (0x{hexOfChar}); see:\n`{_expression}`",
                pos);
            return ex;
        }

        private void SkipWhiteSpace()
        {
            while (_nextPosition < _expression.Length && char.IsWhiteSpace(_expression[_nextPosition]))
            {
                _nextPosition++;
            }
        }

        private Function? FunctionFromString(string name)
        {
            name = name.ToUpper();
            return _functions.FirstOrDefault(function => function.Name == name);
        }

        private Constant? ConstantFromString(string name)
        {
            name = name.ToUpper();
            return _constants.FirstOrDefault(constant => constant.Name == name);
        }

        private Variable? VariableFromString(string name)
        {
            name = name.ToUpper();
            return _variables.FirstOrDefault(variable => variable.Name == name);
        }

        private double Exact(double d)
        {
            if (d >= double.MinValue && d <= double.MaxValue)
            {
                return d;
            }
            throw new CalcException("Result to large. Conversion impossible.", _position);
        }

        private double Round(double d, double dPrecision)
        {
            return Math.Round(d / dPrecision) * dPrecision;
        }

        private dynamic Abs()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Abs(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Acos()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Acos(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Asin()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Asin(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Atan()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Atan(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Atan2()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = Expression();
            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = Expression();
            var value = Math.Atan2(value1, value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Ceil()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Ceiling(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Cos()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Cos(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Cosh()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Cosh(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Exp()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Exp(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Floor()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Floor(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Log()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Log(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Log10()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Log10(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Sin()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Sin(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Sinh()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Sinh(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Sqrt()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Sqrt(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Tan()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Tan(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Tanh()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Tanh(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Truncate()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = Math.Truncate(Expression());
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Max()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = Expression();
            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = Expression();
            double dValue = Math.Max(value1, value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return dValue;
        }

        private dynamic Min()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = Expression();
            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = Expression();
            var value = Math.Min(value1, value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic Tolower()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = BitExpression();
            if (!(value is string))
            {
                throw new CalcException("Expected string", _position);
            }

            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value.ToLower();
        }

        private dynamic Toupper()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = BitExpression();
            if (!(value is string))
            {
                throw new CalcException("Expected string", _position);
            }

            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value.ToUpper();
        }

        private dynamic Contains()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = BitExpression();
            if (!(value1 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = BitExpression();
            if (!(value2 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            var value = ((string)value1).Contains((string)value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic StartsWith()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = BitExpression();
            if (!(value1 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = BitExpression();
            if (!(value2 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            var value = ((string)value1).StartsWith((string)value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic EndsWith()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value1 = BitExpression();
            if (!(value1 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            if (_tokenType != TokenType.Comma)
            {
                throw new CalcException("expected comma", _position);
            }
            NextToken();

            var value2 = BitExpression();
            if (!(value2 is string))
            {
                throw new CalcException("Expected string", _position);
            }

            var value = ((string)value1).EndsWith((string)value2);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        }

        private dynamic IsEmpty()
        {
            if (_tokenType != TokenType.OpenParen)
            {
                throw new CalcException("Expected '('", _position);
            }
            NextToken();

            var value = BitExpression();
            if (!(value is string))
            {
                throw new CalcException("Expected string", _position);
            }

            value = string.IsNullOrEmpty((string)value);
            if (_tokenType != TokenType.CloseParen)
            {
                throw new CalcException("Expected ')'", _position);
            }
            NextToken();

            return value;
        } 

        public string StrFromDRadix(double d, Radix radix)
        {
            if (radix == Radix.Dec)
            {
                return d.ToString("G");
            }
            else if (d >= long.MinValue && d <= long.MaxValue)
            {
                string szValue;
                if (radix > Radix.Dec)
                {
                    szValue = "0" + Convert.ToString((long)d, (int)radix);
                }
                else
                {
                    szValue = Convert.ToString((long)d, (int)radix);
                }
                szValue = szValue.ToUpper();
                switch (radix)
                {
                    case Radix.Hex:
                        szValue += "h";
                        break;
                    case Radix.Oct:
                        szValue += "o";
                        break;
                    case Radix.Bin:
                        szValue += "b";
                        break;
                }
                return szValue;
            }
            else
            {
                throw new CalcException("Result to large. Radix conversion impossible.", _position);
            }
        }

        private string _expression = string.Empty;
        private int _position, _nextPosition;
        private TokenType _tokenType;
        private string _token = string.Empty;
        private readonly Constant _constantE;
        private readonly Constant _constantPI;
        private readonly Constant _constantNAN;
        private readonly List<Function> _functions;
        private readonly List<Constant> _constants;
        private readonly List<Variable> _variables;
    }
}