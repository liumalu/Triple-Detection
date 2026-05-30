using System;
using System.IO;
using System.Windows;
using TripleDetection.App;
using TripleDetection.Views;
using TripleDetection;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize database before login window
        InitializeDatabase();

        // Show login window first
        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            // User closed login window without authenticating → exit app
            Shutdown();
            return;
        }

        // Authentication succeeded → run Bootstrapper to show MainWindow
        var bootstrapper = new Bootstrapper();
        bootstrapper.Run();
    }

    private void InitializeDatabase()
    {
        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tripledetection.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Initialize DB schema and seed data
            DatabaseConfig.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}