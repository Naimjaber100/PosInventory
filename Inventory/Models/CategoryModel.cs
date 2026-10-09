namespace Inventory.Models
{
    // Row returned by fn_category_get_all (categories form a tree through ParentId)
    public class CategoryModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Path { get; set; } = string.Empty;   // "Shampoo > Sunsilk"
        public int Level { get; set; }                     // 0 = top level
        public long ItemCount { get; set; }                // items directly in this category
    }
}
