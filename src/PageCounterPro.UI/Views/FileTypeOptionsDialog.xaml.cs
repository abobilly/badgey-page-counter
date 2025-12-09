using System.Windows;
using PageCounterPro.UI.ViewModels;

namespace PageCounterPro.UI.Views;

/// <summary>
/// Interaction logic for FileTypeOptionsDialog.xaml
/// </summary>
public partial class FileTypeOptionsDialog : Window
{
    public FileTypeOptionsDialog()
    {
        InitializeComponent();

        if (DataContext is FileTypeOptionsViewModel vm)
        {
            vm.CloseAction = () => Close();
        }
    }

    public FileTypeOptionsDialog(FileTypeOptionsViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = () => Close();
    }

    public new bool? DialogResult => (DataContext as FileTypeOptionsViewModel)?.DialogResult;
}
