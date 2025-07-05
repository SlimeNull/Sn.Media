using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class AudioFileSampleStream : ISampleStream
    {
        private readonly MediaFoundationReader _rawReader;
        private readonly SampleStreamWrapper _wrapper;
        public AudioFileSampleStream(string filePath)
        {
            _rawReader = new MediaFoundationReader(filePath);
            _wrapper = new SampleStreamWrapper(_rawReader);
        }

        public SampleFormat Format => ((ISampleStream)_wrapper).Format;

        public int SampleRate => ((ISampleStream)_wrapper).SampleRate;

        public int Channels => ((ISampleStream)_wrapper).Channels;

        public bool HasPosition => ((ISampleStream)_wrapper).HasPosition;

        public bool CanSeek => ((ISampleStream)_wrapper).CanSeek;

        public long Position => ((ISampleStream)_wrapper).Position;

        public int ReadSamples(byte[] buffer, int offset, int count)
        {
            return ((ISampleStream)_wrapper).ReadSamples(buffer, offset, count);
        }

        public void Seek(long position)
        {
            ((ISampleStream)_wrapper).Seek(position);
        }
    }
}
