using System;
using System.Collections.Generic;

namespace WpfApp1
{
    public static class PolizEvaluator
    {
        public static int? Evaluate(List<string> poliz, List<SyntaxError> errors)
        {
            try
            {
                var stack = new Stack<int>();
                foreach (string item in poliz)
                {
                    if (int.TryParse(item, out int num))
                    {
                        stack.Push(num);
                        continue;
                    }

                    if (stack.Count < 2)
                    {
                        errors.Add(new SyntaxError
                        {
                            ErrorFragment = item,
                            Location = "вычисление",
                            Description = "Недостаточно операндов для операции '" + item + "'",
                            Line = 1,
                            Column = 1
                        });
                        return null;
                    }

                    int b = stack.Pop();
                    int a = stack.Pop();
                    int result;

                    switch (item)
                    {
                        case "+": result = a + b; break;
                        case "-": result = a - b; break;
                        case "*": result = a * b; break;
                        case "/":
                            if (b == 0) throw new DivideByZeroException();
                            result = a / b;
                            break;
                        case "//":
                            if (b == 0) throw new DivideByZeroException();
                            result = a / b;
                            break;
                        case "%":
                            if (b == 0) throw new DivideByZeroException();
                            result = a % b;
                            break;
                        case "**": result = (int)Math.Pow(a, b); break;
                        default:
                            errors.Add(new SyntaxError
                            {
                                ErrorFragment = item,
                                Location = "вычисление",
                                Description = "Неизвестный оператор '" + item + "'",
                                Line = 1,
                                Column = 1
                            });
                            return null;
                    }
                    stack.Push(result);
                }

                return stack.Count == 1 ? (int?)stack.Pop() : null;
            }
            catch (DivideByZeroException)
            {
                errors.Add(new SyntaxError
                {
                    ErrorFragment = "",
                    Location = "вычисление",
                    Description = "Деление на ноль",
                    Line = 1,
                    Column = 1
                });
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}