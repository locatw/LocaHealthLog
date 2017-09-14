using LocaHealthLog.HealthPlanet;
using LocaHealthLog.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocaHealthLog
{
    class EntityFactory
    {
        private static readonly Dictionary<InnerScanTag, Func<Data, InnerScanStatusEntity>> partialEntityFactory =
            new Dictionary<InnerScanTag, Func<Data, InnerScanStatusEntity>>()
            {
                { InnerScanTag.Weight, data => { return new WeightEntity { Weight = double.Parse(data.KeyData) }; } },
                { InnerScanTag.BodyFatPercentage, data => { return new BodyFatPercentageEntity { BodyFatPercentage = double.Parse(data.KeyData) }; } },
                { InnerScanTag.MuscleMass, data => { return new MuscleMassEntity { MuscleMass = double.Parse(data.KeyData) }; } },
                { InnerScanTag.MuscleScore, data => { return new MuscleScoreEntity { MuscleScore = int.Parse(data.KeyData) }; } },
                { InnerScanTag.VisceralFatLevel2, data => { return new VisceralFatLevel2Entity { VisceralFatLevel2 = double.Parse(data.KeyData) }; } },
                { InnerScanTag.VesceralFatLevel, data => { return new VisceralFatLevelEntity { VisceralFatLevel = int.Parse(data.KeyData) }; } },
                { InnerScanTag.BasalMetabolicRate, data => { return new BasalMetabolicRateEntity { BasalMetabolicRate = int.Parse(data.KeyData) }; } },
                { InnerScanTag.BodyAge, data => { return new BodyAgeEntity { BodyAge = int.Parse(data.KeyData) }; } },
                { InnerScanTag.EstimatedBoneMass, data => { return new EstimatedBoneMassEntity { EstimatedBoneMass = double.Parse(data.KeyData) }; } }
            };

        public IEnumerable<InnerScanStatusEntity> MakeFrom(Status status)
        {
            DateTimeOffset birthDate = ParseBirthDate(status.BirthDate);
            Gender gender = ParseGender(status.Sex);
            double height = ParseHeight(status.Height);

            return status.Data.Select(data =>
            {
                InnerScanTag tag = InnerScanTagHelper.MakeFrom(data.Tag);
                InnerScanStatusEntity entity = partialEntityFactory[tag](data);
                entity.PartitionKey = data.Date;
                entity.RowKey = data.Tag;
                entity.BirthDate = birthDate;
                entity.Gender = gender;
                entity.Height = height;
                entity.MeasurementDate = ParseMeasurementDate(data.Date);
                entity.Model = data.Model;
                entity.Tag = tag;

                return entity;
            });
        }

        private Gender ParseGender(string sex)
        {
            switch (sex.ToLower())
            {
                case "male":
                    return Gender.Male;
                case "female":
                    return Gender.Female;
                default:
                    throw new Exception($"Invalid sex: {sex}");
            }
        }

        private double ParseHeight(string height)
        {
            if (double.TryParse(height, out double value))
            {
                return value;
            }
            else
            {
                throw new Exception($"Invalid height value: {height}");
            }
        }

        private DateTimeOffset ParseMeasurementDate(string dataDate)
        {
            try
            {
                int year = int.Parse(dataDate.Substring(0, 4));
                int month = int.Parse(dataDate.Substring(4, 2));
                int day = int.Parse(dataDate.Substring(6, 2));
                int hour = int.Parse(dataDate.Substring(8, 2));
                int minute = int.Parse(dataDate.Substring(10, 2));

                return new DateTimeOffset(year, month, day, hour, minute, 0, AppConfig.LocalTimeZone.BaseUtcOffset);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Cannot parse measurement date from {dataDate}", "dataDate", e);
            }
        }

        private DateTimeOffset ParseBirthDate(string birthDate)
        {
            try
            {
                int year = int.Parse(birthDate.Substring(0, 4));
                int month = int.Parse(birthDate.Substring(4, 2));
                int day = int.Parse(birthDate.Substring(6, 2));

                return new DateTimeOffset(year, month, day, 0, 0, 0, AppConfig.LocalTimeZone.BaseUtcOffset);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Cannot parse birth date from {birthDate}", "birthDate", e);
            }
        }
    }
}
