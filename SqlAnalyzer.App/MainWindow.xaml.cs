using System.Windows;
using SqlAnalyzer.App.ViewModels;
using SqlAnalyzer.App.Views;
using SqlAnalyzer.SqlServer.Analysis;
using SqlAnalyzer.SqlServer.Boundary;

namespace SqlAnalyzer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BoundaryExtractionHarness.VerifyOrThrow();

        MainViewModel viewModel = new(new DummyAnalyzer());
        viewModel.OpenSettingsAction = OpenSettingsWindow;
        DataContext = viewModel;
    }

    private void OpenSettingsWindow()
    {
        SettingsWindow settingsWindow = new()
        {
            Owner = this
        };

        settingsWindow.ShowDialog();
    }
}
