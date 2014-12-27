using System;
using System.Data;
using System.Data.SQLite;
using System.Reactive.Linq;
using System.Threading.Tasks;

using AirBreather.Core.Utilities;

namespace RandomFireplace.Core
{
    public sealed class CardCatalog
    {
        private readonly string cardDatabaseConnectionString;

        public CardCatalog(string cardDatabasePath)
        {
            this.cardDatabaseConnectionString = "DataSource=" + cardDatabasePath + ";Version=3";
        }

        public IObservable<Card> FetchAllCards()
        {
            return Select("SELECT CardId, CardName FROM Card",
                          async r => new Card(await r.GetFieldValueAsync<long>(0).ConfigureAwait(false),
                                              await r.GetFieldValueAsync<string>(1).ConfigureAwait(false)));
        }

        public IObservable<Tag> FetchAllTags()
        {
            return Select("SELECT TagId, TagName FROM Tag",
                          async r => new Tag(await r.GetFieldValueAsync<long>(0).ConfigureAwait(false),
                                             await r.GetFieldValueAsync<string>(1).ConfigureAwait(false)));
        }

        public IObservable<CardWithMetadata> FetchAllCardMetadata()
        {
            return Select("SELECT CardId, TagId FROM Metadata",
                          async r => new CardWithMetadata(await r.GetFieldValueAsync<long>(0).ConfigureAwait(false),
                                                          await r.GetFieldValueAsync<long>(1).ConfigureAwait(false)));
        }

        private IObservable<T> Select<T>(string selectQuery, Func<SQLiteDataReader, Task<T>> selector)
        {
            return Observable.Using(() => new SQLiteConnection(this.cardDatabaseConnectionString),
                                    conn =>
                                    {
                                        conn.Open();
                                        return Observable.Using(() => new SQLiteCommand(conn),
                                                                selectCommand =>
                                                                {
                                                                    selectCommand.CommandType = CommandType.Text;
                                                                    selectCommand.CommandText = selectQuery;
                                                                    return Observable.Using(selectCommand.ExecuteReader,
                                                                                            reader => reader.ToObservable().Select(selector).Merge());
                                                                });
                                    });
        }
    }
}
