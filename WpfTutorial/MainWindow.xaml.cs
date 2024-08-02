using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using MySql.Data.MySqlClient;


namespace WpfTutorial
{
    public partial class MainWindow : Window
    {
        private string currentInput = "";
        private Node expressionTree;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            string inorder = InorderTextBox.Text;
            string preorder = PreorderTextBox.Text;
            string postorder = PostorderTextBox.Text;
            string binary = BinaryTextBox.Text;
            string expression = DecimalTextBox.Text; // 直接使用字串

            // 確保有有效的運算式和結果
            if (string.IsNullOrEmpty(inorder) || string.IsNullOrEmpty(preorder) ||
                string.IsNullOrEmpty(postorder) || string.IsNullOrEmpty(binary))
            {
                MessageBox.Show("請先計算運算式再插入！");
                return;
            }

            DatabaseHelper dbHelper = new DatabaseHelper();
            dbHelper.InsertExpression(inorder, preorder, postorder, binary, expression); // 傳遞字串
        }


        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            QueryWindow queryWindow = new QueryWindow();
            queryWindow.Show(); // 顯示查詢視窗
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                currentInput += button.Content.ToString();
                InorderTextBox.Text = currentInput;
            }
        }

        private void Equal_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValidExpression(currentInput))
            {
                MessageBox.Show("請輸入有效運算式，例如 '1 + 2'");
                return;
            }

            // 使用空格分隔輸入
            string[] parts = currentInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                MessageBox.Show("請輸入有效的運算式，例如 '1 + 2'");
                return;
            }

            // 建立運算式樹
            expressionTree = BuildExpressionTree(parts);
            if (expressionTree != null)
            {
                // 計算結果
                double result = EvaluateExpressionTree(expressionTree);

                // 顯示結果
                PreorderTextBox.Text = expressionTree.Preorder();
                PostorderTextBox.Text = expressionTree.Postorder();
                DecimalTextBox.Text = result.ToString();
                BinaryTextBox.Text = Convert.ToString((int)result, 2);
            }

          
        }


        private bool IsValidExpression(string expression)
        {
            // 移除空白字符
            expression = expression.Replace(" ", "");

            // 檢查是否為空或只包含運算符
            if (string.IsNullOrEmpty(expression) || "+-*/".Contains(expression))
                return false;

            // 檢查是否以運算符開頭或結尾
            if ("+-*/".Contains(expression[0]) || "+-*/".Contains(expression[expression.Length - 1]))
                return false;

            // 檢查運算符是否連續出現
            for (int i = 1; i < expression.Length; i++)
            {
                if ("+-*/".Contains(expression[i]) && "+-*/".Contains(expression[i - 1]))
                    return false;
            }

            // 檢查是否有數字
            return expression.Any(char.IsDigit);
        }


        private Node BuildExpressionTree(string[] parts)
        {
            Stack<Node> values = new Stack<Node>();
            Stack<string> operators = new Stack<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                if (double.TryParse(parts[i], out double number))
                {
                    values.Push(new Node(number));
                }
                else
                {
                    while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(parts[i]))
                    {
                        var right = values.Pop();
                        var left = values.Pop();
                        var op = operators.Pop();
                        // 將運算符和左右子樹組合成新的節點
                        values.Push(new Node(op, left, right));
                    }
                    operators.Push(parts[i]);
                }
            }

            while (operators.Count > 0)
            {
                var right = values.Pop();
                var left = values.Pop();
                var op = operators.Pop();
                values.Push(new Node(op, left, right));
            }

            return values.Pop();
        }

        private int Precedence(string op)
        {
            switch (op)
            {
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                    return 2;
                default:
                    return 0;
            }
        }

        private double EvaluateExpressionTree(Node node)
        {
            if (node == null)
                return 0;

            if (node.Left == null && node.Right == null)
                return node.Value;

            double leftValue = EvaluateExpressionTree(node.Left);
            double rightValue = EvaluateExpressionTree(node.Right);

            switch (node.Operator)
            {
                case "+":
                    return leftValue + rightValue;
                case "-":
                    return leftValue - rightValue;
                case "*":
                    return leftValue * rightValue;
                case "/":
                    return rightValue != 0 ? leftValue / rightValue : throw new DivideByZeroException();
                default:
                    return 0;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InorderTextBox.Text = "";
            currentInput = "";
            expressionTree = null;
            PreorderTextBox.Text = "";
            PostorderTextBox.Text = "";
            DecimalTextBox.Text = "";
            BinaryTextBox.Text = "";
        }

        private void Operator_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                currentInput += " " + button.Content.ToString() + " ";
                InorderTextBox.Text = currentInput; // 更新顯示框
            }
        }

        public class DatabaseHelper
        {
            // 插入表達式的方法
            public void InsertExpression(string inorder, string preorder, string postorder, string binary, string expression)
            {
                string connectionString = "Server=localhost;Database=calculator;User ID=root;Password=;";

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM Expressions WHERE inorder_expression = @inorder";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@inorder", inorder);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("此 in-order 表達式已存在！");
                            return;
                        }
                    }

                    string insertQuery = "INSERT INTO Expressions (inorder_expression, preorder_expression, postorder_expression, binary_expression, decimal_expression) VALUES (@inorder, @preorder, @postorder, @binary, @expression)";
                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@inorder", inorder);
                        insertCommand.Parameters.AddWithValue("@preorder", preorder);
                        insertCommand.Parameters.AddWithValue("@postorder", postorder);
                        insertCommand.Parameters.AddWithValue("@binary", binary);
                        insertCommand.Parameters.AddWithValue("@expression", expression); 
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            public void DeleteLatestExpression()
            {
                string connectionString = "Server=localhost;Database=calculator;User ID=root;Password=;";

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM Expressions ORDER BY id DESC LIMIT 1"; // 假設有一個自增的 id 欄位
                    using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }

            public class Expression
            {
                public int Id { get; set; }
                public string Inorder { get; set; }
                public string Preorder { get; set; }
                public string Postorder { get; set; }
                public string Binary { get; set; }
                public string Decimal { get; set; }

                // 無參數建構函式
                public Expression() { }

                // 如果有其他參數的建構函式，可以保留
                public Expression(int id, string inorder, string preorder, string postorder, string binary, string decimalValue)
                {
                    Id = id;
                    Inorder = inorder;
                    Preorder = preorder;
                    Postorder = postorder;
                    Binary = binary;
                    Decimal = decimalValue;
                }
            }


            public List<Expression> GetAllExpressions()
            {
                List<Expression> expressions = new List<Expression>();
                string connectionString = "Server=localhost;Database=calculator;User ID=root;Password=;";

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string selectQuery = "SELECT * FROM Expressions"; // 根據需要選擇欄位
                    using (var command = new MySqlCommand(selectQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // 假設有一個 Expression 類來存儲資料
                                var expression = new Expression
                                {
                                    Id = reader.GetInt32("id"),
                                    Inorder = reader.GetString("inorder_expression"),
                                    Preorder = reader.GetString("preorder_expression"),
                                    Postorder = reader.GetString("postorder_expression"),
                                    Binary = reader.GetString("binary_expression"),
                                    Decimal = reader.GetDecimal("decimal_expression").ToString()
                                };
                                expressions.Add(expression);
                            }
                        }
                    }
                }
                return expressions;
            }
        }



        // 樹節點類
        public class Node
        {
            public double Value;
            public string Operator;
            public Node Left;
            public Node Right;

            // 只用於數字節點
            public Node(double value)
            {
                Value = value;
                Operator = ""; // 運算符為空
                Left = null;
                Right = null;
            }

            // 用於運算符節點
            public Node(string op, Node left, Node right)
            {
                Operator = op;
                Left = left;
                Right = right;
                Value = 0; // 占位符
            }

            public string Preorder()
            {
                // 前序遍歷：根 -> 左 -> 右
                string result = $"{Operator} {Left?.Preorder() ?? ""} {Right?.Preorder() ?? ""}".Trim();
                if (Left == null && Right == null)
                {
                    result = Value.ToString(); // 如果是數字節點，返回數字
                }
                return result;
            }

            public string Postorder()
            {
                // 後序遍歷：左 -> 右 -> 根
                string result = $"{Left?.Postorder() ?? ""} {Right?.Postorder() ?? ""} {Operator}".Trim();
                if (Left == null && Right == null)
                {
                    result = Value.ToString(); // 如果是數字節點，返回數字
                }
                return result;
            }
        }
    }
}

