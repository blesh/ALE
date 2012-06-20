﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace ALE.SqlClient
{
    public class Sql : IDisposable
    {
        protected readonly SqlConnection Connection;
        protected readonly string ConnectionString;

        private Sql(string connectionString)
        {
            ConnectionString = connectionString;
            Connection = new SqlConnection(ConnectionString);
        }

        public static Sql Create(string connectionString)
        {
            return new Sql(connectionString);
        }

        private SqlParameter[] ConvertObjectToSqlParams(object args)
        {
            var properties = args.GetType().GetProperties(BindingFlags.Public | BindingFlags.GetField);
            var parameters = new SqlParameter[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                parameters[i] = new SqlParameter(property.Name, property.GetValue(args, null));
            }
            return parameters;
        }
        public SqlCommand CreateCommand(string commandText, object args = null, CommandType commandType = CommandType.Text)
        {
            var cmd = new SqlCommand(commandText, Connection);
            cmd.CommandType = commandType;
            if (args != null)
            {
                cmd.Parameters.AddRange(ConvertObjectToSqlParams(args));
            }
            return cmd;
        }
        public Sql ExecuteReader(string commandText, Action<SqlDataReader> callback)
        {
            return ExecuteReader(commandText, null, callback);
        }
        public Sql ExecuteReader(string commandText, object args, Action<SqlDataReader> callback)
        {
            return ExecuteReader(commandText, args, CommandType.Text, callback);
        }
        public Sql ExecuteReader(string commandText, object args, CommandType cmdType, Action<SqlDataReader> callback)
        {
            var cmd = CreateCommand(commandText, args, cmdType);
            return ExecuteReader(cmd, callback);
        }
        public Sql ExecuteReader(SqlCommand cmd, Action<SqlDataReader> callback)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            new Action(() =>
            {
                cmd.Connection.Open();
                var reader = cmd.ExecuteReader();
                EventLoop.Current.Pend(() =>
                {
                    callback(reader);
                    cmd.Connection.Close();
                });
            }).BeginInvoke(null, null);
            return this;
        }
        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
            }
        }
    }
}
