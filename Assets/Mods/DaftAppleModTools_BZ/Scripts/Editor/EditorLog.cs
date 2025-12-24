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

        public EditorLog(bool logToConsole, bool includeDetailedLogging)
        {
            DetailedLogging = includeDetailedLogging;
            LogToConsole = logToConsole;
            _logEntries = new List<string>();
        }

        public void AddToLog(string logEntry)
        {
            AddToLog(LogLevel.Info, logEntry);
        }

        /// <summary>
        /// Adds to the log without ever mirroring to the console. Useful for things like welcome messages
        /// </summary>
        public void AddToLogNoConsole(LogLevel logLevel, string logEntry)
        {
            _logEntries.Add(logEntry);
            LogChangedEvent?.Invoke(this);
        }

        public void AddToLog(LogLevel logLevel, List<string> logEntries, bool forceLog = false)
        {
            foreach (string entry in logEntries)
            {
                AddToLog(logLevel, entry, forceLog);
            }
        }

        public void AddToLog(LogLevel logLevel, string logEntry, bool forceLog = false)
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