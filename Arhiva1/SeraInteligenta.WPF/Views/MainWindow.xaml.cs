using System.Windows;
using SeraInteligenta.WPF.ViewModels;

namespace SeraInteligenta.WPF.Views;

public partial class MainWindow : Window
{
    // ViewModel-ul vine din containerul de IoC. Fereastra nu conține logică:
    // tot ce se întâmplă trece prin binding-uri și comenzi (MVVM).
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
