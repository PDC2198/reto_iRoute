namespace CommerceApi.Models;

public class CommerceCsvRow { public DateTime PcProcessDate { get; set; } public string? PcNomcomred { get; set; } public string? PcNumdoc { get; set; } public string? PcAddress { get; set; } public string? PcCity { get; set; } }
public record QuarantineRow(
    int Id,
    DateTime? PcProcessDate,
    string? PcNomcomred,
    string? PcNumdoc,
    string? PcAddress,
    string? PcCity,
    string Motivo
);
