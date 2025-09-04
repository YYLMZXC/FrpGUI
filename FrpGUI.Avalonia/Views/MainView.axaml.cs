using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Models;
using FzLib.Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FrpGUI.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView( )
    {
        InitializeComponent();
        App.Services.GetRequiredService<IProgressOverlayService>().Register(ring);
    }
}