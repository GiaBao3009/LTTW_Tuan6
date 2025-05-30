using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LTTW_Tuan6.Models;
using LTTW_Tuan6.Repository;

namespace LTTW_Tuan6.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;

        // Allowed file extensions and max file size
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productRepository.GetAllWithCategoriesAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách sản phẩm.";
                return View(new List<Product>());
            }
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdWithCategoryAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["Error"] = "Có lỗi xảy ra khi tải form tạo sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFileCollection images)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Process uploaded images
                    if (images != null && images.Count > 0)
                    {
                        var uploadResults = await ProcessUploadedImages(images);
                        if (uploadResults.HasErrors)
                        {
                            foreach (var error in uploadResults.Errors)
                            {
                                ModelState.AddModelError("", error);
                            }
                        }
                        else
                        {
                            product.Images = uploadResults.ProductImages;
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        await _productRepository.AddAsync(product);
                        TempData["Success"] = "Sản phẩm đã được tạo thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo sản phẩm.");
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdWithImagesAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for product {ProductId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải form chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFileCollection images)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await _productRepository.GetByIdWithImagesAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Process new uploaded images
                    if (images != null && images.Count > 0)
                    {
                        var uploadResults = await ProcessUploadedImages(images);
                        if (uploadResults.HasErrors)
                        {
                            foreach (var error in uploadResults.Errors)
                            {
                                ModelState.AddModelError("", error);
                            }
                        }
                        else
                        {
                            if (existingProduct.Images == null)
                                existingProduct.Images = new List<ProductImage>();

                            existingProduct.Images.AddRange(uploadResults.ProductImages);
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        // Update product properties
                        existingProduct.Name = product.Name;
                        existingProduct.Price = product.Price;
                        existingProduct.Description = product.Description;
                        existingProduct.CategoryId = product.CategoryId;

                        await _productRepository.UpdateAsync(existingProduct);
                        TempData["Success"] = "Sản phẩm đã được cập nhật thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm.");
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdWithCategoryAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdWithImagesAsync(id);
                if (product != null)
                {
                    // Delete associated images from the file system
                    if (product.Images != null)
                    {
                        await DeleteProductImages(product.Images);
                    }
                    await _productRepository.DeleteAsync(id);
                    TempData["Success"] = "Sản phẩm đã được xóa thành công!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Product/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            try
            {
                var product = await _productRepository.GetByIdWithImagesAsync(productId);
                if (product == null)
                {
                    return NotFound();
                }

                var image = product.Images?.FirstOrDefault(i => i.Id == imageId);
                if (image == null)
                {
                    return NotFound();
                }

                // Delete the image file from the file system
                await DeleteSingleImage(image);

                // Remove the image from the product's collection
                product.Images?.Remove(image);

                // Update the product to save the changes
                await _productRepository.UpdateAsync(product);

                TempData["Success"] = "Hình ảnh đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId} from product {ProductId}", imageId, productId);
                TempData["Error"] = "Có lỗi xảy ra khi xóa hình ảnh.";
            }

            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        private async Task<bool> ProductExists(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product != null;
        }

        private async Task<ImageUploadResult> ProcessUploadedImages(IFormFileCollection images)
        {
            var result = new ImageUploadResult();
            var productImages = new List<ProductImage>();

            foreach (var image in images)
            {
                if (image.Length <= 0) continue;

                // Validate file
                var validationError = ValidateImageFile(image);
                if (!string.IsNullOrEmpty(validationError))
                {
                    result.Errors.Add(validationError);
                    continue;
                }

                try
                {
                    // Create uploads directory if not exists
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);

                    // Generate unique filename
                    string fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    // Create ProductImage entity
                    var productImage = new ProductImage
                    {
                        Url = $"/images/products/{uniqueFileName}",
                        FileName = uniqueFileName,
                        OriginalFileName = image.FileName
                    };

                    productImages.Add(productImage);
                    _logger.LogInformation("Successfully uploaded image: {FileName}", uniqueFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading image: {FileName}", image.FileName);
                    result.Errors.Add($"Lỗi khi upload file {image.FileName}: {ex.Message}");
                }
            }

            result.ProductImages = productImages;
            return result;
        }

        private string ValidateImageFile(IFormFile file)
        {
            // Check file size
            if (file.Length > _maxFileSize)
            {
                return $"File {file.FileName} vượt quá kích thước cho phép (5MB).";
            }

            // Check file extension
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return $"File {file.FileName} không đúng định dạng. Chỉ chấp nhận: {string.Join(", ", _allowedExtensions)}";
            }

            // Check if file is actually an image
            if (!file.ContentType.StartsWith("image/"))
            {
                return $"File {file.FileName} không phải là hình ảnh hợp lệ.";
            }

            return string.Empty;
        }

        private async Task DeleteProductImages(ICollection<ProductImage> images)
        {
            if (images == null) return;

            foreach (var image in images)
            {
                await DeleteSingleImage(image);
            }
        }

        private async Task DeleteSingleImage(ProductImage image)
        {
            if (string.IsNullOrEmpty(image.Url))
                return;

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting image file {FilePath}", filePath);
                    throw;
                }
            }
        }
    }

    // Helper classes
    public class ImageUploadResult
    {
        public List<ProductImage> ProductImages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }
}