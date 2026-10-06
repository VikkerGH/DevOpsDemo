using System.ComponentModel.DataAnnotations;

namespace DevOpsDemo.Contracts;

/// <summary>Alle redigerbare oplysninger om en bog, da PUT erstatter hele indholdet.</summary>
public sealed record UpdateBookRequest
{
    [Required(ErrorMessage = "Titel skal udfyldes.")]
    [MaxLength(200, ErrorMessage = "Titel kan maksimalt have 200 tegn.")]
    public required string Title { get; init; }

    [Required(ErrorMessage = "Forfatter skal udfyldes.")]
    [MaxLength(120, ErrorMessage = "Forfatter kan maksimalt have 120 tegn.")]
    public required string Author { get; init; }

    public required bool IsRead { get; init; }
}
