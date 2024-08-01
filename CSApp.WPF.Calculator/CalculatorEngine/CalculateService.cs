using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CSApp.WPF.Calculator.CalculatorEngine
{
    public class CalculateService
    {
        private static string _input;
        private static string _result;
        private static string unaryResult;
        private static string lastComputation;
        private static int _inputRestriction;

        private static bool isFirstNumberSet;
        private static bool isContainsPreviousNumber;
        private static bool isContainsConstant;
        private static bool isUnary;
        private static bool isContainsParentheses;

        private static double firstNumber;
        private static double secondNumber;

        public static event EventHandler InputChanged;
        public static event EventHandler ResultChanged;
        public static event EventHandler ComputationEnded;

        private static OperationType next;
        private static OperationType previous;

        private static char separator;

        public enum OperationType { Add, Substract, Div, Mult, None };
        protected static void OnInputChanged(EventArgs e) => InputChanged?.Invoke(null, e);
        protected static void OnResultChanged(EventArgs e) => ResultChanged?.Invoke(null, e);
        
        public static string Input
        {
            get
            {
                return _input;
            }
            private set
            {
                if (isContainsPreviousNumber || isContainsConstant)
                {
                    if (_input != string.Empty)
                    {
                        value = value.Remove(0, _input.Length);
                    }

                    if (value != string.Empty && value[0] == separator)
                    {
                        value = value.Insert(0, "0");
                    }

                    isContainsPreviousNumber = false;
                    isContainsConstant = false;
                }

                if ((value.Length > 1 && value[0] == '0') && value[1] != separator)
                {
                    value = value.Substring(1);
                }

                _input = value;
                OnInputChanged(EventArgs.Empty);
            }
        }
        public static string Result
        {
            get
            {
                if (_result.Length >= 40)
                {
                    return "..." + _result.Substring(_result.Length - 40, 40);
                }
                return _result;
            }
            private set
            {
                _result = value;
                OnResultChanged(EventArgs.Empty);
            }
        }

        public static int InputRestriction
        {
            get { return _inputRestriction; }
            set
            {
                if (value < -1)
                {
                    value = Math.Abs(value);
                }
                _inputRestriction = value;
            }
        }
        static CalculateService()
        {
            Input = "0";
            Result = string.Empty;
            InputRestriction = -1;

            isFirstNumberSet = false;
            isContainsPreviousNumber = false;
            isContainsConstant = false;
            isUnary = false;
            isContainsParentheses = false;

            firstNumber = 0;
            secondNumber = 0;
            unaryResult = string.Empty;
            lastComputation = string.Empty;

            next = OperationType.None;
            previous = OperationType.None;

            separator = Convert.ToChar(System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        public static void EnterNumber(int num)
        {
            if (!isInputAllowed())
            {
                return;
            }

            if (isUnary)
            {
                ClearUnary();
                Input = num.ToString();
            }
            else
            {
                Input += num.ToString();
            }
        }

        public static void EnterDot()
        {
            if (isInputAllowed()
                && (!Input.Contains(separator.ToString())
                || isContainsPreviousNumber
                || isContainsConstant))
            {
                if (isUnary)
                {
                    ClearUnary();
                    Input = "0" + separator.ToString();
                }
                else
                {
                    Input += separator;
                }
            }
        }

        public static void DeleteLast()
        {
            if (!isContainsPreviousNumber && !isContainsConstant && !isUnary)
            {
                Input = Input.Remove(Input.Length - 1);

                if (Input.Length == 0 || (Input.Length == 1 && Input[0] == '-'))
                {
                    Input = "0";
                }
            }
        }

        public static void ClearEntry()
        {
            isContainsConstant = false;
            isContainsPreviousNumber = false;

            if (isUnary)
            {
                ClearUnary();
            }

            Input = "0";
            Result = string.Empty;
            unaryResult = string.Empty;
        }

        public static void Clear()
        {
            isContainsPreviousNumber = false;
            ClearResult();
            Input = "0";
        }

        public static void Equal()
        {
            if (Result == string.Empty)
            {
                ClearResult();
                return;
            }

            if (isContainsPreviousNumber || (next == OperationType.None && isUnary))
            {

                isContainsPreviousNumber = true;

                if (isUnary)
                {
                    lastComputation = _result + " = " + Input;

                }
                else if (previous != OperationType.None || _result[_result.Length - 4] == ')')
                {
                    lastComputation = _result.Remove(_result.Length - 2, 2) + "= " + Input;
                }

                ComputationEnded?.Invoke(null, new EventArgs());
                ClearResult();

            }
            else if ((previous != OperationType.None
                || (previous == OperationType.None && next != OperationType.None)))
            {
                if (Double.TryParse(Input, out secondNumber))
                {
                    string secondNumber = Input;
                    Compute(next);
                    if (isUnary)
                    {
                        lastComputation = _result + " = " + Input;
                    }
                    else
                    {
                        lastComputation = _result + secondNumber + " = " + Input;
                    }

                    ComputationEnded?.Invoke(null, new EventArgs());
                    ClearResult();

                }
            }
        }

        public static void Negate()
        {
            double number;
            if (Double.TryParse(Input, out number))
            {
                PrintUnaryOperation(number, "negate");
                Input = (number * (-1)).ToString();

            }
        }

        public static void SquareRoot()
        {
            double num;
            if (Double.TryParse(Input, out num))
            {
                if (num < 0)
                {
                    ThrowError(num + " less than zero");
                    return;
                }

                PrintUnaryOperation(num, "sqrt");
                Input = Math.Sqrt(num).ToString();
            }
        }
        public static void Square()
        {
            double num;
            if (Double.TryParse(Input, out num))
            {
                PrintUnaryOperation(num, "^2");
                Input = Math.Pow(num, 2).ToString();
            }
        }

        public static void ExecuteOperation(OperationType type)
        {
            if (type != OperationType.None)
            {
                switch (type)
                {
                    case OperationType.Add:
                        AddNext(OperationType.Add, '+');
                        break;
                    case OperationType.Substract:
                        AddNext(OperationType.Substract, '-');
                        break;
                    case OperationType.Div:
                        AddNext(OperationType.Div, '/');
                        break;
                    case OperationType.Mult:
                        AddNext(OperationType.Mult, '*');
                        break;
                }
            }
        }

        public static string getLastComputation()
        {
            return lastComputation;
        }

        private static void AddNext(in OperationType operationType, char operationSign)
        {
            double number;
            if (Double.TryParse(Input, out number))
            {
                if (isFirstNumberSet)
                {
                    secondNumber = number;
                }
                else
                {
                    firstNumber = number;
                    isFirstNumberSet = true;
                }

                string resultValue;

                if (next != OperationType.None && (!isContainsPreviousNumber || isContainsConstant))
                {
                    Compute(next);

                    if (!isUnary)
                    {
                        resultValue = secondNumber.ToString();
                    }
                    else
                    {
                        resultValue = string.Empty;
                        isUnary = false;
                        unaryResult = string.Empty;
                    }

                    if ((operationType == OperationType.Mult || operationType == OperationType.Div)
                        && (previous == OperationType.Add || previous == OperationType.Substract)
                        && !isContainsParentheses)
                    {
                        Result = "(" + _result + resultValue + ")" + ' ' + operationSign + ' ';
                        isContainsParentheses = true;
                    }
                    else
                    {
                        Result = _result + resultValue + ' ' + operationSign + ' ';
                    }

                }
                else if (!isContainsPreviousNumber || isContainsConstant)
                {
                    if (isUnary)
                    {
                        Result = _result + " " + operationSign + " ";
                        isUnary = false;
                        unaryResult = string.Empty;
                    }
                    else
                    {
                        Result = _result + firstNumber.ToString() + ' ' + operationSign + ' ';
                    }

                    if (Input[Input.Length - 1] == separator || (Input.Length > 1 && Input[Input.Length - 2] == separator && Input[Input.Length - 1] == '0'))
                    {
                        Input = firstNumber.ToString();
                    }
                }
                else
                {
                    if (previous != OperationType.None
                        && (operationType == OperationType.Substract || operationType == OperationType.Add)
                        && isContainsParentheses)
                    {
                        Result = (_result.Remove(_result.Length - 4, 4) + ' ' + operationSign + ' ').Remove(0, 1);
                        isContainsParentheses = false;
                    }
                    else if (previous != OperationType.None
                        && (operationType == OperationType.Mult || operationType == OperationType.Div)
                        && !isContainsParentheses)
                    {
                        Result = "(" + _result.Remove(_result.Length - 3, 3) + ")" + ' ' + operationSign + ' ';
                        isContainsParentheses = true;
                    }
                    else
                    {

                        if (Result == string.Empty && isContainsPreviousNumber)
                        {
                            Result = firstNumber.ToString() + " " + operationSign + " ";
                        }
                        else
                        {
                            Result = _result.Remove(_result.Length - 2, 2) + operationSign + ' ';
                        }
                    }
                }

                next = operationType;
                isContainsPreviousNumber = true;
                isContainsConstant = false;
            }
        }
        private static void Compute(OperationType op)
        {
            switch (op)
            {
                case OperationType.Add:
                    firstNumber += secondNumber;
                    break;
                case OperationType.Substract:
                    firstNumber -= secondNumber;
                    break;
                case OperationType.Div:
                    if (secondNumber == 0)
                    {
                        ThrowError("Can not divide by zero");
                        return;
                    }
                    else
                    {
                        firstNumber /= secondNumber;
                    }
                    break;
                case OperationType.Mult:
                    firstNumber *= secondNumber;
                    break;
            }
            previous = next;
            next = OperationType.None;
            unaryResult = string.Empty;
            isContainsConstant = false;
            Input = firstNumber.ToString();
            isContainsPreviousNumber = true;
            isContainsParentheses = false;

        }

        private static bool isInputAllowed()
        {
            return ((isContainsPreviousNumber || (InputRestriction == -1 || Input.Length + 1 <= InputRestriction) || isUnary));
        }

        private static void ClearUnary()
        {
            if (Result != string.Empty && next != OperationType.None)
            {

                if (unaryResult != string.Empty)
                {
                    Result = _result.Substring(0, _result.Length - (unaryResult.Length + 1)) + ' ';
                }
            }
            else
            {
                Result = string.Empty;
            }

            isUnary = false;
            unaryResult = string.Empty;
        }

        private static void PrintUnaryOperation(double num, string functionName)
        {
            if (unaryResult != string.Empty)
            {
                string tempResult = unaryResult;
                ClearUnary();
                unaryResult = functionName + "(" + tempResult + ")";
            }
            else
            {
                unaryResult = functionName + "(" + num.ToString() + ")";
            }

            isContainsPreviousNumber = false;
            isUnary = true;
            Result = _result + unaryResult;
        }

        private static void ThrowError(string errorMessage)
        {
            previous = OperationType.None;
            next = OperationType.None;
            isContainsPreviousNumber = false;
            isFirstNumberSet = false;
            MessageBox.Show(errorMessage);
        }

        private static void ClearResult()
        {
            isFirstNumberSet = false;
            isContainsConstant = false;

            isUnary = false;

            Result = string.Empty;
            unaryResult = string.Empty;
            isContainsParentheses = false;

            next = OperationType.None;
            previous = OperationType.None;
        }
    }
}
