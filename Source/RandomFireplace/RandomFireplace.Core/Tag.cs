using System;

using AirBreather.Core.Utilities;

namespace RandomFireplace.Core
{
    public struct Tag : IEquatable<Tag>
    {
        // purposely not making these readonly
        // I know the perf difference is practically negligible -- when you make your own
        // pet project, you can make them readonly... this one's mine, so there
        private long tagId;

        private string tagName;

        public Tag(long tagId, string tagName)
        {
            this.tagId = tagId;
            this.tagName = tagName;
        }

        public long TagId { get { return this.tagId; } }

        public string TagName { get { return this.tagName; } }

        public override bool Equals(object obj)
        {
            return obj is Tag &&
                   this.Equals((Tag)obj);
        }

        public bool Equals(Tag other)
        {
            return this.tagId == other.tagId;
        }

        public override int GetHashCode()
        {
            return this.tagId.GetHashCode();
        }

        public override string ToString()
        {
            return ToStringUtility.Begin(this)
                                  .AddProperty("TagId", this.tagId)
                                  .AddProperty("TagName", this.tagName)
                                  .End();
        }
    }
}