using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourMap.AdminWeb.Models;

public class TourPoiMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string TourId { get; set; } = string.Empty;

    [Required]
    public string PoiId { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    [ForeignKey(nameof(TourId))]
    public Tour? Tour { get; set; }

    [ForeignKey(nameof(PoiId))]
    public Poi? Poi { get; set; }
}
