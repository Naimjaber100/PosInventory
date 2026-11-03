using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Inventory.Licensing
{
    // One license per PC, made by the License app (C:\Projects\License) with its private key:
    //   key = base64url(payload JSON) "." base64url(ECDSA P-256 signature of the payload)
    //   payload = { "c": customer, "m": machine code, "i": issued (yyyy-MM-dd), "e": expires (yyyy-MM-dd) }
    // This program only holds the public key: it can check a license but never make one.
    // The machine code comes from this Windows installation and its board, so a copied program folder
    // (or a license made for another PC) does not work. The license is kept in %ProgramData%\Inventory\license.lic.
    public enum LicenseStatus { Valid, Missing, Invalid, OtherMachine, Expired, ClockBack }

    public sealed record LicenseInfo(LicenseStatus Status, string? Customer = null, DateTime? Expires = null)
    {
        public bool IsValid => Status == LicenseStatus.Valid;

        /// <summary>Days left including today (0 when not valid).</summary>
        public int DaysLeft => IsValid && Expires is { } e ? (e.Date - DateTime.Today).Days + 1 : 0;

        public string Message => Status switch
        {
            LicenseStatus.Valid => $"Licensed to {Customer} until {Expires:yyyy-MM-dd}.",
            LicenseStatus.Missing => "This PC has no license.",
            LicenseStatus.Invalid => "The license is not valid.",
            LicenseStatus.OtherMachine => "This license was made for another PC.",
            LicenseStatus.Expired => $"The license of {Customer} expired on {Expires:yyyy-MM-dd}.",
            LicenseStatus.ClockBack => "The date of this PC is earlier than when the program last ran. Correct the date and time of Windows.",
            _ => "The license is not valid.",
        };
    }

    public static class LicenseManager
    {
        // Public key of the License app (SubjectPublicKeyInfo, base64). Its private key stays with the License app only.
        private const string PublicKey =
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAECualghqowovpQV53G66fhIPmcWtR9dCtayqsDtfyL1xFKVfphZT1Am5THvFpJynn4MIuCuKoeB5+vUL9Lwqicw==";

        private static readonly string Folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Inventory");
        private static string LicensePath => Path.Combine(Folder, "license.lic");
        private static string SeenPath => Path.Combine(Folder, "license.seen");   // last day the program ran (clock set back = refused)

        private static string? _machineCode;

        /// <summary>This PC's code (XXXXX-XXXXX-XXXXX-XXXXX), given to the License app to make its license.</summary>
        public static string MachineCode => _machineCode ??= ComputeMachineCode();

        private static string ComputeMachineCode()
        {
            var parts = new StringBuilder();
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // Unique per Windows installation
                parts.Append(hklm.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?.GetValue("MachineGuid")).Append('|');
                // The board and BIOS
                using var bios = hklm.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                foreach (var name in new[] { "SystemManufacturer", "SystemProductName", "BaseBoardManufacturer", "BaseBoardProduct" })
                    parts.Append(bios?.GetValue(name)).Append('|');
            }
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(parts.ToString().ToUpperInvariant()));
            return FormatCode(hash.AsSpan(0, 13));
        }

        // Base32 (no 0/O/1/I), in groups of 5
        private static string FormatCode(ReadOnlySpan<byte> bytes)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var text = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    text.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            var code = text.ToString()[..20];
            return string.Join('-', Enumerable.Range(0, 4).Select(i => code.Substring(i * 5, 5)));
        }

        /// <summary>The license saved on this PC, checked.</summary>
        public static LicenseInfo Check()
        {
            string key;
            try
            {
                if (!File.Exists(LicensePath)) return new LicenseInfo(LicenseStatus.Missing);
                key = File.ReadAllText(LicensePath);
            }
            catch { return new LicenseInfo(LicenseStatus.Missing); }

            var info = Verify(key);
            if (!info.IsValid) return info;

            // The clock set back (to use an expired license): refused; otherwise today is remembered
            try
            {
                if (File.Exists(SeenPath) && DateTime.TryParse(File.ReadAllText(SeenPath), out var seen) && DateTime.Today < seen.Date.AddDays(-1))
                    return info with { Status = LicenseStatus.ClockBack };
                File.WriteAllText(SeenPath, DateTime.Today.ToString("yyyy-MM-dd"));
            }
            catch { /* another Windows user may not write here: the check is skipped */ }
            return info;
        }

        /// <summary>Checks a license key for this PC (signature, machine, expiry) without saving it.</summary>
        public static LicenseInfo Verify(string? key)
        {
            key = string.Concat((key ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));
            var dot = key.IndexOf('.');
            if (dot <= 0) return new LicenseInfo(LicenseStatus.Invalid);
            try
            {
                var payload = FromBase64Url(key[..dot]);
                var signature = FromBase64Url(key[(dot + 1)..]);
                using var ec = ECDsa.Create();
                ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKey), out _);
                if (!ec.VerifyData(payload, signature, HashAlgorithmName.SHA256)) return new LicenseInfo(LicenseStatus.Invalid);

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var customer = root.GetProperty("c").GetString();
                var machine = root.GetProperty("m").GetString();
                var expires = DateTime.ParseExact(root.GetProperty("e").GetString()!, "yyyy-MM-dd", null);

                if (!string.Equals(machine, MachineCode, StringComparison.OrdinalIgnoreCase))
                    return new LicenseInfo(LicenseStatus.OtherMachine, customer, expires);
                if (DateTime.Today > expires.Date) return new LicenseInfo(LicenseStatus.Expired, customer, expires);
                return new LicenseInfo(LicenseStatus.Valid, customer, expires);
            }
            catch { return new LicenseInfo(LicenseStatus.Invalid); }
        }

        /// <summary>Checks the key and, when it is valid for this PC, saves it as this PC's license.</summary>
        public static LicenseInfo Install(string key)
        {
            var info = Verify(key);
            if (!info.IsValid) return info;
            Directory.CreateDirectory(Folder);
            File.WriteAllText(LicensePath, string.Concat(key.Where(c => !char.IsWhiteSpace(c))));
            try { File.WriteAllText(SeenPath, DateTime.Today.ToString("yyyy-MM-dd")); } catch { }
            return info;
        }

        private static byte[] FromBase64Url(string text)
        {
            var s = text.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }
    }
}
