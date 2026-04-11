using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TourMap.AdminWeb.ViewModels;

public class TourEditViewModel
{
    public string? Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> SelectedPoiIds { get; set; } = new();

    public List<SelectListItem> AvailablePois { get; set; } = new();
}
