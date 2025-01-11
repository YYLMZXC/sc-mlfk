using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Engine;
using Game;
namespace Mlfk
{
    public class ExpressionCalculator
    {
        public class CompositeExpression
        {
            public string Expression;

            public List<string> Includes = new List<string>();

            public string[] Separations = new string[3];

            public int Rank;

            public float Result;
        }

        public static string[] Variables = new string[3] { "x", "y", "z" };

        public static char[] Nums = new char[10] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static string[] Funcs = new string[7] { "sin", "cos", "tan", "ln", "abs", "^", "%" };

        public string Expression;

        public Dictionary<string, CompositeExpression> SubExpressions = new Dictionary<string, CompositeExpression>();

        public Dictionary<int, List<string>> Ranks = new Dictionary<int, List<string>>();

        public ExpressionCalculator(string expression)
        {
            Expression = expression;
            string[] funcs = Funcs;
            foreach (string func in funcs)
            {
                SetFuncExpression(expression, func);
            }

            foreach (string key in SubExpressions.Keys)
            {
                List<string> list = new List<string>();
                foreach (string key2 in SubExpressions.Keys)
                {
                    if (key != key2 && key.Contains(key2))
                    {
                        list.Add(key2);
                    }
                }

                SubExpressions[key].Includes = list;
            }

            SetExpressionRank();
        }

        public int Calculate(int x, int y, int z)
        {
            return Calculate(new int[3] { x, y, z });
        }

        public int Calculate(int[] variableValue)
        {
            foreach (int key in Ranks.Keys)
            {
                foreach (string item in Ranks[key])
                {
                    CompositeExpression compositeExpression = SubExpressions[item];
                    string text = compositeExpression.Separations[1];
                    foreach (string include in compositeExpression.Includes)
                    {
                        if (float.IsNaN(SubExpressions[include].Result))
                        {
                            return int.MinValue;
                        }

                        text = ((!(SubExpressions[include].Result < 0f)) ? text.Replace(include, SubExpressions[include].Result.ToString() ?? "") : text.Replace(include, "(0" + SubExpressions[include].Result + ")"));
                    }

                    string s = CalculateVariableExpression(text, Variables, variableValue);
                    float num = (float)double.Parse(s);
                    string empty = string.Empty;
                    float num2 = 1f;
                    if (compositeExpression.Separations[0] == "^" || compositeExpression.Separations[0] == "%")
                    {
                        empty = compositeExpression.Separations[2];
                        foreach (string include2 in compositeExpression.Includes)
                        {
                            empty = ((!(SubExpressions[include2].Result < 0f)) ? empty.Replace(include2, SubExpressions[include2].Result.ToString() ?? "") : empty.Replace(include2, "(0" + SubExpressions[include2].Result + ")"));
                        }

                        s = CalculateVariableExpression(empty, Variables, variableValue);
                        num2 = (float)double.Parse(s);
                    }

                    switch (compositeExpression.Separations[0])
                    {
                        case "sin":
                            num = MathUtils.Sin(num);
                            break;
                        case "cos":
                            num = MathUtils.Cos(num);
                            break;
                        case "tan":
                            num = MathUtils.Tan(num);
                            break;
                        case "ln":
                            num = MathUtils.Log(num);
                            break;
                        case "abs":
                            num = MathUtils.Abs(num);
                            break;
                        case "%":
                            num %= num2;
                            break;
                        case "^":
                            num = MathUtils.Pow(num, num2);
                            break;
                    }

                    compositeExpression.Result = num;
                }
            }

            string text2 = Expression;
            foreach (string key2 in SubExpressions.Keys)
            {
                text2 = ((!(SubExpressions[key2].Result < 0f)) ? text2.Replace(key2, SubExpressions[key2].Result.ToString() ?? "") : text2.Replace(key2, "(0" + SubExpressions[key2].Result + ")"));
            }

            string s2 = CalculateVariableExpression(text2, Variables, variableValue);
            float x = (float)double.Parse(s2);
            return (int)MathUtils.Round(x);
        }

        public void SetExpressionRank()
        {
            List<string> list = new List<string>();
            List<string> list2 = new List<string>();
            List<string> list3 = new List<string>();
            List<string> list4 = new List<string>();
            List<string> list5 = new List<string>();
            foreach (string key2 in SubExpressions.Keys)
            {
                if (SubExpressions[key2].Includes.Count == 0)
                {
                    list.Add(key2);
                    SubExpressions[key2].Rank = 1;
                }
            }

            foreach (string key3 in SubExpressions.Keys)
            {
                if (SubExpressions[key3].Includes.Count <= 0 || list.Contains(key3))
                {
                    continue;
                }

                bool flag = true;
                foreach (string include in SubExpressions[key3].Includes)
                {
                    if (!list.Contains(include))
                    {
                        flag = false;
                        break;
                    }
                }

                if (flag)
                {
                    list2.Add(key3);
                    SubExpressions[key3].Rank = 2;
                }
            }

            foreach (string key4 in SubExpressions.Keys)
            {
                if (SubExpressions[key4].Includes.Count <= 1 || list2.Contains(key4) || list.Contains(key4))
                {
                    continue;
                }

                bool flag2 = true;
                foreach (string include2 in SubExpressions[key4].Includes)
                {
                    if (!list2.Contains(include2) && !list.Contains(include2))
                    {
                        flag2 = false;
                        break;
                    }
                }

                if (flag2)
                {
                    list3.Add(key4);
                    SubExpressions[key4].Rank = 3;
                }
            }

            foreach (string key5 in SubExpressions.Keys)
            {
                if (SubExpressions[key5].Includes.Count <= 2 || list3.Contains(key5) || list2.Contains(key5) || list.Contains(key5))
                {
                    continue;
                }

                bool flag3 = true;
                foreach (string include3 in SubExpressions[key5].Includes)
                {
                    if (!list3.Contains(include3) && !list2.Contains(include3) && !list.Contains(include3))
                    {
                        flag3 = false;
                        break;
                    }
                }

                if (flag3)
                {
                    list4.Add(key5);
                    SubExpressions[key5].Rank = 4;
                }
            }

            foreach (string key6 in SubExpressions.Keys)
            {
                if (SubExpressions[key6].Includes.Count <= 3 || list4.Contains(key6) || list3.Contains(key6) || list2.Contains(key6) || list.Contains(key6))
                {
                    continue;
                }

                bool flag4 = true;
                foreach (string include4 in SubExpressions[key6].Includes)
                {
                    if (!list4.Contains(include4) && !list3.Contains(include4) && !list2.Contains(include4) && !list.Contains(include4))
                    {
                        flag4 = false;
                        break;
                    }
                }

                if (flag4)
                {
                    list5.Add(key6);
                    SubExpressions[key6].Rank = 5;
                }
            }

            if (list.Count > 0)
            {
                Ranks[1] = list;
            }

            if (list2.Count > 0)
            {
                Ranks[2] = list2;
            }

            if (list3.Count > 0)
            {
                Ranks[3] = list3;
            }

            if (list4.Count > 0)
            {
                Ranks[4] = list4;
            }

            if (list5.Count > 0)
            {
                Ranks[5] = list5;
            }

            string[] array = new string[SubExpressions.Count];
            int num = 0;
            foreach (string key7 in SubExpressions.Keys)
            {
                array[num++] = key7;
            }

            for (int i = 0; i < array.Length - 1; i++)
            {
                for (int j = 0; j < array.Length - 1 - i; j++)
                {
                    if (SubExpressions[array[j]].Rank < SubExpressions[array[j + 1]].Rank)
                    {
                        string text = array[j + 1];
                        array[j + 1] = array[j];
                        array[j] = text;
                    }
                }
            }

            Dictionary<string, CompositeExpression> dictionary = new Dictionary<string, CompositeExpression>();
            string[] array2 = array;
            foreach (string key in array2)
            {
                dictionary[key] = SubExpressions[key];
            }

            SubExpressions = dictionary;
            foreach (string key8 in SubExpressions.Keys)
            {
                string[] array3 = SubExpressions[key8].Includes.ToArray();
                for (int l = 0; l < array3.Length - 1; l++)
                {
                    for (int m = 0; m < array3.Length - 1 - l; m++)
                    {
                        if (SubExpressions[array3[m]].Rank < SubExpressions[array3[m + 1]].Rank)
                        {
                            string text2 = array3[m + 1];
                            array3[m + 1] = array3[m];
                            array3[m] = text2;
                        }
                    }
                }

                SubExpressions[key8].Includes.Clear();
                string[] array4 = array3;
                foreach (string item in array4)
                {
                    SubExpressions[key8].Includes.Add(item);
                }
            }
        }

        public void SetFuncExpression(string expression, string func)
        {
            if (!expression.Contains(func))
            {
                return;
            }

            List<int> list = new List<int>();
            for (int num = expression.IndexOf(func); num != -1; num = expression.IndexOf(func, num + 1))
            {
                list.Add(num);
            }

            if (func == "^" || func == "%")
            {
                foreach (int item in list)
                {
                    string text = string.Empty;
                    string text2 = string.Empty;
                    if (expression[item - 1] == ')')
                    {
                        int num2 = 0;
                        int num3 = 0;
                        for (int num4 = item; num4 >= 0; num4--)
                        {
                            if (expression[num4] == '(')
                            {
                                num2++;
                            }

                            if (expression[num4] == ')')
                            {
                                num3++;
                            }

                            if (num2 == num3 && num2 != 0)
                            {
                                text = expression.Substring(num4, item - num4);
                                break;
                            }
                        }
                    }
                    else
                    {
                        int num5 = item - 1;
                        while (num5 >= 0 && (IsNum(expression[num5]) || expression[num5] == '.' || IsVariables(expression[num5], Variables)))
                        {
                            text = expression[num5] + text;
                            num5--;
                        }
                    }

                    if (expression[item + 1] == '(')
                    {
                        int num6 = 0;
                        int num7 = 0;
                        for (int i = item; i < expression.Length; i++)
                        {
                            if (expression[i] == '(')
                            {
                                num6++;
                            }

                            if (expression[i] == ')')
                            {
                                num7++;
                            }

                            if (num6 == num7 && num6 != 0)
                            {
                                text2 = expression.Substring(item + 1, i - item);
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int j = item + 1; j < expression.Length && (IsNum(expression[j]) || expression[j] == '.' || IsVariables(expression[j], Variables)); j++)
                        {
                            text2 += expression[j];
                        }
                    }

                    CompositeExpression compositeExpression = new CompositeExpression();
                    compositeExpression.Expression = text + func + text2;
                    compositeExpression.Separations[0] = func;
                    compositeExpression.Separations[1] = text;
                    compositeExpression.Separations[2] = text2;
                    SubExpressions[compositeExpression.Expression] = compositeExpression;
                }

                return;
            }

            foreach (int item2 in list)
            {
                int num8 = 0;
                int num9 = 0;
                for (int k = item2; k < expression.Length; k++)
                {
                    if (expression[k] == '(')
                    {
                        num8++;
                    }

                    if (expression[k] == ')')
                    {
                        num9++;
                    }

                    if (num8 == num9 && num8 != 0)
                    {
                        string text3 = expression.Substring(item2, k - item2 + 1);
                        CompositeExpression compositeExpression2 = new CompositeExpression();
                        compositeExpression2.Expression = text3;
                        compositeExpression2.Separations[0] = func;
                        compositeExpression2.Separations[1] = compositeExpression2.Expression.Substring(compositeExpression2.Separations[0].Length + 1, compositeExpression2.Expression.Length - compositeExpression2.Separations[0].Length - 2);
                        SubExpressions[text3] = compositeExpression2;
                        break;
                    }
                }
            }
        }

        public string CalculateVariableExpression(string expression, string[] variableChar, int[] variableValue)
        {
            if (variableChar.Length != variableValue.Length)
            {
                return expression;
            }

            if (expression.Contains("NaN"))
            {
                return "NaN";
            }

            for (int i = 0; i < variableChar.Length; i++)
            {
                expression = ((variableValue[i] >= 0) ? expression.Replace(variableChar[i], variableValue[i].ToString() ?? "") : expression.Replace(variableChar[i], "(0" + variableValue[i] + ")"));
            }

            expression = Standardize(expression);
            if (IsNum(expression))
            {
                return expression;
            }

            string epostfixExpression = GetEpostfixExpression(expression);
            return CalculateEpostfixExpression(epostfixExpression);
        }

        public bool IsNum(char c)
        {
            char[] nums = Nums;
            foreach (char c2 in nums)
            {
                if (c == c2)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsNum(string expression)
        {
            if (expression.StartsWith("-"))
            {
                expression = expression.Substring(1);
            }

            Regex regex = new Regex("^[0-9\\.]+$");
            return !string.IsNullOrEmpty(regex.Match(expression).Value);
        }

        public bool IsVariables(char c, string[] variables)
        {
            foreach (string s in variables)
            {
                if (char.Parse(s) == c)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsExpression(string expression)
        {
            Regex regex = new Regex("^[0-9\\+\\-\\*\\/\\(\\)\\.]+$");
            return !string.IsNullOrEmpty(regex.Match(expression).Value);
        }

        public string Standardize(string expression)
        {
            expression = expression.Replace(" ", "");
            expression = expression.Replace("(-", "(0-");
            expression = expression.Replace("pi", ((float)Math.PI).ToString());
            expression = expression.Replace("e", ((float)Math.E).ToString());
            return expression;
        }

        public string GetEpostfixExpression(string expression)
        {
            List<string> list = new List<string>();
            string text = "";
            string text2;
            while (expression.Length > 0)
            {
                text2 = "";
                if (IsNum(expression[0]))
                {
                    while (IsNum(expression[0]) || expression[0] == '.')
                    {
                        text2 += expression[0];
                        expression = expression.Substring(1);
                        if (expression == "")
                        {
                            break;
                        }
                    }

                    text = text + text2 + "|";
                }

                if (expression.Length > 0 && expression[0].ToString() == "(")
                {
                    list.Add("(");
                    expression = expression.Substring(1);
                }

                text2 = "";
                if (expression.Length > 0 && expression[0].ToString() == ")")
                {
                    while (list[list.Count - 1].ToString() != "(")
                    {
                        text2 = text2 + list[list.Count - 1].ToString() + "|";
                        list.RemoveAt(list.Count - 1);
                        bool flag = true;
                    }

                    list.RemoveAt(list.Count - 1);
                    text += text2;
                    expression = expression.Substring(1);
                }

                text2 = "";
                if (expression.Length <= 0 || (!(expression[0].ToString() == "*") && !(expression[0].ToString() == "/") && !(expression[0].ToString() == "+") && !(expression[0].ToString() == "-")))
                {
                    continue;
                }

                string text3 = expression[0].ToString();
                if (list.Count > 0)
                {
                    if (list[list.Count - 1].ToString() == "(" || OperatorPriority(text3, list[list.Count - 1].ToString()))
                    {
                        list.Add(text3);
                    }
                    else
                    {
                        text2 = text2 + list[list.Count - 1].ToString() + "|";
                        list.RemoveAt(list.Count - 1);
                        list.Add(text3);
                        text += text2;
                    }
                }
                else
                {
                    list.Add(text3);
                }

                expression = expression.Substring(1);
            }

            text2 = "";
            while (list.Count != 0)
            {
                text2 = text2 + list[list.Count - 1].ToString() + "|";
                list.RemoveAt(list.Count - 1);
            }

            if (text2.Length > 0)
            {
                text += text2.Substring(0, text2.Length - 1);
            }

            return text;
        }

        public string CalculateEpostfixExpression(string epostfixExpression)
        {
            List<string> list = new List<string>();
            epostfixExpression = epostfixExpression.Replace(" ", "");
            string[] array = epostfixExpression.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++)
            {
                if (IsNum(array[i][0]))
                {
                    list.Add(array[i].ToString());
                    continue;
                }

                float operand = (float)double.Parse(list[list.Count - 1]);
                list.RemoveAt(list.Count - 1);
                float operand2 = (float)double.Parse(list[list.Count - 1]);
                list.RemoveAt(list.Count - 1);
                list.Add(Arithmetic(operand2, operand, array[i]).ToString());
            }

            return list[0].ToString();
        }

        public bool OperatorPriority(string operator1, string operator2)
        {
            if (operator1 == "*" && operator2 == "+")
            {
                return true;
            }

            if (operator1 == "*" && operator2 == "-")
            {
                return true;
            }

            if (operator1 == "/" && operator2 == "+")
            {
                return true;
            }

            if (operator1 == "/" && operator2 == "-")
            {
                return true;
            }

            return false;
        }

        public float Arithmetic(float operand1, float operand2, string str_operator)
        {
            switch (str_operator)
            {
                case "*":
                    operand1 *= operand2;
                    break;
                case "/":
                    operand1 /= operand2;
                    break;
                case "+":
                    operand1 += operand2;
                    break;
                case "-":
                    operand1 -= operand2;
                    break;
            }

            return operand1;
        }
    }
}