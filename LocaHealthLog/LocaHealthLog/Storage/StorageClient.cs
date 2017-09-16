using LocaHealthLog.HealthPlanet;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocaHealthLog.Storage
{
    class StorageClient
    {
        private static readonly string tableName = "LocaHealthLog";

        private EntityResolver<InnerScanStatusEntity> entityResolver =
            (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                InnerScanStatusEntity entity = null;

                InnerScanTag tag = (InnerScanTag)Enum.Parse(typeof(InnerScanTag), properties["Tag"].StringValue);

                switch (tag)
                {
                    case InnerScanTag.BasalMetabolicRate:
                        entity = new BasalMetabolicRateEntity()
                        {
                            BasalMetabolicRate = properties["BasalMetabolicRate"].Int32Value.Value
                        };
                        break;
                    case InnerScanTag.BodyAge:
                        entity = new BodyAgeEntity()
                        {
                            BodyAge = properties["BodyAge"].Int32Value.Value
                        };
                        break;
                    case InnerScanTag.BodyFatPercentage:
                        entity = new BodyFatPercentageEntity()
                        {
                            BodyFatPercentage = properties["BodyFatPercentage"].DoubleValue.Value
                        };
                        break;
                    case InnerScanTag.EstimatedBoneMass:
                        entity = new EstimatedBoneMassEntity()
                        {
                            EstimatedBoneMass = properties["EstimatedBoneMass"].DoubleValue.Value
                        };
                        break;
                    case InnerScanTag.MuscleMass:
                        entity = new MuscleMassEntity()
                        {
                            MuscleMass = properties["MuscleMass"].DoubleValue.Value
                        };
                        break;
                    case InnerScanTag.MuscleScore:
                        entity = new MuscleScoreEntity()
                        {
                            MuscleScore = properties["MuscleScore"].Int32Value.Value
                        };
                        break;
                    case InnerScanTag.VesceralFatLevel:
                        entity = new VisceralFatLevelEntity()
                        {
                            VisceralFatLevel = properties["VisceralFatLeve"].Int32Value.Value
                        };
                        break;
                    case InnerScanTag.VisceralFatLevel2:
                        entity = new VisceralFatLevel2Entity()
                        {
                            VisceralFatLevel2 = properties["VisceralFatLevel2"].Int32Value.Value
                        };
                        break;
                    case InnerScanTag.Weight:
                        entity = new WeightEntity()
                        {
                            Weight = properties["Weight"].DoubleValue.Value
                        };
                        break;
                    default:
                        throw new NotImplementedException($"tag: {properties["Tag"].StringValue}");
                }

                entity.PartitionKey = partitionKey;
                entity.RowKey = rowKey;
                entity.Timestamp = timestamp;
                entity.ETag = etag;
                entity.ReadEntity(properties, null);

                return entity;
            };

        private CloudTable table;

        public async Task ConnectAsync(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();
        }

        public DateTimeOffset? LoadLastMeasurementDate()
        {
            var partitionCond = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "App");
            var rowCond = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "LastMeasurementDate");
            var cond = TableQuery.CombineFilters(partitionCond, TableOperators.And, rowCond);
            var query = new TableQuery<AppPropertyEntity>().Where(cond);
            var result = table.ExecuteQuery(query);

            if (0 < result.Count())
            {
                return result.First().LastMeasurementDate;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<InnerScanStatusEntity> LoadMeasurementData(string tag, DateTimeOffset begin, DateTimeOffset end)
        {
            var tagCond =
                TableQuery.GenerateFilterCondition("Tag", QueryComparisons.Equal, tag);
            var measurementDateCond =
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForDate("MeasurementDate", QueryComparisons.GreaterThanOrEqual, begin),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("MeasurementDate", QueryComparisons.LessThan, end));
            var cond = TableQuery.CombineFilters(tagCond, TableOperators.And, measurementDateCond);
            var query = new TableQuery<InnerScanStatusEntity>().Where(cond);

            return table.ExecuteQuery(query, entityResolver);
        }

        public async Task UpdateLastMeasurementDate(DateTimeOffset lastMeasurementDate)
        {
            var entity = new AppPropertyEntity() { LastMeasurementDate = lastMeasurementDate };
            var insertOperation = TableOperation.InsertOrReplace(entity);

            await table.ExecuteAsync(insertOperation);
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
