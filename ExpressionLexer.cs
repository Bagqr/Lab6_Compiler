using System;
using System.Collections.Generic;

namespace WpfApp1
{
    public class ExpressionLexer
    {
        private enum State { Start, InNumber, InIdentifier }

        public List<Lexem> Tokenize(string text)
        {
            var result = new List<Lexem>();
            if (string.IsNullOrEmpty(text)) return result;

            int line = 1, col = 1;
            int idx = 0;
            State state = State.Start;
            int startLine = 1, startCol = 1;
            string buffer = "";

            while (idx <= text.Length)
            {
                char c = idx < text.Length ? text[idx] : '\0';
                bool endOfInput = idx == text.Length;

                switch (state)
                {
                    case State.Start:
                        if (endOfInput) break;

                        if (char.IsWhiteSpace(c))
                        {
                            if (c == '\n')
                            {
                                result.Add(CreateLexem(51, "\n", line, col, col));
                                line++;
                                col = 1;
                            }
                            else if (c == '\r')
                            {
                                // ignore
                            }
                            else
                            {
                                result.Add(CreateLexem(50, c.ToString(), line, col, col));
                            }
                            idx++;
                            col++;
                            continue;
                        }

                        if (char.IsDigit(c))
                        {
                            state = State.InNumber;
                            startLine = line;
                            startCol = col;
                            buffer = "";
                            continue;
                        }

                        if (char.IsLetter(c) || c == '_')
                        {
                            state = State.InIdentifier;
                            startLine = line;
                            startCol = col;
                            buffer = "";
                            continue;
                        }

                        // Операторы и символы
                        int opCode = GetOperatorCode(text, idx);
                        if (opCode != -1)
                        {
                            string op = GetOperatorString(text, idx);
                            result.Add(CreateLexem(opCode, op, line, col, col + op.Length - 1));
                            idx += op.Length;
                            col += op.Length;
                            continue;
                        }

                        // Недопустимый символ
                        result.Add(CreateLexem(90, c.ToString(), line, col, col, true));
                        idx++;
                        col++;
                        break;

                    case State.InNumber:
                        if (endOfInput || !char.IsDigit(c))
                        {
                            result.Add(CreateLexem(20, buffer, startLine, startCol, col - 1));
                            state = State.Start;
                            // не увеличиваем idx здесь, чтобы обработать текущий символ заново
                            continue;
                        }
                        buffer += c;
                        idx++;
                        col++;
                        break;

                    case State.InIdentifier:
                        if (endOfInput || !(char.IsLetterOrDigit(c) || c == '_'))
                        {
                            // Проверка на ключевое слово
                            int keywordCode = GetKeywordCode(buffer);
                            if (keywordCode != -1)
                            {
                                result.Add(CreateLexem(keywordCode, buffer, startLine, startCol, col - 1));
                            }
                            else
                            {
                                result.Add(CreateLexem(10, buffer, startLine, startCol, col - 1));
                            }
                            state = State.Start;
                            continue;
                        }
                        buffer += c;
                        idx++;
                        col++;
                        break;
                }

                if (endOfInput) break;
            }

            return result;
        }

        private int GetOperatorCode(string text, int pos)
        {
            if (pos + 1 < text.Length)
            {
                string two = text.Substring(pos, 2);
                switch (two)
                {
                    case "**": return 70; // степень
                    case "//": return 71; // целочисленное деление
                    case "==": return 31;
                    case "<=": return 34;
                    case ">=": return 35;
                    case "!=": return 36;
                }
            }
            char c = text[pos];
            switch (c)
            {
                case '+': return 40;
                case '-': return 41;
                case '*': return 42;
                case '/': return 43;
                case '%': return 44;
                case '(': return 62;
                case ')': return 63;
                case '=': return 30;
                case '<': return 32;
                case '>': return 33;
                case ';': return 64;
                case ':': return 60;
                case ',': return 61;
                default: return -1;
            }
        }

        private string GetOperatorString(string text, int pos)
        {
            if (pos + 1 < text.Length)
            {
                string two = text.Substring(pos, 2);
                if (two == "**" || two == "//" || two == "==" || two == "<=" || two == ">=" || two == "!=")
                    return two;
            }
            return text[pos].ToString();
        }

        private int GetKeywordCode(string word)
        {
            switch (word)
            {
                case "for": return 1;
                case "in": return 2;
                case "range": return 3;
                case "print": return 4;
                default: return -1;
            }
        }

        private Lexem CreateLexem(int code, string value, int line, int start, int end, bool isError = false)
        {
            string typeName;
            switch (code)
            {
                case 1:
                    typeName = "ключевое слово (for)";
                    break;
                case 2:
                    typeName = "ключевое слово (in)";
                    break;
                case 3:
                    typeName = "ключевое слово (range)";
                    break;
                case 4:
                    typeName = "ключевое слово (print)";
                    break;
                case 10:
                    typeName = "идентификатор";
                    break;
                case 20:
                    typeName = "целое число";
                    break;
                case 30:
                    typeName = "присваивание (=)";
                    break;
                case 31:
                    typeName = "сравнение (==)";
                    break;
                case 32:
                    typeName = "меньше (<)";
                    break;
                case 33:
                    typeName = "больше (>)";
                    break;
                case 34:
                    typeName = "меньше/равно (<=)";
                    break;
                case 35:
                    typeName = "больше/равно (>=)";
                    break;
                case 36:
                    typeName = "неравно (!=)";
                    break;
                case 40:
                    typeName = "плюс (+)";
                    break;
                case 41:
                    typeName = "минус (-)";
                    break;
                case 42:
                    typeName = "умножение (*)";
                    break;
                case 43:
                    typeName = "деление (/)";
                    break;
                case 44:
                    typeName = "остаток (%)";
                    break;
                case 50:
                    typeName = "пробел/табуляция";
                    break;
                case 51:
                    typeName = "новая строка";
                    break;
                case 60:
                    typeName = "двоеточие (:)";
                    break;
                case 61:
                    typeName = "запятая (,)";
                    break;
                case 62:
                    typeName = "открывающая скобка (()";
                    break;
                case 63:
                    typeName = "закрывающая скобка ())";
                    break;
                case 64:
                    typeName = "точка с запятой (;)";
                    break;
                case 70:
                    typeName = "возведение в степень (**)";
                    break;
                case 71:
                    typeName = "целочисленное деление (//)";
                    break;
                case 90:
                    typeName = "ошибка";
                    break;
                default:
                    typeName = "неизвестно";
                    break;
            }
            return new Lexem
            {
                Code = code,
                Type = typeName,
                Value = value,
                Line = line,
                StartPos = start,
                EndPos = end,
                IsError = isError
            };
        }
    }
}