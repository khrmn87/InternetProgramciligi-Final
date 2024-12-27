using AspNetCoreHero.ToastNotification.Abstractions;
using AutoMapper;
using Webapplication.Repositories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Webapplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Webapplication.Hubs;


namespace Webapplication.Controllers
{
    [Authorize(Roles="Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly FileRepository _fileRepository;
        private readonly IHubContext<FileHub> _hubContext;
        private readonly INotyfService _notyf;

        public AdminController(
            UserManager<AppUser> userManager,
            FileRepository fileRepository,
            IHubContext<FileHub> hubContext,
            INotyfService notyf)
        {
            _userManager = userManager;
            _fileRepository = fileRepository;
            _hubContext = hubContext;
            _notyf = notyf;
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var storageInfo = await _fileRepository.GetAllUsersStorageInfo();

                var userList = users.Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    StorageSize = storageInfo.FirstOrDefault(s => s.UserId == u.Id)?.TotalStorage ?? 0,
                    StorageLimit = u.StorageLimit > 0 ? u.StorageLimit : 5 * 1024 // Varsayılan 5GB (MB cinsinden)
                }).ToList();

                return Json(userList);
            }
            catch (Exception ex)
            {
                
                return Json(new { error = "Kullanıcı listesi alınamadı" });
            }
        }

        [HttpGet]
        public IActionResult UserList()
        {
            return View("Users");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserStorage(string userId, double newStorageLimit)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });

                // GB'ı MB'a çeviriyoruz (1 GB = 1024 MB)
                user.StorageLimit = newStorageLimit * 1024; // GB'ı MB'a çevirme
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = "Güncelleme başarısız oldu" });
                }

                var currentStorage = await _fileRepository.GetUserStorageSize(userId);
                
                // SignalR ile bildirimleri gönder
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveStorageUpdate", currentStorage);
                await _hubContext.Clients.User(userId).SendAsync("ReceiveUserStorageUpdate", currentStorage);
                await _hubContext.Clients.User(userId).SendAsync("ReceiveStorageLimitUpdate", newStorageLimit);
                
                _notyf.Success($"Kullanıcının depolama alanı {newStorageLimit} GB olarak güncellendi");
                
                return Json(new { 
                    success = true, 
                    message = $"Depolama alanı {newStorageLimit} GB olarak güncellendi",
                    currentStorage = currentStorage,
                    newLimit = newStorageLimit
                });
            }
            catch (Exception ex)
            {
            
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        public async Task<IActionResult> Index()
        {
            var totalStorage = await _fileRepository.GetSystemTotalStorageSize();
            ViewBag.TotalStorage = (totalStorage / 1024).ToString("F2"); // GB cinsinden
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStorageInfo()
        {
            var totalSize = await _fileRepository.GetTotalStorageSize();
            return Json(new { totalSize });
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemStorageInfo()
        {
            var totalStorage = await _fileRepository.GetSystemTotalStorageSize();
            return Json(new { totalStorage });
        }

      

       
    }
}
