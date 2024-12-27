namespace Webapplication.Models
{
    public class File
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public double FileSize { get; set; } // Dosya boyutu (MB)
        public DateTime UploadedAt { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; } // Dosyayı yükleyen kullanıcı
        public int? CategoryId { get; set; } 
        public Category Category { get; set; } 
    }
}
