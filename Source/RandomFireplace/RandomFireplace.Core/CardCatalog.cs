using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

using AirBreather.Core.Utilities;

namespace RandomFireplace.Core
{
    public sealed class CardCatalog : ICardCatalog
    {
        private readonly string cardDatabaseConnectionString;

        public CardCatalog(string cardDatabasePath)
        {
            this.cardDatabaseConnectionString = "DataSource=" + cardDatabasePath + ";Version=3";
        }

        public IEnumerable<Card> FetchAllCards()
        {
            return Select("SELECT CardId, CardName FROM Card",
                          r => new Card(r.GetInt64(0), r.GetString(1)));
        }

        public IEnumerable<Tag> FetchAllTags()
        {
            return Select("SELECT TagId, TagName FROM Tag",
                          r => new Tag(r.GetInt64(0), r.GetString(1)));
        }

        // This is probably going away soon.
        // Other than ease of me getting an initial thing working quickly,
        // there's no need for this once we have FetchCardMetadatForTagIds.
        public IEnumerable<CardMetadata> FetchAllCardMetadata()
        {
            return Select("SELECT CardId, TagId FROM Metadata",
                          r => new CardMetadata(r.GetInt64(0), r.GetInt64(1)));
        }

        public IEnumerable<CardMetadata> FetchCardMetadataForTagIds(IEnumerable<long> tagIds)
        {
            if (tagIds == null)
            {
                throw new ArgumentNullException("tagIds");
            }

            // This was using ToHashSet to make extra-special-sure that we only
            // return metadata with the given tags, even if SQLite doesn't
            // filter properly... but if we can't trust SQLite, then we're not
            // exactly in fantastic shape anyway, now, are we?
            string filter = String.Join(",", tagIds.Select(x => x.ToString(CultureInfo.InvariantCulture)));

            if (filter.Length == 0)
            {
                // No need to run a query that we know will return no values.
                return Enumerable.Empty<CardMetadata>();
            }

            return Select("SELECT CardId, TagId FROM Metadata WHERE TagId IN (" + filter + ")",
                          r => new CardMetadata(r.GetInt64(0), r.GetInt64(1)));
        }

        private IEnumerable<T> Select<T>(string selectQuery, Func<IDataReader, T> selector)
        {
            using (var conn = new SQLiteConnection(this.cardDatabaseConnectionString))
            using (var selectCommand = new SQLiteCommand(conn))
            {
                selectCommand.CommandType = CommandType.Text;
                selectCommand.CommandText = selectQuery;
                conn.Open();
                using (var reader = selectCommand.ExecuteReader())
                {
                    // Important: do not just return .ToEnumerable().Select(selector) here.
                    // That would dispose the things too early.
                    foreach (var result in reader.ToEnumerable().Select(selector))
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}
