using System;

namespace TypeTreeDumper
{
    public sealed class MissingModuleException : Exception
    {
        const string DefaultMessage = "The specified module could not be found.";

        public string ModuleName { get; }

        public MissingModuleException()
            : base(DefaultMessage)
        {
        }

        public MissingModuleException(Exception inner)
            : base(DefaultMessage, inner)
        {
        }

        public MissingModuleException(string moduleName)
            : base(DefaultMessage)
        {
            ModuleName = moduleName;
        }

        public MissingModuleException(string moduleName, Exception inner)
            : base(DefaultMessage, inner)
        {
            ModuleName = moduleName;
        }

        public MissingModuleException(string moduleName, string message)
            : base(message)
        {
            ModuleName = moduleName;
        }

        public MissingModuleException(string moduleName, string message, Exception inner)
            : base(message, inner)
        {
            ModuleName = moduleName;
        }

        public override string Message
        {
            get
            {
                var message = base.Message;

                if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(ModuleName))
                    message += $" ({ModuleName})";

                return message;
            }
        }
    }
}
