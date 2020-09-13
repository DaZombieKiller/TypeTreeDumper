using System;

namespace Unity
{
    public sealed class UnresolvedSymbolException : Exception
    {
        const string DefaultMessage = "Symbol has not been resolved.";

        public string SymbolName { get; }

        public UnresolvedSymbolException()
            : base(DefaultMessage)
        {
        }

        public UnresolvedSymbolException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public UnresolvedSymbolException(string symbol)
            : base(DefaultMessage)
        {
            SymbolName = symbol;
        }

        public UnresolvedSymbolException(string symbol, string message)
            : base(message)
        {
            SymbolName = symbol;
        }

        public UnresolvedSymbolException(string symbol, string message, Exception inner)
            : base(message, inner)
        {
            SymbolName = symbol;
        }

        public override string Message
        {
            get
            {
                var message = base.Message;

                if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(SymbolName))
                    message += $" ({SymbolName})";

                return message;
            }
        }
    }

}
