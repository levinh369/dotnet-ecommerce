using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;

namespace ProjectTest1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly DataContext db;
        public CategoryController(DataContext db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new CategoryViewModel
            {
                ParentCategoryList = db.Categories
            .Where(c => c.ParentId == null)
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList()
            };

            return PartialView("Create", viewModel); // partial chứa modal
        }
        [HttpPost]
          public async Task<JsonResult> create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            var isDuplicate = await db.Categories
        .AnyAsync(c =>
            c.CategoryName.Trim().ToLower() == model.CategoryName.Trim().ToLower() &&
            c.ParentId == model.ParentId // chỉ kiểm tra trùng trong cùng cha
        );

            if (isDuplicate)
            {
                return Json(new { success = false, message = "Tên danh mục đã tồn tại." });
            }
            var category = new CategoryModel
            {
                
                CategoryName = model.CategoryName,
                ParentId = model.ParentId,
                IsActive = model.IsActive
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Json(new { success = true, message = "Thêm mới danh mục thành công." });
        }
        public async Task<PartialViewResult> ListData(int page = 1, int pageSize = 5, string keySearch = "", int status=0)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;

            // Lấy danh sách categories dưới dạng IQueryable
            var query = db.Categories.AsNoTracking().AsQueryable();

            // Nếu có từ khóa tìm kiếm, lọc theo CategoryName
            if (!string.IsNullOrEmpty(keySearch))
            {
                query = query.Where(c => c.CategoryName.Contains(keySearch));
            }
            if (status == 1)
            {
                query = query.Where(g => g.IsActive == true);
            }
            else if(status == 2)
            {
                query = query.Where(g => g.IsActive == false);
            }

                // Đếm tổng số bản ghi sau lọc
            var total = await query.CountAsync();
            ViewBag.total = total;
            ViewBag.totalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.stt = (page - 1) * pageSize;

            // Lấy dữ liệu theo trang, đồng thời project sang ViewModel
            var listData = await query
                .OrderBy(c => c.CategoryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ParentName = c.Parent != null ? c.Parent.CategoryName : "Danh mục gốc",
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return PartialView(listData);
            
        }

        public JsonResult Delete(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, message = "Id không được để trống" });
            }
            try
            {
                var obj = db.Categories.FirstOrDefault(p => p.CategoryId == id);
                if (obj == null)
                {
                    return Json(new { status = false, message = "Không tìm thấy bản ghi." });
                }
                db.Categories.Remove(obj);
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không xóa được bản ghi này" });
            }
            return Json(new { status = true, message = "Danh mục đã được xóa thành công" });
        }
        [HttpPost]
        public async Task<JsonResult> deleteAll(int[] id)
        {
            if (id == null || id.Length == 0)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            try
            {
                var listObj = await db.Categories.Where(p => id.Contains(p.CategoryId)).ToListAsync();
                if (listObj?.Any() != true)
                {
                    return Json(new { status = false, message = "Không tìm thấy danh sách bản ghi." });
                }
                db.Categories.RemoveRange(listObj);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không xóa được bản ghi này" });
            }
            return Json(new { status = true, message = "Bản ghi đã được xóa thành công" });
        }
        public async Task<JsonResult> Status(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            var Category = await db.Categories.FindAsync(id);
            if (Category == null)
            {
                return Json(new { status = false, Message = "Bản ghi đã bị xóa" });
            }
            try
            {
                Category.IsActive = !Category.IsActive;
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không thay đổi được trạng thái Danh mục này" });
            }
            return Json(new { status = true, message = "Danh mục đã được cập nhật trạng thái thành công" });
        }
        public async Task<ActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, message = "Không được để trống Id" });
            }

            var objData = await db.Categories.FindAsync(id);
            if (objData == null)
            {
                return Json(new { status = false, message = "Bản ghi không tồn tại" });
            }

            string parentLevel1Name = "Danh mục gốc";  // Mặc định danh mục gốc
            string parentLevel2Name = "Không có";

            if (objData.ParentId != null)
            {
                var parentLevel2 = await db.Categories.FindAsync(objData.ParentId);
                if (parentLevel2 != null)
                {
                    parentLevel2Name = parentLevel2.CategoryName;

                    if (parentLevel2.ParentId != null)
                    {
                        var parentLevel1 = await db.Categories.FindAsync(parentLevel2.ParentId);
                        if (parentLevel1 != null)
                        {
                            parentLevel1Name = parentLevel1.CategoryName;
                        }
                        else
                        {
                            parentLevel1Name = "Danh mục gốc";
                        }
                    }
                    else
                    {
                        // parentLevel2 là cấp 1, cấp 2 không có
                        parentLevel1Name = parentLevel2.CategoryName;
                        parentLevel2Name = "Không có";
                    }
                }
            }
            else
            {
                // objData là cấp 1 (gốc)
                parentLevel1Name = "Danh mục gốc";  // KHÔNG lấy tên chính nó
                parentLevel2Name = "Không có";
            }

            var model = new CategoryViewModel
            {
                CategoryName = objData.CategoryName,
                IsActive = objData.IsActive,
                ParentName = parentLevel1Name,
                ParentName2 = parentLevel2Name
            };

            return PartialView("Detail", model);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return Json(new { status = false, message = "Không được để trống Id" });

            var objData = await db.Categories.FindAsync(id);
            if (objData == null)
                return Json(new { status = false, message = "Bản ghi không tồn tại" });

            CategoryModel parentLevel2 = null;
            CategoryModel parentLevel1 = null;

            if (objData.ParentId != null)
            {
                parentLevel2 = await db.Categories.FindAsync(objData.ParentId);
                if (parentLevel2?.ParentId != null)
                {
                    parentLevel1 = await db.Categories.FindAsync(parentLevel2.ParentId);
                }
                else
                {
                    parentLevel1 = parentLevel2;
                    parentLevel2 = null;
                }
            }

            // Load danh sách cấp 1 (ParentLevel1) - bắt buộc phải có, không phụ thuộc objData có phải gốc hay không
            var parentCategoryList = await db.Categories
                .Where(c => c.ParentId == null )
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();

            // Load danh sách cấp 2 theo cấp 1 đã chọn (nếu có)
            List<SelectListItem> parentLevel2List = new List<SelectListItem>();
            if (parentLevel1 != null)
            {
                parentLevel2List = await db.Categories
                    .Where(c => c.ParentId == parentLevel1.CategoryId && c.CategoryId != id)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync();
            }

            var model = new CategoryViewModel
            {
                CategoryId = objData.CategoryId,
                CategoryName = objData.CategoryName,
                IsActive = objData.IsActive,

                ParentLevel1Id = parentLevel1?.CategoryId,
                ParentLevel2Id = parentLevel2?.CategoryId,
                ParentId = objData.ParentId,

                ParentCategoryList = parentCategoryList,
                ParentLevel2List = parentLevel2List
            };

            return PartialView("Edit", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]  // Nếu dùng form có token
        public async Task<JsonResult> EditPost(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            var entity = await db.Categories.FindAsync(model.CategoryId);
            if (entity == null)
                return Json(new { status = false, message = "Bản ghi không tồn tại" });
            entity.CategoryName = model.CategoryName;
            entity.ParentId = model.ParentId;
            entity.IsActive = model.IsActive;
            try
            {
                await db.SaveChangesAsync();
                return Json(new { status = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Có lỗi xảy ra khi lưu dữ liệu" });
            }
        }
        public JsonResult GetChildCategories(int parentId)
        {
            var categories = db.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName
                })
                .ToList();

            return Json(categories);
        }


    }
}
