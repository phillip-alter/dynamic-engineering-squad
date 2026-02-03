using System.ComponentModel.DataAnnotations;

namespace InfrastructureApp.Models;

public class AddPointsRequest
{
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(64, ErrorMessage = "Display name must be 64 characters or fewer.")]
    public string? DisplayName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = = "Points must be greaer than 0.")]
    public int Points { get; set; }
}