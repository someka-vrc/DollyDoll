using System.Collections.Generic;

namespace Somekasu.DollyDoll
{
    public class FastListEqualityComparer : IEqualityComparer<List<int>>
    {
        public bool Equals(List<int> x, List<int> y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null)
                return false;
            if (x.Count != y.Count)
                return false;

            // 100個以下なら単純ループが最速
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] != y[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode(List<int> obj)
        {
            if (obj == null)
                return 0;

            // XORベースの高速ハッシュ（順序考慮）
            unchecked
            {
                int hash = obj.Count;
                for (int i = 0; i < obj.Count; i++)
                {
                    hash = (hash << 5) + hash + obj[i];
                }
                return hash;
            }
        }
    }
}