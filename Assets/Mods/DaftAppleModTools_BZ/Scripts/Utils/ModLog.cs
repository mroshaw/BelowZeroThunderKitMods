using BepInEx.Logging;
using UnityEngine;

namespace DaftAppleGames.ModTools
{
    public enum LogLevel { Info, Debug, Warning, Error }

    /// <summary>
    /// Class that sits on BepInEx logging to offer finer control of mod logging to the Player.log
    /// </summary>
    public class ModLog
    {
        private bool _detailedLogging;
        private readonly ManualLogSource _bepLog;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ModLog(ManualLogSource bepInExLog, bool detailedLogging)
        {
            _bepLog = bepInExLog;
            _detailedLogging = detailedLogging;
        }
        
        /// <summary>
        /// Toggle detailed logging at run-time. Be sure to subscribe to this in the ModConfig
        /// </summary>
        public void SetDetailedLoggingState(bool newState)
        {
            _detailedLogging = newState;
        }

        /// <summary>
        /// Write to the log
        /// </summary>
        public void Log(LogLevel logLevel, string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
            return;
#endif
            
            // Handle writing the appropriate log
            switch (logLevel)
            {
                case LogLevel.Info:
                    // Always write info logs
                    _bepLog.LogInfo(message);
                    break;
                
                case LogLevel.Debug:
                    // Only write debug logs if detailed logging enabled
                    if (_detailedLogging)
                    {
                        _bepLog.LogDebug(message);
                    }
                    break;
                case LogLevel.Warning:
                    // Always write warning logs
                    _bepLog.LogWarning(message);
                    break;
                
                case LogLevel.Error:
                    // Always write error logs
                    _bepLog.LogError(message);
                    break;
            }
        }

        /// <summary>
        /// Helper method to write an info log entry
        /// </summary>
        public void LogInfo(string logEntry)
        {
            Log(LogLevel.Info, logEntry);
        }
        
        /// <summary>
        /// Helper method to write a debug log entry
        /// </summary>
        public void LogDebug(string logEntry)
        {
            Log(LogLevel.Debug, logEntry);
        }

        /// <summary>
        /// Helper method to write an error log entry
        /// </summary>
        public void LogWarning(string logEntry)
        {
            Log(LogLevel.Warning, logEntry);
        }
        
        /// <summary>
        /// Helper method to write an error log entry
        /// </summary>
        public void LogError(string logEntry)
        {
            Log(LogLevel.Error, logEntry);
        }
    }
}