using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

using AirBreather.Core.Utilities;

using RandomFireplace.Core;

namespace RandomFireplace
{
    internal static class Program
    {
        private static void Main()
        {
            int seed;
            Console.WriteLine("Welcome to Random Fireplace 0.1.0.0.  Would you like to input a seed? (y/[n])");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.Y:
                    Console.WriteLine("Please enter the seed, then press Enter:");
                    seed = Int32.Parse(Console.ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;

                default:
                    seed = Guid.NewGuid().GetHashCode();
                    break;
            }

            Console.WriteLine("Now select the file that contains the card database (build it with JSONTranslator)");
            string filePath = Console.ReadLine();

            var catalog = new CardCatalog(filePath);
            IDictionary<long, Card> cardMapping = catalog.FetchAllCards()
                                                         .ToDictionary(card => card.CardId)
                                                         .Wait();

            IDictionary<long, Tag> tagMapping = catalog.FetchAllTags()
                                                       .ToDictionary(tag => tag.TagId)
                                                       .Wait();

            Console.WriteLine("The following are the ids of all tags.  Select the ids of tags to include in the pool, comma-separated (e.g.: \"6,8\"):");
            foreach (var tag in tagMapping.Values
                                          .OrderBy(tag => tag.TagId))
            {
                Console.WriteLine("{0}: {1}", tag.TagId, tag.TagName);
            }

            HashSet<long> tagIds = new HashSet<long>(Console.ReadLine().Split(',').Select(x => Int64.Parse(x, NumberStyles.None, CultureInfo.InvariantCulture)));

            ILookup<long, long> cardIdToTagIdsLookup = catalog.FetchAllCardMetadata()
                                                              .ToLookup(metadata => metadata.CardId, metadata => metadata.TagId)
                                                              .Wait();

            long[] includedCardIds = cardIdToTagIdsLookup.Where(grp => grp.Any(tagIds.Contains))
                                                         .Select(grp => grp.Key)
                                                         .Distinct()
                                                         .OrderBy(_ => Guid.NewGuid())
                                                         .ToArray();

            long commonTagId = tagMapping.Values.Single(tag => String.Equals("common", tag.TagName, StringComparison.OrdinalIgnoreCase)).TagId;
            long rareTagId = tagMapping.Values.Single(tag => String.Equals("rare", tag.TagName, StringComparison.OrdinalIgnoreCase)).TagId;
            long epicTagId = tagMapping.Values.Single(tag => String.Equals("epic", tag.TagName, StringComparison.OrdinalIgnoreCase)).TagId;
            long legendaryTagId = tagMapping.Values.Single(tag => String.Equals("legendary", tag.TagName, StringComparison.OrdinalIgnoreCase)).TagId;

            ILookup<long, long> rarityTagIdToIncludedCardIdsLookup = includedCardIds.SelectMany(cardId => cardIdToTagIdsLookup[cardId].Select(tagId => new CardWithMetadata(cardId, tagId)))
                                                                                    .Where(metadata => metadata.TagId == commonTagId ||
                                                                                                       metadata.TagId == rareTagId ||
                                                                                                       metadata.TagId == epicTagId ||
                                                                                                       metadata.TagId == legendaryTagId)
                                                                                    .ToLookup(metadata => metadata.TagId, metadata => metadata.CardId);

            long[] picks = new long[30];

            long[] choices = new long[3];

            HashSet<long> singlyPicked = new HashSet<long>();
            HashSet<long> doublyPicked = new HashSet<long>();

            Random rand = new Random(seed);
            for (int pick = 0; pick < 30; pick++)
            {
                string rarityText = String.Empty;
                long rarity = commonTagId;

                // On most rounds, there's a 10% chance to upgrade to rare.
                // 20% of rounds that upgrade to rare also upgrade to epic.
                // 20% of rounds that upgrade to epic also upgrade to legendary.
                const double DefaultUpgradeToRareTarget = 0.1;
                const double UpgradeToEpicTarget = 0.2;
                const double UpgradeToLegendaryTarget = 0.2;

                double upgradeToRareTarget = DefaultUpgradeToRareTarget;

                // The first, tenth, twentieth, and thirtieth rounds
                // automatically upgrade to rare, no matter what.
                switch (pick)
                {
                    case 0:
                    case 9:
                    case 19:
                    case 29:
                        upgradeToRareTarget = 1;
                        break;
                }

                if (rand.NextDouble() < upgradeToRareTarget)
                {
                    rarity = rareTagId;
                    rarityText = "(Rare!)";

                    if (rand.NextDouble() < UpgradeToEpicTarget)
                    {
                        rarity = epicTagId;
                        rarityText = "(EPIC!)";

                        if (rand.NextDouble() < UpgradeToLegendaryTarget)
                        {
                            rarity = legendaryTagId;
                            rarityText = "(WHOA, LEGENDARY!!!)";
                        }
                    }
                }

                long[] available = rarityTagIdToIncludedCardIdsLookup[rarity].ExceptWhere(doublyPicked.Contains)
                                                                             .OrderBy(x => x)
                                                                             .ToArray();

                // Note how crazy this would start to get above 3 per round.
                // I might actually have to think really hard if I didn't feel like
                // hand-waving and saying "all rounds for Irvine-family strategies have 3 cards".
                int first = rand.Next(available.Length);
                int second = rand.Next(available.Length - 1);
                int third = rand.Next(available.Length - 2);

                if (second >= first)
                {
                    second++;
                }

                if (third >= Math.Min(first, second))
                {
                    third++;
                }

                if (third >= Math.Max(first, second))
                {
                    third++;
                }

                choices[0] = available[first];
                choices[1] = available[second];
                choices[2] = available[third];

                bool picked = false;
                while (!picked)
                {
                    Console.Clear();
                    Console.WriteLine("Round {0:00} / 30    {1}", pick + 1, rarityText);
                    Console.WriteLine("Seed: {0}", seed);
                    Console.WriteLine("1: " + cardMapping[choices[0]].CardName);
                    Console.WriteLine("2: " + cardMapping[choices[1]].CardName);
                    Console.WriteLine("3: " + cardMapping[choices[2]].CardName);
                    Console.WriteLine();
                    Console.WriteLine("Make your choice.");

                    Console.WriteLine("1, 2, or 3: pick that card");
                    Console.WriteLine("r: restart this draft");
                    Console.WriteLine("q: exit");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Cards picked so far:");
                    Console.WriteLine();
                    for (int i = 0; i < pick; i++)
                    {
                        Console.WriteLine(cardMapping[picks[i]].CardName);
                    }

                    // SOMEHOW, I get the feeling that this could have been
                    // just a little bit nicer... maybe that's just my imagination.
                    var keyInfo = Console.ReadKey(true);
                    long pickedCard;
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.D1:
                        case ConsoleKey.NumPad1:
                            picked = true;
                            pickedCard = choices[0];
                            break;

                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                            picked = true;
                            pickedCard = choices[1];
                            break;

                        case ConsoleKey.D3:
                        case ConsoleKey.NumPad3:
                            picked = true;
                            pickedCard = choices[2];
                            break;

                        case ConsoleKey.Q:
                            return;

                        case ConsoleKey.R:
                            picked = true;
                            singlyPicked.Clear();
                            doublyPicked.Clear();
                            pick = -1;
                            rand = new Random(seed);
                            continue;

                        default:
                            continue;
                    }

                    picks[pick] = pickedCard;
                    if (singlyPicked.Remove(pickedCard))
                    {
                        doublyPicked.Add(pickedCard);
                    }
                    else
                    {
                        singlyPicked.Add(pickedCard);
                    }
                }
            }

            Console.Clear();
            Console.WriteLine("Your final draft was:");
            Console.WriteLine();
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(cardMapping[picks[i]].CardName);
            }

            Console.WriteLine();
            Console.WriteLine("The seed for this draft was: {0}", seed);
            Console.WriteLine();
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();
        }
    }
}
