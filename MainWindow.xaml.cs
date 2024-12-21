using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SimplexCalculator
{
    public partial class MainWindow : Window
    {
        private int variableCount = 3;
        private int constraintCount = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateInterface(object sender, RoutedEventArgs e)
        {
            try
            {
                variableCount = int.Parse(TxtNumVariables.Text);
                constraintCount = int.Parse(TxtNumConstraints.Text);
                TargetFunctionPanel.Children.Clear();
                ConstraintsPanel.Children.Clear();

                for (int i = 0; i < variableCount; i++)
                {
                    TargetFunctionPanel.Children.Add(new TextBox
                    {
                        Width = 60,
                        Margin = new Thickness(5),
                        Tag = $"C{i + 1}"
                    });
                }

                for (int i = 0; i < constraintCount; i++)
                {
                    StackPanel constraintRow = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5)
                    };

                    for (int j = 0; j < variableCount; j++)
                    {
                        constraintRow.Children.Add(new TextBox
                        {
                            Width = 60,
                            Margin = new Thickness(5),
                            Tag = $"A{i + 1}{j + 1}"
                        });
                    }

                    constraintRow.Children.Add(new ComboBox
                    {
                        Width = 60,
                        ItemsSource = new List<string> { "≤", "≥", "=" },
                        SelectedIndex = 0,
                        Margin = new Thickness(5)
                    });

                    constraintRow.Children.Add(new TextBox
                    {
                        Width = 60,
                        Margin = new Thickness(5),
                        Tag = $"B{i + 1}"
                    });

                    ConstraintsPanel.Children.Add(constraintRow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления интерфейса: {ex.Message}");
            }
        }

        private void Solve(object sender, RoutedEventArgs e)
        {
            try
            {
                double[] coefficients = TargetFunctionPanel.Children
                    .OfType<TextBox>()
                    .Select(tb => double.Parse(tb.Text))
                    .ToArray();

                double[,] matrixA = new double[constraintCount, variableCount];
                double[] vectorB = new double[constraintCount];
                string[] inequalitySigns = new string[constraintCount];

                for (int i = 0; i < constraintCount; i++)
                {
                    StackPanel row = (StackPanel)ConstraintsPanel.Children[i];
                    for (int j = 0; j < variableCount; j++)
                    {
                        matrixA[i, j] = double.Parse(((TextBox)row.Children[j]).Text);
                    }

                    inequalitySigns[i] = ((ComboBox)row.Children[variableCount]).SelectedItem.ToString();
                    vectorB[i] = double.Parse(((TextBox)row.Children[variableCount + 1]).Text);
                }

                var (standardMatrixA, standardVectorB, standardCoefficients) = ConvertToStandardForm(matrixA, vectorB, coefficients, inequalitySigns);
                string result = SolveSimplexMethod(standardMatrixA, standardVectorB, standardCoefficients);
                TxtResult.Text = result;
            }
            catch (Exception ex)
            {
                TxtResult.Text = $"Ошибка: {ex.Message}";
            }
        }

        private (double[,] standardA, double[] standardB, double[] standardC) ConvertToStandardForm(double[,] A, double[] b, double[] c, string[] signs)
        {
            int rowCount = A.GetLength(0);
            int colCount = A.GetLength(1);
            double[,] standardA = new double[rowCount, colCount + rowCount];
            double[] standardB = new double[rowCount];
            double[] standardC = new double[colCount + rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    standardA[i, j] = A[i, j];
                }

                if (signs[i] == "≤")
                {
                    standardA[i, colCount + i] = 1;
                }
                else if (signs[i] == "≥")
                {
                    standardA[i, colCount + i] = -1;
                }

                standardB[i] = b[i];
            }

            for (int j = 0; j < colCount; j++)
            {
                standardC[j] = c[j];
            }

            return (standardA, standardB, standardC);
        }

        private string SolveSimplexMethod(double[,] A, double[] b, double[] c)
        {
            int numRows = b.Length;
            int numCols = c.Length + 1;
            double[,] tableau = new double[numRows + 1, numCols];
            int[] basis = new int[numRows];

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < c.Length; j++)
                {
                    tableau[i, j] = A[i, j];
                }
                tableau[i, numCols - 1] = b[i];
                basis[i] = c.Length + i;
            }

            for (int j = 0; j < c.Length; j++)
            {
                tableau[numRows, j] = -c[j];
            }

            while (true)
            {
                int pivotCol = FindPivotColumn(tableau, numRows, numCols);
                if (pivotCol == -1)
                    return GetSolution(tableau, numRows, numCols, basis);

                int pivotRow = FindPivotRow(tableau, pivotCol, numRows, numCols);
                if (pivotRow == -1)
                    throw new Exception("Нет конечного решения: задача неограниченна.");

                PerformPivot(tableau, pivotRow, pivotCol, numRows, numCols, basis);
            }
        }

        private int FindPivotColumn(double[,] tableau, int numRows, int numCols)
        {
            int pivotCol = -1;
            double minValue = 0;

            for (int j = 0; j < numCols - 1; j++)
            {
                if (tableau[numRows, j] < minValue)
                {
                    minValue = tableau[numRows, j];
                    pivotCol = j;
                }
            }

            return pivotCol;
        }

        private int FindPivotRow(double[,] tableau, int pivotCol, int numRows, int numCols)
        {
            int pivotRow = -1;
            double minRatio = double.PositiveInfinity;

            for (int i = 0; i < numRows; i++)
            {
                double value = tableau[i, pivotCol];
                if (value > 0)
                {
                    double ratio = tableau[i, numCols - 1] / value;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }

            return pivotRow;
        }

        private void PerformPivot(double[,] tableau, int pivotRow, int pivotCol, int numRows, int numCols, int[] basis)
        {
            double pivotValue = tableau[pivotRow, pivotCol];
            for (int j = 0; j < numCols; j++)
            {
                tableau[pivotRow, j] /= pivotValue;
            }

            for (int i = 0; i <= numRows; i++)
            {
                if (i != pivotRow)
                {
                    double factor = tableau[i, pivotCol];
                    for (int j = 0; j < numCols; j++)
                        tableau[i, j] -= factor * tableau[pivotRow, j];
                }
            }

            basis[pivotRow] = pivotCol;
        }

        private string GetSolution(double[,] tableau, int numRows, int numCols, int[] basis)
        {
            double[] solution = new double[numCols - 1];
            for (int i = 0; i < numRows; i++)
            {
                if (basis[i] < solution.Length)
                    solution[basis[i]] = tableau[i, numCols - 1];
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine("Оптимальное решение:");
            for (int i = 0; i < variableCount; i++)
            {
                result.AppendLine($"x{i + 1} = {Math.Round(solution[i], 2)}");
            }

            double maxValue = tableau[numRows, numCols - 1];
            result.AppendLine($"Максимальное значение целевой функции: {Math.Round(maxValue, 2)}");
            return result.ToString();
        }
    }
}