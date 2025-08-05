using System;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Somekasu.DollyDoll
{
    internal static class MyLog
    {
        private static readonly MyLogger LOGGER = new();

        internal static LogType FILTER_LOG_TYPE
        {
            get => LOGGER.filterLogType;
            set => LOGGER.filterLogType = value;
        }
        internal static bool IS_DEBUG_ENABLED { get; set; } = true;
        internal static readonly string APP_NAME = "DollyDoll";

        static MyLog()
        {
            // default log level
            LOGGER.filterLogType = LogType.Log;
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        internal static void LogDebug(object message)
        {
            if (IS_DEBUG_ENABLED)
                LOGGER.Log(LogType.Log, (object)$"[{APP_NAME}][DBG] {message}");
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        internal static void LogDebug(object message, Object context)
        {
            if (IS_DEBUG_ENABLED)
                LOGGER.Log(LogType.Log, (object)$"[{APP_NAME}][DBG] {message}", context);
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        internal static void LogWarning(object message)
        {
            LOGGER.Log(LogType.Warning, $"[{APP_NAME}][WRN] {message}");
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        internal static void LogWarning(object message, Object context)
        {
            LOGGER.Log(LogType.Warning, (object)$"[{APP_NAME}][WRN] {message}", context);
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        internal static void Log(object message)
        {
            LOGGER.Log(LogType.Log, $"[{APP_NAME}][INF] {message}");
        }

        /// <summary>
        ///     Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        internal static void Log(object message, Object context)
        {
            LOGGER.Log(LogType.Log, (object)$"[{APP_NAME}][INF] {message}", context);
        }

        /// <summary>
        ///     Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     Format arguments.
        /// </param>
        internal static void LogFormat(string format, params object[] args)
        {
            LOGGER.LogFormat(LogType.Log, format, args);
        }

        /// <summary>
        ///     Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     Format arguments.
        /// </param>
        internal static void LogFormat(Object context, string format, params object[] args)
        {
            LOGGER.LogFormat(LogType.Log, context, format, args);
        }

        /// <summary>
        ///     Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="logType">
        ///     Type of message e.g. warn or error etc.
        /// </param>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     Format arguments.
        /// </param>
        internal static void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            LOGGER.LogFormat(logType, context, format, args);
        }

        /// <summary>
        ///     A variant of Debug.Log that logs an error message to the console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        internal static void LogError(object message)
        {
            LOGGER.Log(LogType.Error, $"[{APP_NAME}][ERR] {message}");
        }

        /// <summary>
        ///     A variant of Debug.Log that logs an error message to the console.
        /// </summary>
        /// <param name="message">
        ///     String or object to be converted to string representation for display.
        /// </param>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        internal static void LogError(object message, Object context)
        {
            LOGGER.Log(LogType.Error, (object)$"[{APP_NAME}][ERR] {message}", context);
        }

        /// <summary>
        ///     Logs a formatted error message to the Unity console.
        /// </summary>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     Format arguments.
        /// </param>
        internal static void LogErrorFormat(string format, params object[] args)
        {
            LOGGER.LogFormat(LogType.Error, format, args);
        }

        /// <summary>
        ///     Logs a formatted error message to the Unity console.
        /// </summary>
        /// <param name="context">
        ///     Object to which the message applies.
        /// </param>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     Format arguments.
        /// </param>
        internal static void LogErrorFormat(Object context, string format, params object[] args)
        {
            LOGGER.LogFormat(LogType.Error, context, format, args);
        }
    }

    internal class MyLogger : ILogger
    {
        private readonly ILogger _instance = Debug.unityLogger;

        public ILogHandler logHandler
        {
            get => _instance.logHandler;
            set => _instance.logHandler = value;
        }
        public bool logEnabled
        {
            get => _instance.logEnabled;
            set => _instance.logEnabled = value;
        }
        public LogType filterLogType
        {
            get => _instance.filterLogType;
            set => _instance.filterLogType = value;
        }

        public bool IsLogTypeAllowed(LogType logType)
        {
            return logType <= filterLogType;
        }

        public void Log(LogType logType, object message)
        {
            if (IsLogTypeAllowed(logType))
                _instance.Log(logType, message);
        }

        public void Log(LogType logType, object message, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(logType))
                _instance.Log(logType, message, context);
        }

        public void Log(LogType logType, string tag, object message)
        {
            if (IsLogTypeAllowed(logType))
                _instance.Log(logType, tag, message);
        }

        public void Log(LogType logType, string tag, object message, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(logType))
                _instance.Log(logType, tag, message, context);
        }

        public void Log(object message)
        {
            if (IsLogTypeAllowed(LogType.Log))
                _instance.Log(message);
        }

        public void Log(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Log))
                _instance.Log(tag, message);
        }

        public void Log(string tag, object message, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(LogType.Log))
                _instance.Log(tag, message, context);
        }

        public void LogError(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Error))
                _instance.LogError(tag, message);
        }

        public void LogError(string tag, object message, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(LogType.Error))
                _instance.LogError(tag, message, context);
        }

        public void LogException(Exception exception)
        {
            if (IsLogTypeAllowed(LogType.Exception))
                _instance.LogException(exception);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(LogType.Exception))
                _instance.LogException(exception, context);
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            if (IsLogTypeAllowed(logType))
                _instance.LogFormat(logType, format, args);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (IsLogTypeAllowed(logType))
                _instance.LogFormat(logType, context, format, args);
        }

        public void LogWarning(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Warning))
                _instance.LogWarning(tag, message);
        }

        public void LogWarning(string tag, object message, UnityEngine.Object context)
        {
            if (IsLogTypeAllowed(LogType.Warning))
                _instance.LogWarning(tag, message, context);
        }
    }
}
