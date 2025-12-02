using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Migrations;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace ProjectTest1.Controllers
{
    
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _context;

        public HomeController(DataContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.fullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Người dùng";

            }
            var cates = _context.Categories
                .Where(c => c.ParentId == null)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName
                })
                .ToList();

            return View(cates);   // Trả về List<CategoryViewModel>

        }
        [HttpGet]
        public IActionResult GetCategories(int? parentId)
        {
            var categories = _context.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => new {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    HasChildren = _context.Categories.Any(x => x.ParentId == c.CategoryId)
                })
                .ToList();

            return Json(categories);
        }

        public IActionResult Detail()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            
            return View();
        }
        [HttpGet]
        public async Task<PartialViewResult> ListData(int page = 1)
        {
            int pageSize = 4;
            List<ProductModel> products = new List<ProductModel>();
            try
            {
                 products = await _context.Product
                    .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            }catch(Exception ex)
            {

            }
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)_context.Product.Count() / pageSize);
            return PartialView(products);

        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
