using VitaRaiz.Domain.Entities;

namespace VitaRaiz.Application.Interfaces;

public interface ISalePhotoRepository
{
    Task<int> AddSalePhotoAsync(int saleId, string photoType, string filePath, 
        decimal? gpsLat, decimal? gpsLon, long? fileSize, int? uploadedBy);
    
    Task<List<SalePhoto>> GetSalePhotosAsync(int saleId);
    
    Task<bool> DeleteSalePhotoAsync(int photoId);
}
