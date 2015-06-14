// Copyright (c) 2010 Joe Moorhouse

using System.IO;
using System.Text;

namespace PythonConsoleControl
{
    public class PythonOutputStream : Stream
    {
        readonly PythonTextEditor _textEditor;

        public PythonOutputStream(PythonTextEditor textEditor)
        {
            _textEditor = textEditor;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position
        {
            get { return 0; }
            set { }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        /// <summary>
        /// Assumes the bytes are UTF8 and writes them to the text editor.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var text = Encoding.UTF8.GetString(buffer, offset, count);
            _textEditor.Write(text);
        }
    }
}
