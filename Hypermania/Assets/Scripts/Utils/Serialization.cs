using System;

namespace Utils
{
    public interface ISerializable
    {
        public abstract int SerdeSize();
        public abstract int Serialize(Span<byte> outBytes);
        public abstract int Deserialize(ReadOnlySpan<byte> inBytes);
    }

    public static class Serializer<T>
        where T : ISerializable
    {
        private static readonly T _sample = default;

        public static int DefaultSize()
        {
            return _sample.SerdeSize();
        }
    }
}
