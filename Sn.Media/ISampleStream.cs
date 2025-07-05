using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public interface ISampleStream
    {
        public SampleFormat Format { get; }
        public int SampleRate { get; }
        public int Channels { get; }

        public bool HasPosition { get; }
        public bool CanSeek { get; }


        public long Position { get; }

        /// <summary>
        /// 设置或获取当前样本位置（以一组样本为单位）
        /// </summary>
        /// <param name="position">样本位置</param>
        public void Seek(long position);

        /// <summary>
        /// 读取样本数据到指定的缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int ReadSamples(byte[] buffer, int offset, int count);
    }
}
