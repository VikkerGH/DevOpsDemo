using System.ComponentModel.DataAnnotations;

namespace DevOpsDemo.Contracts;

/// <summary>Oplysninger til at oprette en bog. Id tildeles af serveren.</summary>
public sealed record CreateBookRequest
{
    [Required(ErrorMessage = "Titel skal udfyldes.")]
    [MaxLength(200, ErrorMessage = "Titel kan maksimalt have 200 tegn.")]
    public required string Title { get; init; }

    [Required(ErrorMessage = "Forfatter skal udfyldes.")]
    [MaxLength(120, ErrorMessage = "Forfatter kan maksimalt have 120 tegn.")]
    public required string Author { get; init; }

    public bool IsRead { get; init; }
}
