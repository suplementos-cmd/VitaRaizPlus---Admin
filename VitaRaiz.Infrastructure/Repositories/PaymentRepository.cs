using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly VitaRaizDbContext _context;

    public PaymentRepository(VitaRaizDbContext context)
    {
        _context = context;
    }

    public async Task<int> RegisterPaymentAsync(Payment payment)
    {
        // Llamar al procedimiento del paquete Oracle: EM_VITARAIZ_AD.sp_register_payment
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "EM_VITARAIZ_AD.sp_register_payment";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        // Parámetro de salida: p_payment_id
        var paymentIdParam = new OracleParameter("p_payment_id", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(paymentIdParam);

        // Parámetros de entrada
        command.Parameters.Add(new OracleParameter("p_sale_id", payment.SaleId));
        command.Parameters.Add(new OracleParameter("p_collector_id", payment.CollectorId));
        command.Parameters.Add(new OracleParameter("p_amount", payment.Amount));
        command.Parameters.Add(new OracleParameter("p_gps_lat", payment.GpsLatitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lon", payment.GpsLongitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_notes", payment.Notes ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        // Obtener el payment_id generado
        int paymentId = Convert.ToInt32(((OracleDecimal)paymentIdParam.Value).ToInt32());

        return paymentId;
    }

    public async Task<IEnumerable<Payment>> GetPaymentsBySaleIdAsync(int saleId)
    {
        return await _context.Payments
            .Where(p => p.SaleId == saleId)
            .Include(p => p.Collector)
            .Include(p => p.PaymentPhotos)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Sale)
            .Include(p => p.Collector)
            .Include(p => p.PaymentPhotos)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }
}
