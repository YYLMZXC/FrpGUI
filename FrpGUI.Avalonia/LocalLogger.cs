using FrpGUI.Models;
using FrpGUI.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FrpGUI.Avalonia
{
    public class LocalLogger : LoggerBase
    {
        public ConcurrentBag<LogEntity> savedLogs = new ConcurrentBag<LogEntity>();

        public event EventHandler<NewLogEventArgs> NewLog;

        public bool SaveLogs { get; set; } = true;

        public LogEntity[] GetSavedLogs() => [.. savedLogs];

        protected override void AddLog(LogEntity logEntity)
        {
            NewLog?.Invoke(this, new NewLogEventArgs(logEntity));
            if (SaveLogs)
            {
                savedLogs.Add(logEntity);
            }
        }

        public class NewLogEventArgs : EventArgs
        {
            public NewLogEventArgs(LogEntity log)
            {
                ArgumentNullException.ThrowIfNull(log);
                Log = log;
            }

            public LogEntity Log { get; }
        }
    }
}