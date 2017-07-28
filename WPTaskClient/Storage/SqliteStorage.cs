using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite.Internal;
using System.Collections.Immutable;
using Json = Windows.Data.Json;

namespace WPTaskClient.Storage
{
    public class SqliteStorage
    {
        private static readonly SqliteConnection connection = new SqliteConnection("Filename=tasks.db");

        public static void Init()
        {
            SqliteEngine.UseWinSqlite3(); // use builtin Sqlite library
            using (connection)
            {
                // TODO: handle SqliteException (but how, reasonably?)
                connection.Open();
                new SqliteCommand("CREATE TABLE IF NOT EXISTS tasks ( uuid TEXT PRIMARY KEY, task TEXT NOT NULL )", connection).ExecuteNonQuery();
                new SqliteCommand("CREATE TABLE IF NOT EXISTS sync_backlog ( backlog_id INTEGER PRIMARY KEY AUTOINCREMENT, sync_key TEXT, task TEXT NOT NULL )", connection).ExecuteNonQuery();
                new SqliteCommand("CREATE TABLE IF NOT EXISTS metadata ( key TEXT PRIMARY KEY, value TEXT NOT NULL )", connection).ExecuteNonQuery();
            }
        }

        async public static Task UpsertTask(Data.Task task)
        {
            var taskJsonString = task.ToJson().ToString();
            using (connection)
            {
                // TODO: handle SqliteException
                await connection.OpenAsync();
                var tx = connection.BeginTransaction();
                var insert = new SqliteCommand(@"
                    INSERT OR REPLACE INTO tasks (uuid, task) VALUES (@uuid, @task);
                    INSERT INTO sync_backlog(sync_key, task) VALUES((SELECT value FROM metadata WHERE key = 'sync_key'), @task);
                ", connection, tx);
                insert.Parameters.AddWithValue("@uuid", task.Uuid);
                insert.Parameters.AddWithValue("@task", taskJsonString);
                await insert.ExecuteNonQueryAsync();
                tx.Commit();
            }
        }

        async public static Task<ImmutableList<Data.Task>> GetTasks()
        {
            using (connection)
            {
                // TODO: handle SqliteException
                await connection.OpenAsync();
                var reader = await new SqliteCommand("SELECT (task) FROM tasks", connection).ExecuteReaderAsync();
                var builder = ImmutableList.CreateBuilder<Data.Task>();
                while(await reader.ReadAsync())
                {
                    builder.Add(Data.Task.FromJson(Json.JsonObject.Parse(reader.GetString(0))));
                }
                return builder.ToImmutable();
            }
        }
    }
}
