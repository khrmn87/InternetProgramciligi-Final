using Webapplication.Models;

namespace Webapplication.Repositories
{
    public class CategoryRepository : GenericRepository<Category>
    {
        public CategoryRepository(AppDbContext context) : base(context, context.Categories)
        {
        }
    }
}
