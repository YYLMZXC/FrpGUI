using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FzLib.Avalonia.Services;

namespace FrpGUI.Avalonia.ViewModels;

public partial class LogViewModel : ViewModelBase
{
    private readonly IClipboardService clipboard;
    private readonly UIConfig config;
    private readonly LocalLogger logger;

    [ObservableProperty]
    private LogInfo selectedLog;

    public LogViewModel(IDataProvider provider, IClipboardService clipboard, UIConfig config, LocalLogger logger) :
        base(provider, null, null)
    {
        this.clipboard = clipboard;
        this.config = config;
        this.logger = logger;
        StartTimer();
    }

    public ObservableCollection<LogInfo> Logs { get; } = new ObservableCollection<LogInfo>();

    public void AddLog(LogEntity e, bool select)
    {
        try
        {
            //不知道为什么，加了内置浏览器后，Logs会报莫名其妙的错误，有时候Logs最后一个是null，
            //但是CollectionChanged里也不触发。所以加了最后一个null的判断以及try-catch
            while (Logs.Count > 0 && Logs[^1] == null)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }

            IBrush brush = Brushes.Transparent;
            if (e.Type == 'W')
            {
                brush = Brushes.Orange;
            }
            else if (e.Type == 'E')
            {
                brush = Brushes.Red;
            }

            if (Logs.Count >= 2)
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (Logs[^i].Message == e.Message)
                    {
                        Logs[^i].UpdateTimes++;
                        return;
                    }
                }
            }

            var log = new LogInfo(e)
            {
                TypeBrush = brush,
            };

            Logs.Add(log);
            if (select)
            {
                SelectedLog = log;
            }
        }
        catch (Exception ex)
        {
        }
    }

    [RelayCommand]
    private async Task CopyLogAsync(LogInfo log)
    {
        await clipboard.SetTextAsync(log.Message);
    }

    private void StartTimer()
    {
        if (DataProvider is WebDataProvider webDataProvider)
        {
            DateTime lastRequestTime = DateTime.MinValue;
            webDataProvider.AddTimerTask("获取日志", async () =>
            {
                var logs = await DataProvider.GetLogsAsync(lastRequestTime);
                if (logs.Count > 0)
                {
                    lastRequestTime = logs[^1].Time;
                    foreach (var log in logs)
                    {
                        AddLog(log, false);
                    }
                    SelectedLog = Logs[^1];
                }
            });
        }

        logger.NewLog += (s, e) => AddLog(e.Log, true);
        logger.SaveLogs = false;
        foreach (var log in logger.GetSavedLogs())
        {
            AddLog(log, false);
        }
        if (Logs.Count > 0)
        {
            SelectedLog = Logs[^1];
        }
    }
}