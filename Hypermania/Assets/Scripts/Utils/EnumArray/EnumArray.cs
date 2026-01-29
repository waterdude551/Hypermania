using System;
using UnityEngine;

namespace Utils.EnumArray
{
    [Serializable]
    public class EnumArray<TEnum, TValue>
        where TEnum : unmanaged, Enum
    {
        [SerializeField]
        private TValue[] values;

        public TValue this[TEnum key]
        {
            get
            {
                EnsureSize();
                return values[ToIndex(key)];
            }
            set
            {
                EnsureSize();
                values[ToIndex(key)] = value;
            }
        }

        public TValue[] RawValues => values;

        private static int ToIndex(TEnum e) => Convert.ToInt32(e);

        private void EnsureSize()
        {
            int n = Enum.GetValues(typeof(TEnum)).Length;
            if (values == null || values.Length != n)
            {
                Array.Resize(ref values, n);
            }
        }
    }
}
