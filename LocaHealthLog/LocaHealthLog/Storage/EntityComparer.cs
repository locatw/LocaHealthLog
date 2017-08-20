using System.Collections.Generic;

namespace LocaHealthLog.Storage
{
    class EntityComparer : IEqualityComparer<InnerScanStatusEntity>
    {
        public bool Equals(InnerScanStatusEntity x, InnerScanStatusEntity y)
        {
            return x.PartitionKey == y.PartitionKey && x.RowKey == y.RowKey;
        }

        public int GetHashCode(InnerScanStatusEntity obj)
        {
            return obj.PartitionKey.GetHashCode() ^ obj.RowKey.GetHashCode();
        }
    }
}
