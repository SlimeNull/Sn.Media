using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media.Internal
{
    internal static class BinaryUtils
    {
#if NET8_0_OR_GREATER

#else
        public static unsafe byte[] StructureToBytes<T>(in T value)
            where T : unmanaged
        {
            fixed (T* ptr = &value)
            {
                byte[] buffer = new byte[sizeof(T)];
                Marshal.Copy((nint)ptr, buffer, 0, sizeof(T));
                return buffer;
            }
        }
#endif

        public static unsafe void WriteStructure<T>(this Stream stream, in T value)
            where T : unmanaged
        {
#if NET8_0_OR_GREATER
            fixed (T* ptr = &value)
            {
                stream.Write(new ReadOnlySpan<byte>(ptr, sizeof(T)));
            }
#else
            var valueBytes = StructureToBytes(value);
#endif
        }
    }
}
