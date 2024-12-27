using Webapplication.Models;
using Webapplication.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using model = Webapplication.Models;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Webapplication.Hubs;

namespace Webapplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly INotyfService _notyf;
        private readonly FileRepository _fileRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<FileHub> _hubContext;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger, 
            INotyfService notyf,
            FileRepository fileRepository,
            IWebHostEnvironment env,
            CategoryRepository categoryRepository,
            IHubContext<FileHub> hubContext,
            UserManager<AppUser> userManager)
        {
            _logger = logger;
            _notyf = notyf;
            _fileRepository = fileRepository;
            _env = env;
            _categoryRepository = categoryRepository;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var category = await _categoryRepository.GetAllAsync();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if(userId == null)
            {
                return RedirectToAction("Login", "User");
            }
            // Kullanıcının mevcut depolama kullanımını hesapla
            var userStorage = await _fileRepository.GetUserStorageSize(userId);
            var user = await _userManager.FindByIdAsync(userId);
            
            ViewBag.UserStorage = (userStorage / 1024).ToString("F2"); // GB cinsinden
            ViewBag.StorageLimit = (user.StorageLimit / 1024); // GB cinsinden
            
            return View(category);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadFile(IFormFile file, int categoryId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Dosya seçilmedi" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Kullanıcının mevcut depolama alanını kontrol et
                var currentStorage = await _fileRepository.GetUserStorageSize(userId);
                var newFileSize = file.Length / (1024.0 * 1024.0); // MB cinsinden
                
                if ((currentStorage + newFileSize) > 5 * 1024) // 5GB limit
                {
                    return Json(new { success = false, message = "Depolama alanınız yetersiz!" });
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileEntity = new model.File
                {
                    FileName = file.FileName,
                    FilePath = $"/uploads/{uniqueFileName}",
                    FileSize = file.Length / 1024.0 / 1024.0, // MB cinsinden
                    UploadedAt = DateTime.Now,
                    UserId = userId,
                    CategoryId = categoryId
                };

                await _fileRepository.AddAsync(fileEntity);

                // Sistem toplam depolama alanını hesapla
                var systemTotalStorage = await _fileRepository.GetSystemTotalStorageSize();
                
                // SignalR ile bildirimleri gönder
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveSystemStorageUpdate", systemTotalStorage);
                await _hubContext.Clients.User(userId).SendAsync("ReceiveUserStorageUpdate", systemTotalStorage);

                return Json(new { success = true, message = "Dosya başarıyla yüklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yükleme hatası");
                return Json(new { success = false, message = "Dosya yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFiles(int? categoryId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var query = _fileRepository.Where(f => f.UserId == userId);

                if (categoryId.HasValue)
                {
                    query = query.Where(f => f.CategoryId == categoryId);
                }

                var files = await query
                    .Include(f => f.Category)
                    .OrderByDescending(f => f.UploadedAt)
                    .Select(f => new
                    {
                        id = f.Id,
                        fileName = f.FileName,
                        filePath = f.FilePath,
                        fileSize = f.FileSize,
                        uploadedAt = f.UploadedAt,
                        category = new
                        {
                            id = f.Category.Id,
                            name = f.Category.Name
                        }
                    })
                    .ToListAsync();

                return Json(new { success = true, data = files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosyalar listelenirken bir hata oluştu");
                return Json(new { success = false, message = "Dosyalar listelenirken bir hata oluştu" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Dosyanın bu kullanıcıya ait olduğunu kontrol et
                var file = await _fileRepository.GetByIdAsync(id);
                if (file == null || file.UserId != userId)
                {
                    return Json(new { success = false, message = "Dosya bulunamadı veya silme yetkiniz yok" });
                }

                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Veritabanından dosyayı sil
                await _fileRepository.DeleteAsync(id);

                // Sistem toplam depolama alanını hesapla
                var systemTotalStorage = await _fileRepository.GetSystemTotalStorageSize();
                
                // SignalR ile bildirimleri gönder
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveSystemStorageUpdate", systemTotalStorage);
                await _hubContext.Clients.User(userId).SendAsync("ReceiveUserStorageUpdate", systemTotalStorage);

                return Json(new { success = true, message = "Dosya başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silme hatası");
                return Json(new { success = false, message = "Dosya silinirken bir hata oluştu" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserStorageInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userStorage = await _fileRepository.GetUserStorageSize(userId);
            var user = await _userManager.FindByIdAsync(userId);
            
            return Json(new { 
                currentStorage = userStorage,
                storageLimit = user.StorageLimit
            });
        }
    }
}
