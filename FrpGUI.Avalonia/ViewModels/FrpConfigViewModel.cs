using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Avalonia.Views;
using FrpGUI.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FrpGUI.Avalonia.Factories;
using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.ViewModels;

public partial class FrpConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    private IFrpProcess frp;

    [ObservableProperty]
    private ObservableCollection<Rule> rules;

    public static readonly string AddButtonDataName = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public FrpConfigViewModel(IDataProvider provider,
        DialogFactory dialogFactory,
        IDialogService dialogService) : base(provider, dialogService, dialogFactory)
    {
    }

    [RelayCommand]
    public async Task AddRuleAsync()
    {
        var result = await DialogService.ShowCustomDialogAsync<Rule>(DialogFactory.CreateRuleDialog());
        if (result is Rule newRule)
        {
            Rules.Insert(Rules.Count - 1, newRule);
        }
    }

    public void LoadConfig(IFrpProcess frp)
    {
        Frp = frp;
        if (frp?.Config is ClientConfig cc)
        {
            Rules = new ObservableCollection<Rule>(cc.Rules);
            Rules.Add(new Rule() { Name = AddButtonDataName });
            Rules.CollectionChanged += (s, e) => cc.Rules = Rules.Take(Rules.Count - 1).ToList();
        }
    }

    [RelayCommand]
    private void DisableRule(Rule rule)
    {
        rule.Enable = false;
    }

    [RelayCommand]
    private void EnableRule(Rule rule)
    {
        rule.Enable = true;
    }

    [RelayCommand]
    private async Task ModifyRuleAsync(Rule rule)
    {
        var result = await DialogService.ShowCustomDialogAsync<Rule>(DialogFactory.CreateRuleDialog(rule));
        if (result is Rule newRule)
        {
            Rules[Rules.IndexOf(rule)] = newRule;
        }
    }

    [RelayCommand]
    private void RemoveRule(Rule rule)
    {
        Rules.Remove(rule);
    }
}