using LTTW_Tuan6.Models;

namespace LTTW_Tuan6.Repository
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetAllWithCategoriesAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByIdWithCategoryAsync(int id);
        Task<Product?> GetByIdWithImagesAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
