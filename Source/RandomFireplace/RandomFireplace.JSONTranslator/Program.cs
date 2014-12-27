using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

using AirBreather.Core.Text;
using AirBreather.Core.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RandomFireplace.JSONTranslator
{
    internal static class Program
    {
        // Hopefully the fact that this is all basically one method
        // gives some insight as to how frequently you're supposed to use it
        // and how bulletproof I feel that it is.
        private static void Main()
        {
            // grab an example from http://hearthstonejson.com/json/AllSets.enUS.json
            Console.WriteLine("Type in the file path where your JSON file is stored and then hit Enter.");
            string jsonPath = Console.ReadLine();

            FileInfo file = null;
            while (file == null ||
                   file.Extension != ".sqlite")
            {
                Console.WriteLine("Now type in the file path where you want your SQLite database to be stored and then hit Enter.  Must have a .sqlite extension.");
                string fileName = Console.ReadLine();
                file = new FileInfo(fileName);
            }

            SQLiteConnection.CreateFile(file.FullName);

            // This is the only metadata we currently care about for the arena.
            // If we wanted more, then we would add those here.
            string[] metadata =
            {
                "Free",
                "Common",
                "Rare",
                "Epic",
                "Legendary",
                "Neutral",
                "Druid",
                "Hunter",
                "Mage",
                "Paladin",
                "Priest",
                "Rogue",
                "Shaman",
                "Warlock",
                "Warrior"
            };

            using (var conn = new SQLiteConnection("Data Source=" + file.FullName + ";Version=3"))
            {
                conn.Open();
                using (var comm = conn.CreateCommand())
                {
                    comm.CommandText = "CREATE TABLE Card (CardId INTEGER PRIMARY KEY AUTOINCREMENT, CardOfficialKey VARCHAR(255) UNIQUE, CardName NVARCHAR(255) NOT NULL)";
                    comm.CommandType = CommandType.Text;
                    comm.ExecuteNonQuery();

                    comm.CommandText = "CREATE TABLE Tag (TagId INTEGER PRIMARY KEY AUTOINCREMENT, TagName NVARCHAR(255) UNIQUE)";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "CREATE TABLE Metadata (TagId INTEGER NOT NULL REFERENCES Tag(TagId), CardId INTEGER NOT NULL REFERENCES Card(CardId), PRIMARY KEY (TagId, CardId))";
                    comm.ExecuteNonQuery();

                    comm.CommandText = "INSERT INTO Tag (TagName) VALUES (@TagName)";
                    var tagNameParameter = comm.CreateParameter();
                    tagNameParameter.ParameterName = "TagName";
                    comm.Parameters.Add(tagNameParameter);

                    foreach (string item in metadata)
                    {
                        tagNameParameter.Value = item;
                        comm.ExecuteNonQuery();
                    }
                }

                JObject cards;
                using (var fileStream = File.OpenRead(jsonPath))
                using (var fileStreamReader = new StreamReader(fileStream, EncodingEx.UTF8NoBOM))
                using (var jsonReader = new JsonTextReader(fileStreamReader))
                {
                    cards = JObject.Load(jsonReader);
                }

                var pq = from JProperty set in cards.Properties()
                         from card in set.Value
                         select card;

                // We'll do this in three stages:
                // 1. Create the list of inserts to the Metadata table in memory.
                // 2. Sort the inserts in-memory for proper insertion order.
                // 3. Insert the things.
                List<CardMetadata> metadataToInsert = new List<CardMetadata>();

                using (var cardInsertCommand = conn.CreateCommand())
                using (var cardIdSelectCommand = conn.CreateCommand())
                {
                    cardInsertCommand.CommandType = CommandType.Text;
                    cardInsertCommand.CommandText = "INSERT INTO Card (CardOfficialKey, CardName) VALUES (@CardOfficialKey, @CardName)";

                    SQLiteParameter officialKeyParameter = new SQLiteParameter("@CardOfficialKey");
                    cardInsertCommand.Parameters.Add(officialKeyParameter);

                    SQLiteParameter nameParameter = new SQLiteParameter("@CardName");
                    cardInsertCommand.Parameters.Add(nameParameter);

                    cardIdSelectCommand.CommandType = CommandType.Text;
                    cardIdSelectCommand.CommandText = "SELECT last_insert_rowid()";

                    foreach (var card in pq)
                    {
                        if (!card.Value<bool>("collectible") ||
                            card.Value<string>("type") == "Hero")
                        {
                            continue;
                        }

                        officialKeyParameter.Value = card.Value<string>("id");
                        nameParameter.Value = card.Value<string>("name");
                        cardInsertCommand.ExecuteNonQuery();

                        // Metadata time.
                        CardMetadata m = new CardMetadata();
                        ushort cardId = Convert.ToUInt16(cardIdSelectCommand.ExecuteScalar());
                        m.CardId = cardId;

                        // Class
                        string playerClass = card.Value<string>("playerClass") ?? "Neutral";
                        m.TagId = (ushort)(Array.IndexOf(metadata, playerClass) + 1);
                        metadataToInsert.Add(m);

                        // Rarity
                        string rarity = card.Value<string>("rarity");
                        m.TagId = (ushort)(Array.IndexOf(metadata, rarity) + 1);

                        // Aren't mutable structs fun?
                        metadataToInsert.Add(m);

                        // If we wanted more metadata from the cards, then
                        // this is where we would deal with that.
                    }
                }

                using (var metadataInsertCommand = conn.CreateCommand())
                {
                    metadataInsertCommand.CommandType = CommandType.Text;
                    metadataInsertCommand.CommandText = "INSERT INTO Metadata (CardId, TagId) VALUES (@CardId, @TagId)";

                    SQLiteParameter cardIdParameter = new SQLiteParameter("@CardId");
                    metadataInsertCommand.Parameters.Add(cardIdParameter);

                    SQLiteParameter tagIdParameter = new SQLiteParameter("@TagId");
                    metadataInsertCommand.Parameters.Add(tagIdParameter);

                    foreach (var m in metadataToInsert.OrderBy(x => x.TagId).ThenBy(x => x.CardId))
                    {
                        tagIdParameter.Value = m.TagId;
                        cardIdParameter.Value = m.CardId;
                        metadataInsertCommand.ExecuteNonQuery();
                    }
                }

                // Just to prove this is usable, let's use this DB to select the cards
                // that would be available to druids, and output their rarities while we're at it.
                // Rarity tag IDs are 1, 2, 3, 4, and 5.
                // Druid cards are neutrals (tag id 6) and druid class cards (tag id 7).
                const string SelectCardsForDruidsQuery = @"
                                                         SELECT
                                                             c.CardName,
                                                             rarityTag.TagName
                                                         FROM
                                                             Card c
                                                         JOIN
                                                             Metadata mRarity
                                                         ON
                                                             mRarity.CardId = c.CardId
                                                         JOIN
                                                             Tag rarityTag
                                                         ON
                                                             rarityTag.TagId = mRarity.TagId
                                                         WHERE
                                                             rarityTag.TagId IN (1, 2, 3, 4, 5) AND
                                                             EXISTS
                                                             (
                                                                 SELECT 1
                                                                 FROM
                                                                     Metadata mClass
                                                                 WHERE
                                                                     mClass.TagId IN (6, 7) AND
                                                                     mClass.CardId = c.CardId
                                                             )";
                using (var selComm = conn.CreateCommand())
                {
                    selComm.CommandType = CommandType.Text;
                    selComm.CommandText = SelectCardsForDruidsQuery;
                    using (IDataReader reader = selComm.ExecuteReader())
                    {
                        int cardNameOrdinal = 0;
                        int rarityOrdinal = 1;

                        Console.WriteLine("Druid cards:");
                        while (reader.Read())
                        {
                            string cardName = reader.GetString(cardNameOrdinal);
                            string rarity = reader.GetString(rarityOrdinal);
                            Console.WriteLine("{0} ({1})", cardName, rarity);
                        }
                    }
                }
            }
        }

        private struct CardMetadata
        {
            // we could be making a ton of these all in memory at once,
            // so try to make... OK, fine, I admit, this is just more fun.
            private uint data;

            internal ushort CardId
            {
                get { return (ushort)(this.data & 0x0000FFFF); }
                set
                {
                    this.data &= 0xFFFF0000;
                    this.data |= value;
                }
            }

            internal ushort TagId
            {
                get { return (ushort)(this.data >> 16); }
                set
                {
                    this.data &= 0x0000FFFF;
                    this.data |= ((uint)value) << 16;
                }
            }

            public override string ToString()
            {
                return ToStringUtility.Begin(this)
                                      .AddProperty("CardId", this.CardId)
                                      .AddProperty("TagId", this.TagId)
                                      .End();
            }
        }
    }
}
