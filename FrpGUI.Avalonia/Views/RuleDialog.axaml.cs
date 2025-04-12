using FrpGUI.Avalonia.ViewModels;

using FrpGUI.Models;
using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.Views;

public partial class RuleDialog : DialogHost
{
    public RuleDialog()
    {
        InitializeComponent();
    }

    public void SetRule(Rule rule)
    {
        (DataContext as RuleViewModel).Rule = rule.Clone() as Rule;
    }

    protected override void OnCloseButtonClick()
    {
        Close();
    }

    protected override void OnPrimaryButtonClick()
    {
        var vm = DataContext as RuleViewModel;
        if (vm.Check())
        {
            Close(vm.Rule);
        }
    }
}