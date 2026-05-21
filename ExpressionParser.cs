using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class ExpressionParser
    {
        private const int TOKEN_NUM = 20;
        private const int TOKEN_ID = 10;
        private const int TOKEN_PLUS = 40;
        private const int TOKEN_MINUS = 41;
        private const int TOKEN_MUL = 42;
        private const int TOKEN_DIV = 43;
        private const int TOKEN_MOD = 44;
        private const int TOKEN_POW = 70;
        private const int TOKEN_INTDIV = 71;
        private const int TOKEN_LPAREN = 62;
        private const int TOKEN_RPAREN = 63;
        private const int TOKEN_WS = 50;
        private const int TOKEN_NL = 51;

        private List<Lexem> _tokens;
        private int _pos;
        private Lexem _current;
        private List<SyntaxError> _errors;
        private List<Tetrad> _tetrads;
        private int _tempCounter;
        private List<string> _poliz;
        private bool _hasErrors;
        private bool _allIntegers;

        public (List<Tetrad> Tetrads, List<string> Poliz, List<SyntaxError> Errors, int? Result) Parse(List<Lexem> lexems)
        {
            // Фильтруем пробелы и переводы строк
            _tokens = lexems.Where(t => t.Code != TOKEN_WS && t.Code != TOKEN_NL).ToList();
            _pos = 0;
            _errors = new List<SyntaxError>();
            _tetrads = new List<Tetrad>();
            _tempCounter = 0;
            _poliz = new List<string>();
            _hasErrors = false;
            _allIntegers = true;

            if (_tokens.Count == 0)
            {
                _errors.Add(new SyntaxError
                {
                    ErrorFragment = "",
                    Location = "строка 1, позиция 1",
                    Description = "Пустое выражение",
                    Line = 1,
                    Column = 1
                });
                return (_tetrads, _poliz, _errors, null);
            }

            _current = _tokens[0];

            // Проверяем, все ли операнды — числа (нет идентификаторов)
            foreach (var t in _tokens)
            {
                if (t.Code == TOKEN_ID)
                {
                    _allIntegers = false;
                    break;
                }
            }

            // Запуск разбора
            string result = ParseE();

            if (!_hasErrors && _pos < _tokens.Count)
            {
                var tok = _tokens[_pos];
                AddError(tok.Value, tok.Line, tok.StartPos,
                    $"Неожиданный токен '{tok.Value}' после завершения выражения");
                _hasErrors = true;
            }

            if (_hasErrors)
            {
                _poliz.Clear();
                _tetrads.Clear();
                return (_tetrads, _poliz, _errors, null);
            }

            // Вычисление ПОЛИЗ (только если все операнды — числа)
            int? evalResult = null;
            if (_allIntegers && _poliz.Count > 0)
            {
                evalResult = PolizEvaluator.Evaluate(_poliz, _errors);
            }

            return (_tetrads, _poliz, _errors, evalResult);
        }

        private Lexem NextToken()
        {
            _pos++;
            _current = _pos < _tokens.Count ? _tokens[_pos] : null;
            return _current;
        }

        private Lexem Peek() => _pos + 1 < _tokens.Count ? _tokens[_pos + 1] : null;

        private bool Match(int code)
        {
            if (_current != null && _current.Code == code)
            {
                NextToken();
                return true;
            }
            return false;
        }

        private void Expect(int code, string expectedName)
        {
            if (_current != null && _current.Code == code)
            {
                NextToken();
            }
            else
            {
                string found = _current != null ? $"'{_current.Value}'" : "конец строки";
                AddError(found, _current?.Line ?? 1, _current?.StartPos ?? 1,
                    $"Ожидалось: {expectedName}, найдено: {found}");
                _hasErrors = true;
            }
        }

        private void AddError(string fragment, int line, int col, string description)
        {
            _errors.Add(new SyntaxError
            {
                ErrorFragment = fragment,
                Location = $"строка {line}, позиция {col}",
                Description = description,
                Line = line,
                Column = col
            });
        }

        private string NewTemp()
        {
            _tempCounter++;
            return $"t{_tempCounter}";
        }

        private void AddTetrad(string op, string arg1, string arg2, string result)
        {
            _tetrads.Add(new Tetrad
            {
                Index = _tetrads.Count + 1,
                Op = op,
                Arg1 = arg1,
                Arg2 = arg2,
                Result = result
            });
        }

        // E → T A
        private string ParseE()
        {
            string t = ParseT();
            return ParseA(t);
        }

        // A → ε | + T A | - T A
        private string ParseA(string inherited)
        {
            if (_current == null) return inherited;

            if (_current.Code == TOKEN_PLUS || _current.Code == TOKEN_MINUS)
            {
                string op = _current.Value;
                int code = _current.Code;
                NextToken();
                string t = ParseT();
                string temp = NewTemp();
                AddTetrad(op, inherited, t, temp);

                if (_allIntegers)
                {
                    _poliz.Add(op);
                }

                return ParseA(temp);
            }
            return inherited;
        }

        // T → F B
        private string ParseT()
        {
            string f = ParseF();
            return ParseB(f);
        }

        // B → ε | * F B | / F B | // F B | % F B | ** F B
        private string ParseB(string inherited)
        {
            if (_current == null) return inherited;

            if (_current.Code == TOKEN_MUL || _current.Code == TOKEN_DIV ||
                _current.Code == TOKEN_INTDIV || _current.Code == TOKEN_MOD ||
                _current.Code == TOKEN_POW)
            {
                string op = _current.Value;
                int code = _current.Code;
                NextToken();
                string f = ParseF();
                string temp = NewTemp();
                AddTetrad(op, inherited, f, temp);

                if (_allIntegers)
                {
                    _poliz.Add(op);
                }

                return ParseB(temp);
            }
            return inherited;
        }

        // F → num | id | ( E )
        private string ParseF()
        {
            if (_current == null)
            {
                AddError("", 1, 1, "Ожидался операнд (число, идентификатор или '(')");
                _hasErrors = true;
                return "error";
            }

            if (_current.Code == TOKEN_NUM)
            {
                string val = _current.Value;
                NextToken();
                if (_allIntegers)
                    _poliz.Add(val);
                return val;
            }

            if (_current.Code == TOKEN_ID)
            {
                string val = _current.Value;
                NextToken();
                if (_allIntegers)
                {
                    // Если есть идентификатор, ПОЛИЗ не строится
                    _poliz.Clear();
                }
                return val;
            }

            if (_current.Code == TOKEN_LPAREN)
            {
                NextToken();
                string e = ParseE();
                Expect(TOKEN_RPAREN, "')'");
                return e;
            }

            AddError(_current.Value, _current.Line, _current.StartPos,
                $"Неожиданный токен '{_current.Value}'. Ожидался операнд.");
            _hasErrors = true;
            return "error";
        }
    }
}