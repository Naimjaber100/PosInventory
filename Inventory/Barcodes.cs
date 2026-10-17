namespace Inventory
{
    // Other ways the same barcode label can be written, for a scan that does not match exactly
    // (the same rules as fn_barcode_variants, Database/product_functions.sql):
    //   - without its last digit, when that digit is the right check digit (EAN-13 / EAN-8 / UPC-A, GS1):
    //     the item was saved without it, the scanner sends it;
    //   - without a leading 0 (EAN-13 "0" + UPC-A) and with one (the other way round).
    // Only digits, 7 or more; anything else has no other way.
    public static class Barcodes
    {
        public static IEnumerable<string> Variants(string code)
        {
            if (code.Length < 7 || !code.All(char.IsAsciiDigit)) yield break;
            if (code[^1] - '0' == CheckDigit(code[..^1])) yield return code[..^1];
            if (code[0] == '0') yield return code[1..];
            yield return "0" + code;
        }

        /// <summary>The GS1 check digit of these digits: from the right they are weighted 3, 1, 3, 1...</summary>
        public static int CheckDigit(string digits)
        {
            var sum = 0;
            for (var i = 0; i < digits.Length; i++)
                sum += (digits[digits.Length - 1 - i] - '0') * (i % 2 == 0 ? 3 : 1);
            return (10 - sum % 10) % 10;
        }
    }
}
