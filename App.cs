using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace Store
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
                optionsBuilder.UseSqlite("Data Source = UserData.db");

                using (var db = new DataContext(optionsBuilder.Options))
                {
                    db.Database.EnsureCreated();
                }
                var app = new App();

                var mainWindow = new Main();
                app.Run(mainWindow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Can you guess what broke again?", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}