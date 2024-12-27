using Microsoft.AspNetCore.Identity;

namespace Webapplication.Models
{
    public class AppUser: IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public double? StorageLimit { get; set; } = 5 * 1024; // GB olarak depolama limiti
        public double? UsedStorage { get; set; } = 0; // Kullanılmış depolama miktarı
    }
}
