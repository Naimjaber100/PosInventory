using System.ComponentModel;
using System.Globalization;

namespace Inventory.Models
{
    // Row returned by fn_currency_get_all: a currency and its latest rate (in the primary currency)
    public class CurrencyModel
    {
        [DisplayName("Code")] public string Code { get; set; } = string.Empty;
        [DisplayName("Name")] public string Name { get; set; } = string.Empty;
        [DisplayName("Symbol")] public string Symbol { get; set; } = string.Empty;
        [DisplayName("Decimals")] public short Decimals { get; set; } = 2;
        [DisplayName("Primary")] public bool IsPrimary { get; set; }
        [DisplayName("Active")] public bool IsActive { get; set; } = true;
        [Browsable(false)] public int SortOrder { get; set; }
        [Browsable(false)] public decimal? Rate { get; set; }        // 1 unit = Rate of the primary currency; 1 for the primary
        [DisplayName("Exchange rate")] public string RateText { get; set; } = string.Empty;   // filled by CurrencySet
        [DisplayName("Rate set on")]
        public DateTime? RateAt
        {
            get => _rateAt;
            set => _rateAt = value?.Kind == DateTimeKind.Utc ? value.Value.ToLocalTime() : value;
        }
        private DateTime? _rateAt;
        [DisplayName("Set by")] public string? RateBy { get; set; }

        /// <summary>Has a rate, so amounts can be taken or given back in it.</summary>
        [Browsable(false)] public bool HasRate => IsPrimary || Rate > 0;

        /// <summary>"45,000 LBP", "$ 0.50", "€ 1.00": a one-character symbol goes in front.</summary>
        public string Format(decimal amount)
        {
            var number = Math.Round(amount, Decimals).ToString(Decimals == 0 ? "#,0" : "#,0." + new string('0', Decimals), CultureInfo.CurrentCulture);
            return Symbol.Length == 1 ? $"{Symbol} {number}" : $"{number} {Symbol}";
        }
    }

    // Row returned by fn_currency_rates: one rate of a currency's history
    public class CurrencyRateModel
    {
        [DisplayName("Set on")]
        public DateTime EffectiveAt
        {
            get => _effectiveAt;
            set => _effectiveAt = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        }
        private DateTime _effectiveAt;
        [DisplayName("Rate")] public decimal Rate { get; set; }
        [DisplayName("Set by")] public string? SetBy { get; set; }
    }

    // The currencies the screens work with: the primary one (all amounts are in it), the second one
    // (shown next to totals, USD today) and those that can be paid in (active, with a rate).
    public class CurrencySet
    {
        public CurrencySet(IEnumerable<CurrencyModel> all)
        {
            All = all.ToList();
            Primary = All.FirstOrDefault(c => c.IsPrimary) ?? new CurrencyModel { Code = "LBP", Name = "Lebanese pound", Symbol = "LBP", Decimals = 0, IsPrimary = true, Rate = 1 };
            if (!All.Contains(Primary)) All.Insert(0, Primary);
            Second = All.Where(c => c.IsActive && !c.IsPrimary).OrderBy(c => c.SortOrder).ThenBy(c => c.Code).FirstOrDefault();
            foreach (var c in All)
                c.RateText = c.IsPrimary ? "primary" : c.Rate > 0 ? $"1 {c.Code} = {Primary.Format(c.Rate.Value)}" : "no rate yet";
        }

        /// <summary>LBP primary, USD without a rate: used until the database answers.</summary>
        public static CurrencySet Default { get; } = new(new[]
        {
            new CurrencyModel { Code = "LBP", Name = "Lebanese pound", Symbol = "LBP", Decimals = 0, IsPrimary = true, Rate = 1 },
            new CurrencyModel { Code = "USD", Name = "US dollar", Symbol = "$", Decimals = 2, SortOrder = 1 },
        });

        public List<CurrencyModel> All { get; }
        public CurrencyModel Primary { get; }
        public CurrencyModel? Second { get; }

        /// <summary>Active currencies with a rate, primary first.</summary>
        public IEnumerable<CurrencyModel> Payable =>
            All.Where(c => c.IsActive && c.HasRate).OrderBy(c => !c.IsPrimary).ThenBy(c => c.SortOrder).ThenBy(c => c.Code);

        /// <summary>True when the second currency exists but has no rate yet (sales cannot be saved).</summary>
        public bool SecondRateMissing => Second != null && !Second.HasRate;

        public CurrencyModel? Find(string code) => All.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        /// <summary>An amount in a currency, in the primary currency (as fn_save_sale converts it).</summary>
        public decimal ToPrimary(decimal amount, string code) =>
            Find(code) is { HasRate: true } c ? Math.Round(amount * (c.IsPrimary ? 1 : c.Rate!.Value), 2) : 0;

        /// <summary>A primary amount in another currency, rounded up to its decimals (so it is enough).</summary>
        public decimal FromPrimary(decimal primaryAmount, CurrencyModel currency)
        {
            if (currency.IsPrimary || !(currency.Rate > 0)) return primaryAmount;
            var factor = (decimal)Math.Pow(10, currency.Decimals);
            return Math.Ceiling(primaryAmount / currency.Rate.Value * factor) / factor;
        }

        public string FormatPrimary(decimal amount) => Primary.Format(amount);

        /// <summary>A primary amount shown in the second currency ("$ 0.50"), or empty when there is none.</summary>
        public string FormatSecond(decimal primaryAmount) =>
            Second is { Rate: > 0 } s ? s.Format(primaryAmount / s.Rate.Value) : string.Empty;

        /// <summary>"1 USD = 89,500 LBP" for the second currency (empty when there is none).</summary>
        public string SecondRateText => Second?.RateText ?? string.Empty;
    }
}
