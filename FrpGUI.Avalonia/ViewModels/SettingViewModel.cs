using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using FzLib.Application.Startup;
using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.ViewModels
{
    public partial class SettingViewModel : ViewModelBase
    {
        private readonly IStartupManager startupManager;

        [ObservableProperty]
        private string newToken;

        [ObservableProperty]
        private string oldToken;

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> processes;

        [ObservableProperty]
        private string serverAddress;

        [ObservableProperty]
        private bool startup;

        [ObservableProperty]
        private string token;

        public SettingViewModel(IDataProvider provider, IDialogService dialogService, IStartupManager startupManager,
            UIConfig config) : base(provider, dialogService)
        {
            this.startupManager = startupManager;
            if (!OperatingSystem.IsBrowser())
            {
                startup = startupManager.IsStartupEnabled();
            }

            Config = config;
            ServerAddress = config.ServerAddress;
            FillProcesses();
            Config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Config.RunningMode) && Config.RunningMode == RunningMode.Service)
                {
                    Startup = false;
                }
            };
        }

        public UIConfig Config { get; }

        private async void FillProcesses()
        {
            try
            {
                Processes = new ObservableCollection<ProcessInfo>(await DataProvider.GetSystemProcesses());
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        private async Task KillProcessAsync(ProcessInfo p)
        {
            Debug.Assert(p != null);
            try
            {
                await DataProvider.KillProcess(p.Id);
                Processes.Remove(p);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("结束进程失败",ex);
            }
        }

        partial void OnStartupChanged(bool value)
        {
            if (!OperatingSystem.IsBrowser())
            {
                if (value)
                {
                    startupManager.EnableStartup("s");
                    Config.ShowTrayIcon = true;
                }
                else
                {
                    startupManager.DisableStartup();
                }
            }
        }

        [RelayCommand]
        private async Task RestartAsync()
        {
            Config.ServerAddress = ServerAddress;
            if (!string.IsNullOrEmpty(Token))
            {
                Config.ServerToken = Token;
            }

            Config.Save();

            if (OperatingSystem.IsBrowser())
            {
                JsInterop.Reload();
            }
            else
            {
                string exePath = Environment.ProcessPath;
                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true
                });
                await (App.Current as App).ShutdownAsync();
            }
        }

        [RelayCommand]
        private async Task SetTokenAsync()
        {
            try
            {
                Config.ServerAddress = ServerAddress;
                await DataProvider.SetTokenAsync(OldToken, NewToken);
                Config.ServerToken = NewToken;
                Config.Save();

                await DialogService.ShowOkDialogAsync("修改密码", "修改密码成功");
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("修改密码失败",ex);
            }
        }
    }
}