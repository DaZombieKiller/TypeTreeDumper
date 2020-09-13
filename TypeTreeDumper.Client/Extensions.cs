namespace TypeTreeDumper
{
    public static class Extensions
    {
        public static void WriteLine(this IpcInterface server, string value, params object[] args)
        {
            server.WriteLine(string.Format(value, args));
        }
    }
}
