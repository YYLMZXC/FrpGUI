using FrpGUI.Avalonia.ViewModels;

using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.Views;

public partial class SettingsDialog : DialogHost
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    protected override async void OnCloseButtonClick()
    {
        CloseButtonEnable = false;
        if (await ((SettingViewModel)DataContext).TryCloseAsync())
        {
          Close();  
        }
        else
        {
            CloseButtonEnable = true;
        }
    }
}