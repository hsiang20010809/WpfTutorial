using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static WpfTutorial.MainWindow;

namespace WpfTutorial
{
    public partial class QueryWindow : Window
    {
        private DatabaseHelper dbHelper;

        public QueryWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            LoadExpressions();
        }

        private void LoadExpressions()
        {
            UpdateDisplayData();
        }

        private void UpdateDisplayData()
        {
            var expressions = dbHelper.GetAllExpressions(); // 假設這個方法返回所有資料

            // 清空現有項目
            ExpressionsListBox.Items.Clear();

            // 更新 ItemsSource
            foreach (var expression in expressions)
            {
                ExpressionsListBox.Items.Add(expression.Inorder); // 假設你只想顯示 in-order 表達式
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            dbHelper.DeleteLatestExpression();
            UpdateDisplayData();
            MessageBox.Show("最新的一筆資料已刪除！");
        }
    }
}


