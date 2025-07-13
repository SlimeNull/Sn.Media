using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sn.Media.Windows
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class WaveOutSamplePlayer : ISamplePlayer, IDisposable
    {
        private const int WAVE_MAPPER = -1;
        private const int MM_WOM_DONE = 0x3BD;
        private const int CALLBACK_FUNCTION = 0x30000;

        private ISampleStream? _source;
        private IntPtr _hWaveOut = IntPtr.Zero;
        private bool _isPlaying = false;
        private bool _disposed = false;

        private WaveOutBuffer[]? _buffers;
        private int _bufferSize = 8192; // 缓冲区大小
        private int _bufferCount = 4;   // 缓冲区数量
        private Thread? _playbackThread;
        private volatile bool _pausePlayback = false;  // 暂停标志
        private volatile bool _stopPlayback = false;   // 停止标志
        private readonly object _lockObject = new object();
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); // 用于暂停控制

        // WaveOut 回调委托
        private delegate void WaveOutProc(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
        private WaveOutProc _waveOutProc;

        public ISampleStream? Source
        {
            get => _source;
            set
            {
                lock (_lockObject)
                {
                    if (_isPlaying)
                        IsPlaying = false;

                    _source = value;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_source == null) return TimeSpan.Zero;
                return TimeSpan.FromSeconds((double)_source.Position / _source.SampleRate);
            }
            set
            {
                if (_source?.CanSeek == true)
                {
                    lock (_lockObject)
                    {
                        long samplePosition = (long)(value.TotalSeconds * _source.SampleRate);
                        _source.Seek(samplePosition);
                    }
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (_source?.HasLength != true) return TimeSpan.Zero;
                return TimeSpan.FromSeconds((double)_source.Length / _source.SampleRate);
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                lock (_lockObject)
                {
                    if (_isPlaying == value) return;

                    if (value)
                        ResumePlayback();
                    else
                        PausePlayback();

                    _isPlaying = value;
                }
            }
        }

        public WaveOutSamplePlayer()
        {
            _waveOutProc = new WaveOutProc(WaveOutCallback);
        }

        private void ResumePlayback()
        {
            if (_source == null) return;

            // 如果已经初始化过，只需要恢复播放
            if (_hWaveOut != IntPtr.Zero && _playbackThread?.IsAlive == true)
            {
                _pausePlayback = false;
                _pauseEvent.Set(); // 唤醒播放线程
                waveOutRestart(_hWaveOut); // 恢复 WaveOut 设备
                return;
            }

            // 首次播放，需要完整初始化
            try
            {
                // 打开 WaveOut 设备
                var waveFormat = CreateWaveFormat(_source);
                int result = waveOutOpen(out _hWaveOut, WAVE_MAPPER, ref waveFormat, _waveOutProc, IntPtr.Zero, CALLBACK_FUNCTION);

                if (result != 0)
                    throw new InvalidOperationException($"Failed to open WaveOut device. Error: {result}");

                // 创建缓冲区
                _buffers = new WaveOutBuffer[_bufferCount];
                for (int i = 0; i < _bufferCount; i++)
                {
                    _buffers[i] = new WaveOutBuffer(_hWaveOut, _bufferSize);
                }

                // 启动播放线程
                _stopPlayback = false;
                _pausePlayback = false;
                _pauseEvent.Set();

                _playbackThread = new Thread(PlaybackThreadProc)
                {
                    IsBackground = true,
                    Name = "WaveOut Playback Thread"
                };
                _playbackThread.Start();
            }
            catch
            {
                CleanupWaveOut();
                throw;
            }
        }

        private void PausePlayback()
        {
            if (_hWaveOut != IntPtr.Zero)
            {
                _pausePlayback = true;
                _pauseEvent.Reset(); // 暂停播放线程
                waveOutPause(_hWaveOut); // 暂停 WaveOut 设备，但不清空缓冲区
            }
        }

        /// <summary>
        /// 完全停止播放（与暂停不同，这会清理所有资源）
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                _isPlaying = false;
                StopPlayback();
            }
        }

        private void StopPlayback()
        {
            _stopPlayback = true;
            _pauseEvent.Set(); // 确保线程能够退出

            if (_hWaveOut != IntPtr.Zero)
            {
                waveOutReset(_hWaveOut); // 清空所有缓冲区
            }

            _playbackThread?.Join(1000);
            CleanupWaveOut();
        }

        private void PlaybackThreadProc()
        {
            try
            {
                // 预填充缓冲区
                for (int i = 0; i < _bufferCount && !_stopPlayback; i++)
                {
                    _pauseEvent.Wait(); // 等待非暂停状态
                    if (_stopPlayback) break;

                    if (!FillBuffer(_buffers[i]))
                        break;
                }

                // 主播放循环
                while (!_stopPlayback)
                {
                    // 等待非暂停状态
                    _pauseEvent.Wait();
                    if (_stopPlayback) break;

                    bool hasActiveBuffers = false;
                    bool needMoreData = false;

                    // 检查缓冲区状态
                    for (int i = 0; i < _bufferCount && !_stopPlayback; i++)
                    {
                        if (_buffers[i].IsQueued)
                        {
                            hasActiveBuffers = true;
                        }
                        else if (!_pausePlayback)
                        {
                            // 重新填充完成的缓冲区
                            if (FillBuffer(_buffers[i]))
                            {
                                needMoreData = true;
                            }
                            else
                            {
                                // 没有更多数据，播放完成
                                if (!hasActiveBuffers)
                                {
                                    // 所有缓冲区都播放完毕
                                    lock (_lockObject)
                                    {
                                        _isPlaying = false;
                                    }
                                    return;
                                }
                            }
                        }
                    }

                    // 如果暂停中或没有活动缓冲区，稍作等待
                    if (_pausePlayback || (!hasActiveBuffers && !needMoreData))
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Thread.Sleep(1); // 减少 CPU 占用
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常（这里简单忽略，实际应用中应该记录日志）
                System.Diagnostics.Debug.WriteLine($"Playback thread error: {ex.Message}");
            }
            finally
            {
                lock (_lockObject)
                {
                    _isPlaying = false;
                }
            }
        }

        private bool FillBuffer(WaveOutBuffer buffer)
        {
            if (_source == null || _pausePlayback) return false;

            var data = new byte[_bufferSize];
            int bytesRead = _source.Read(data.AsSpan());

            if (bytesRead == 0)
                return false;

            buffer.WriteData(data, bytesRead);
            return true;
        }

        private WAVEFORMATEX CreateWaveFormat(ISampleStream source)
        {
            var format = new WAVEFORMATEX();
            format.wFormatTag = 1; // PCM
            format.nChannels = (ushort)source.Channels;
            format.nSamplesPerSec = (uint)source.SampleRate;

            format.wBitsPerSample = source.Format switch
            {
                SampleFormat.UInt8 => 8,
                SampleFormat.Int16 => 16,
                SampleFormat.Int32 => 32,
                SampleFormat.Float32 => 32,
                _ => throw new NotSupportedException($"Unsupported sample format: {source.Format}")
            };

            format.nBlockAlign = (ushort)(format.nChannels * format.wBitsPerSample / 8);
            format.nAvgBytesPerSec = format.nSamplesPerSec * format.nBlockAlign;
            format.cbSize = 0;

            return format;
        }

        private void WaveOutCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (uMsg == MM_WOM_DONE)
            {
                // 缓冲区播放完成，标记为可用
                var header = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
                GCHandle handle = GCHandle.FromIntPtr(header.dwUser);
                if (handle.Target is WaveOutBuffer buffer)
                {
                    buffer.OnCompleted();
                }
            }
        }

        private void CleanupWaveOut()
        {
            if (_buffers != null)
            {
                foreach (var buffer in _buffers)
                {
                    buffer?.Dispose();
                }
                _buffers = null;
            }

            if (_hWaveOut != IntPtr.Zero)
            {
                waveOutClose(_hWaveOut);
                _hWaveOut = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop(); // 使用 Stop 而不是设置 IsPlaying = false
                _pauseEvent?.Dispose();
                _disposed = true;
            }
        }

        // WinAPI 声明
        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, WaveOutProc dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutPause(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutRestart(IntPtr hWaveOut);

        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }
    }

    // WaveOutBuffer 类保持不变
    internal class WaveOutBuffer : IDisposable
    {
        private readonly IntPtr _hWaveOut;
        private readonly int _bufferSize;
        private GCHandle _headerHandle;
        private GCHandle _bufferHandle;
        private WAVEHDR _header;
        private byte[] _buffer;
        private bool _disposed = false;

        public bool IsQueued { get; private set; }

        public WaveOutBuffer(IntPtr hWaveOut, int bufferSize)
        {
            _hWaveOut = hWaveOut;
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];

            _bufferHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            _header = new WAVEHDR();
            _header.lpData = _bufferHandle.AddrOfPinnedObject();
            _header.dwBufferLength = (uint)bufferSize;
            _header.dwUser = GCHandle.ToIntPtr(GCHandle.Alloc(this));

            _headerHandle = GCHandle.Alloc(_header, GCHandleType.Pinned);
        }

        public void WriteData(byte[] data, int length)
        {
            if (IsQueued) return;

            Array.Copy(data, _buffer, Math.Min(length, _bufferSize));
            _header.dwBufferLength = (uint)length;

            // 更新固定的 header
            Marshal.StructureToPtr(_header, _headerHandle.AddrOfPinnedObject(), false);

            // 准备并写入缓冲区
            waveOutPrepareHeader(_hWaveOut, _headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WAVEHDR>());
            waveOutWrite(_hWaveOut, _headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WAVEHDR>());

            IsQueued = true;
        }

        public void OnCompleted()
        {
            if (IsQueued)
            {
                waveOutUnprepareHeader(_hWaveOut, _headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WAVEHDR>());
                IsQueued = false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                OnCompleted();

                if (_headerHandle.IsAllocated)
                {
                    // 释放用户数据的 GCHandle
                    if (_header.dwUser != IntPtr.Zero)
                    {
                        GCHandle userHandle = GCHandle.FromIntPtr(_header.dwUser);
                        if (userHandle.IsAllocated)
                            userHandle.Free();
                    }

                    _headerHandle.Free();
                }

                if (_bufferHandle.IsAllocated)
                    _bufferHandle.Free();

                _disposed = true;
            }
        }

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }
    }

}
