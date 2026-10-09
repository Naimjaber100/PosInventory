namespace Inventory.Models
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public long TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }

        public int TotalPages => TotalCount == 0 ? 1 : (int)((TotalCount + PageSize - 1) / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
