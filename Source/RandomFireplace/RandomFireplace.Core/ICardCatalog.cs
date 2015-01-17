using System.Collections.Generic;

namespace RandomFireplace.Core
{
    public interface ICardCatalog
    {
        IEnumerable<Card> FetchAllCards();

        IEnumerable<Tag> FetchAllTags();

        IEnumerable<CardMetadata> FetchAllCardMetadata();

        IEnumerable<CardMetadata> FetchCardMetadataForTagIds(IEnumerable<long> tagIds);
    }
}