namespace AuthenticationAPI.Utilities.Logger
{
    public class TextFileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public TextFileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TextFileLogger(_filePath);
        }

        public void Dispose()
        {
        }
    }

    public class TextFileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public TextFileLogger(string filePath)
        {
            _filePath = filePath;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_lock)
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {FormatState(state)}{Environment.NewLine}";

                File.AppendAllText(_filePath, logMessage);
            }
        }

        private string FormatState<TState>(TState state)
        {
            if (state is System.Collections.Generic.List<object> list)
            {
                // Format the list content here
                var formattedList = string.Join(", ", list);
                return $"List contents: {formattedList}";
            }
            // Add more type-specific formatting logic as needed
            else
            {
                // Default formatting
                return state.ToString();
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true; // Log all messages
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null; // No scope support in this example
        }
    }
}
