namespace Inventory.Validation
{
    // Field limits shared by Company, Client and Supplier
    public static class InputRules
    {
        public const int MaxNameLength = 50;
        public const int MaxAddressLength = 200;
        public const int MaxPhoneDigits = 15;

        /// <summary>Phone / mobile box: digits only, a "+" allowed in front, at most MaxPhoneDigits digits.</summary>
        public static void UsePhoneInput(TextBox box)
        {
            box.MaxLength = MaxPhoneDigits + 1;   // "+" and the digits
            box.KeyPress += (_, e) =>
            {
                if (char.IsControl(e.KeyChar) || char.IsAsciiDigit(e.KeyChar)) return;
                if (e.KeyChar == '+' && box.SelectionStart == 0 && !box.Text.Contains('+')) return;
                e.Handled = true;
            };
        }

        /// <summary>True for an empty phone or digits only ("+" allowed in front), at most MaxPhoneDigits digits (pasted text is not filtered while typing).</summary>
        public static bool IsPhone(string phone)
        {
            var digits = phone.StartsWith('+') ? phone[1..] : phone;
            return digits.All(char.IsAsciiDigit) && digits.Length <= MaxPhoneDigits;
        }

        public const string PhoneProblem = "The phone number can only have digits (a \"+\" is allowed in front), at most 15 digits.";

        /// <summary>The first problem with a name, address and phone, and the box to fix; null when all is right.</summary>
        public static (string Problem, Control Field)? Check(TextBox name, TextBox address, TextBox phone)
        {
            var n = name.Text.Trim();
            var a = address.Text.Trim();
            if (n.Length > MaxNameLength) return ($"The name can have at most {MaxNameLength} characters (now {n.Length}).", name);
            if (a.Length > MaxAddressLength) return ($"The address can have at most {MaxAddressLength} characters (now {a.Length}).", address);
            if (!IsPhone(phone.Text.Trim())) return (PhoneProblem, phone);
            return null;
        }
    }
}
