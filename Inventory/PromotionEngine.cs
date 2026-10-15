using Inventory.Models;

namespace Inventory
{
    // What the promotions give on the sale on the cashier screen
    public sealed class PromotionResult
    {
        public required decimal[] LineDiscounts { get; init; }   // per line, same order as the lines
        public required string?[] LineNames { get; init; }       // the promotion(s) a line got its discount from
        public decimal InvoiceDiscount { get; init; }
        public string? InvoiceName { get; init; }
    }

    // Works out the promotions (Item > Promotions) for the lines of a sale. Nothing is read or saved here.
    //
    //   Price cut     new price, or % off, per unit
    //   Multi-buy     every full group of N of the same item costs a fixed price
    //   Buy X get Y   in every group of X + Y, the cheapest Y are free
    //   Mix & match   as multi-buy, but the group is made across a list of items
    //   Category %    % off every item of the category
    //   Invoice       % off the total from a minimum, after the item promotions
    //
    // Rules when promotions meet:
    //   - a line with a discount typed by the cashier (F5) gets no promotion;
    //   - a unit is in one promotion only. Groups are made first, from the most expensive units (in the order the
    //     promotions were added); a group is only made when it is at least as good as the price cut / category %
    //     of its units. The units left over get the best of the price cuts and category % they are in;
    //   - items sold by weight get price cuts and category % only (a group needs whole pieces);
    //   - of the invoice promotions the sale reaches, the best one is used.
    public static class PromotionEngine
    {
        /// <param name="decimals">Decimals of the primary currency: the discounts are rounded to it.</param>
        public static PromotionResult Apply(IReadOnlyList<PosLine> lines, IReadOnlyList<PromotionModel> promotions, int decimals = 2)
        {
            var count = lines.Count;
            var discounts = new decimal[count];
            var names = new List<string>?[count];
            var itemSets = promotions.ToDictionary(p => p, p => p.PackagingIds.ToHashSet());

            bool IsFor(PromotionModel promotion, int line) =>
                lines[line].Discount == 0 && lines[line].Quantity > 0 && lines[line].UnitPrice > 0
                && itemSets[promotion].Contains(lines[line].PackagingId);

            void Give(int line, decimal amount, PromotionModel promotion)
            {
                if (amount <= 0) return;
                discounts[line] += amount;
                names[line] ??= new List<string>();
                if (!names[line]!.Contains(promotion.Name)) names[line]!.Add(promotion.Name);
            }

            // 1. The best price cut / category % of each line, per unit
            var unitOff = new decimal[count];
            var unitPromotion = new PromotionModel?[count];
            foreach (var promotion in promotions.Where(p => p.PromoType is PromotionType.PriceCut or PromotionType.Category))
                for (var i = 0; i < count; i++)
                {
                    if (!IsFor(promotion, i)) continue;
                    var price = lines[i].UnitPrice;
                    var off = promotion.PromoType == PromotionType.PriceCut && promotion.NewPrice is { } newPrice
                        ? price - newPrice
                        : price * (promotion.Percent ?? 0) / 100;
                    off = Math.Min(off, price);
                    if (off > unitOff[i]) (unitOff[i], unitPromotion[i]) = (off, promotion);
                }

            // 2. Groups of whole pieces: multi-buy, buy X get Y, mix & match
            var free = lines.Select(l => l.IsWeighted ? 0 : (int)Math.Floor(l.Quantity)).ToArray();   // pieces not in a group yet
            foreach (var promotion in promotions)
            {
                var size = promotion.PromoType switch
                {
                    PromotionType.MultiBuy or PromotionType.MixMatch => promotion.BuyQty ?? 0,
                    PromotionType.BuyXGetY => (promotion.BuyQty ?? 0) + (promotion.FreeQty ?? 0),
                    _ => 0,
                };
                if (size < 2) continue;

                var mine = Enumerable.Range(0, count).Where(i => IsFor(promotion, i) && free[i] > 0).ToList();
                // Multi-buy groups pieces of the same item; the others group across the list
                var pools = promotion.PromoType == PromotionType.MultiBuy
                    ? mine.GroupBy(i => lines[i].PackagingId).Select(g => g.ToList())
                    : new[] { mine }.AsEnumerable();

                foreach (var pool in pools)
                {
                    // One entry per piece, the most expensive first
                    var pieces = pool.SelectMany(i => Enumerable.Repeat(i, free[i]))
                                     .OrderByDescending(i => lines[i].UnitPrice).ToList();
                    for (var start = 0; start + size <= pieces.Count; start += size)
                    {
                        var group = pieces.GetRange(start, size);
                        var full = group.Sum(i => lines[i].UnitPrice);
                        var saves = promotion.PromoType == PromotionType.BuyXGetY
                            ? group.Skip(promotion.BuyQty ?? 0).Sum(i => lines[i].UnitPrice)   // the cheapest are last
                            : full - (promotion.GroupPrice ?? full);
                        if (saves <= 0 || saves < group.Sum(i => unitOff[i])) continue;

                        if (promotion.PromoType == PromotionType.BuyXGetY)
                        {
                            foreach (var i in group.Skip(promotion.BuyQty ?? 0)) Give(i, lines[i].UnitPrice, promotion);
                        }
                        else
                        {
                            // The saving is shared over the pieces by their price; the last one takes the rounding
                            var left = saves;
                            for (var n = 0; n < size; n++)
                            {
                                var share = n == size - 1 ? left : Math.Round(saves * lines[group[n]].UnitPrice / full, decimals);
                                Give(group[n], share, promotion);
                                left -= share;
                            }
                        }
                        foreach (var i in group) free[i]--;
                    }
                }
            }

            // 3. What is not in a group gets the line's price cut / category %
            for (var i = 0; i < count; i++)
            {
                if (unitPromotion[i] is not { } promotion) continue;
                var grouped = lines[i].IsWeighted ? 0 : (int)Math.Floor(lines[i].Quantity) - free[i];
                Give(i, (lines[i].Quantity - grouped) * unitOff[i], promotion);
            }

            var total = 0m;
            for (var i = 0; i < count; i++)
            {
                var gross = lines[i].Quantity * lines[i].UnitPrice - lines[i].Discount;
                discounts[i] = Math.Clamp(Math.Round(discounts[i], decimals), 0, Math.Max(gross, 0));
                total += Math.Round(gross - discounts[i], 2);
            }

            // 4. Invoice promotion: on the total after the item promotions
            var invoiceDiscount = 0m;
            string? invoiceName = null;
            foreach (var promotion in promotions.Where(p => p.PromoType == PromotionType.Invoice && total > 0 && total >= (p.MinTotal ?? 0)))
            {
                var off = Math.Round(total * (promotion.Percent ?? 0) / 100, decimals);
                if (off > invoiceDiscount) (invoiceDiscount, invoiceName) = (off, promotion.Name);
            }

            return new PromotionResult
            {
                LineDiscounts = discounts,
                LineNames = names.Select(n => n == null ? null : string.Join(" + ", n)).ToArray(),
                InvoiceDiscount = invoiceDiscount,
                InvoiceName = invoiceName,
            };
        }
    }
}
