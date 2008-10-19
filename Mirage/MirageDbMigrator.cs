/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2008 Bertrand Lorentz <bertrand.lorentz@gmail.com>
 * with a lot of borrowing from the banshee source code.
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */


using System;
using System.Data;
using System.Reflection;
using Mono.Data;
using Mono.Data.SqliteClient;

namespace Mirage
{
    public class MirageDbMigrator
    {
        // NOTE: Whenever there is a change in ANY of the database schema,
        //       this version MUST be incremented and a migration method
        //       MUST be supplied to match the new version number.
        //       This method should return true if the step should allow 
        //       the driver to continue or return false if the step should 
        //       terminate the driver

        protected const int CURRENT_VERSION = 1;
        
        private Db mirage_db;
        
        public MirageDbMigrator(Db connection)
        {
            this.mirage_db = connection;
        }
        
#region Migration driver
        protected class DatabaseVersionAttribute : Attribute 
        {
            private int version;
            
            public DatabaseVersionAttribute(int version)
            {
                this.version = version;
            }
            
            public int Version {
                get { return version; }
            }
        }
        
        public void Migrate ()
        {
            try {
                if (DatabaseVersion < CURRENT_VERSION) {
                    Execute ("BEGIN");
                    InnerMigrate ();
                    Execute ("COMMIT");
                } else {
                    Dbg.WriteLine ("Mirage - Database version {0} is up to date", DatabaseVersion);
                }
            } catch (Exception e) {
                Dbg.WriteLine (e.ToString());
                Dbg.WriteLine ("Mirage - Rolling back database migration");
                Execute ("ROLLBACK");
                throw;
            }
        }
        
        private void InnerMigrate ()
        {
            MethodInfo [] methods = GetType ().GetMethods (BindingFlags.Instance | BindingFlags.NonPublic);
            bool terminate = false;
            bool ran_migration_step = false;
            
            Dbg.WriteLine ("Mirage - Migrating from database version {0} to {1}", DatabaseVersion, CURRENT_VERSION);
            for (int i = DatabaseVersion + 1; i <= CURRENT_VERSION; i++) {
                foreach (MethodInfo method in methods) {
                    foreach (DatabaseVersionAttribute attr in method.GetCustomAttributes (
                        typeof (DatabaseVersionAttribute), false)) {
                        if (attr.Version != i) {
                            continue;
                        }
                        
                        if (!ran_migration_step) {
                            ran_migration_step = true;
                        }

                        if (!(bool)method.Invoke (this, null)) {
                            terminate = true;
                        }
                        
                        break;
                    }
                }
                
                if (terminate) {
                    break;
                }
            }
            
            Execute (String.Format ("UPDATE MirageConfiguration SET Value = {0} WHERE Key = 'DatabaseVersion'", CURRENT_VERSION));
        }
        
        protected bool TableExists(string tableName)
        {
            int result = Convert.ToInt32 (mirage_db.ExecuteScalar (String.Format (
                "SELECT COUNT(*) FROM sqlite_master WHERE Type='table' AND Name='{0}'",
                tableName)));
            
            return result > 0;
        }
        
        protected void Execute(string query)
        {
            mirage_db.Execute (query);
        }
            
        protected int DatabaseVersion {
            get {
                if (!TableExists("MirageConfiguration")) {
                    return 0;
                }
                
                return Convert.ToInt32 (mirage_db.ExecuteScalar ("SELECT Value FROM MirageConfiguration WHERE Key = 'DatabaseVersion'"));
            }
        }
#endregion

#pragma warning disable 0169

#region Version 1
                                
        [DatabaseVersion (1)]
        private bool Migrate_1 ()
        {
            InitializeFreshDatabase ();
            mirage_db.was_reset = true;
            
            return false;
        }
#endregion

#pragma warning restore 0169
        
#region Fresh database setup
        
        private void InitializeFreshDatabase ()
        {
            Execute("DROP TABLE IF EXISTS mirage");
            Execute("DROP TABLE IF EXISTS MirageConfiguration");
            
            Execute (@"
                CREATE TABLE mirage (
                    trackid             INTEGER PRIMARY KEY,
                    scms                BLOB
                )
            ");

            Execute (@"
                CREATE TABLE MirageConfiguration (
                    EntryID             INTEGER PRIMARY KEY,
                    Key                 TEXT,
                    Value               TEXT
                )
            ");
            Execute (String.Format ("INSERT INTO MirageConfiguration VALUES (null, 'DatabaseVersion', {0})", CURRENT_VERSION));
        }
#endregion
    }
}
