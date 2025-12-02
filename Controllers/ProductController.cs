using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using ProjectTest1.Helpper;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ProjectTest1.Controllers
{
    public class ProductController : Controller
    {
        
        private readonly DataContext db;
        private readonly CloudinaryHelper _cloudinaryService;
        public ProductController(DataContext db, CloudinaryHelper _cloudinaryService)
        {
            this.db = db;
            this._cloudinaryService = _cloudinaryService;
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
             return View();
        }
        public async Task<PartialViewResult> ListData(int page = 1, int pageSize = 5, String keySearch = "", DateTime? fromDate = null, DateTime? toDate = null,float minPrice=0,float maxPrice=0)
        {
            ViewBag.page = page;
            ViewBag.pageSize = pageSize;
            var query = db.Product.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(keySearch))
            {
                query = query.Where(c => c.Name.Contains(keySearch));
            }
            if (fromDate != null && toDate != null)
            {
                query = query.Where(g => g.CreatedAt >= fromDate && g.CreatedAt <= toDate);
            }
            if(minPrice > 0 && maxPrice > 0)
            {
                query = query.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
            } 
            var total = await query.CountAsync();
            ViewBag.total = total;
            ViewBag.totalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.stt = (page - 1) * pageSize;

            // Lấy dữ liệu theo trang, đồng thời project sang ViewModel
            var listData = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ProductViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Img = c.Img,
                    Price = c.Price,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdateAt = c.UpdatedAt
                })
                .ToListAsync();
           
            return PartialView(listData);
        }
       

        public IActionResult Create()
        {
            var categories = db.Categories.Where(c => c.Parent == null);
            var categorySelectList = categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList();
            var sizes = db.Sizes.Select(s => new SizeViewModel
            {
                SizeId = s.SizeId,
                SizeName = s.SizeName,
                Selected = false
            }).ToList();

            // Lấy color từ DB
            var colors = db.Colors.Select(c => new ColorViewModel
            {
                ColorId = c.ColorId,
                ColorName = c.ColorName,
                Selected = false
            }).ToList();
            // Tạo ViewModel và gán danh sách
            var model = new ProductViewModel
            {
                CategoriesLevel1 = categorySelectList,
                Sizes = sizes,
                Colors = colors
            };
            return PartialView("Create", model); // Trả về partial view modal

        }

        [HttpPost]
        public async Task<JsonResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // =========================
                // 1️⃣ Upload ảnh chính + phụ
                // =========================
                var uploadTasks = new List<Task<string>>();

                if (model.MainImageFile != null)
                    uploadTasks.Add(_cloudinaryService.UploadImageAsync(model.MainImageFile));

                if (model.SubImagesFiles != null && model.SubImagesFiles.Any())
                    uploadTasks.AddRange(model.SubImagesFiles.Select(file => _cloudinaryService.UploadImageAsync(file)));

                var urls = await Task.WhenAll(uploadTasks);

                string mainImageUrl = model.MainImageFile != null ? urls[0] : null;
                var subImageUrls = model.MainImageFile != null ? urls.Skip(1).ToList() : urls.ToList();

                // =========================
                // 2️⃣ Lưu thông tin sản phẩm
                // =========================
                var product = new ProductModel
                {
                    Name = model.Name,
                    Price = model.Price,
                    Img = mainImageUrl,
                    IsActive = model.IsActive,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                };

                db.Product.Add(product);
                await db.SaveChangesAsync();

                // =========================
                // 3️⃣ Lưu ảnh phụ (nếu có)
                // =========================
                if (subImageUrls.Any())
                {
                    var productImages = subImageUrls
                        .Where(url => !string.IsNullOrEmpty(url))
                        .Select(url => new ProductImageModel
                        {
                            ProductId = product.Id,
                            ImageUrl = url,
                            IsMain = false,
                            CreatedAt = DateTime.Now
                        })
                        .ToList();

                    db.ProductImages.AddRange(productImages);
                }

                // =========================
                // 4️⃣ Upload ảnh theo màu
                // =========================
                var colorImageMap = new Dictionary<int, string>();

                if (model.ColorImages != null && model.ColorImages.Any())
                {
                    foreach (var colorImage in model.ColorImages)
                    {
                        if (colorImage.ImageFile != null)
                        {
                            var colorUrl = await _cloudinaryService.UploadImageAsync(colorImage.ImageFile);
                            colorImageMap[colorImage.ColorId] = colorUrl;

                            db.ProductColorImages.Add(new ProductColorImageModel
                            {
                                ProductId = product.Id,
                                ColorId = colorImage.ColorId,
                                ImageUrl = colorUrl,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }

                if (model.Variants != null && model.Variants.Any())
                {
                    foreach (var variant in model.Variants)
                    {
                        if (variant.StockQuantity <= 0)
                            continue;

                        var sku = $"{product.Id}-{variant.ColorId}-{variant.SizeId}";

                        db.ProductVariants.Add(new ProductVariantModel
                        {
                            ProductId = product.Id,
                            ColorId = variant.ColorId ?? 0,
                            SizeId = variant.SizeId ?? 0,
                            StockQuantity = variant.StockQuantity,
                            SKU = sku
                        });
                    }
                }


                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thêm mới sản phẩm thành công." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var inner = ex.InnerException?.Message ?? ex.Message;

                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi thêm sản phẩm: " + inner
                });
            }
        }

        public JsonResult Delete(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, message = "Id không được để trống" });
            }
            try
            {
                var obj = db.Product
            .Include(p => p.Images)
            .FirstOrDefault(p => p.Id == id);
                if (obj == null)
                {
                    return Json(new { status = false, message = "Không tìm thấy bản ghi." });
                }
                if (obj.Images != null && obj.Images.Any())
                {
                    db.ProductImages.RemoveRange(obj.Images);
                }
                db.Product.Remove(obj);
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không xóa được bản ghi này" });
            }
            return Json(new { status = true, message = "Bản ghi đã được xóa thành công" });
        }
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return Json(new { status = false, message = "Không được để trống Id" });

            var objData = await db.Product.FindAsync(id);
            if (objData == null)
                return Json(new { status = false, message = "Bản ghi không tồn tại" });
            var cateLevel3 = db.Categories.FirstOrDefault(c => c.CategoryId == objData.CategoryId);
            var cateLevel2 = cateLevel3 != null ? db.Categories.FirstOrDefault(c => c.CategoryId == cateLevel3.ParentId) : null;
            var cateLevel1 = cateLevel2 != null ? db.Categories.FirstOrDefault(c => c.CategoryId == cateLevel2.ParentId) : null;
            var subImages = await db.ProductImages
        .AsNoTracking()
        .Where(img => img.ProductId == id)
        .Select(img => img.ImageUrl)
        .ToListAsync();
            var model = new ProductViewModel
            {
                Id = objData.Id,

                Name = objData.Name,
                IsActive = objData.IsActive,
                Price = objData.Price,
                Img = objData.Img,
                Description = objData.Description,

                SubImages = subImages,
                SelectedCategoryLevel1Id = cateLevel1?.CategoryId.ToString(),
                SelectedCategoryLevel2Id = cateLevel2?.CategoryId.ToString(),
                SelectedCategoryLevel3Id = cateLevel3?.CategoryId.ToString(),

                // Selected Names
                SelectedCategoryLevel1Name = cateLevel1?.CategoryName,
                SelectedCategoryLevel2Name = cateLevel2?.CategoryName,
                SelectedCategoryLevel3Name = cateLevel3?.CategoryName,

                // Lists
                CategoriesLevel1 = await db.Categories
            .Where(c => c.ParentId == null)
            .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
            .ToListAsync(),

                CategoriesLevel2 = cateLevel1 != null
            ? await db.Categories
                .Where(c => c.ParentId == cateLevel1.CategoryId)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                .ToListAsync()
            : new List<SelectListItem>(),

                CategoriesLevel3 = cateLevel2 != null
            ? await db.Categories
                .Where(c => c.ParentId == cateLevel2.CategoryId)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                .ToListAsync()
            : new List<SelectListItem>()
            };
            ProductViewModel v = new ProductViewModel();
            var s = v.SelectedCategoryLevel1Id;
            return PartialView("Edit", model);
        }
        [HttpGet]
        public JsonResult GetChildCategories(int parentId)
        {
            var categories = db.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToList();

            return Json(categories);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]  // Nếu dùng form có token
        public async Task<JsonResult> EditPost(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            var product = await db.Product
        .Include(p => p.Images)
        .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                // Upload ảnh mới nếu có
                string mainImageUrl = product.Img; // giữ ảnh cũ nếu không upload mới
                var subImageUrls = new List<string>();

                var uploadTasks = new List<Task<string>>();

                if (model.MainImageFile != null)
                {
                    uploadTasks.Add(_cloudinaryService.UploadImageAsync(model.MainImageFile));
                }
                if (!string.IsNullOrEmpty(model.ExistingSubImages))
                {
                    var oldImages = model.ExistingSubImages
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(url => Task.FromResult(url));

                    uploadTasks.AddRange(oldImages);
                }
                if (model.SubImagesFiles != null && model.SubImagesFiles.Any())
                {
                    uploadTasks.AddRange(model.SubImagesFiles.Select(file => _cloudinaryService.UploadImageAsync(file)));
                }

                var urls = await Task.WhenAll(uploadTasks);

                if (model.MainImageFile != null)
                {
                    mainImageUrl = urls[0];
                    subImageUrls = urls.Skip(1).ToList();
                }
                else
                {
                    subImageUrls = urls.ToList();
                }

                // Cập nhật product
                product.Name = model.Name;
                product.Price = model.Price;
                product.Img = mainImageUrl;
                product.IsActive = model.IsActive;
                product.Description = model.Description;
                product.CategoryId = model.CategoryId;
                product.UpdatedAt = DateTime.Now; // giờ hệ thống (theo múi giờ server)

                db.Product.Update(product);
                await db.SaveChangesAsync();

                // Xóa ảnh phụ cũ nếu muốn
                db.ProductImages.RemoveRange(product.Images);

                // Thêm ảnh phụ mới nếu có
                if (subImageUrls.Any())
                {
                    var productImages = subImageUrls.Select(url => new ProductImageModel
                    {
                        ProductId = product.Id,
                        ImageUrl = url,
                        IsMain = false,
                        CreatedAt = DateTime.Now
                    }).ToList();

                    db.ProductImages.AddRange(productImages);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Cập nhật sản phẩm thành công." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message });
            }
        }
        public async Task<JsonResult> Status(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            var product = await db.Product.FindAsync(id);
            if(product == null)
            {
                return Json(new { status = false, Message = "Bản ghi đã bị xóa" });
            }
            try
            {
                product.IsActive = !product.IsActive;
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không thay đổi được trạng thái Danh mục này" });
            }
            return Json(new { status = true, message = "Sản phẩm đã được cập nhật trạng thái thành công" });
        }
        public async Task<ActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }
            var product = await db.Product
            .Include(p => p.Category)
                .ThenInclude(c => c.Parent)
                    .ThenInclude(pc => pc.Parent) // để lấy cấp 1, 2, 3
            .Include(p => p.Images) // lấy luôn ảnh phụ
            .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return Json(new { status = false, message = "Bản ghi không tồn tại" });
            }

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
               
                SelectedCategoryLevel1Name = product.Category?.Parent?.Parent?.CategoryName,
                SelectedCategoryLevel2Name = product.Category?.Parent?.CategoryName,
                SelectedCategoryLevel3Name = product.Category?.CategoryName,
                Price = product.Price,
                IsActive = product.IsActive,
                Description = product.Description,
                Img = product.Img,
                SubImages = product.Images.Select(s => s.ImageUrl).ToList()
            };

            return PartialView("Detail", model);
        }
        [AllowAnonymous]
        public async Task<IActionResult> ProductDetail(int? id, int? orderDetailId)
        {
            if (id == null)
            {
                return Json(new { status = false, Message = "Id không được để trống" });
            }

            var product = await db.Product
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return Json(new { status = false, Message = "Not found" });
            }
            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Quantity = product.Variants.Sum(v => v.StockQuantity),
                Price = product.Price,
                IsActive = product.IsActive,
                Description = product.Description,
                Img = product.Img,
                SubImages = product.Images.Select(s => s.ImageUrl).ToList(),
                ProductVariantId = product.Variants.FirstOrDefault()?.ProductVariantId ?? 0,
                Sizes = product.Variants
                               .Where(v => v.Size != null)
                               .GroupBy(v => v.Size.SizeId)
                               .Select(g => new SizeViewModel
                               {
                                   SizeId = g.Key,
                                   SizeName = g.First().Size.SizeName
                               }).ToList(),

                            Colors = product.Variants
                .Where(v => v.Color != null)
                .GroupBy(v => v.Color.ColorId)
                .Select(g => new ColorViewModel
                {
                    ColorId = g.Key,
                    ColorName = g.First().Color.ColorName,   // "Red"
                    ColorCode = g.First().Color.ColorCode        // "#FF0000"
                }).ToList(),
                //Reviews = reviews,

            };
            ViewBag.OrderDetailId = orderDetailId;
            //Console.WriteLine($"SelectedVariantId = {model.SelectedVariantId}");
            return PartialView("ProductDetail",model);
        }
        [HttpGet]
        public IActionResult GetChatProduct(int productId)
        {
            var product = db.Product
                .Where(p => p.Id == productId)
                .Select(p => new {
                    p.Id,
                    name = p.Name,
                    image = p.Img,
                    price = p.Price
                })
                .FirstOrDefault();
            return Json(product);
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
                var listObj = await db.Product.Where(p => id.Contains(p.Id)).ToListAsync();
                if (listObj?.Any() != true)
                {
                    return Json(new { status = false, message = "Không tìm thấy danh sách bản ghi." });
                }
                db.Product.RemoveRange(listObj);
                await db.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException ex)
            {
                return Json(new { status = false, message = "Không xóa được bản ghi này" });
            }
            return Json(new { status = true, message = "Bản ghi đã được xóa thành công" });
        }

        public JsonResult GetQuantity(int? id, int? colorId, int? sizeId)
        {
            if (!id.HasValue)
            {
                return Json(new { status = false, quantity = 0, productVariantId = 0, Message = "Vui lòng chọn lại" });
            }

            var productQuery = db.ProductVariants
                .Where(p => p.ProductId == id.Value);

            if (colorId.HasValue)
                productQuery = productQuery.Where(p => p.ColorId == colorId.Value);

            if (sizeId.HasValue)
                productQuery = productQuery.Where(p => p.SizeId == sizeId.Value);

            // Lấy ảnh sản phẩm (ưu tiên ảnh của màu, nếu không có thì lấy ảnh chính)
            var imageProduct = db.ProductColorImages
            .Where(ci => ci.ProductId == id && ci.ColorId == colorId)
            .Select(ci => ci.ImageUrl)
            .FirstOrDefault()
            ?? db.Product
                .Where(p => p.Id == id.Value)
                .Select(p => p.Img)
                .FirstOrDefault()
            ?? "/images/no-image.png";


            var variant = productQuery
                .Select(p => new
                {
                    ProductVariantId = p.ProductVariantId,
                    StockQuantity = p.StockQuantity
                })
                .FirstOrDefault();

            if (variant == null)
            {
                return Json(new { status = false,imageUrl=imageProduct, quantity = 0, productVariantId = 0, Message = "Không tìm thấy sản phẩm" });
            }

            return Json(new
            {
                status = true,
                quantity = variant.StockQuantity,
                productVariantId = variant.ProductVariantId,
                imageUrl = imageProduct
            });
        }

        private List<int> GetAllChildCategoryIds(CategoryModel category)
        {
            var ids = new List<int> { category.CategoryId };

            foreach (var child in category.Children)
            {
                ids.AddRange(GetAllChildCategoryIds(child));
            }

            return ids;
        }

        [HttpGet]
        public async Task<IActionResult> SearchResult(string? query)
        {
           
            // Bắt đầu query sản phẩm
            var productsQuery = db.Product
                                  .Where(p => p.IsActive);

            // Lọc theo tên sản phẩm (keyword)
            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query));
            }

            // Lấy danh sách sản phẩm sau khi lọc
            var products = await productsQuery.ToListAsync();

            // Lấy danh mục liên quan đến query
            var categories = await db.Categories
                                     .Where(c => c.IsActive
                                                 && c.CategoryName.Contains(query)
                                                 && c.ParentId != null)
                                     .Include(c => c.Children)
                                     .ToListAsync();
            ViewData["SearchQuery"] = query;
            // Trả về model
            var model = new SearchResultViewModel
            {
                Keyword = query,
                Products = products,
                Categories = categories,

            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> SearchResultPartial(string query, int? categoryId, float? minPrice, float? maxPrice)
        {
            var productsQuery = db.Product.Where(p => p.IsActive);

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Lọc theo giá
            if (minPrice.HasValue && minPrice.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            var products = await productsQuery.ToListAsync();

            return PartialView("_ProductList", products);
        }
       
    }
}
