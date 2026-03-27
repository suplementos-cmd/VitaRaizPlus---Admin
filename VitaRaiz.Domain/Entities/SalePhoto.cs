namespace VitaRaiz.Domain.Entities;

public class SalePhoto
{
    public int PhotoId { get; set; }
    public int SaleId { get; set; }
    public string PhotoType { get; set; } = string.Empty; // FACHADA, CLIENTE, CONTRATO, ADICIONAL
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int? UploadedBy { get; set; }
    public bool Synced { get; set; } = true;

    // Navigation properties
    public Sale? Sale { get; set; }
    public User? UploadedByUser { get; set; }
}
