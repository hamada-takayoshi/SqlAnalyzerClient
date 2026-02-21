using System.Windows;
using SqlAnalyzer.App.ViewModels;
using SqlAnalyzer.App.Verification;
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
        Phase4VerificationHarness.VerifyOrThrow();
        Phase5VerificationHarness.VerifyOrThrow();

        MainViewModel viewModel = new(new SqlServerAnalyzer());
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
