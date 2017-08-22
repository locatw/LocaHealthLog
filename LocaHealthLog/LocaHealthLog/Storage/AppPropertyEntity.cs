using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;

namespace LocaHealthLog.Storage
{
    class AppPropertyEntity : TableEntity
    {
        private static readonly TimeZoneInfo jstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        public AppPropertyEntity()
        {
            PartitionKey = "App";
            RowKey = "LastMeasurementDate";
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            LastMeasurementDate = LastMeasurementDate.ToOffset(jstTimeZone.BaseUtcOffset);
        }

        public DateTimeOffset LastMeasurementDate { get; set; }
    }
}
