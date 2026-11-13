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
using System.Windows.Shapes;

namespace Store
{
    public partial class FilterWindow : Window
    {
        private Main mainWindow;

        public FilterWindow(Main Window)
        {
            InitializeComponent();
            mainWindow = Window;
            Owner = Window;
            DpStart.SelectedDate = mainWindow.StartDateFilter;
            DpEnd.SelectedDate = mainWindow.EndDateFilter;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (DpStart.SelectedDate.HasValue && DpEnd.SelectedDate.HasValue)
            {
                mainWindow.StartDateFilter = DpStart.SelectedDate.Value;
                mainWindow.EndDateFilter = DpEnd.SelectedDate.Value;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter valid range windows.", "Validation error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
