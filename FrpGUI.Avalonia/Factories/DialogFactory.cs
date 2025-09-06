using System;
using FrpGUI.Avalonia.Views;
using FrpGUI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FrpGUI.Avalonia.Factories;

public class DialogFactory
{
    private readonly IServiceProvider services;

    public DialogFactory(IServiceProvider services)
    {
        this.services = services;
    }

    public RuleDialog CreateRuleDialog()
    {
        return services.GetRequiredService<RuleDialog>();
    }

    public RuleDialog CreateRuleDialog(Rule rule)
    {
        var dialog = services.GetRequiredService<RuleDialog>();
        dialog.SetRule(rule);
        return dialog;
    }

    public SettingsDialog CreateSettingsDialog()
    {
        var dialog = services.GetRequiredService<SettingsDialog>();
        return dialog;
    }
}