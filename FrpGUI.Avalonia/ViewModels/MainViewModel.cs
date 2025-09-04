﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
//using AvaloniaWebView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Avalonia.Factories;
using FrpGUI.Avalonia.Views;
using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Models;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Dialogs.Pickers;
using FzLib.Avalonia.Services;

namespace FrpGUI.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly UIConfig config;
    private readonly LocalLogger logger;
    private readonly IStorageProviderService storage;

    [ObservableProperty]
    private bool activeProgressRingOverlay = true;

    [ObservableProperty]
    private IFrpProcess currentFrpProcess;

    [ObservableProperty]
    private FrpConfigViewModel currentPanelViewModel;

    [ObservableProperty]
    private ObservableCollection<IFrpProcess> frpProcesses = new ObservableCollection<IFrpProcess>();

    [ObservableProperty]
    private bool isClientPanelVisible;

    [ObservableProperty]
    private bool isServerPanelVisible;

    private DateTime lastUpdateStatusTime = DateTime.MinValue;

    private TaskCompletionSource tcsUpdate;

    public MainViewModel(IDataProvider provider,
        IDialogService dialogService,
        DialogFactory dialogFactory,
        IStorageProviderService storage,
        UIConfig config,
        FrpConfigViewModel frpConfigViewModel,
        LocalLogger logger) : base(provider, dialogService, dialogFactory)
    {
        this.storage = storage;
        this.config = config;
        InitializeDataAndStartTimer();
        CurrentPanelViewModel = frpConfigViewModel;
        this.logger = logger;
    }


    public Task WaitForNextUpdate()
    {
        tcsUpdate = new TaskCompletionSource();
        return tcsUpdate.Task;
    }

    [RelayCommand]
    private async Task AddClientAsync()
    {
        try
        {
            var newConfig = await DataProvider.AddClientAsync();
            var fp = new FrpStatusInfo(newConfig);
            FrpProcesses.Add(fp);
            CurrentFrpProcess = fp;
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("新增客户端失败", ex);
        }
    }

    [RelayCommand]
    private async Task AddRuleAsync()
    {
        await CurrentPanelViewModel.AddRuleAsync();
    }

    [RelayCommand]
    private async Task AddServerAsync()
    {
        try
        {
            var newConfig = await DataProvider.AddServerAsync();
            var fp = new FrpStatusInfo(newConfig);
            FrpProcesses.Add(fp);
            CurrentFrpProcess = fp;
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("新增客户端失败", ex);
        }
    }

    [RelayCommand]
    private void BrowseAdmin()
    {
        var frpConfig = CurrentFrpProcess.Config;
        string user = frpConfig.DashBoardUsername;
        string pswd = frpConfig.DashBoardPassword;
        string ip = config.RunningMode == RunningMode.Singleton
            ? "localhost"
            : new Uri(config.ServerAddress).Host;
        ushort port = frpConfig.DashBoardPort;
        string url = $"http://{ip}:{port}";
        //string url = $"http://{user}:{pswd}@{ip}:{port}";
        if (OperatingSystem.IsBrowser())
        {
            JsInterop.OpenUrl(url);
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void CancelChecking()
    {
        ActiveProgressRingOverlay = false;
    }

    [ObservableProperty]
    private string progressRingMessage="正在初始化";
    
    private async Task CheckNetworkAndToken()
    {
        start:
        if (config.RunningMode == RunningMode.Singleton)
        {
            return;
        }

        try
        {
            ProgressRingMessage="正在验证服务器连接密钥";
            var result = await DataProvider.VerifyTokenAsync();
            string token;
            switch (result)
            {
                case TokenVerification.OK:
                    return;

                case TokenVerification.NotEqual:
                    await DialogService.ShowErrorDialogAsync("密码验证错误", "密码不正确，请重新设置密码");
                    await OpenSettingsDialogAsync();
                    goto start;

                case TokenVerification.NeedSet:
                    await DialogService.ShowErrorDialogAsync("密码为空", "服务端密码为空，请先设置密码");
                    await OpenSettingsDialogAsync();
                    goto start;
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("网络错误，无法连接到FrpGUI服务端", ex);
            await OpenSettingsDialogAsync();
            goto start;
        }
    }

    [RelayCommand]
    private async Task CreateCopyAsync(IFrpProcess fp)
    {
        try
        {
            FrpConfigBase serverConfig;
            //在服务器只是新增获取一个ID号，在本地克隆替换ID号提交到服务器
            if (fp.Config is ClientConfig)
            {
                serverConfig = await DataProvider.AddClientAsync();
            }
            else if (fp.Config is ServerConfig)
            {
                serverConfig = await DataProvider.AddServerAsync();
            }
            else
            {
                throw new Exception("未知的当前选择的配置类型");
            }

            var newConfig = fp.Config.Clone() as FrpConfigBase;
            newConfig.ID = serverConfig.ID;
            await DataProvider.ModifyConfigAsync(newConfig);

            var newFp = new FrpStatusInfo(newConfig);
            FrpProcesses.Add(newFp);
            CurrentFrpProcess = newFp;
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("创建副本失败", ex);
        }
    }

    private void CurrentViewFrp_StatusChanged(object sender, EventArgs e)
    {
        UpdateConfigPanelVisible();
    }

    [RelayCommand]
    private async Task DeleteConfigAsync(IFrpProcess fp)
    {
        var result = await DialogService.ShowYesNoDialogAsync("删除配置", $"是否删除配置“{fp.Config.Name}”？");
        if (true.Equals(result))
        {
            try
            {
                await DataProvider.DeleteFrpConfigAsync(fp.Config.ID);
                FrpProcesses.Remove(fp);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("删除失败", ex);
            }
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            string config;
            FilePickerFileType filter;

            config = CurrentFrpProcess.Config.ToToml();

            var result = await DialogService.ShowYesNoDialogAsync("导出配置", $"是否导出配置文件？", config);

            if (true.Equals(result))
            {
                var options = FilePickerOptionsBuilder.Create()
                    .AddFilter("TOML配置文件", ["*.toml"], ["application/toml"])
                    .SuggestedFileName(CurrentFrpProcess.Config.Name)
                    .BuildSaveOptions();

                var file = await storage.SaveFilePickerAndGetPathAsync(options);
                if (file != null)
                {
                    await File.WriteAllTextAsync(file, config, new UTF8Encoding(false));
                }
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("启动失败", ex);
        }
    }

    private async void InitializeDataAndStartTimer()
    {
        await CheckNetworkAndToken();
        ActiveProgressRingOverlay = false;
        try
        {
            FrpProcesses = new ObservableCollection<IFrpProcess>(await DataProvider.GetFrpStatusesAsync());
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("获取配置列表失败", ex);
        }

        if (DataProvider is WebDataProvider webDataProvider)
        {
            webDataProvider.AddTimerTask("更新状态", () => UpdateStatusAsync(false));
        }
    }

    partial void OnCurrentFrpProcessChanged(IFrpProcess oldValue, IFrpProcess newValue)
    {
        CurrentPanelViewModel.LoadConfig(newValue);
        if (newValue != null)
        {
            newValue.StatusChanged += CurrentViewFrp_StatusChanged;
        }

        UpdateConfigPanelVisible();
    }

    partial void OnCurrentFrpProcessChanging(IFrpProcess oldValue, IFrpProcess newValue)
    {
        if (oldValue != null && FrpProcesses.Contains(oldValue))
        {
            DataProvider.ModifyConfigAsync(oldValue.Config);
            oldValue.StatusChanged -= CurrentViewFrp_StatusChanged;
        }
    }

    private Task OpenSettingsDialogAsync()
    {
        return DialogService.ShowCustomDialogAsync(DialogFactory.CreateSettingsDialog());
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        try
        {
            await DataProvider.ModifyConfigAsync(CurrentFrpProcess.Config);
            await DataProvider.RestartFrpAsync(CurrentFrpProcess.Config.ID);
            await UpdateStatusAsync(true);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("重启失败", ex);
        }
    }

    [RelayCommand]
    private async Task SettingsAsync()
    {
        await OpenSettingsDialogAsync();
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        try
        {
            await DataProvider.ModifyConfigAsync(CurrentFrpProcess.Config);
            await DataProvider.StartFrpAsync(CurrentFrpProcess.Config.ID);
            await UpdateStatusAsync(true);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("启动失败", ex);
        }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        try
        {
            await DataProvider.StopFrpAsync(CurrentFrpProcess.Config.ID);
            await UpdateStatusAsync(true);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync("停止失败", ex);
        }
    }

    private void UpdateConfigPanelVisible()
    {
        IsServerPanelVisible = CurrentFrpProcess?.Config?.Type == 's';
        IsClientPanelVisible = CurrentFrpProcess?.Config?.Type == 'c';
    }

    private async Task UpdateStatusAsync(bool force)
    {
        if (!force && (DateTime.Now - lastUpdateStatusTime).TotalSeconds < 1
            || config.RunningMode == RunningMode.Singleton)
        {
            return;
        }

        try
        {
            lastUpdateStatusTime = DateTime.Now;
            var fps = await DataProvider.GetFrpStatusesAsync();
            var local = FrpProcesses.ToDictionary(p => p.Config.ID);
            foreach (var fp in fps)
            {
                if (local.TryGetValue(fp.Config.ID, out var localFp))
                {
                    if (localFp.ProcessStatus != fp.ProcessStatus)
                    {
                        localFp.ProcessStatus = fp.ProcessStatus;
                        if (localFp == CurrentFrpProcess)
                        {
                            UpdateConfigPanelVisible();
                        }
                    }
                }
            }


            if (tcsUpdate != null)
            {
                tcsUpdate.SetResult();
                tcsUpdate = null;
            }
        }
        catch (Exception ex)
        {
            logger.Error("更新frp进程状态失败", null, ex);
        }
    }
}