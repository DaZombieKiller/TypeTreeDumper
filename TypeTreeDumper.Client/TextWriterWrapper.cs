using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TypeTreeDumper
{
    sealed class TextWriterWrapper : TextWriter
    {
        public TextWriter BaseWriter { get; }

        public override Encoding Encoding => BaseWriter.Encoding;

        public override IFormatProvider FormatProvider => BaseWriter.FormatProvider;

        public override string NewLine
        {
            get => BaseWriter.NewLine;
            set => BaseWriter.NewLine = value;
        }

        public TextWriterWrapper(TextWriter baseWriter)
        {
            BaseWriter = baseWriter;
        }

        public override void Write(char value) => BaseWriter.Write(value);

        public override void Write(char[] buffer) => BaseWriter.Write(buffer);

        public override void Write(char[] buffer, int index, int count) => BaseWriter.Write(buffer, index, count);

        public override void Write(bool value) => BaseWriter.Write(value);

        public override void Write(int value) => BaseWriter.Write(value);

        public override void Write(uint value) => BaseWriter.Write(value);

        public override void Write(long value) => BaseWriter.Write(value);

        public override void Write(ulong value) => BaseWriter.Write(value);

        public override void Write(float value) => BaseWriter.Write(value);

        public override void Write(double value) => BaseWriter.Write(value);

        public override void Write(decimal value) => BaseWriter.Write(value);

        public override void Write(string value) => BaseWriter.Write(value);

        public override void Write(object value) => BaseWriter.Write(string.Format(FormatProvider, "{0}", value));

        public override void Write(string format, object arg0) => BaseWriter.Write(string.Format(FormatProvider, format, arg0));

        public override void Write(string format, object arg0, object arg1) => BaseWriter.Write(string.Format(FormatProvider, format, arg0, arg1));

        public override void Write(string format, object arg0, object arg1, object arg2) => BaseWriter.Write(string.Format(FormatProvider, format, arg0, arg1, arg2));

        public override void Write(string format, params object[] arg) => BaseWriter.Write(string.Format(FormatProvider, format, arg));

        public override void WriteLine() => BaseWriter.WriteLine();

        public override void WriteLine(char value) => BaseWriter.WriteLine(value);

        public override void WriteLine(char[] buffer) => BaseWriter.WriteLine(buffer);

        public override void WriteLine(char[] buffer, int index, int count) => BaseWriter.WriteLine(buffer, index, count);

        public override void WriteLine(bool value) => BaseWriter.WriteLine(value);

        public override void WriteLine(int value) => BaseWriter.WriteLine(value);

        public override void WriteLine(uint value) => BaseWriter.WriteLine(value);

        public override void WriteLine(long value) => BaseWriter.WriteLine(value);

        public override void WriteLine(ulong value) => BaseWriter.WriteLine(value);

        public override void WriteLine(float value) => BaseWriter.WriteLine(value);

        public override void WriteLine(double value) => BaseWriter.WriteLine(value);

        public override void WriteLine(decimal value) => BaseWriter.WriteLine(value);

        public override void WriteLine(string value) => BaseWriter.WriteLine(value);

        public override void WriteLine(object value) => BaseWriter.WriteLine(string.Format(FormatProvider, "{0}", value));

        public override void WriteLine(string format, object arg0) => BaseWriter.WriteLine(string.Format(FormatProvider, format, arg0));

        public override void WriteLine(string format, object arg0, object arg1) => BaseWriter.WriteLine(string.Format(FormatProvider, format, arg0, arg1));

        public override void WriteLine(string format, object arg0, object arg1, object arg2) => BaseWriter.WriteLine(string.Format(FormatProvider, format, arg0, arg1, arg2));

        public override void WriteLine(string format, params object[] arg) => BaseWriter.WriteLine(string.Format(FormatProvider, format, arg));

        public override void Flush() => BaseWriter.Flush();

        public override Task WriteAsync(char value) => BaseWriter.WriteAsync(value);

        public override Task WriteAsync(string value) => BaseWriter.WriteAsync(value);

        public override Task WriteAsync(char[] buffer, int index, int count) => BaseWriter.WriteAsync(buffer, index, count);

        public override Task WriteLineAsync(char value) => BaseWriter.WriteLineAsync(value);

        public override Task WriteLineAsync(string value) => BaseWriter.WriteLineAsync(value);

        public override Task WriteLineAsync(char[] buffer, int index, int count) => BaseWriter.WriteLineAsync(buffer, index, count);

        public override Task WriteLineAsync() => BaseWriter.WriteLineAsync();

        public override Task FlushAsync() => BaseWriter.FlushAsync();

        public override void Close() => BaseWriter.Close();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                BaseWriter.Dispose();
        }
    }
}
