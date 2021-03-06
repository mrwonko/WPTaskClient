﻿using System;
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

        async public static Task UpsertTask(Data.Task task, bool addToBacklog = true)
        {
            var taskJsonString = task.ToJson().ToString();
            using (connection)
            {
                // TODO: handle SqliteException
                await connection.OpenAsync();
                using (var tx = connection.BeginTransaction())
                {
                    var sql = "INSERT OR REPLACE INTO tasks (uuid, task) VALUES (@uuid, @task);";
                    if (addToBacklog)
                    {
                        sql += "\nINSERT INTO sync_backlog(sync_key, task) VALUES ((SELECT value FROM metadata WHERE key = 'sync_key'), @task);";
                    }
                    var insert = new SqliteCommand(sql, connection, tx);
                    insert.Parameters.AddWithValue("@uuid", task.Uuid.ToString());
                    insert.Parameters.AddWithValue("@task", taskJsonString);
                    await insert.ExecuteNonQueryAsync();
                    tx.Commit();
                }
            }
        }

        async public static Task<Data.SyncBacklog> GetSyncBacklog()
        {
            using (connection)
            {
                string syncKey;
                var tasksBuilder = ImmutableList.CreateBuilder<string>();
                await connection.OpenAsync();
                using (var tx = connection.BeginTransaction())
                {
                    using (var reader = await new SqliteCommand("SELECT value FROM metadata WHERE key = 'sync_key';", connection, tx).ExecuteReaderAsync())
                    {
                        syncKey = await reader.ReadAsync() ? reader.GetString(0) : null;
                    }
                    var select = new SqliteCommand("SELECT task FROM sync_backlog WHERE sync_key " + (syncKey == null ? "IS NULL;" : "= @syncKey;"), connection, tx);
                    if (syncKey != null)
                    {
                        select.Parameters.AddWithValue("@syncKey", ((object)syncKey) ?? DBNull.Value);
                    }
                    using (var reader = await select.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tasksBuilder.Add(reader.GetString(0));
                        }
                    }
                    tx.Commit();
                }
                return new Data.SyncBacklog
                {
                    SyncKey = syncKey,
                    Tasks = tasksBuilder.ToImmutable(),
                };
            }
        }

        async public static Task SetSyncKey(string syncKey)
        {
            using (connection)
            {
                await connection.OpenAsync();
                using (var tx = connection.BeginTransaction())
                {
                    // this is somewhat annoying, but the cleanest way to handle it:
                    // on our first sync, we send no local changes (because that isn't idempotent for whatever stupid reason), we only store the sync key
                    // but that means we need to change the sync key on our existing sync backlog to stay idempotent
                    // (thank god for transactions)
                    // on subsequent syncs, we clear our sync backlog
                    string oldSyncKey = null;
                    var reader = await new SqliteCommand("SELECT value FROM metadata WHERE key = 'sync_key';", connection, tx).ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        oldSyncKey = reader.GetString(0);
                        break;
                    }
                    if (oldSyncKey == null)
                    {
                        var update = new SqliteCommand("UPDATE sync_backlog SET sync_key = @syncKey WHERE sync_key IS NULL;", connection, tx);
                        update.Parameters.AddWithValue("@syncKey", syncKey);
                        await update.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        var delete = new SqliteCommand("DELETE FROM sync_backlog WHERE sync_key = @syncKey;", connection, tx);
                        delete.Parameters.AddWithValue("@syncKey", syncKey);
                        await delete.ExecuteNonQueryAsync();
                    }
                    var insert = new SqliteCommand("INSERT OR REPLACE INTO metadata (key, value) VALUES ('sync_key', @value);", connection, tx);
                    insert.Parameters.AddWithValue("@value", syncKey);
                    await insert.ExecuteNonQueryAsync();
                    tx.Commit();
                }
            }
        }

        async public static Task<ImmutableList<Data.Task>> GetTasks()
        {
            using (connection)
            {
                // TODO: handle SqliteException
                await connection.OpenAsync();
                return await ReadTasks(await new SqliteCommand("SELECT (task) FROM tasks", connection).ExecuteReaderAsync());
            }
        }

        async private static Task<ImmutableList<Data.Task>> ReadTasks(SqliteDataReader reader)
        {
            using (reader)
            {
                var builder = ImmutableList.CreateBuilder<Data.Task>();
                while (await reader.ReadAsync())
                {
                    builder.Add(Data.Task.FromJson(Json.JsonObject.Parse(reader.GetString(0))));
                }
                return builder.ToImmutable();
            }
        }
    }
}
