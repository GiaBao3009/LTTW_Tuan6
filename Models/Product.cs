using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LTTW_Tuan6.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DisplayName("Mã sản phẩm")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        [DisplayName("Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [DisplayName("Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        [DisplayName("Giá bán")]
        public decimal Price { get; set; }

        [DisplayName("Hình ảnh sản phẩm")]
        public List<ProductImage> Images { get; set; } = new();

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [ForeignKey("Category")]
        [DisplayName("Mã danh mục")]
        public int CategoryId { get; set; }

        [DisplayName("Danh mục")]
        public Category? Category { get; set; }
    }
}
