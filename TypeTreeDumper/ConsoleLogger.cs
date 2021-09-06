using System;

namespace TypeTreeDumper
{
    public static class ConsoleLogger
    {
        /// <summary>
        /// Ignore everything but errors
        /// </summary>
        private static bool Silent { get; set; }
        private static bool Verbose { get; set; }

        internal static void Initialize(bool silent, bool verbose)
        {
            Logger.InfoLog += Info;
            Logger.VerbLog += Verb;
            Logger.ErrorLog += Error;
            Silent = silent;
            Verbose = verbose;
            Logger.Info("Console Logging Initialized");
        }

        private static void Info(string message)
        {
            if (!Silent) Console.WriteLine(message);
        }
        private static void Verb(string message)
        {
            if (!Silent && Verbose) Console.WriteLine(message);
        }
        private static void Error(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}
