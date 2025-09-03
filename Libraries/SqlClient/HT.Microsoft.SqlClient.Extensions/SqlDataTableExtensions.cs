using System;
using System.Data;
using System.Text.Json;

namespace HT.Microsoft.SqlClient.Extensions
{
    public static class SqlDataTableExtensions
    {
        public static string TruncateTo(this string target, int maxLength)
        {
            if (target == null) return target;
            if (target.Length <= maxLength) return target;
            return target.Substring(0, maxLength - 3) + "...";

        }
        public static string ReadString(this DataRow reader, string field, string nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return dbObject as string;
        }
        public static int ReadInt(this DataRow reader, string field, int nullValue = default(int)) => ReadNullableInt(reader, field, nullValue).Value;
        public static int? ReadNullableInt(this DataRow reader, string field, int? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (int)dbObject;
        }
        public static long Readlong(this DataRow reader, string field, long nullValue = default(long)) => ReadNullablelong(reader, field, nullValue).Value;
        public static long? ReadNullablelong(this DataRow reader, string field, long? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (long)dbObject;
        }

        public static System.Int16 ReadInt16(this DataRow reader, string field, System.Int16 nullValue = default(System.Int16)) => ReadNullableInt16(reader, field, nullValue).Value;
        public static System.Int16? ReadNullableInt16(this DataRow reader, string field, System.Int16? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (Int16)dbObject;
        }

        public static short ReadShort(this DataRow reader, string field, Int16 nullValue = default(Int16)) => ReadNullableInt16(reader, field, nullValue).Value;
        public static short ReadNullableShort(this DataRow reader, string field, Int16? nullValue = null) => ReadNullableInt16(reader, field, nullValue).Value;


        public static double? ReadNullableDouble(this DataRow reader, string field, double? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (double)dbObject;
        }
        public static double ReadDouble(this DataRow reader, string field, double nullValue = default(double)) => ReadNullableDouble(reader, field, nullValue).Value;

        public static double? ReadNullablePureDouble(this DataRow reader, string field, double? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (double) dbObject;
        }

        public static decimal? ReadNullableDecimal(this DataRow reader, string field, decimal? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (decimal)dbObject;
        }
        public static decimal ReadDecimal(this DataRow reader, string field, decimal nullValue = default(decimal)) => ReadNullableDecimal(reader, field, nullValue).Value;


        public static float? ReadNullableFloat(this DataRow reader, string field, float? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (float)dbObject;
        }
        public static float ReadFloat(this DataRow reader, string field, float nullValue = default(float)) => ReadNullableFloat(reader, field, nullValue).Value;


        public static byte[] ReadBinary(this DataRow reader, string field)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return null;
            return (byte[])dbObject;
        }
        public static T ReadJson<T>(this DataRow reader, string field) where T : new()
        {
            var dbData = ReadString(reader, field, null);
            if (string.IsNullOrWhiteSpace(dbData)) return default(T);
            return JsonSerializer.Deserialize<T>(dbData);
        }
        public static T ReadEnum<T>(this DataRow reader, string field, T nullValue = default(T)) where T : Enum
        {
            var dbData = ReadString(reader, field);
            if (string.IsNullOrWhiteSpace(dbData)) return nullValue;
            dbData = dbData.Trim();

            if (Enum.TryParse(typeof(T), dbData, true, out object retVal) == false)
            {
                return nullValue;
            }
            return (T)retVal;
        }

        public static DateTimeOffset? ReadNullableDateTimeOffset(this DataRow reader, string field, DateTimeOffset? nullValue = null)
        {
            var dbData = reader[field];
            if (dbData == null || dbData == DBNull.Value) return nullValue;
            if (dbData is DateTimeOffset? || dbData is DateTimeOffset) { return (DateTimeOffset?)dbData; }
            else if (dbData is DateTime? || dbData is DateTime)
            {
                var constructDateTimeOffset = new DateTimeOffset((DateTime)dbData);
                return constructDateTimeOffset;
            }

            return (DateTimeOffset?)dbData;
        }
        public static DateTimeOffset ReadDateTimeOffset(this DataRow reader, string field, DateTimeOffset nullValue = default(DateTimeOffset)) => ReadNullableDateTimeOffset(reader, field, nullValue).Value;

        public static DateTime? ReadNullableDateTime(this DataRow reader, string field, DateTime? nullValue = null)
        {

            var dbData = reader[field];
            if (dbData == null || dbData == DBNull.Value) return nullValue;
            return (DateTime?)dbData;

        }
        public static DateTime ReadDateTime(this DataRow reader, string field, DateTime nullValue = default(DateTime)) => ReadNullableDateTime(reader, field, nullValue).Value;



        public static bool ReadBoolean(this DataRow reader, string field, bool nullValue = default(bool)) => ReadNullableBoolean(reader, field, nullValue).Value;

        public static bool? ReadNullableBoolean(this DataRow reader, string field, bool? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (bool)dbObject;
        }

        public static TimeSpan ReadNullableTimeSpan(this DataRow reader, string field, TimeSpan? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue == null ? TimeSpan.MinValue : nullValue.Value;
            var timespanString = (string)dbObject;
            if (string.IsNullOrWhiteSpace(timespanString)) return nullValue == null ? TimeSpan.MinValue : nullValue.Value;

            if (!TimeSpan.TryParse(timespanString, out var ts))
            {
                return TimeSpan.MinValue;
            }
            return ts;
        }
        public static TimeSpan ReadTimeSpan(this DataRow reader, string field, TimeSpan nullValue = default(TimeSpan)) => ReadNullableTimeSpan(reader, field, nullValue);
    }
}
