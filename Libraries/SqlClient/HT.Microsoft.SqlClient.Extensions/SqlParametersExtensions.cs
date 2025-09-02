using System;
using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static partial class SqlParametersExtensions
    {
        public static SqlParameter AddVarBinary(this SqlParameterCollection parameters, string parameterName, byte[] parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (parameterValue == null)
            {
                return parameters.Add(new SqlParameter()
                {
                    SqlDbType = SqlDbType.VarBinary,
                    DbType = DbType.Binary,
                    Direction = ParameterDirection.Input,
                    ParameterName = parameterName,
                    Value = DBNull.Value
                });
            }
            else
            {
                return parameters.Add(new SqlParameter()
                {
                    SqlDbType = SqlDbType.VarBinary,
                    DbType = DbType.Binary,
                    Direction = ParameterDirection.Input,
                    Size = parameterValue.Length,
                    ParameterName = parameterName,
                    Value = parameterValue
                });
            }
        }

        public static SqlParameter AddVarBinaryJson(this SqlParameterCollection parameters, string parameterName,
            object objectToSerialize)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (objectToSerialize == null)
            {
                return new SqlParameter()
                {
                    SqlDbType = SqlDbType.VarBinary,
                    DbType = DbType.String,
                    Direction = ParameterDirection.Input,
                    ParameterName = parameterName,
                    Value =  DBNull.Value
                };
            }

            var jsonString = JsonSerializer.Serialize(objectToSerialize);
            var bytes = UTF8Encoding.UTF8.GetBytes(jsonString);
            return parameters.Add(new SqlParameter()
            {
                SqlDbType = SqlDbType.VarBinary,
                DbType = DbType.Binary,
                Direction = ParameterDirection.Input,
                Size = bytes.Length,
                ParameterName = parameterName,
                Value = bytes
            });
        }
     
        public static SqlParameter AddNVarchar(this SqlParameterCollection parameters, string parameterName, string parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            var retVal = parameters.Add(new SqlParameter()
            {
                SqlDbType = SqlDbType.NVarChar,
                DbType = DbType.String,
                Direction = ParameterDirection.Input,
                ParameterName = parameterName,
                Value = parameterValue ?? (object)DBNull.Value
            });
            return retVal;
        }
        public static SqlParameter AddVarchar(this SqlParameterCollection parameters, string parameterName, string parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            var retVal = parameters.Add(new SqlParameter()
            {
                SqlDbType = SqlDbType.VarChar,
                DbType = DbType.AnsiString,
                Direction = ParameterDirection.Input,
                ParameterName = parameterName,
                Value = (object)parameterValue ?? DBNull.Value
            });
            return retVal;
        }
        public static SqlParameter AddNullable(this SqlParameterCollection parameters, string parameterName, object parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (parameterValue == null)
            {
                return parameters.AddWithValue(parameterName, DBNull.Value);
            }
            return parameters.AddWithValue(parameterName, parameterValue);

        }

        public static SqlParameter AddNVarCharJson(this SqlParameterCollection parameters, string parameterName, object objectToSerialize)
        {
            if (objectToSerialize == null)
            {
                return parameters.AddWithValue(parameterName, DBNull.Value);
            }

            var serializedObject = JsonSerializer.Serialize(objectToSerialize);
            return parameters.AddNVarchar(parameterName, serializedObject);
        }


        public static SqlParameter AddBit(this SqlParameterCollection parameters, string parameterName, bool? parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (parameterValue == null)
            {
                return parameters.AddWithValue(parameterName, DBNull.Value);
            }
            return parameters.AddWithValue(parameterName, parameterValue);
        }

       
        public static SqlParameter AddDateTimeOffset(this SqlParameterCollection parameters, string parameterName, DateTimeOffset? parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (parameterValue == null)
            {
                return parameters.AddWithValue(parameterName, DBNull.Value);
            }
            return parameters.AddWithValue(parameterName, parameterValue);
        }

        public static SqlParameter AddDateTime(this SqlParameterCollection parameters, string parameterName, DateTimeOffset parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;

            return parameters.AddWithValue(parameterName, parameterValue);
        }
        public static SqlParameter AddDateTime(this SqlParameterCollection parameters, string parameterName, DateTime parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;

            return parameters.AddWithValue(parameterName, parameterValue);
        }
        public static SqlParameter AddInteger(this SqlParameterCollection parameters, string parameterName, int parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;

            return parameters.AddWithValue(parameterName, parameterValue);
        }
        public static SqlParameter AddInteger(this SqlParameterCollection parameters, string parameterName, int? parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;
            if (parameterValue == null)
            {
                return parameters.AddWithValue(parameterName, DBNull.Value);
            }
            return parameters.AddWithValue(parameterName, parameterValue);
        }
        public static SqlParameter AddDouble(this SqlParameterCollection parameters, string parameterName, double parameterValue)
        {
            if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;

            return parameters.AddWithValue(parameterName, parameterValue);
        }


    }
}
