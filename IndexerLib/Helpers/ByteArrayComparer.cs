using System.Collections;
using System.Collections.Generic;

namespace IndexerLib.Helpers
{
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int len = x.Length < y.Length ? x.Length : y.Length;
            for (int i = 0; i < len; i++)
            {
                int diff = x[i] - y[i];
                if (diff != 0) 
                    return diff;
            }

            return x.Length.CompareTo(y.Length);
        }
    }

    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null) return 0;

            unchecked
            {
                int hash = 17;
                foreach (var b in obj)
                    hash = hash * 31 + b;
                return hash;
            }
        }
    }

}
