using System.Windows;
using OpenIPRadar.Presentation.ViewModels;

namespace OpenIPRadar;

/// <summary>
/// Interaction logic for MainWindow.xaml. Contains no application logic — it only receives the
/// main view model (via the composition root) and assigns it as the data context.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window with its view model.</summary>
    /// <param name="viewModel">The main view model to bind.</param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
