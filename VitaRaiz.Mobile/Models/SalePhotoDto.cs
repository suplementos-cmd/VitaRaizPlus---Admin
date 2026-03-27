namespace VitaRaiz.Mobile.Models;

public class SalePhotoDto
{
    public int PhotoId { get; set; }
    public int SaleId { get; set; }
    public string PhotoType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public int? UploadedBy { get; set; }
    public bool Synced { get; set; }
}
