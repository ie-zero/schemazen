﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace model {
	using System.Text.RegularExpressions;

	public class Database {

		#region " Constructors "

		public Database() {
            Props.Add(new DbProp("COMPATIBILITY_LEVEL", ""));
            Props.Add(new DbProp("COLLATE", ""));
            Props.Add(new DbProp("AUTO_CLOSE", ""));
            Props.Add(new DbProp("AUTO_SHRINK", ""));
            Props.Add(new DbProp("ALLOW_SNAPSHOT_ISOLATION", ""));
            Props.Add(new DbProp("READ_COMMITTED_SNAPSHOT", ""));
            Props.Add(new DbProp("RECOVERY", ""));
            Props.Add(new DbProp("PAGE_VERIFY", ""));
            Props.Add(new DbProp("AUTO_CREATE_STATISTICS", ""));
            Props.Add(new DbProp("AUTO_UPDATE_STATISTICS", ""));
            Props.Add(new DbProp("AUTO_UPDATE_STATISTICS_ASYNC", ""));
            Props.Add(new DbProp("ANSI_NULL_DEFAULT", ""));
            Props.Add(new DbProp("ANSI_NULLS", ""));          
            Props.Add(new DbProp("ANSI_PADDING", ""));
            Props.Add(new DbProp("ANSI_WARNINGS", ""));
            Props.Add(new DbProp("ARITHABORT", ""));
            Props.Add(new DbProp("CONCAT_NULL_YIELDS_NULL", ""));
            Props.Add(new DbProp("NUMERIC_ROUNDABORT", ""));
            Props.Add(new DbProp("QUOTED_IDENTIFIER", ""));
            Props.Add(new DbProp("RECURSIVE_TRIGGERS", ""));
            Props.Add(new DbProp("CURSOR_CLOSE_ON_COMMIT", ""));
            Props.Add(new DbProp("CURSOR_DEFAULT", ""));
            Props.Add(new DbProp("TRUSTWORTHY", ""));
            Props.Add(new DbProp("DB_CHAINING", ""));
            Props.Add(new DbProp("PARAMETERIZATION", ""));
            Props.Add(new DbProp("DATE_CORRELATION_OPTIMIZATION", ""));
		}

		public Database(string name) : this() {
			this.Name = name;
		}

		#endregion

		#region " Properties "

		public string Connection = "";
		public string Dir = "";
		public string Name;
        
        public List<DbProp> Props = new List<DbProp>();        
		public List<Table> Tables = new List<Table>();
		public List<Routine> Routines = new List<Routine>();
		public List<ForeignKey> ForeignKeys = new List<ForeignKey>();
		public List<Table> DataTables = new List<Table>();

        public DbProp FindProp(string name) {
            foreach (DbProp p in Props) {                
                if (p.Name.ToUpper() == name.ToUpper()) return p;
            }
            return null;
        }

		public Table FindTable(string name) {
			foreach (Table t in Tables) {
				if (t.Name == name) return t;
			}
			return null;
		}

		public Table FindTableByPk(string name) {
			foreach (Table t in Tables) {
				if (t.PrimaryKey != null && t.PrimaryKey.Name == name) {
					return t;
				}
			}
			return null;
		}

		public Constraint FindConstraint(string name) {
			foreach (Table t in Tables) {
				foreach (Constraint c in t.Constraints) {
					if (c.Name == name) return c;
				}
			}
			return null;
		}

		public ForeignKey FindForeignKey(string name) {
			foreach (ForeignKey fk in ForeignKeys) {
				if (fk.Name == name) return fk;
			}
			return null;
		}

		public Routine FindRoutine(string name) {
			foreach (Routine r in Routines) {
				if (r.Name == name) return r;
			}
			return null;
		}

		public List<Table> FindTablesRegEx(string pattern) {
			List<Table> matches = new List<Table>();
			foreach (Table t in Tables) {
				if (Regex.Match(t.Name, pattern).Success) {
					matches.Add(t);
				}
			}
			return matches;
		}

		#endregion

        private void SetPropOnOff(string propName, object dbVal) {            
            if (dbVal != DBNull.Value) {
                FindProp(propName).Value = (bool)dbVal ? "ON" : "OFF";
            }
        }
        private void SetPropString(string propName, object dbVal) {
            if (dbVal != DBNull.Value) {
                FindProp(propName).Value = dbVal.ToString();
            }
        }

		public void Load() {
            var cnStrBuilder = new SqlConnectionStringBuilder(Connection);            

			Tables.Clear();
			Routines.Clear();
			ForeignKeys.Clear();
			DataTables.Clear();
			using (SqlConnection cn = new SqlConnection(Connection)) {
				cn.Open();
				using (SqlCommand cm = cn.CreateCommand()) {                 
                    // query schema for database properties
                    cm.CommandText = @"
select
    [compatibility_level],
    [collation_name],
    [is_auto_close_on],
    [is_auto_shrink_on],
    [snapshot_isolation_state],
    [is_read_committed_snapshot_on],
    [recovery_model_desc],
    [page_verify_option_desc],
    [is_auto_create_stats_on],
    [is_auto_update_stats_on],
    [is_auto_update_stats_async_on],
    [is_ansi_null_default_on],
    [is_ansi_nulls_on],
    [is_ansi_padding_on],
    [is_ansi_warnings_on],
    [is_arithabort_on],
    [is_concat_null_yields_null_on],
    [is_numeric_roundabort_on],
    [is_quoted_identifier_on],
    [is_recursive_triggers_on],
    [is_cursor_close_on_commit_on],
    [is_local_cursor_default],
    [is_trustworthy_on],
    [is_db_chaining_on],
    [is_parameterization_forced],
    [is_date_correlation_on]
from sys.databases
where name = @dbname
";
                    cm.Parameters.AddWithValue("@dbname", cnStrBuilder.InitialCatalog);
                    using (System.Data.IDataReader dr = cm.ExecuteReader()) {
                        if (dr.Read()) {
                            SetPropString("COMPATIBILITY_LEVEL", dr["compatibility_level"]);
                            SetPropString("COLLATE", dr["collation_name"]);
                            SetPropOnOff("AUTO_CLOSE", dr["is_auto_close_on"]);
                            SetPropOnOff("AUTO_SHRINK", dr["is_auto_shrink_on"]);
                            if (dr["snapshot_isolation_state"] != DBNull.Value) {                                
                                FindProp("ALLOW_SNAPSHOT_ISOLATION").Value = 
                                    (byte)dr["snapshot_isolation_state"] == 0 || 
                                    (byte)dr["snapshot_isolation_state"] == 2 ? "OFF" : "ON";
                            }                           
                            SetPropOnOff("READ_COMMITTED_SNAPSHOT", dr["is_read_committed_snapshot_on"]);
                            SetPropString("RECOVERY", dr["recovery_model_desc"]);
                            SetPropString("PAGE_VERIFY", dr["page_verify_option_desc"]);
                            SetPropOnOff("AUTO_CREATE_STATISTICS", dr["is_auto_create_stats_on"]);
                            SetPropOnOff("AUTO_UPDATE_STATISTICS", dr["is_auto_update_stats_on"]);
                            SetPropOnOff("AUTO_UPDATE_STATISTICS_ASYNC", dr["is_auto_update_stats_async_on"]);
                            SetPropOnOff("ANSI_NULL_DEFAULT", dr["is_ansi_null_default_on"]);
                            SetPropOnOff("ANSI_NULLS", dr["is_ansi_nulls_on"]);
                            SetPropOnOff("ANSI_PADDING", dr["is_ansi_padding_on"]);
                            SetPropOnOff("ANSI_WARNINGS", dr["is_ansi_warnings_on"]);
                            SetPropOnOff("ARITHABORT", dr["is_arithabort_on"]);
                            SetPropOnOff("CONCAT_NULL_YIELDS_NULL", dr["is_concat_null_yields_null_on"]);
                            SetPropOnOff("NUMERIC_ROUNDABORT", dr["is_numeric_roundabort_on"]);
                            SetPropOnOff("QUOTED_IDENTIFIER", dr["is_quoted_identifier_on"]);
                            SetPropOnOff("RECURSIVE_TRIGGERS", dr["is_recursive_triggers_on"]);
                            SetPropOnOff("CURSOR_CLOSE_ON_COMMIT", dr["is_cursor_close_on_commit_on"]);
                            if (dr["is_local_cursor_default"] != DBNull.Value){
                                FindProp("CURSOR_DEFAULT").Value =
                                    (bool)dr["is_local_cursor_default"] ? "LOCAL" : "GLOBAL";
                            }
                            SetPropOnOff("TRUSTWORTHY", dr["is_trustworthy_on"]);
                            SetPropOnOff("DB_CHAINING", dr["is_db_chaining_on"]);
                            if (dr["is_parameterization_forced"] != DBNull.Value) {
                                FindProp("PARAMETERIZATION").Value =
                                    (bool)dr["is_parameterization_forced"] ? "FORCED" : "SIMPLE";
                            }
                            SetPropOnOff("DATE_CORRELATION_OPTIMIZATION", dr["is_date_correlation_on"]);
                        }
                    }
                                       

                    //get tables
					cm.CommandText = @"
					select 
						TABLE_SCHEMA, 
						TABLE_NAME 
					from INFORMATION_SCHEMA.TABLES
                    where TABLE_TYPE = 'BASE TABLE'";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Tables.Add(new Table((string)dr["TABLE_SCHEMA"], (string)dr["TABLE_NAME"]));
						}
					}

					//get columns
					cm.CommandText = @"
					select 
						c.TABLE_NAME,
						c.COLUMN_NAME,
						c.DATA_TYPE,
						c.IS_NULLABLE,
						c.CHARACTER_MAXIMUM_LENGTH,
						c.NUMERIC_PRECISION,
						c.NUMERIC_SCALE 
					from INFORMATION_SCHEMA.COLUMNS c
                        inner join INFORMATION_SCHEMA.TABLES t
                                on t.TABLE_NAME = c.TABLE_NAME
                                    and t.TABLE_SCHEMA = c.TABLE_SCHEMA
                                    and t.TABLE_CATALOG = c.TABLE_CATALOG
                    where
                        t.TABLE_TYPE = 'BASE TABLE'
";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Column c = new Column();
							c.Name = (string)dr["COLUMN_NAME"];
							c.Type = (string)dr["DATA_TYPE"];
							c.IsNullable = (string)dr["IS_NULLABLE"] == "YES";

							switch (c.Type) {
								case "binary":
								case "char":
								case "nchar":
								case "nvarchar":
								case "varbinary":
								case "varchar":
									c.Length = (int)dr["CHARACTER_MAXIMUM_LENGTH"];
									break;
								case "decimal":
								case "numeric":
									c.Precision = (byte)dr["NUMERIC_PRECISION"];
									c.Scale =  (int)dr["NUMERIC_SCALE"];
									break;
							}

							FindTable((string)dr["TABLE_NAME"]).Columns.Add(c);
						}
					}

					//get column identities
					cm.CommandText = @"
					select 
						t.name as TABLE_NAME, 
						c.name AS COLUMN_NAME,
						i.SEED_VALUE, i.INCREMENT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.identity_columns i on i.object_id = c.object_id
							and i.column_id = c.column_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
                            try
                            {
                                Table t = FindTable((string)dr["TABLE_NAME"]);
                                var c = t.Columns.Find((string)dr["COLUMN_NAME"]);
                                var seed = dr["SEED_VALUE"].ToString();
                                var increment = dr["INCREMENT_VALUE"].ToString();
                                c.Identity = new Identity(seed, increment);
                            } catch (Exception ex){
                                throw new ApplicationException(string.Format("{0} : {1}", (string)dr["TABLE_NAME"], ex.Message), ex);
                            }
						}
					}

					//get column defaults
					cm.CommandText = @"
					select 
						t.name as TABLE_NAME, 
						c.name as COLUMN_NAME, 
						d.name as DEFAULT_NAME, 
						d.definition as DEFAULT_VALUE
					from sys.tables t 
						inner join sys.columns c on c.object_id = t.object_id
						inner join sys.default_constraints d on c.column_id = d.parent_column_id
							and d.parent_object_id = c.object_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Table t = FindTable((string)dr["TABLE_NAME"]);
							t.Columns.Find((string)dr["COLUMN_NAME"]).Default = 
								new Default((string)dr["DEFAULT_NAME"], (string)dr["DEFAULT_VALUE"]);
						}
					}

					//get constraints & indexes
					cm.CommandText = @"
					select 
						t.name as tableName, 
						i.name as indexName, 
						c.name as columnName,
						i.is_primary_key, 
						i.is_unique_constraint,
                        i.is_unique, 
						i.type_desc,
                        isnull(ic.is_included_column, 0) as is_included_column
					from sys.tables t 
						inner join sys.indexes i on i.object_id = t.object_id
						inner join sys.index_columns ic on ic.object_id = t.object_id
							and ic.index_id = i.index_id
						inner join sys.columns c on c.object_id = t.object_id
							and c.column_id = ic.column_id
					order by t.name, i.name, ic.key_ordinal, ic.index_column_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Constraint c = FindConstraint((string)dr["indexName"]);
							if (c == null) {
								c = new Constraint((string)dr["indexName"], "", "");
								Table t = FindTable((string)dr["tableName"]);
								t.Constraints.Add(c);
								c.Table = t;
							}
							c.Clustered = (string)dr["type_desc"] == "CLUSTERED";
                            c.Unique = (bool)dr["is_unique"];
                            if ((bool)dr["is_included_column"]) {
                                c.IncludedColumns.Add((string)dr["columnName"]);
                            } else {
                                c.Columns.Add((string)dr["columnName"]);
                            }
							
							c.Type = "INDEX";
							if ((bool)dr["is_primary_key"]) c.Type = "PRIMARY KEY";
							if ((bool)dr["is_unique_constraint"]) c.Type = "UNIQUE";
						}
					}

					//get foreign keys
					cm.CommandText = @"
					select 
						TABLE_NAME, 
						CONSTRAINT_NAME 
					from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
					where CONSTRAINT_TYPE = 'FOREIGN KEY'";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Table t = FindTable((string)dr["TABLE_NAME"]);
							ForeignKey fk = new ForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.Table = t;
							ForeignKeys.Add(fk);
						}
					}

					//get foreign key props
					cm.CommandText = @"
select 
	CONSTRAINT_NAME, 
	UNIQUE_CONSTRAINT_NAME, 
	UPDATE_RULE, 
	DELETE_RULE,
	fk.is_disabled
from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	inner join sys.foreign_keys fk on rc.CONSTRAINT_NAME = fk.name";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							fk.RefTable = FindTableByPk((string)dr["UNIQUE_CONSTRAINT_NAME"]);							
							fk.OnUpdate = (string)dr["UPDATE_RULE"];
							fk.OnDelete = (string)dr["DELETE_RULE"];
                            fk.Check = !(bool)dr["is_disabled"];
						}
					}

					//get foreign key columns
					cm.CommandText = @"
select
	fk.name as CONSTRAINT_NAME,
	c.name as COLUMN_NAME,
	rc.name as REF_COLUMN_NAME
from
	sys.foreign_key_columns fkc	
		inner join sys.foreign_keys fk 
			on fk.object_id = fkc.constraint_object_id	
		inner join sys.columns c
			on fkc.parent_object_id = c.object_id
				and fkc.parent_column_id = c.column_id
		inner join sys.columns rc
			on fkc.referenced_object_id = rc.object_id
				and fkc.referenced_column_id = rc.column_id
order by fkc.constraint_column_id
";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							ForeignKey fk = FindForeignKey((string)dr["CONSTRAINT_NAME"]);
							if (fk != null) fk.Columns.Add((string)dr["COLUMN_NAME"]);
                            if (fk != null) fk.RefColumns.Add((string)dr["REF_COLUMN_NAME"]);
						}
					}

					//get routines
					cm.CommandText = @"
					select
						s.name as schemaName,
						o.name as routineName,
						o.type_desc,
						m.definition,
                        m.uses_ansi_nulls,
						m.uses_quoted_identifier,
						t.name as tableName
					from sys.sql_modules m
						inner join sys.objects o on m.object_id = o.object_id
						inner join sys.schemas s on s.schema_id = o.schema_id
						left join sys.triggers tr on m.object_id = tr.object_id
						left join sys.tables t on tr.parent_id = t.object_id";
					using (SqlDataReader dr = cm.ExecuteReader()) {
						while (dr.Read()) {
							Routine r = new Routine((string)dr["schemaName"], (string)dr["routineName"]);
							r.Text = (string)dr["definition"];
                            r.AnsiNull = (bool)dr["uses_ansi_nulls"];
                            r.QuotedId = (bool)dr["uses_quoted_identifier"];
							Routines.Add(r);

							switch ((string)dr["type_desc"]) {
								case "SQL_STORED_PROCEDURE":
									r.Type = "PROCEDURE";
									break;
								case "SQL_TRIGGER":
									r.Type = "TRIGGER";
									break;
								case "SQL_SCALAR_FUNCTION":
									r.Type = "FUNCTION";
									break;
							}
						}
					}
				}
			}
		}

		public DatabaseDiff Compare(Database db) {
			DatabaseDiff diff = new DatabaseDiff();

            //compare database properties           
            foreach (DbProp p in Props) {
                DbProp p2 = db.FindProp(p.Name);
                if (p.Script() != p2.Script()) {
                    diff.PropsChanged.Add(p);
                }                
            }
            
			//get tables added and changed
			foreach (Table t in Tables) {
				Table t2 = db.FindTable(t.Name);
				if (t2 == null) {
					diff.TablesAdded.Add(t);
				} else {
					//compare mutual tables
					TableDiff tDiff = t.Compare(t2);
					if (tDiff.IsDiff) {
						diff.TablesDiff.Add(tDiff);
					}
				}
			}
			//get deleted tables
			foreach (Table t in db.Tables) {
				if (FindTable(t.Name) == null) {
					diff.TablesDeleted.Add(t);
				}
			}

			//get procs added and changed
			foreach (Routine r in Routines) {
				Routine r2 = db.FindRoutine(r.Name);
				if (r2 == null) {
					diff.RoutinesAdded.Add(r);
				} else {
					//compare mutual procs
					if (r.Text != r2.Text) {
						diff.RoutinesDiff.Add(r);
					}
				}
			}
			//get procs deleted
			foreach (Routine r in db.Routines) {
				if (FindRoutine(r.Name) == null) {
					diff.RoutinesDeleted.Add(r);
				}
			}

			//get added and compare mutual foreign keys
			foreach (ForeignKey fk in ForeignKeys) {
				ForeignKey fk2 = db.FindForeignKey(fk.Name);
				if (fk2 == null) {
					diff.ForeignKeysAdded.Add(fk);
				} else {
					if (fk.ScriptCreate() != fk2.ScriptCreate()) {
						diff.ForeignKeysDiff.Add(fk);
					}
				}
			}
			//get deleted foreign keys
			foreach (ForeignKey fk in db.ForeignKeys) {
				if (FindForeignKey(fk.Name) == null) {
					diff.ForeignKeysDeleted.Add(fk);
				}
			}

			return diff;
		}

		public string ScriptCreate() {
			StringBuilder text = new StringBuilder();

			text.AppendFormat("CREATE DATABASE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendFormat("USE {0}", Name);
			text.AppendLine();
			text.AppendLine("GO");
			text.AppendLine();

            if (Props.Count > 0) {
                text.Append(ScriptPropList(Props));
                text.AppendLine("GO");
                text.AppendLine();
            }

			foreach (Table t in Tables) {
				text.AppendLine(t.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (ForeignKey fk in ForeignKeys) {
				text.AppendLine(fk.ScriptCreate());
			}
			text.AppendLine();
			text.AppendLine("GO");

			foreach (Routine r in Routines) {
				text.AppendLine(r.ScriptCreate());
				text.AppendLine();
				text.AppendLine("GO");
			}

			return text.ToString();
		}

		private static string[] dirs = { "tables", "foreign_keys", "functions", "procs", "triggers" };

		public void ScriptToDir(bool overwrite) {
			if (Directory.Exists(Dir)) {		
				// delete the existing script files
				foreach (string dir in dirs) {
					if (!Directory.Exists(Dir + "/" + dir)) break;
					foreach (string f in Directory.GetFiles(Dir + "/" + dir)) {
						File.Delete(f);
					}
				}
			}
			// create dir tree
			foreach (string dir in dirs) {
				if (!Directory.Exists(Dir + "/" + dir)) {
					Directory.CreateDirectory(Dir + "/" + dir);
				}
			}

            var text = new StringBuilder();
            text.Append(ScriptPropList(Props));
            text.AppendLine("GO");
            text.AppendLine();
            File.WriteAllText(string.Format("{0}/props.sql", Dir),
                text.ToString());
            
			foreach (Table t in Tables) {
				File.WriteAllText(
					String.Format("{0}/tables/{1}.sql", Dir, t.Name),
					t.ScriptCreate() + "\r\nGO\r\n"
				);
			}

			foreach (ForeignKey fk in ForeignKeys) {
				File.AppendAllText(
					String.Format("{0}/foreign_keys/{1}.sql", Dir, fk.Table.Name),
					fk.ScriptCreate() + "\r\nGO\r\n"
				);
			}

			foreach (Routine r in Routines) {
				string dir = "procs";
				if (r.Type == "TRIGGER") { dir = "triggers"; }
				if (r.Type == "FUNCTION") { dir = "functions"; }
				File.WriteAllText(
					String.Format("{0}/{1}/{2}.sql", Dir, dir, r.Name),
					r.ScriptCreate() + "\r\nGO\r\n"
				);
			}

			ExportData();
		}

		public void ExportData() {
			var dataDir = Dir + "/data";
			if (!Directory.Exists(dataDir)) {
				Directory.CreateDirectory(dataDir);
			}			
			foreach (Table t in DataTables) {
				File.WriteAllText(dataDir + "/" + t.Name, t.ExportData(Connection));
			}
		}

		public void ImportData() {
			var dataDir = Dir + "\\data";
			var tables = new List<Table>();
			if (!Directory.Exists(dataDir)) { return; }
			foreach (string f in Directory.GetFiles(dataDir)) {
				var t = FindTable(f.Replace(String.Concat(dataDir,"\\"), ""));
				if (t == null) { continue; }
				try {
					t.ImportData(Connection, File.ReadAllText(dataDir + "/" + t.Name));
				} catch (DataException ex) {
					throw new DataFileException(ex.Message, f, ex.LineNumber);
				}
			}
		}

		public void CreateFromDir(bool overwrite) {
			var cnBuilder = new SqlConnectionStringBuilder(Connection);
			if (DBHelper.DbExists(Connection)) {				
				DBHelper.DropDb(Connection);
			}

			//create database
			DBHelper.CreateDb(Connection);

			//run scripts
            if (File.Exists(Dir + "/props.sql")) {
                try {
                    DBHelper.ExecBatchSql(Connection, File.ReadAllText(Dir + "/props.sql"));
                } catch (SqlBatchException ex) {
                    throw new SqlFileException(Dir + "/props.sql", ex);
                }

                // COLLATE can cause connection to be reset
                // so clear the pool so we get a new connection
                DBHelper.ClearPool(Connection);
            }
			foreach (string dir in dirs) {
				if ("foreign_keys" == dir) { continue; }
				var dirPath = Dir + "/" + dir;
				if (!Directory.Exists(dirPath)) { continue; }

				foreach (string f in Directory.GetFiles(dirPath, "*.sql")) {
					try {
						DBHelper.ExecBatchSql(Connection, File.ReadAllText(f));
					} catch (SqlBatchException ex) {
						throw new SqlFileException(f, ex);
					}
				}
			}
								
			Load();			// load the schema first so we can import data
			ImportData();	// load data

			// foreign keys
			if (Directory.Exists(Dir + "/foreign_keys")) {
				foreach (string f in Directory.GetFiles(Dir + "/foreign_keys", "*.sql")) {
					try {
						DBHelper.ExecBatchSql(Connection, File.ReadAllText(f));
					} catch (SqlBatchException ex) {
						throw new SqlFileException(f, ex);
					}
				}
			}
		}

		public void ExecCreate(bool dropIfExists) {
			var conStr = new SqlConnectionStringBuilder(Connection);
			var dbName = conStr.InitialCatalog;
			conStr.InitialCatalog = "master";
			if (DBHelper.DbExists(Connection)) {
				if (dropIfExists) {
					DBHelper.DropDb(Connection);
				} else {
					throw new ApplicationException(String.Format("Database {0} {1} already exists.",
						conStr.DataSource, dbName));
				}
			}
			DBHelper.ExecBatchSql(conStr.ToString(), ScriptCreate());
		}

        public static string ScriptPropList(IList<DbProp> props) {
            var text = new StringBuilder();
            
            text.AppendLine("DECLARE @DB VARCHAR(255)");
            text.AppendLine("SET @DB = DB_NAME()");
            foreach (DbProp p in props) {
                if (!string.IsNullOrEmpty(p.Script())) {
                    text.AppendLine(p.Script());
                }
            }
            return text.ToString();
        }
	}

	public class DatabaseDiff {
        public List<DbProp> PropsChanged = new List<DbProp>();

		public List<Table> TablesAdded = new List<Table>();
		public List<TableDiff> TablesDiff = new List<TableDiff>();
		public List<Table> TablesDeleted = new List<Table>();

		public List<Routine> RoutinesAdded = new List<Routine>();
		public List<Routine> RoutinesDiff = new List<Routine>();
		public List<Routine> RoutinesDeleted = new List<Routine>();

		public List<ForeignKey> ForeignKeysAdded = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDiff = new List<ForeignKey>();
		public List<ForeignKey> ForeignKeysDeleted = new List<ForeignKey>();

		public bool IsDiff {
			get {
				return PropsChanged.Count > 0
                    || TablesAdded.Count > 0
					|| TablesDiff.Count > 0
					|| TablesDeleted.Count > 0
					|| RoutinesAdded.Count > 0
					|| RoutinesDiff.Count > 0
					|| RoutinesDeleted.Count > 0
					|| ForeignKeysAdded.Count > 0
					|| ForeignKeysDiff.Count > 0
					|| ForeignKeysDeleted.Count > 0;
			}
		}

		public string Script() {
			StringBuilder text = new StringBuilder();
            //alter database props
            //TODO need to check dependencies for collation change
            //TODO how can collation be set to null at the server level?
            if (PropsChanged.Count > 0) {
                text.Append(Database.ScriptPropList(PropsChanged));
                text.AppendLine("GO");
                text.AppendLine();
            }

			//delete foreign keys
			if (ForeignKeysDeleted.Count + ForeignKeysDiff.Count > 0) {
				foreach (ForeignKey fk in ForeignKeysDeleted) {
					text.AppendLine(fk.ScriptDrop());
				}
				//delete modified foreign keys
				foreach (ForeignKey fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptDrop());
				}
				text.AppendLine("GO");
			}
			
			//add tables
			if (TablesAdded.Count > 0) {
				foreach (Table t in TablesAdded) {
					text.Append(t.ScriptCreate());
				}
				text.AppendLine("GO");
			}
			
			//modify tables
			if (TablesDiff.Count > 0) {
				foreach (TableDiff t in TablesDiff) {
					text.Append(t.Script());
				}
				text.AppendLine("GO");
			}
			
			//delete tables
			if (TablesDeleted.Count > 0) {
				foreach (Table t in TablesDeleted) {
					text.AppendLine(t.ScriptDrop());
				}
				text.AppendLine("GO");
			}
			
			//add foreign keys
			if (ForeignKeysAdded.Count + ForeignKeysDiff.Count > 0) {
				foreach (ForeignKey fk in ForeignKeysAdded) {
					text.AppendLine(fk.ScriptCreate());
				}
				//add modified foreign keys
				foreach (ForeignKey fk in ForeignKeysDiff) {
					text.AppendLine(fk.ScriptCreate());
				}
				text.AppendLine("GO");
			}
			
			//add & delete procs, functions, & triggers
			foreach (Routine r in RoutinesAdded) {
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (Routine r in RoutinesDiff) {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
				text.AppendLine(r.ScriptCreate());
				text.AppendLine("GO");
			}
			foreach (Routine r in RoutinesDeleted) {
				text.AppendLine(r.ScriptDrop());
				text.AppendLine("GO");
			}

			return text.ToString();
		}
	}
}
