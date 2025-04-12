﻿using Avalonia.Platform.Storage;
using Avalonia.Threading;
//using AvaloniaWebView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Avalonia.Views;

using FrpGUI.Enums;
using FrpGUI.Models;
using FzLib.Avalonia.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static FzLib.Avalonia.Messages.CommonDialogMessage;
using static FzLib.Program.Runtime.SimplePipe;
using FrpGUI.Configs;
using Serilog;
using System.Diagnostics;

namespace FrpGUI.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly UIConfig config;
    private readonly LocalLogger logger;
    private readonly IServiceProvider services;
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
        UIConfig config,
        IServiceProvider services,
        FrpConfigViewModel frpConfigViewModel,
        LocalLogger logger) : base(provider)
    {
        this.config = config;
        this.services = services;
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
            await ShowErrorAsync(ex, "新增客户端失败");
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
            await ShowErrorAsync(ex, "新增客户端失败");
        }
    }

    [RelayCommand]
    private void BrowseAdmin()
    {
        var frpConfig = CurrentFrpProcess.Config;
        string user = frpConfig.DashBoardUsername;
        string pswd = frpConfig.DashBoardPassword;
        string ip = config.RunningMode == RunningMode.Singleton ?
            "localhost"
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

    private async Task CheckNetworkAndToken()
    {
    start:
        if (config.RunningMode == RunningMode.Singleton)
        {
            return;
        }

        try
        {
            var result = await DataProvider.VerifyTokenAsync();
            string token;
            switch (result)
            {
                case TokenVerification.OK:
                    return;

                case TokenVerification.NotEqual:
                    await ShowErrorAsync("密码不正确，请重新设置密码", "验证密码错误");
                    await SendMessage(new DialogHostMessage(services.GetRequiredService<SettingsDialog>())).Task;
                    goto start;

                case TokenVerification.NeedSet:
                    await ShowErrorAsync("服务端密码为空，请先设置密码", "密码为空");
                    await SendMessage(new DialogHostMessage(services.GetRequiredService<SettingsDialog>())).Task;
                    goto start;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "网络错误，无法连接到FrpGUI服务端");
            await SendMessage(new DialogHostMessage(services.GetRequiredService<SettingsDialog>())).Task;
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
            await ShowErrorAsync(ex, "创建副本失败");
        }
    }

    private void CurrentViewFrp_StatusChanged(object sender, EventArgs e)
    {
        UpdateConfigPanelVisible();
    }

    [RelayCommand]
    private async Task DeleteConfigAsync(IFrpProcess fp)
    {
        var message = SendMessage(new CommonDialogMessage()
        {
            Type = CommonDialogType.YesNo,
            Title = "删除配置",
            Message = $"是否删除配置“{fp.Config.Name}”？"
        });
        if (true.Equals(await message.Task))
        {
            try
            {
                await DataProvider.DeleteFrpConfigAsync(fp.Config.ID);
                FrpProcesses.Remove(fp);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex, "删除失败");
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
            filter = new FilePickerFileType("ini配置文件")
            {
                Patterns = ["*.toml"],
                MimeTypes = ["application/toml"]
            };
            var message = SendMessage(new CommonDialogMessage()
            {
                Type = CommonDialogType.YesNo,
                Title = "导出配置",
                Message = "是否导出配置文件？",
                Detail = config
            });

            if (true.Equals(await message.Task))
            {
                var file = await SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        FileTypeChoices = [filter],
                        SuggestedFileName = CurrentFrpProcess.Config.Name,
                        DefaultExtension = filter.Patterns[0].Split('.')[1]
                    });
                if (file != null)
                {
                    string path = file.TryGetLocalPath();
                    if (path != null)
                    {
                        File.WriteAllText(path, config, new UTF8Encoding(false));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "启动失败");
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
            SendMessage(new CommonDialogMessage()
            {
                Type = CommonDialogType.Error,
                Title = "获取配置列表失败",
                Exception = ex
            });
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
            await ShowErrorAsync(ex, "重启失败");
        }
    }

    [RelayCommand]
    private async Task SettingsAsync()
    {
        await SendMessage(new DialogHostMessage(services.GetRequiredService<SettingsDialog>())).Task;
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
            await ShowErrorAsync(ex, "启动失败");
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
            await ShowErrorAsync(ex, "停止失败");
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