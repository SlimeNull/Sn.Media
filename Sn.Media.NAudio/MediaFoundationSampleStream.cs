using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class MediaFoundationSampleStream : ISampleStream, IDisposable
    {
        private readonly MediaFoundationReader _rawReader;
        private readonly WaveSampleStream _wrapper;
        public MediaFoundationSampleStream(string filePath)
        {
            _rawReader = new MediaFoundationReader(filePath);
            _wrapper = new WaveSampleStream(_rawReader);
        }

        public SampleFormat Format => ((ISampleStream)_wrapper).Format;

        public int SampleRate => ((ISampleStream)_wrapper).SampleRate;

        public int Channels => ((ISampleStream)_wrapper).Channels;

        public bool CanSeek => ((ISampleStream)_wrapper).CanSeek;

        public long Position => ((ISampleStream)_wrapper).Position;

        public bool HasLength => ((ISampleStream)_wrapper).HasLength;

        public long Length => ((ISampleStream)_wrapper).Length;

        public void Dispose()
        {
            ((IDisposable)_rawReader).Dispose();
        }

        public int Read(Span<byte> buffer)
        {
            return ((ISampleStream)_wrapper).Read(buffer);
        }

        public void Seek(long position)
        {
            ((ISampleStream)_wrapper).Seek(position);
        }
    }
}
