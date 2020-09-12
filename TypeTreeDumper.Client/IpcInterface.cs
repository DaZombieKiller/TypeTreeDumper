using System;

namespace TypeTreeDumper
{
    public class IpcInterface : MarshalByRefObject
    {
        public void Ping()
        {
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        public void WriteLine(string value, params object[] args)
        {
            Console.WriteLine(string.Format(value, args));
        }
    }
}
