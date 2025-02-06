using FrpGUI.Models;
using FrpGUI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;

namespace FrpGUI.WebAPI;

public class Logger : LoggerBase
{
    public const int MaxCacheCount = 1000;

    private object lockObj = new object();

    public ConcurrentBag<LogEntity> CacheLogs { get; private set; } = new ConcurrentBag<LogEntity>();

    protected override void AddLog(LogEntity logEntity)
    {
        LogEventLevel level = logEntity.Type switch
        {
            'E' => LogEventLevel.Error,
            'W' => LogEventLevel.Warning,
            _ => LogEventLevel.Information,
        };
        if (logEntity.Exception != null)
        {
            if (logEntity.FromFrp)
            {
                Log.Logger.Write(level, logEntity.Exception, "Frp Log ({id}, {name}):  {msg}", logEntity.InstanceId, logEntity.InstanceName, logEntity.Message);
            }
            else
            {
                Log.Logger.Write(level, logEntity.Exception, "App Log:  {msg}", logEntity.Message);
            }
        }
        else
        {
            if (logEntity.FromFrp)
            {
                Log.Logger.Write(level, "Frp Log ({id}, {name}):  {msg}", logEntity.InstanceId, logEntity.InstanceName, logEntity.Message);
            }
            else
            {
                Log.Logger.Write(level, "App Log:  {msg}", logEntity.Message);
            }
        }
        CacheLogs.Add(logEntity);
        if (CacheLogs.Count > MaxCacheCount)
        {
            lock (lockObj)
            {
                if (CacheLogs.Count > MaxCacheCount)
                {
                    CacheLogs = new ConcurrentBag<LogEntity>(CacheLogs.OrderByDescending(p => p.Time).Take(MaxCacheCount / 2));
                }
            }
        }
    }
}