using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class Poi
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "Tên địa điểm không được để trống")]
    [Display(Name = "Tên địa điểm")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Vĩ độ (Latitude)")]
    public double Latitude { get; set; }

    [Required]
    [Display(Name = "Kinh độ (Longitude)")]
    public double Longitude { get; set; }

    [Display(Name = "Bán kính kích hoạt (m)")]
    public int RadiusMeters { get; set; } = 50;

    [Display(Name = "Mức độ ưu tiên")]
    public int Priority { get; set; } = 0;

    [Display(Name = "Ảnh minh họa (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Audio (URL)")]
    public string? AudioUrl { get; set; }

    [Display(Name = "Link bản đồ (Google Maps)")]
    public string? MapLink { get; set; }

    // --- AI Multilingual Fields ---
    [Display(Name = "Mô tả (Tiếng Anh)")]
    public string? DescriptionEn { get; set; }
    
    [Display(Name = "Audio (Tiếng Anh)")]
    public string? AudioUrlEn { get; set; }

    [Display(Name = "Mô tả (Tiếng Trung)")]
    public string? DescriptionZh { get; set; }
    
    [Display(Name = "Audio (Tiếng Trung)")]
    public string? AudioUrlZh { get; set; }

    [Display(Name = "Mô tả (Tiếng Hàn)")]
    public string? DescriptionKo { get; set; }
    
    [Display(Name = "Audio (Tiếng Hàn)")]
    public string? AudioUrlKo { get; set; }
}
