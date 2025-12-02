using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Helpper;
using ProjectTest1.Migrations;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;

namespace ProjectTest1.Controllers
{
    public class ProductVariantController : Controller
    {
        private readonly DataContext db;
        private readonly CloudinaryHelper _cloudinaryService;
        public ProductVariantController(DataContext db, CloudinaryHelper cloudinaryService)
        {
            this.db = db;
            _cloudinaryService = cloudinaryService;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            if (productId <= 0)
            {
                return RedirectToAction("Index", "Product"); // Quay về danh sách sản phẩm nếu id sai
            }

            // Lấy thông tin sản phẩm
            var product = await db.Product
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            // Lấy tất cả màu và size có trong biến thể của sản phẩm đó
            // Lấy màu và size theo ProductId, cả Id và Name
            var colors = await db.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => new { v.ColorId, v.Color.ColorName })
                .Distinct()
                .ToListAsync();

            var sizes = await db.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => new { v.SizeId, v.Size.SizeName })
                .Distinct()
                .ToListAsync();

            // Gộp dữ liệu vào ViewModel
            var model = new ProductVariantPageViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Colors = colors
                    .Select(c => new ColorViewModel
                    {
                        ColorId = c.ColorId,
                        ColorName = c.ColorName
                    })
                    .ToList(),
                Sizes = sizes
                    .Select(s => new SizeViewModel
                    {
                        SizeId = s.SizeId,
                        SizeName = s.SizeName
                    })
                    .ToList()
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ListData(int productId)
        {
            if (productId <= 0)
                return BadRequest("Product ID không hợp lệ");

            try
            {
                // Lấy danh sách variant kèm product, color, size và ảnh theo màu
                var variants = await db.ProductVariants
                    .Include(v => v.Product)
                    .Include(v => v.Color)
                    .Include(v => v.Size)
                    .Where(v => v.ProductId == productId)
                    .Select(v => new ProductVariantViewModel
                    {
                        ProductVariantId = v.ProductVariantId,
                        ProductId = v.ProductId,
                        ColorId = v.ColorId,
                        ColorName = v.Color.ColorName,
                        SizeId = v.SizeId,
                        SizeName = v.Size.SizeName,
                        StockQuantity = v.StockQuantity,
                        // ✅ Nếu variant.Price null → lấy giá của product
                        Price = v.Price ?? v.Product.Price ?? 0,

                        // ✅ Join sang bảng ProductColorImage để lấy ảnh theo ColorId
                        ImageUrl = db.ProductColorImages
                            .Where(ci => ci.ProductId == v.ProductId && ci.ColorId == v.ColorId)
                            .Select(ci => ci.ImageUrl)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return PartialView(variants);
            }
            catch (Exception ex)
            {
                // Ghi log nếu cần
                return StatusCode(500, "Đã xảy ra lỗi khi lấy dữ liệu.");
            }
        }

        public async Task<IActionResult> Create(int productId)
        {
            if(productId <= 0)
            {
                return RedirectToAction("Index", "Product");
            }
            var model = new ProductVariantPageViewModel
            {
                ProductId = productId,
                Colors = await db.Colors
                         .Select(c => new ColorViewModel
                         {
                             ColorId = c.ColorId,
                             ColorName = c.ColorName,
                         }).ToListAsync(),
                Sizes = await db.Sizes
                        .Select(s => new SizeViewModel
                        {
                            SizeId = s.SizeId,
                            SizeName = s.SizeName
                        }).ToListAsync()
            };
            if (model.Colors == null || model.Sizes == null)
            {
                return NotFound("Không tìm thấy màu sắc hoặc kích thước nào.");
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? productVariantId)
        {
            if (productVariantId<=0 || productVariantId == null)
            {
                return RedirectToAction("Index", "Product");
            }
            var variant = await db.ProductVariants
                .Include(v => v.Color)
                .Include(v => v.Size)
                .FirstOrDefaultAsync(v => v.ProductVariantId == productVariantId);
            if (variant == null)
            {
                return NotFound("Không tìm thấy biến thể sản phẩm");
            }
            var product = await db.Product
                .FirstOrDefaultAsync(p => p.Id == variant.ProductId);
            if (product == null)
                return NotFound("Không tìm thấy sản phẩm");
            var model = new ProductVariantViewModel
            {
                ProductVariantId = variant.ProductVariantId,
                ProductId = variant.ProductId,
                ColorName = variant.Color.ColorName,
                SizeName = variant.Size.SizeName,
                StockQuantity = variant.StockQuantity,
                SKU = variant.SKU,
                ImageUrl = variant.ImageUrl,
                IsActive = variant.IsActive,
                Price = (variant.Price ?? product.Price) ?? 0f
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(ProductVariantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            
            try
            {
                var existingVariant = await db.ProductVariants
               .FirstOrDefaultAsync(v => v.ProductId == model.ProductId
                                      && v.ColorId == model.ColorId
                                      && v.SizeId == model.SizeId);

                if (existingVariant != null)
                {
                    return Json(new { success = false, message = "Biến thể này đã tồn tại!" });
                }
                // Tạo entity mới từ ViewModel
                var variant = new ProductVariantModel
                {
                    ProductId = model.ProductId ?? 0,   // hoặc giá trị mặc định hợp lý
                    SizeId = model.SizeId ?? 0,
                    ColorId = model.ColorId ?? 0,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    SKU = $"{model.ProductId}-{model.ColorId}-{model.SizeId}",
                    IsActive = true
                    
                };

                // Thêm vào DB
                db.ProductVariants.Add(variant);
                await db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm biến thể thành công",
                    
                });
            }
            catch (Exception ex)
            {
                // Log ra console hoặc hệ thống logging
                Console.WriteLine(ex.ToString());

                // Trả JSON với chi tiết lỗi (debug only)
                return Json(new
                {
                    success = false,
                    message = "Đã có lỗi xảy ra",
                    detail = ex.ToString() // bao gồm stack trace và inner exception
                });
            }

        }
        [HttpPost]
        public async Task<IActionResult> EditPost(ProductVariantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => new {
                        Field = ms.Key,
                        Messages = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                foreach (var err in errors)
                {
                    Console.WriteLine($"Field {err.Field} lỗi: {string.Join(", ", err.Messages)}");
                }

                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
            }


            try
            {
                var variant = await db.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductVariantId == model.ProductVariantId);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm." });
                }
                // Cập nhật các trường từ ViewModel
                variant.StockQuantity += model.StockQuantity;
                variant.Price = model.Price;
                db.ProductVariants.Update(variant);
                await db.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Cập nhật biến thể thành công"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Đã có lỗi xảy ra",
                    detail = ex.ToString() // bao gồm stack trace và inner exception
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UploadVariantImage(int ProductId, int ColorId, IFormFile ImageFile)
        {
            if (ImageFile == null || ImageFile.Length == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng chọn ảnh để upload."
                });
            }

            try
            {
                // Upload ảnh lên Cloudinary
                var url = await _cloudinaryService.UploadImageAsync(ImageFile);

                // Tìm bản ghi ProductColorImage theo ProductId + ColorId
                var colorImage = await db.ProductColorImages
                    .FirstOrDefaultAsync(c => c.ProductId == ProductId && c.ColorId == ColorId);

                if (colorImage == null)
                {
                    // Nếu chưa có, tạo mới
                    db.ProductColorImages.Add(new ProductColorImageModel
                    {
                        ProductId = ProductId,
                        ColorId = ColorId,
                        ImageUrl = url
                    });
                }
                else
                {
                    // Nếu đã có, cập nhật ảnh
                    colorImage.ImageUrl = url;
                    db.ProductColorImages.Update(colorImage);
                }

                await db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Upload ảnh thành công!",
                    imageUrl = url
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Upload ảnh thất bại: {ex.Message}"
                });
            }
        }

    }
}
