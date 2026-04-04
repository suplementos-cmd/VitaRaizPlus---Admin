using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class SalePhotoRepository : BaseOracleRepository, ISalePhotoRepository
{
    public SalePhotoRepository(VitaRaizDbContext context) : base(context)
    {
    }

    public async Task<int> AddSalePhotoAsync(int saleId, string photoType, string filePath,
        decimal? gpsLat, decimal? gpsLon, long? fileSize, int? uploadedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_add_sale_photo");

        var photoIdParam = AddOutputParameter(command, "p_photo_id");

        AddInputParameter(command, "p_sale_id", saleId);
        AddInputParameter(command, "p_photo_type", photoType);
        AddInputParameter(command, "p_file_path", filePath);
        AddInputParameter(command, "p_gps_lat", gpsLat.HasValue ? (object)gpsLat.Value : DBNull.Value);
        AddInputParameter(command, "p_gps_lon", gpsLon.HasValue ? (object)gpsLon.Value : DBNull.Value);
        AddInputParameter(command, "p_file_size", fileSize.HasValue ? (object)fileSize.Value : DBNull.Value);
        AddInputParameter(command, "p_uploaded_by", uploadedBy.HasValue ? (object)uploadedBy.Value : DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return GetOutputValue((OracleParameter)photoIdParam);
    }

    public async Task<List<SalePhoto>> GetSalePhotosAsync(int saleId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_sale_photos");

        AddInputParameter(command, "p_sale_id", saleId);
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();
        
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();
        var photos = new List<SalePhoto>();

        while (await reader.ReadAsync())
        {
            photos.Add(new SalePhoto
            {
                PhotoId = reader.GetInt32(reader.GetOrdinal("photo_id")),
                SaleId = reader.GetInt32(reader.GetOrdinal("sale_id")),
                PhotoType = reader.GetString(reader.GetOrdinal("photo_type")),
                FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                ThumbnailPath = reader.IsDBNull(reader.GetOrdinal("thumbnail_path")) ? null : reader.GetString(reader.GetOrdinal("thumbnail_path")),
                GpsLatitude = reader.IsDBNull(reader.GetOrdinal("gps_latitude")) ? null : reader.GetDecimal(reader.GetOrdinal("gps_latitude")),
                GpsLongitude = reader.IsDBNull(reader.GetOrdinal("gps_longitude")) ? null : reader.GetDecimal(reader.GetOrdinal("gps_longitude")),
                FileSize = reader.IsDBNull(reader.GetOrdinal("file_size")) ? null : reader.GetInt64(reader.GetOrdinal("file_size")),
                UploadedAt = reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? null : reader.GetInt32(reader.GetOrdinal("uploaded_by")),
                Synced = reader.GetString(reader.GetOrdinal("synced")) == "1"
            });
        }

        return photos;
    }

    public async Task<SalePhoto?> GetPhotoByIdAsync(int photoId)
    {
        return await _context.SalePhotos.FindAsync(photoId);
    }

    public async Task<bool> DeleteSalePhotoAsync(int photoId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_sale_photo");

        AddInputParameter(command, "p_photo_id", photoId);

        await command.ExecuteNonQueryAsync();
        return true;
    }
}
