using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocaHealthLog.Storage
{
    class StorageClient
    {
        private static readonly string tableName = "LocaHealthLog";

        private CloudTable table;

        public async Task ConnectAsync(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();
        }

        public async Task BatchInsertAsync(IEnumerable<InnerScanStatusEntity> entities)
        {
            foreach (var group in entities.GroupBy(entity => entity.PartitionKey))
            {
                var existingEntities = QueryExistingEntities(group.Key);
                var insertingEntities = group.Except(existingEntities, new EntityComparer()).ToList();

                if (insertingEntities.Count <= 0)
                {
                    continue;
                }

                var batchOperation = new TableBatchOperation();
                insertingEntities.ForEach(entity => batchOperation.Insert(entity));

                await table.ExecuteBatchAsync(batchOperation);
            }
        }

        private IEnumerable<InnerScanStatusEntity> QueryExistingEntities(string partitionKey)
        {
            var condition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            var query = new TableQuery<InnerScanStatusEntity>().Where(condition);

            return table.ExecuteQuery(query);
        }
    }
}
