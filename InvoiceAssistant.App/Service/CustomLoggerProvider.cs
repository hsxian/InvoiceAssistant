using System;
using Microsoft.Extensions.Logging;

public class CustomLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CustomLogger(categoryName);
    }
    public static event Action<string> OnLogMessageSent;
    public void Dispose() { }
    private class CustomLogger(string categoryName) : ILogger
    {

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            // 可以发送到其他地方：文件、数据库、消息队列等
            SendToExternalSystem(logLevel, categoryName, message, exception);
        }
        private void SendToExternalSystem(LogLevel level, string category, string message, Exception ex)
        {
            // 实现发送逻辑
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            if (ex != null)
            {
                logMessage += $" Exception: {ex.Message}";
            }
            OnLogMessageSent?.Invoke(logMessage);
        }
    }
}