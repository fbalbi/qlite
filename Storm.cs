using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace project.Classes
{
    //##------------------------------
    //## Storm: SimpleTinyORM
    //## version 1.01
    //## (c) 2015-2018 Federico Balbi
    //##------------------------------

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    public class AutoIncrementAttribute : Attribute
    {
    }

    public class Storm
    {
        private DbProviderFactory DbFactory;

        public const string DEFAULT_SCHEMA = "SATURN";
        private DbConnection DbConn = null;
        private string ConnectionString;

        //public Storm(string providerName = "Oracle.DataAccess.Client")
        //{
        //    this.DbFactory = DbProviderFactories.GetFactory(providerName);
        //    ConnectionString = DbConnStr("DEV"); // Config.Environment.AppEnv);
        //}

        public Storm(string ConnectionString, string providerName = "Oracle.DataAccess.Client")
        {
            this.DbFactory = DbProviderFactories.GetFactory(providerName);
            this.ConnectionString = ConnectionString;
        }

        private bool IsNullable<T>(T value)
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }

        // this method applies to Oracle only
        public int SequenceNextVal(string sequence)
        {
            int NewId = Convert.ToInt32(ExecuteScalar($"SELECT {sequence}.NEXTVAL FROM DUAL"));
            return NewId;
        }

        public T GetRecordById<T>(decimal id, string idField)
        {
            List<T> result = new List<T>();

            DbOpen();

            DbCommand cmd = DbConn.CreateCommand();
            //cmd.Connection = DbConn;

            T item = Activator.CreateInstance<T>();
            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();
            int DotPos = FullTypeName.LastIndexOf('.');
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            // costruisse a query usando e proprieta e tipo del ogeto
            string query = "SELECT " + string.Join(",", info.Select(x => x.Name)) + " FROM " + typeof(T).Name + " WHERE " + idField + " = " + id;

            cmd.CommandText = query;

            DbDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                item = GetRecord<T>(rd);
            }
            rd.Close();

            DbClose();
            return item;
        }

        private T GetRecord<T>(DbDataReader rd)
        {
            T item = Activator.CreateInstance<T>();
            PropertyInfo[] info = item.GetType().GetProperties();

            if (info.Length == 0)
            {
                item = (T)rd[0];
            }
            else
            {
                // type check
                foreach (PropertyInfo prop in info)
                {
                    try
                    {
                        switch (GetType(prop.PropertyType.FullName))
                        {
                            case "System.DateTime":
                                prop.SetValue(item, Convert.ToDateTime(rd[prop.Name]));
                                break;
                            case "System.DateTime?":
                                prop.SetValue(item, rd[prop.Name] == DBNull.Value ? null : (DateTime?)rd[prop.Name]);
                                break;
                            case "System.Int32":
                                prop.SetValue(item, Convert.ToInt32(rd[prop.Name]));
                                break;
                            case "System.Int32?":
                                prop.SetValue(item, rd[prop.Name] == DBNull.Value ? null : (System.Int32?)Convert.ToInt32(rd[prop.Name]));
                                break;
                            case "System.Single":
                                prop.SetValue(item, (float)rd[prop.Name]);
                                break;
                            case "System.Single?":
                                prop.SetValue(item, rd[prop.Name] == DBNull.Value ? null : (float?)rd[prop.Name]);
                                break;
                            case "System.Decimal":
                                prop.SetValue(item, Convert.ToDecimal(rd[prop.Name]));
                                break;
                            case "System.Decimal?":
                                prop.SetValue(item, rd[prop.Name] == DBNull.Value ? null : (decimal?)rd[prop.Name]);
                                break;
                            case "System.Double":
                                prop.SetValue(item, Convert.ToDouble(rd[prop.Name]));
                                break;
                            case "System.Double?":
                                prop.SetValue(item, rd[prop.Name] == DBNull.Value ? null : (double?)rd[prop.Name]);
                                break;
                            case "System.String":
                                prop.SetValue(item, rd[prop.Name].ToString());
                                break;
                            case "System.Boolean":
                                prop.SetValue(item, rd[prop.Name].ToString() == "Y"); // Y/N => T/F
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        string PropName = prop.Name;
                        string TypeName = GetType(prop.PropertyType.FullName);
                        Console.WriteLine($"Error converting {PropName} to {TypeName}");
                    }
                }
            }

            return item;
        }

        private string GetType(string TypeInfo)
        {
            if (TypeInfo.IndexOf("System.Nullable") == 0)
            {
                int start = TypeInfo.IndexOf("[[");
                int end = TypeInfo.IndexOf(",");
                return TypeInfo.Substring(start + 2, end - start - 2) + "?";
            }
            return TypeInfo;
        }

        //private string DbConnStr(string env)
        //{
        //    switch (env)
        //    {
        //        case "PROD": return "Data Source=DWDBPROD;User Id=incentive;Password=incentive2016";
        //        //case "DEV": return "Data Source=PPRD.UTSA.EDU;User Id=CRFB;Password=Gastone38!";
        //        //case "DEV": return "Data Source=PPRD.UTSA.EDU;User Id=MDMSERVICE;Password=nji9_tgb";                    
        //        case "DEV": return "Data Source=PPRD.UTSA.EDU;User Id=FUTURERR;Password=Ff_Rr199";
        //        case "QA": return "";
        //        default: return "DEV";
        //    }
        //}

        private bool DbOpen()
        {
            try
            {
                if (DbConn == null)
                {
                    DbConn = this.DbFactory.CreateConnection();
                    DbConn.ConnectionString = this.ConnectionString;
                }

                if (DbConn.State == System.Data.ConnectionState.Closed)
                {
                    DbConn.Open();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool DbClose()
        {
            try
            {
                DbConn.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public object ExecuteScalar(string query)
        {
            object result = null;

            if (DbOpen())
            {
                DbCommand cmd = this.DbConn.CreateCommand();
                cmd.CommandText = query;
                result = cmd.ExecuteScalar();
                DbClose();
            }

            return result;
        }

        public int ExecuteNonQuery(string query)
        {
            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();
            cmd.CommandText = query;
            int result = cmd.ExecuteNonQuery();
            DbClose();
            return result;
        }

        public int Count<T>(string filter = "", string SchemaName = "")
        {
            DbOpen();

            T item = Activator.CreateInstance<T>();
            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();
            int DotPos = FullTypeName.LastIndexOf('.');
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);
            string query = "SELECT COUNT(*) FROM " + (SchemaName == "" ? "" : SchemaName + ".") + TableName;

            if (filter != "") // zonta filtri se xe sta defignio criterio
            {
                query += " WHERE " + filter;
            }


            DbCommand cmd = this.DbConn.CreateCommand();

            int result = Convert.ToInt32(cmd.ExecuteScalar());

            DbClose();

            return result;
        }

        public List<T> GetTable<T>(string Filter = "", string SchemaName = "")
        {
            List<T> result = new List<T>();

            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            T item = Activator.CreateInstance<T>();
            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();
            int DotPos = FullTypeName.LastIndexOf('.');
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            // costruisse a query usando e proprieta e tipo del ogeto
            string query = "SELECT " + string.Join(",", info.Select(x => x.Name));

            query += " FROM " + (SchemaName == "" ? "" : SchemaName + ".") + TableName;

            if (Filter != "") // zonta filtri se xe sta defignio criterio
            {
                query += " WHERE " + Filter;
            }

            cmd.CommandText = query;

            DbDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                item = GetRecord<T>(rd);
                result.Add(item);
            }
            rd.Close();

            DbClose();
            return result;
        }

        public T GetFirst<T>(string Filter = "", string SchemaName = "")
        {
            T result = default(T);

            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            T item = Activator.CreateInstance<T>();
            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();
            int DotPos = FullTypeName.LastIndexOf('.');
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            // build a query using object props
            string query = "SELECT " + string.Join(",", info.Select(x => x.Name));

            query += " FROM " + (SchemaName == "" ? "" : SchemaName + ".") + TableName;

            if (Filter != "") // add filters if criteria is defined
            {
                query += " WHERE " + Filter;
            }

            cmd.CommandText = query;

            DbDataReader rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                result = GetRecord<T>(rd);
            }

            rd.Close();

            DbClose();
            return result;
        }
        // esegue na query e ritorna na List<T> usando generics e reflection
        public List<T> GetQueryResult<T>(string query)
        {
            List<T> result = new List<T>();

            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            T item = Activator.CreateInstance<T>();

            cmd.CommandText = query;

            DbDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                item = GetRecord<T>(rd);
                result.Add(item);
            }
            rd.Close();

            DbClose();
            return result;
        }

        //
        // zonta record
        //
        public void AddRecord<T>(T item, bool CloseConnection = true)
        {
            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();    // ex. sentiv.Models.SIZE            
            int DotPos = FullTypeName.LastIndexOf('.');         // ultimo "." trova'
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            //
            // prepare INSERT statement using class properties
            //

            // parameter marker char
            string parameterMarker = DbConn.GetSchema("DataSourceInformation").Rows[0]["ParameterMarkerFormat"].ToString().Substring(0,1);
            
            string query =
                "INSERT INTO " + TableName +
                " (" + string.Join(",",      
                    info.Where(x => x.GetCustomAttribute(typeof(AutoIncrementAttribute),false)==null) // non PK
                        .Select(x => x.Name)) + ") " +

                "VALUES (" + string.Join(",",
                    info.Where(x => x.GetCustomAttribute(typeof(AutoIncrementAttribute), false) == null) // non PK
                        .Select(x => parameterMarker + x.Name)) + ")";    // parametri

            cmd.CommandText = query;
            cmd.Parameters.Clear();
            foreach (PropertyInfo prop in info)
            {
                if (prop.GetCustomAttribute(typeof(AutoIncrementAttribute), false) == null)
                {
                    DbParameter parm = cmd.CreateParameter();
                    parm.ParameterName = prop.Name;
                    parm.Value = prop.GetValue(item);
                    cmd.Parameters.Add(parm);
                }
            }

            cmd.ExecuteNonQuery();

            if (CloseConnection)
            {
                DbClose();
            }
        }

        //
        // agiorna
        //
        public void UpdateRecord<T>(T item, string criteria)
        {
            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            PropertyInfo[] info = item.GetType().GetProperties();

            string FullTypeName = item.GetType().ToString();    // ex. sentiv.Models.SIZE            
            int DotPos = FullTypeName.LastIndexOf('.');
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            // parecia a INSERT usando e proprieta' dea clase
            string query = "UPDATE " + TableName + " SET ";

            foreach (PropertyInfo prop in info) // zonta i parametri
            {
                if (prop.GetCustomAttribute(typeof(PrimaryKeyAttribute), false) == null) // not a primary key => UPDATE field
                {
                    query += prop.Name + "=:" + prop.Name + ",";
                }
            }
            query = query.Remove(query.Length - 1, 1);  // cava a ultima ","
            query += " WHERE " + criteria;      // clausuea WHERE

            cmd.CommandText = query;
            cmd.Parameters.Clear();
            foreach (PropertyInfo prop in info) // vaeori dei parametri
            {
                if (prop.GetCustomAttribute(typeof(PrimaryKeyAttribute), false) == null) // no xe na ciave => zonta el vaeor del parametro
                {
                    DbParameter parm = cmd.CreateParameter();
                    parm.ParameterName = prop.Name;
                    parm.Value = prop.GetValue(item);
                    cmd.Parameters.Add(parm);
                }
            }

            cmd.ExecuteNonQuery();

            DbClose();
        }

        //
        // delete a record
        //
        public void DeleteRecord<T>(string criteria)
        {
            DbOpen();
            DbCommand cmd = this.DbConn.CreateCommand();

            T item = Activator.CreateInstance<T>(); // crea un ogeto de tipo T
            string FullTypeName = item.GetType().ToString();    // ex. sentiv.Models.SIZE            
            int DotPos = FullTypeName.LastIndexOf('.');         // ultimo "." trova'
            string TableName = (DotPos == -1) ? FullTypeName : FullTypeName.Substring(DotPos + 1, FullTypeName.Length - DotPos - 1);

            string query = $"DELETE FROM {TableName} WHERE {criteria}";

            cmd.CommandText = query;
            cmd.ExecuteNonQuery();

            DbClose();
        }
    }
}
