using System;

namespace TypeTreeDumper
{
    public static class Logger
    {
        public static event Action<string> InfoLog;
        public static event Action<string> VerbLog;
        public static event Action<string> ErrorLog;

        public static void Info(object obj) => Info(obj.ToString());
        public static void Info(string message, params object[] parameters) => Info(string.Format(message, parameters));
        public static void Info(string message)
        {
            InfoLog?.Invoke(message);
        }

        public static void Verb(object obj) => Verb(obj.ToString());
        public static void Verb(string message, params object[] parameters) => Verb(string.Format(message, parameters));
        public static void Verb(string message)
        {
            VerbLog?.Invoke(message);
        }

        public static void Error(object obj) => Error(obj.ToString());
        public static void Error(string message, params object[] parameters) => Error(string.Format(message, parameters));
        public static void Error(string message)
        {
            ErrorLog?.Invoke(message);
        }
    }
}
