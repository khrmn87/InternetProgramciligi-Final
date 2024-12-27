using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Webapplication.Models;
using Webapplication.Repositories;
using Webapplication.ViewModel;

namespace Webapplication.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        public CategoryController(CategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var viewModel = _mapper.Map<List<CategoryViewModel>>(categories);
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            var viewModel = _mapper.Map<CategoryViewModel>(category);
            return Json(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = _mapper.Map<Category>(model);
                await _categoryRepository.AddAsync(category);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = _mapper.Map<Category>(model);
                await _categoryRepository.UpdateAsync(category);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryRepository.DeleteAsync(id);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var viewModel = _mapper.Map<List<CategoryViewModel>>(categories);
            return Json(viewModel);
        }
    }
}
