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
        private static string _unaryResult;
        private static string _lastComputation;
        private static int _inputRestriction;

        private static bool _isFirstNumberSet;
        private static bool _containsPreviousNumber;
        private static bool _containsConstant;
        private static bool _isUnary;
        private static bool _containsParentheses;

        private static double _firstNumber;
        private static double _secondNumber;

        private static OperationType _nextType;
        private static OperationType _previousType;

        private static char _separator;

        public static event EventHandler InputChanged;
        public static event EventHandler ResultChanged;
        public static event EventHandler ComputationEnded;
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
                if (_containsPreviousNumber || _containsConstant)
                {
                    if (_input != string.Empty)
                    {
                        value = value.Remove(0, _input.Length);
                    }

                    if (value != string.Empty && value[0] == _separator)
                    {
                        value = value.Insert(0, "0");
                    }

                    _containsPreviousNumber = false;
                    _containsConstant = false;
                }

                if ((value.Length > 1 && value[0] == '0') && value[1] != _separator)
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

            _isFirstNumberSet = false;
            _containsPreviousNumber = false;
            _containsConstant = false;
            _isUnary = false;
            _containsParentheses = false;

            _firstNumber = 0;
            _secondNumber = 0;
            _unaryResult = string.Empty;
            _lastComputation = string.Empty;

            _nextType = OperationType.None;
            _previousType = OperationType.None;

            _separator = Convert.ToChar(System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        public static void EnterNumber(int num)
        {
            if (!CanInput())
            {
                return;
            }

            if (_isUnary)
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
            if (CanInput()
                && (!Input.Contains(_separator.ToString())
                || _containsPreviousNumber
                || _containsConstant))
            {
                if (_isUnary)
                {
                    ClearUnary();
                    Input = "0" + _separator.ToString();
                }
                else
                {
                    Input += _separator;
                }
            }
        }

        public static void DeleteLast()
        {
            if (!_containsPreviousNumber && !_containsConstant && !_isUnary)
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
            _containsConstant = false;
            _containsPreviousNumber = false;

            if (_isUnary)
            {
                ClearUnary();
            }

            Input = "0";
            Result = string.Empty;
            _unaryResult = string.Empty;
        }

        public static void Clear()
        {
            _containsPreviousNumber = false;
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

            if (_containsPreviousNumber || (_nextType == OperationType.None && _isUnary))
            {

                _containsPreviousNumber = true;

                if (_isUnary)
                {
                    _lastComputation = _result + " = " + Input;

                }
                else if (_previousType != OperationType.None || _result[_result.Length - 4] == ')')
                {
                    _lastComputation = _result.Remove(_result.Length - 2, 2) + "= " + Input;
                }

                ComputationEnded?.Invoke(null, new EventArgs());
                ClearResult();

            }
            else if ((_previousType != OperationType.None
                || (_previousType == OperationType.None && _nextType != OperationType.None)))
            {
                if (Double.TryParse(Input, out _secondNumber))
                {
                    string secondNumber = Input;
                    Compute(_nextType);
                    if (_isUnary)
                    {
                        _lastComputation = _result + " = " + Input;
                    }
                    else
                    {
                        _lastComputation = _result + secondNumber + " = " + Input;
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

        public static void DoSqrt()
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
        public static void DoSquare()
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
            return _lastComputation;
        }

        private static void AddNext(in OperationType operationType, char operationSign)
        {
            double number;
            if (Double.TryParse(Input, out number))
            {
                if (_isFirstNumberSet)
                {
                    _secondNumber = number;
                }
                else
                {
                    _firstNumber = number;
                    _isFirstNumberSet = true;
                }

                string resultValue;

                if (_nextType != OperationType.None && (!_containsPreviousNumber || _containsConstant))
                {
                    Compute(_nextType);

                    if (!_isUnary)
                    {
                        resultValue = _secondNumber.ToString();
                    }
                    else
                    {
                        resultValue = string.Empty;
                        _isUnary = false;
                        _unaryResult = string.Empty;
                    }

                    if ((operationType == OperationType.Mult || operationType == OperationType.Div)
                        && (_previousType == OperationType.Add || _previousType == OperationType.Substract)
                        && !_containsParentheses)
                    {
                        Result = "(" + _result + resultValue + ")" + ' ' + operationSign + ' ';
                        _containsParentheses = true;
                    }
                    else
                    {
                        Result = _result + resultValue + ' ' + operationSign + ' ';
                    }

                }
                else if (!_containsPreviousNumber || _containsConstant)
                {
                    if (_isUnary)
                    {
                        Result = _result + " " + operationSign + " ";
                        _isUnary = false;
                        _unaryResult = string.Empty;
                    }
                    else
                    {
                        Result = _result + _firstNumber.ToString() + ' ' + operationSign + ' ';
                    }

                    if (Input[Input.Length - 1] == _separator || (Input.Length > 1 && Input[Input.Length - 2] == _separator && Input[Input.Length - 1] == '0'))
                    {
                        Input = _firstNumber.ToString();
                    }
                }
                else
                {
                    if (_previousType != OperationType.None
                        && (operationType == OperationType.Substract || operationType == OperationType.Add)
                        && _containsParentheses)
                    {
                        Result = (_result.Remove(_result.Length - 4, 4) + ' ' + operationSign + ' ').Remove(0, 1);
                        _containsParentheses = false;
                    }
                    else if (_previousType != OperationType.None
                        && (operationType == OperationType.Mult || operationType == OperationType.Div)
                        && !_containsParentheses)
                    {
                        Result = "(" + _result.Remove(_result.Length - 3, 3) + ")" + ' ' + operationSign + ' ';
                        _containsParentheses = true;
                    }
                    else
                    {

                        if (Result == string.Empty && _containsPreviousNumber)
                        {
                            Result = _firstNumber.ToString() + " " + operationSign + " ";
                        }
                        else
                        {
                            Result = _result.Remove(_result.Length - 2, 2) + operationSign + ' ';
                        }
                    }
                }

                _nextType = operationType;
                _containsPreviousNumber = true;
                _containsConstant = false;
            }
        }
        private static void Compute(OperationType op)
        {
            switch (op)
            {
                case OperationType.Add:
                    _firstNumber += _secondNumber;
                    break;
                case OperationType.Substract:
                    _firstNumber -= _secondNumber;
                    break;
                case OperationType.Div:
                    if (_secondNumber == 0)
                    {
                        ThrowError("Can not divide by zero");
                        return;
                    }
                    else
                    {
                        _firstNumber /= _secondNumber;
                    }
                    break;
                case OperationType.Mult:
                    _firstNumber *= _secondNumber;
                    break;
            }
            _previousType = _nextType;
            _nextType = OperationType.None;
            _unaryResult = string.Empty;
            _containsConstant = false;
            Input = _firstNumber.ToString();
            _containsPreviousNumber = true;
            _containsParentheses = false;
        }

        private static bool CanInput()
        {
            return ((_containsPreviousNumber || (InputRestriction == -1 || Input.Length + 1 <= InputRestriction) || _isUnary));
        }

        private static void ClearUnary()
        {
            if (Result != string.Empty && _nextType != OperationType.None)
            {

                if (_unaryResult != string.Empty)
                {
                    Result = _result.Substring(0, _result.Length - (_unaryResult.Length + 1)) + ' ';
                }
            }
            else
            {
                Result = string.Empty;
            }

            _isUnary = false;
            _unaryResult = string.Empty;
        }

        private static void PrintUnaryOperation(double num, string functionName)
        {
            if (_unaryResult != string.Empty)
            {
                string tempResult = _unaryResult;
                ClearUnary();
                _unaryResult = functionName + "(" + tempResult + ")";
            }
            else
            {
                _unaryResult = functionName + "(" + num.ToString() + ")";
            }

            _containsPreviousNumber = false;
            _isUnary = true;
            Result = _result + _unaryResult;
        }

        private static void ThrowError(string errorMessage)
        {
            _previousType = OperationType.None;
            _nextType = OperationType.None;
            _containsPreviousNumber = false;
            _isFirstNumberSet = false;
            MessageBox.Show(errorMessage);
        }

        private static void ClearResult()
        {
            _isFirstNumberSet = false;
            _containsConstant = false;

            _isUnary = false;

            Result = string.Empty;
            _unaryResult = string.Empty;
            _containsParentheses = false;

            _nextType = OperationType.None;
            _previousType = OperationType.None;
        }
    }
}
