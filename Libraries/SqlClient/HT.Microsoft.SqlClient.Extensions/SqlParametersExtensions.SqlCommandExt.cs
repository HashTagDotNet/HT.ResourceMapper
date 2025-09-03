using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static partial class SqlParametersExtensions
    {
        public static SqlCommand AddVarBinary(this SqlCommand command, string parameterName, byte[] parameterValue)
        {
            command.Parameters.AddVarBinary(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddVarBinaryJson(this SqlCommand command, string parameterName,
           object objectToSerialize)
        {
            command.Parameters.AddVarBinaryJson(parameterName, objectToSerialize);
            return command;
        }
        public static SqlCommand AddNVarchar(this SqlCommand command, string parameterName, string parameterValue)
        {
            command.Parameters.AddNVarchar(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddVarchar(this SqlCommand command, string parameterName, string parameterValue)
        {
            command.Parameters.AddVarchar(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddNullable(this SqlCommand command, string parameterName, object parameterValue)
        {
            command.Parameters.AddNullable(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddNVarCharJson(this SqlCommand command, string parameterName, object objectToSerialize)
        {
            command.Parameters.AddNVarCharJson(parameterName, objectToSerialize);
            return command;
        }
        public static SqlCommand AddBit(this SqlCommand command, string parameterName, bool? parameterValue)
        {
            command.Parameters.AddBit(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddDateTimeOffset(this SqlCommand command, string parameterName, DateTimeOffset? parameterValue)
        {
            command.Parameters.AddDateTimeOffset(parameterName, parameterValue);
            return command;
        }

        public static SqlCommand AddDateTime(this SqlCommand command, string parameterName, DateTimeOffset parameterValue)
        {
            command.Parameters.AddDateTime(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddDateTime(this SqlCommand command, string parameterName, DateTime parameterValue)
        {
            command.Parameters.AddDateTime(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddInteger(this SqlCommand command, string parameterName, int parameterValue, ParameterDirection direction = ParameterDirection.Input)
        {
            var param = command.Parameters.AddInteger(parameterName, parameterValue);
            param.Direction = direction;
            return command;
        }
        public static SqlCommand AddInteger(this SqlCommand command, string parameterName, int? parameterValue)
        {
            command.Parameters.AddInteger(parameterName, parameterValue);
            return command;
        }
        public static SqlCommand AddDouble(this SqlCommand command, string parameterName, double parameterValue)
        {
            command.Parameters.AddDouble(parameterName, parameterValue);
            return command;
        }

        public static int ReadInt(this SqlCommand command, string parameterName)
        {
            var value = command.Parameters[parameterName].Value;
            return value == DBNull.Value ? 0 : (int)value;
        }
    }
}
