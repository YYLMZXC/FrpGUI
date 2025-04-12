using FrpGUI.Avalonia.ViewModels;

using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.Views;

public partial class SettingsDialog : DialogHost
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    protected override void OnCloseButtonClick()
    {
        Close();
    }
}