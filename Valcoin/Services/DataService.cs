using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Services
{
    internal static class DataService
    {
        private static SQLiteAsyncConnection db;

        private static async Task Init()
        {
            if (db != null)
                return;

            var databasePath = "C971Task1.db";
            db = new SQLiteAsyncConnection(databasePath);
#if DEBUG
            // In debug, it's nice to start from a fresh DB each time
            // being in a getter, the UI sometimes loads before the DB is refreshed.
            // just pull to refresh the UI. This doesn't really matter in release
            // since we won't want to clean the DB on initialization

            await db.ExecuteAsync($"drop table if exists {nameof(Term)}");
            await db.ExecuteAsync($"drop table if exists {nameof(Course)}");
            await db.ExecuteAsync($"drop table if exists {nameof(Instructor)}");
            await db.ExecuteAsync($"drop table if exists {nameof(Assessment)}");
#endif
            await db.CreateTableAsync<Term>();
            await db.CreateTableAsync<Course>();
            await db.CreateTableAsync<Instructor>();
            await db.CreateTableAsync<Assessment>();
        }
    }
}
