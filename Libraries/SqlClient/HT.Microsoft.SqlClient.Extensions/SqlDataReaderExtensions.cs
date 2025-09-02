using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions
{
    public  static partial class SqlDataReaderExtensions
    {
        private static JsonSerializerOptions _serializerOptions = GetOptions();

        private static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        public static string TruncateTo(this string target, int maxLength)
        {
            if (target == null) return target;
            if (target.Length <= maxLength) return target;
            return target.Substring(0, maxLength - 3) + "...";

        }
        public static string ReadString(this SqlDataReader reader, string field, string nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return dbObject as string;
        }
        public static int ReadInt(this SqlDataReader reader, string field, int nullValue = default(int)) => ReadNullableInt(reader, field, nullValue).Value;
        public static int? ReadNullableInt(this SqlDataReader reader, string field, int? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (int)dbObject;
        }
        public static long Readlong(this SqlDataReader reader, string field, long nullValue = default(long)) => ReadNullablelong(reader, field, nullValue).Value;
        public static long? ReadNullablelong(this SqlDataReader reader, string field, long? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (long)dbObject;
        }

        public static System.Int16 ReadInt16(this SqlDataReader reader, string field, System.Int16 nullValue = default(System.Int16)) => ReadNullableInt16(reader, field, nullValue).Value;
        public static System.Int16? ReadNullableInt16(this SqlDataReader reader, string field, System.Int16? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (Int16)dbObject;
        }

        public static short ReadShort(this SqlDataReader reader, string field, Int16 nullValue = default(Int16)) => ReadNullableInt16(reader, field, nullValue).Value;
        public static short ReadNullableShort(this SqlDataReader reader, string field, Int16? nullValue = null) => ReadNullableInt16(reader, field, nullValue).Value;


        public static double? ReadNullableDouble(this SqlDataReader reader, string field, double? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (double)dbObject;
        }
        public static double ReadDouble(this SqlDataReader reader, string field, double nullValue = default(double)) => ReadNullableDouble(reader, field, nullValue).Value;

        public static decimal? ReadNullableDecimal(this SqlDataReader reader, string field, decimal? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (decimal)dbObject;
        }
        public static decimal ReadDecimal(this SqlDataReader reader, string field, decimal nullValue = default(decimal)) => ReadNullableDecimal(reader, field, nullValue).Value;

        public static float? ReadNullableFloat(this SqlDataReader reader, string field, float? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (float)dbObject;
        }

        public static float ReadFloat(this SqlDataReader reader, string field, float nullValue = default(float)) => ReadNullableFloat(reader, field, nullValue).Value;


        public static byte[] ReadBinary(this SqlDataReader reader, string field)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return null;
            return (byte[])dbObject;
        }
        public static T ReadJson<T>(this SqlDataReader reader, string field) where T : new()
        {
            var dbData = ReadString(reader, field, null);
            if (string.IsNullOrWhiteSpace(dbData)) return default(T);
            try
            {
                return JsonSerializer.Deserialize<T>(dbData, _serializerOptions);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public static T ReadEnumByString<T>(this SqlDataReader reader, string field, T nullValue = default(T)) where T : Enum
        {
            var dbData = ReadString(reader, field);
            if (string.IsNullOrWhiteSpace(dbData)) return nullValue;
            dbData = dbData.Trim();
            
            if (Enum.TryParse(typeof(T), dbData, true,out object retVal) == false)
            {
                return nullValue;
            }
            return (T)retVal;
        }
        public static T ReadEnumByInt<T>(this SqlDataReader reader, string field, T nullValue = default(T)) where T : Enum
        {
            var dbField = reader[field];
            if (dbField == null || dbField == DBNull.Value) return nullValue;
            var intValue = (object)(int)dbField;
            return (T)intValue;
            
        }
        public static DateTimeOffset? ReadNullableDateTimeOffset(this SqlDataReader reader, string field, DateTimeOffset? nullValue = null)
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

        public static DateTimeOffset ReadDateTimeOffset(this SqlDataReader reader, string field, DateTimeOffset nullValue = default(DateTimeOffset)) => ReadNullableDateTimeOffset(reader, field, nullValue).Value;

        public static DateTime? ReadNullableDateTime(this SqlDataReader reader, string field, DateTime? nullValue = null)
        {

                var dbData = reader[field];
                if (dbData == null || dbData == DBNull.Value) return nullValue;
                return (DateTime?)dbData;
            
        }
        public static DateTime ReadDateTime(this SqlDataReader reader, string field, DateTime nullValue = default(DateTime)) => ReadNullableDateTime(reader, field, nullValue).Value;



        public static bool ReadBoolean(this SqlDataReader reader, string field, bool nullValue = default(bool)) => ReadNullableBoolean(reader, field, nullValue).Value;

        public static bool? ReadNullableBoolean(this SqlDataReader reader, string field, bool? nullValue = null)
        {
            var dbObject = reader[field];
            if (dbObject == DBNull.Value || dbObject == null) return nullValue;
            return (bool)dbObject;
        }

        public static TimeSpan ReadNullableTimeSpan(this SqlDataReader reader, string field, TimeSpan? nullValue = null)
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
        public static TimeSpan ReadTimeSpan(this SqlDataReader reader, string field, TimeSpan nullValue = default(TimeSpan)) => ReadNullableTimeSpan(reader, field, nullValue);
    }
}
