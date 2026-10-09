namespace Inventory.Validation
{
    /// <summary>One problem found by a validator.</summary>
    /// <param name="Message">Text shown to the user.</param>
    /// <param name="Field">Property that is wrong (e.g. nameof(PurchaseHeaderModel.Discount)), so the form can mark it.</param>
    /// <param name="LineIndex">For line validators: index of the wrong line in the list (0-based).</param>
    public sealed record ValidationError(string Message, string? Field = null, int? LineIndex = null);

    /// <summary>The problems found by one or more validators. Empty = valid.</summary>
    public sealed class ValidationResult
    {
        private readonly List<ValidationError> _errors = new();

        public IReadOnlyList<ValidationError> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        public void Add(string message, string? field = null, int? lineIndex = null) =>
            _errors.Add(new ValidationError(message, field, lineIndex));

        public ValidationResult Merge(ValidationResult other)
        {
            _errors.AddRange(other._errors);
            return this;
        }

        /// <summary>All messages as one text (at most maxLines, then "... and N more").</summary>
        public string ToMessage(int maxLines = 15)
        {
            var lines = _errors.Take(maxLines).Select(e => "• " + e.Message).ToList();
            if (_errors.Count > maxLines) lines.Add($"... and {_errors.Count - maxLines} more.");
            return string.Join(Environment.NewLine, lines);
        }
    }
}
