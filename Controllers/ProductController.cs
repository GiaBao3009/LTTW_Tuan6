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

        // DELETE: Product/DeleteImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            try
            {
                var image = await _productRepository.GetImageByIdAsync(imageId);
                if (image == null || image.ProductId != productId)
                {
                    return NotFound();
                }

                await DeleteSingleImage(image);
                TempData["Success"] = "Hình ảnh đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
                TempData["Error"] = "Có lỗi xảy ra khi xóa hình ảnh.";
            }

            // Redirect back to the product edit page or details page
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _productRepository.ExistsAsync(id);
        }

        private async Task<ImageUploadResult> ProcessUploadedImages(IFormFileCollection images)
        {
            var uploadResults = new ImageUploadResult();
            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            foreach (var file in images)
            {
                var validationError = ValidateImageFile(file);
                if (!string.IsNullOrEmpty(validationError))
                {
                    uploadResults.Errors.Add($"File {file.FileName}: {validationError}");
                    continue;
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadResults.ProductImages.Add(new ProductImage
                {
                    Url = $"/images/products/{uniqueFileName}"
                });
            }

            return uploadResults;
        }

        private string ValidateImageFile(IFormFile file)
        {
            if (file == null)
            {
                return "File is null.";
            }

            if (file.Length == 0)
            {
                return "File is empty.";
            }

            if (file.Length > _maxFileSize)
            {
                return $"File size exceeds the limit of {_maxFileSize / (1024 * 1024)} MB.";
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !_allowedExtensions.Contains(fileExtension))
            {
                return $"Invalid file type. Only {string.Join(", ", _allowedExtensions)} are allowed.";
            }

            return string.Empty; // No error
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
            if (image == null) return;

            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            // Also remove from database via repository if needed, but DeleteAsync handles product image cascade delete
            // await _productRepository.DeleteImageAsync(image.Id); // Example if needed separately
        }
    }

    public class ImageUploadResult
    {
        public List<ProductImage> ProductImages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }
}