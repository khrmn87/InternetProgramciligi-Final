using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using model=Webapplication.Models;
using Webapplication.Repositories;
using Webapplication.Models;

public class FileRepository : GenericRepository<model.File>
{
    private readonly AppDbContext _context;
    
    public FileRepository(AppDbContext context) : base(context, context.Files)
    {
        _context = context;
    }

    public async Task<double> GetTotalStorageSize()
    {
        return await _context.Files.SumAsync(f => f.FileSize);
    }

    public async Task<double> GetUserStorageSize(string userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId)
            .SumAsync(f => f.FileSize);
    }

    public async Task<List<UserStorageInfo>> GetAllUsersStorageInfo()
    {
        return await _context.Files
            .GroupBy(f => f.UserId)
            .Select(g => new UserStorageInfo
            {
                UserId = g.Key,
                TotalStorage = g.Sum(f => f.FileSize)
            })
            .ToListAsync();
    }

    public async Task<double> GetSystemTotalStorageSize()
    {
        return await _context.Files.SumAsync(f => f.FileSize);
    }
}

public class UserStorageInfo
{
    public string UserId { get; set; }
    public double TotalStorage { get; set; }
} 