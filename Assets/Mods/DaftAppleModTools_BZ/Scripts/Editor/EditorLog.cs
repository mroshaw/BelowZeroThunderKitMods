using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DaftAppleGames.Editor
{
    public enum LogLevel
    {
        Info,
        Debug,
        Warning,
        Error
    }

    /// <summary>
    /// Editor Log class that can easily be incorporated into UI logging elements in Editor Windows
    /// </summary>
    public class EditorLog
    {
        private readonly List<string> _logEntries;

        public bool DetailedLogging { set; private get; }
        public bool LogToConsole { set; private get; }

        public readonly UnityEvent<EditorLog> LogChangedEvent = new EditorLogEvent();

        public class EditorLogEvent : UnityEvent<EditorLog>
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EditorLog(bool logToConsole, bool includeDetailedLogging)
        {
            DetailedLogging = includeDetailedLogging;
            LogToConsole = logToConsole;
            _logEntries = new List<string>();
        }

        /// <summary>
        /// Add an Info log entry
        /// </summary>
        public void LogInfo(string logEntry)
        {
            AddToLog(LogLevel.Info, logEntry);
        }

        /// <summary>
        /// Add a Debug log entry
        /// </summary>
        public void LogDebug(string logEntry)
        {
            AddToLog(LogLevel.Debug, logEntry);
        }

        /// <summary>
        /// Add an Error log entry
        /// </summary>
        public void LogError(string logEntry)
        {
            AddToLog(LogLevel.Error, logEntry, true);
        }

        /// <summary>
        /// Add a Warning log entry
        /// </summary>
        public void LogWarning(string logEntry)
        {
            AddToLog(LogLevel.Warning, logEntry);
        }

        /// <summary>
        /// Adds to the log without ever mirroring to the console. Useful for things like welcome messages
        /// </summary>
        private void AddToLogNoConsole(LogLevel logLevel, string logEntry)
        {
            _logEntries.Add(logEntry);
            LogChangedEvent?.Invoke(this);
        }

        /// <summary>
        /// Add a new log entry
        /// </summary>
        private void AddToLog(LogLevel logLevel, string logEntry, bool forceLog = false)
        {
            string fullLogText;

            switch (logLevel)
            {
                case LogLevel.Info:
                    fullLogText = $"Info: {logEntry}";
                    if (LogToConsole)
                    {
                        Debug.Log(logEntry);
                    }
                    break;
                case LogLevel.Debug:
                    if (!DetailedLogging && !forceLog)
                    {
                        return;
                    }

                    fullLogText = $"Debug: {logEntry}";
                    if (LogToConsole)
                    {
                        Debug.Log(logEntry);
                    }

                    break;
                case LogLevel.Warning:
                    fullLogText = $"Warning: {logEntry}";
                    if (LogToConsole)
                    {
                        Debug.LogWarning(logEntry);
                    }

                    break;
                case LogLevel.Error:
                    fullLogText = $"Error: {logEntry}";
                    if (LogToConsole)
                    {
                        Debug.LogError(logEntry);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }

            _logEntries.Add(fullLogText);

            LogChangedEvent?.Invoke(this);
        }

        public void Clear()
        {
            _logEntries.Clear();
            LogChangedEvent?.Invoke(this);
        }

        public string GetLogAsString()
        {
            string logString = "";
            foreach (string logEntry in _logEntries)
            {
                logString += logEntry + "\n";
            }

            return logString.TrimEnd('\n');
        }
    }
}