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
        public bool HasLength { get; }
        public bool CanSeek { get; }


        /// <summary>
        /// 采样位置. 一组采样视作 1, 例如双通道时, 两个采样为 1
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// 长度. 一组采样视作 1, 例如双通道时, 两个采样为 1
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// 设置或获取当前样本位置（以一组样本为单位）
        /// </summary>
        /// <param name="position">样本位置</param>
        public void Seek(long position);

        /// <summary>
        /// 读取样本数据到指定的缓冲区
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <returns>读取到的采样数据长度. 以字节为单位. 返回 0 则表示没有更多采样可读</returns>
        public int Read(Span<byte> buffer);
    }
}
