using LocaHealthLog.HealthPlanet;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;

namespace LocaHealthLog.Storage
{
    class InnerScanStatusEntity : TableEntity
    {
        public InnerScanTag Tag { get; set; }

        public DateTimeOffset MeasurementDate { get; set; }

        public string Model { get; set; }

        public DateTimeOffset BirthDate { get; set; }

        public double Height { get; set; }

        public Gender Gender { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            Tag = MakeInnserScanTag(properties["Tag"].StringValue);
            Gender = MakeGender(properties["Gender"].StringValue);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result = base.WriteEntity(operationContext);

            result["Tag"] = new EntityProperty(Tag.ToString());
            result["Gender"] = new EntityProperty(Gender.ToString());

            return result;
        }

        private InnerScanTag MakeInnserScanTag(string value)
        {
            if (Enum.TryParse(value, out InnerScanTag tag))
            {
                return tag;
            }
            else
            {
                throw new ArgumentException($"Invalid inner scan tag value: {value}");
            }
        }

        private Gender MakeGender(string value)
        {
            if (Enum.TryParse(value, out Gender gender))
            {
                return gender;
            }
            else
            {
                throw new ArgumentException($"Invalid gender value: {value}");
            }
        }
    }
}