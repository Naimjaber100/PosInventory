using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Inventory.Models;

namespace Inventory.Scales
{
    // How the items reach a label scale. Every scale model has its own way (its own protocol, or its own PC
    // program that imports a file), so each one is a driver: a class with IScaleDriver, listed in ScaleDrivers.All.
    // A scale (Settings > Label scales) stores the key of its driver; the screen shows only the connection
    // fields that driver needs (Link).
    //
    // To add a scale model that is loaded over the network or a COM port: write a class with IScaleDriver
    // (Link = Network or Serial) that sends the items with that model's protocol, and add it to All.

    public enum ScaleLink { File, Network, Serial }

    /// <summary>One item as it is sent to a scale.</summary>
    public sealed record ScalePlu(int ProductId, string Code, string Name, decimal Price, bool IsWeighted, string Unit, int? HotKey);

    public interface IScaleDriver
    {
        /// <summary>Stored in scales.driver: never change it once scales use it.</summary>
        string Key { get; }

        /// <summary>Shown in the list of models.</summary>
        string Name { get; }

        ScaleLink Link { get; }

        /// <summary>One or two lines shown under the model: what it does and what to do after sending.</summary>
        string Help { get; }

        int DefaultPort => 0;
        int DefaultBaudRate => 9600;
        string FileExtension => string.Empty;   // file drivers: ".csv", ".xlsx"

        /// <summary>Sends the items to the scale; returns what to tell the user. Throws when it fails.</summary>
        Task<string> SendAsync(ScaleModel scale, IReadOnlyList<ScalePlu> items, CancellationToken cancel);
    }

    public static class ScaleDrivers
    {
        public static IReadOnlyList<IScaleDriver> All { get; } = new IScaleDriver[]
        {
            new CsvFileDriver(),
            new ExcelFileDriver(),
        };

        public static IScaleDriver? Find(string? key) => All.FirstOrDefault(d => d.Key == key);

        /// <summary>The file a file driver writes when the scale has none set: Documents\Scale - name.ext</summary>
        public static string DefaultFilePath(string scaleName, IScaleDriver driver)
        {
            var name = string.Concat((scaleName.Trim().Length == 0 ? "scale" : scaleName.Trim()).Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Scale - {name}{driver.FileExtension}");
        }
    }

    // Writes the items to a file that the scale's own PC program imports (most label scales have one:
    // Rongta, Aclas, Dahua, CAS, Digi...). Works with any brand; the last step is done in that program.
    public abstract class FileScaleDriver : IScaleDriver
    {
        public abstract string Key { get; }
        public abstract string Name { get; }
        public ScaleLink Link => ScaleLink.File;
        public abstract string FileExtension { get; }
        public string Help => "Writes the items to a file. Then open the scale's PC program, import this file and send it to the scale. "
                              + "Columns: PLU, Name, Price, Weighed (1 = by weight), Unit, HotKey.";

        protected static readonly string[] Headers = { "PLU", "Name", "Price", "Weighed", "Unit", "HotKey" };

        public async Task<string> SendAsync(ScaleModel scale, IReadOnlyList<ScalePlu> items, CancellationToken cancel)
        {
            var path = string.IsNullOrWhiteSpace(scale.FilePath) ? ScaleDrivers.DefaultFilePath(scale.Name, this) : scale.FilePath.Trim();
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
            await Task.Run(() => Write(path, items), cancel);
            return $"{items.Count} item(s) saved to:\n{path}\n\nOpen this file in the scale's PC program to load it into the scale.";
        }

        protected abstract void Write(string path, IReadOnlyList<ScalePlu> items);

        // 50000 / 12.5: no thousands separator, a dot for decimals
        protected static string PriceText(decimal price) => price.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public sealed class CsvFileDriver : FileScaleDriver
    {
        public override string Key => "file-csv";
        public override string Name => "Any scale: CSV file for the scale's PC program";
        public override string FileExtension => ".csv";

        // UTF-8 with BOM, so Arabic names open right in Excel and in most scale programs
        protected override void Write(string path, IReadOnlyList<ScalePlu> items)
        {
            var text = new StringBuilder();
            text.AppendLine(string.Join(",", Headers));
            foreach (var p in items)
                text.AppendLine(string.Join(",", Field(p.Code), Field(p.Name), PriceText(p.Price), p.IsWeighted ? "1" : "0", Field(p.Unit), p.HotKey?.ToString() ?? string.Empty));
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(true));
        }

        private static string Field(string value) =>
            value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    public sealed class ExcelFileDriver : FileScaleDriver
    {
        public override string Key => "file-xlsx";
        public override string Name => "Any scale: Excel file for the scale's PC program";
        public override string FileExtension => ".xlsx";

        protected override void Write(string path, IReadOnlyList<ScalePlu> items)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("PLU");
            for (var c = 0; c < Headers.Length; c++) sheet.Cell(1, c + 1).Value = Headers[c];
            sheet.Row(1).Style.Font.Bold = true;

            var row = 2;
            foreach (var p in items)
            {
                sheet.Cell(row, 1).SetValue(p.Code);   // text: keeps the leading zeros of the PLU
                sheet.Cell(row, 1).Style.NumberFormat.Format = "@";
                sheet.Cell(row, 2).Value = p.Name;
                sheet.Cell(row, 3).Value = p.Price;
                sheet.Cell(row, 4).Value = p.IsWeighted ? 1 : 0;
                sheet.Cell(row, 5).Value = p.Unit;
                if (p.HotKey is { } key) sheet.Cell(row, 6).Value = key;
                row++;
            }
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(path);
        }
    }
}
