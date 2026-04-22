using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRC_OSC_Handy.NAudio
{
    internal class IgnoreDisposeStream : Stream
    {

        private readonly Stream _innerStream;

        public IgnoreDisposeStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        protected override void Dispose(bool disposing)
        {
            // Do NOT dispose the inner stream.
        }

        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

    }
}
