﻿using System;

using AirBreather.Core.Utilities;

namespace RandomFireplace.Core
{
    public struct CardWithMetadata : IEquatable<CardWithMetadata>
    {
        // purposely not making these readonly
        // I know the perf difference is practically negligible -- when you make your own
        // pet project, you can make them readonly... this one's mine, so there
        private long cardId;

        private long tagId;

        public CardWithMetadata(long cardId, long tagId)
        {
            this.cardId = cardId;
            this.tagId = tagId;
        }

        public long CardId { get { return this.cardId; } }

        public long TagId { get { return this.tagId; } }

        public override bool Equals(object obj)
        {
            return obj is CardWithMetadata &&
                   this.Equals((CardWithMetadata)obj);
        }

        public bool Equals(CardWithMetadata other)
        {
            return this.cardId == other.cardId &&
                   this.tagId == other.tagId;
        }

        public override int GetHashCode()
        {
            return HashCodeUtility.Seed
                                  .HashWith(this.cardId)
                                  .HashWith(this.tagId);
        }

        public override string ToString()
        {
            return ToStringUtility.Begin(this)
                                  .AddProperty("CardId", this.cardId)
                                  .AddProperty("TagId", this.tagId)
                                  .End();
        }
    }
}