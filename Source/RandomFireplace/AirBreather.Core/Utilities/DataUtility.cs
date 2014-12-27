using System;
using System.Data;
using System.Data.Common;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace AirBreather.Core.Utilities
{
    public static class DataUtility
    {
        public static IObservable<IDataReader> ToObservable(this IDataReader reader)
        {
            return Observable.Create<IDataReader>(obs =>
                                                  {
                                                      try
                                                      {
                                                          while (reader.Read())
                                                          {
                                                              obs.OnNext(reader);
                                                          }

                                                          obs.OnCompleted();
                                                      }
                                                      catch (Exception e)
                                                      {
                                                          obs.OnError(e);
                                                      }

                                                      return Disposable.Empty;
                                                  });
        }

        public static IObservable<IDataReader> ToObservable(this IDataReader reader, CancellationToken cancellationToken)
        {
            return Observable.Create<IDataReader>(obs =>
                                                  {
                                                      try
                                                      {
                                                          // Slightly different than normal here,
                                                          // so that we break on cancellation at
                                                          // the exact right times.
                                                          while (true)
                                                          {
                                                              cancellationToken.ThrowIfCancellationRequested();
                                                              if (!reader.Read())
                                                              {
                                                                  break;
                                                              }

                                                              obs.OnNext(reader);
                                                          }

                                                          obs.OnCompleted();
                                                      }
                                                      catch (Exception e)
                                                      {
                                                          obs.OnError(e);
                                                      }

                                                      return Disposable.Empty;
                                                  });
        }

        // Specializations for DbDataReader, because that has async versions of everything.
        public static IObservable<TReader> ToObservable<TReader>(this TReader reader)
            where TReader : DbDataReader
        {
            return reader.ToObservable(CancellationToken.None);
        }

        public static IObservable<TReader> ToObservable<TReader>(this TReader reader, CancellationToken cancellationToken)
            where TReader : DbDataReader
        {
            return Observable.Create<TReader>(async obs =>
                                                    {
                                                        try
                                                        {
                                                            while (await reader.ReadAsync(cancellationToken)
                                                                               .ConfigureAwait(false))
                                                            {
                                                                obs.OnNext(reader);
                                                            }

                                                            obs.OnCompleted();
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            obs.OnError(e);
                                                        }

                                                        return Disposable.Empty;
                                                    });
        }
    }
}
