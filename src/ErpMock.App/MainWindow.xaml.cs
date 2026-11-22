using System.Windows;
using ErpMock.App.ViewModels;

namespace ErpMock.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
