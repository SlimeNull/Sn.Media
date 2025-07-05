namespace Sn.Media
{
    public interface IFrameStream
    {
        public FrameFormat Format { get; }
        public Fraction FrameRate { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int FrameStride { get; }
        public int FrameDataSize { get; }

        public bool HasPosition { get; }
        public bool CanSeek { get; }

        public long Position { get; }

        /// <summary>
        /// 设置或获取当前帧位置（以帧为单位）
        /// </summary>
        /// <param name="position"></param>
        public void Seek(long position);

        /// <summary>
        /// 读取一帧数据到指定的缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int ReadFrame(byte[] buffer, int offset, int count);
    }
}
