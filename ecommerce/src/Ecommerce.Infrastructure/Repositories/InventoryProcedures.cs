using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Ecommerce.Infrastructure.Repositories;

/// <summary>
/// Calls the three MySQL stored procedures via EF Core raw SQL execution.
///
/// sp_reserve_stock(variantId, quantity, OUT success)
///   — used in OrderService.CreateOrderAsync  : atomically reserve stock before order is confirmed
///
/// sp_deduct_stock(variantId, quantity)
///   — used in PaymentService.UpdateStatusAsync : permanently deduct stock on COMPLETED payment
///
/// sp_release_reservation(variantId, quantity)
///   — used in OrderService.UpdateStatusAsync   : undo reservation when order is CANCELLED
/// </summary>
public class InventoryProcedures : IInventoryProcedures
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryProcedures> _logger;

    public InventoryProcedures(AppDbContext context, ILogger<InventoryProcedures> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ReserveStockAsync(long variantId, int quantity, CancellationToken ct = default)
    {
        // MySQL stored procedure with OUT parameter
        // CALL sp_reserve_stock(variantId, quantity, @p_success);
        // SELECT @p_success;
        var conn = _context.Database.GetDbConnection();

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL sp_reserve_stock(@p_variant_id, @p_quantity, @p_success)";

            cmd.Parameters.Add(new MySqlParameter("@p_variant_id", variantId));
            cmd.Parameters.Add(new MySqlParameter("@p_quantity", quantity));

            var outParam = new MySqlParameter("@p_success", MySqlDbType.Byte)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync(ct);

            var success = Convert.ToByte(outParam.Value) == 1;

            _logger.LogInformation(
                "sp_reserve_stock: variant={VariantId} qty={Qty} success={Success}",
                variantId, quantity, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "sp_reserve_stock failed: variant={VariantId} qty={Qty}", variantId, quantity);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeductStockAsync(long variantId, int quantity, CancellationToken ct = default)
    {
        // CALL sp_deduct_stock(variantId, quantity);
        var conn = _context.Database.GetDbConnection();

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL sp_deduct_stock(@p_variant_id, @p_quantity)";
            cmd.Parameters.Add(new MySqlParameter("@p_variant_id", variantId));
            cmd.Parameters.Add(new MySqlParameter("@p_quantity", quantity));

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "sp_deduct_stock: variant={VariantId} qty={Qty}", variantId, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "sp_deduct_stock failed: variant={VariantId} qty={Qty}", variantId, quantity);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseReservationAsync(long variantId, int quantity, CancellationToken ct = default)
    {
        // CALL sp_release_reservation(variantId, quantity);
        var conn = _context.Database.GetDbConnection();

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL sp_release_reservation(@p_variant_id, @p_quantity)";
            cmd.Parameters.Add(new MySqlParameter("@p_variant_id", variantId));
            cmd.Parameters.Add(new MySqlParameter("@p_quantity", quantity));

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "sp_release_reservation: variant={VariantId} qty={Qty}", variantId, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "sp_release_reservation failed: variant={VariantId} qty={Qty}", variantId, quantity);
            throw;
        }
    }
}
