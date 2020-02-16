using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Sidekick.Core.Initialization;
using Sidekick.Core.Settings;

namespace Sidekick.Core.Loggers
{
    public class Logger : ILogger, IDisposable
    {
        private readonly IOptionsMonitor<SidekickSettings> configuration;
        private readonly IInitializer initializer;

        public Logger(IOptionsMonitor<SidekickSettings> configuration,
            IInitializer initializer)
        {
            this.configuration = configuration;
            this.initializer = initializer;

            initializer.OnProgress += Initializer_OnProgress;
        }

        private void Initializer_OnProgress(ProgressEventArgs obj)
        {
            Log($"{obj.TotalPercentage}% - {obj.Message} ({obj.ServiceName})");
        }

        public event Action MessageLogged;
        public List<Log> Logs { get; private set; } = new List<Log>();

        public void Log(string text, LogState state = LogState.None)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            lock (Logs)
            {
                var log = new Log()
                {
                    Date = DateTime.Now,
                    Message = text,
                    State = state
                };

                if (Logs.Count >= 100)
                {
                    Logs.RemoveRange(0, Logs.Count - 100);
                }

                Logs.Add(log);
                MessageLogged?.Invoke();
            }
        }

        public void LogException(Exception e)
        {
            var log = new Log()
            {
                Date = DateTime.Now,
                Message = $"EXCEPTION! {e.Message} | {e.StackTrace}",
                State = LogState.Error
            };

            if (Logs.Count >= 100)
            {
                Logs.RemoveAt(0);
            }

            Logs.Add(log);
        }

        public void Clear()
        {
            Logs.Clear();
        }

        public void Dispose()
        {
            initializer.OnProgress -= Initializer_OnProgress;
        }
    }
}
