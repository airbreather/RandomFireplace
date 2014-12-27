using System;

using AirBreather.Core.Utilities;

namespace RandomFireplace.Core
{
    public struct Card : IEquatable<Card>
    {
        // purposely not making these readonly
        // I know the perf difference is practically negligible -- when you make your own
        // pet project, you can make them readonly... this one's mine, so there
        private long cardId;

        private string cardName;

        public Card(long cardId, string cardName)
        {
            this.cardId = cardId;
            this.cardName = cardName;
        }

        public long CardId { get { return this.cardId; } }

        public string CardName { get { return this.cardName; } }

        public override bool Equals(object obj)
        {
            return obj is Card &&
                   this.Equals((Card)obj);
        }

        public bool Equals(Card other)
        {
            return this.cardId == other.cardId;
        }

        public override int GetHashCode()
        {
            return this.cardId.GetHashCode();
        }

        public override string ToString()
        {
            return ToStringUtility.Begin(this)
                                  .AddProperty("CardId", this.cardId)
                                  .AddProperty("CardName", this.cardName)
                                  .End();
        }
    }
}
