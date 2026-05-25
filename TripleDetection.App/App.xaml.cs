using System.Windows;
using TripleDetection.Services;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DataSeeder.SeedIfNeeded();
    }
}