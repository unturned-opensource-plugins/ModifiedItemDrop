using System;
using Logger = Rocket.Core.Logging.Logger;

namespace FFEmqo.ModifiedItemDrop.Utilities
{
    /// <summary>
    /// Centralized logging helper for consistent error handling and logging across the plugin.
    /// Provides methods for different log levels and error scenarios.
    /// </summary>
    public static class LoggingHelper
    {
        private const string PluginPrefix = "[ModifiedItemDrop]";
        private const string DebugPrefix = "[ModifiedItemDrop::Debug]";
        private const string DebugContentsPrefix = "[ModifiedItemDrop::Debug::Contents]";

        /// <summary>
        /// Logs a debug message (only if debug logging is enabled).
        /// </summary>
        public static void LogDebug(string message, bool isDebugEnabled)
        {
            if (!isDebugEnabled)
            {
                return;
            }

            Logger.Log($"{DebugPrefix} {message}");
        }

        /// <summary>
        /// Logs a debug message for clothing contents (only if both debug and contents debug are enabled).
        /// </summary>
        public static void LogDebugContents(string message, bool isDebugEnabled, bool isContentsDebugEnabled)
        {
            if (!isDebugEnabled || !isContentsDebugEnabled)
            {
                return;
            }

            Logger.Log($"{DebugContentsPrefix} {message}");
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void LogInfo(string message)
        {
            Logger.Log($"{PluginPrefix} {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(string message)
        {
            Logger.Log($"{PluginPrefix} [Warning] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string message)
        {
            Logger.Log($"{PluginPrefix} [Error] {message}");
        }

        /// <summary>
        /// Logs an exception with context information.
        /// </summary>
        public static void LogException(Exception ex, string context = null)
        {
            if (ex == null)
            {
                return;
            }

            var message = context != null
                ? $"{PluginPrefix} [Exception] {context}: {ex.Message}"
                : $"{PluginPrefix} [Exception] {ex.Message}";

            Logger.Log(message);
            Logger.LogException(ex);
        }

        /// <summary>
        /// Safely executes an action and logs any exceptions that occur.
        /// </summary>
        public static void SafeExecute(Action action, string operationName)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                LogException(ex, operationName);
            }
        }

        /// <summary>
        /// Safely executes an action and logs any exceptions that occur, with a fallback action.
        /// </summary>
        public static void SafeExecute(Action action, Action fallback, string operationName)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                LogException(ex, operationName);
                fallback?.Invoke();
            }
        }
    }
}
