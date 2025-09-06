using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FrpGUI.Avalonia.DataProviders;
using System;
using System.Threading.Tasks;
using FrpGUI.Avalonia.Factories;
using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.ViewModels;

public class ViewModelBase(
    IDataProvider provider = null,
    IDialogService dialogService = null,
    DialogFactory dialogFactory = null)
    : ObservableObject
{
    public DialogFactory DialogFactory { get; } = dialogFactory;
    public IDialogService DialogService { get; } = dialogService;
    protected IDataProvider DataProvider { get; } = provider;
    protected TMessage SendMessage<TMessage>(TMessage message) where TMessage : class
    {
        return WeakReferenceMessenger.Default.Send(message);
    }
}