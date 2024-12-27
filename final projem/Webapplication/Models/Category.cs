namespace Webapplication.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } // Kategori adı (örn: Resimler, Belgeler)
        public string Description { get; set; } // Opsiyonel, kategori açıklaması

        public ICollection<File> Files { get; set; }
    }
}
