using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Text;
using System.IO;

namespace Valcoin_Core
{
    public class DbHandler
    {
        //private void InitNewDBStructure()
        //{
        //    using (var db = new SqliteConnection("Filename=blockchain.db"))
        //    {
        //        db.Open();
        //        var commandText =
        //            @"
        //            CREATE TABLE blocks (
        //                block_id INTEGER PRIMARY KEY,
        //                block_number INTEGER NOT NULL,
        //                block_version INTEGER NOT NULL,
        //                block_datetime TEXT NOT NULL,
        //                block_difficulty TEXT NOT NULL,
        //                block_root_hash TEXT NOT NULL,
        //                block_nonce BLOB NOT NULL
        //            );
        //        ";
        //        var createTable = new SqliteCommand(commandText, db);
        //        createTable.ExecuteReader().Close();


        //        // verify it was created successfully
        //        createTable.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        //        using (var reader = createTable.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                Console.WriteLine($"Found databases: {reader.GetString(0)}");
        //            }
        //            reader.Close();
        //        }
        //        db.Close();
        //    }
        //}

        //public void CreateDBConnection()
        //{
        //    if (!File.Exists("blockchain.db"))
        //    {
        //        InitNewDBStructure();
        //    }
        //}

        //public void WriteBlockToDb(Block block)
        //{
        //    using (var db = new SqliteConnection("Filename=blockchain.db"))
        //    {

        //    }
        //}
    }
}
