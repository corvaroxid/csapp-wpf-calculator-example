using CSApp.WPF.Calculator.CalculatorEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSApp.WPF.Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CalculateService.InputRestriction = 15;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string cont = b.Content.ToString();

            switch (cont)
            {
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                case "0":
                    CalculateService.EnterNumber(Int32.Parse(cont));
                    break;
                case ".":
                    CalculateService.EnterDot();
                    break;
                case "⟵":
                    CalculateService.DeleteLast();
                    break;
                case "C":
                    CalculateService.Clear();
                    break;
                case "CE":
                    CalculateService.ClearEntry();
                    break;
                case "+":
                    CalculateService.ExecuteOperation(CalculateService.OperationType.Add);
                    break;
                case "-":
                    CalculateService.ExecuteOperation(CalculateService.OperationType.Substract);
                    break;
                case "*":
                    CalculateService.ExecuteOperation(CalculateService.OperationType.Mult);
                    break;
                case "/":
                    CalculateService.ExecuteOperation(CalculateService.OperationType.Div);
                    break;
                case "=":
                    CalculateService.Equal();
                    break;
                case "±":
                    CalculateService.Negate();
                    break;
                case "√":
                    CalculateService.DoSqrt();
                    break;
                case "x²":
                    CalculateService.DoSquare();
                    break;
            }
        }
    }
}
