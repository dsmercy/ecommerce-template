namespace Ecommerce.Domain.Interfaces;

/// <summary>
/// Wraps the three MySQL stored procedures for inventory management.
/// sp_reserve_stock      — called when order is placed
/// sp_deduct_stock       — called when payment completes
/// sp_release_reservation — called when order is cancelled / payment fails
/// </summary>
public interface IInventoryProcedures
{
    /// <summary>
    /// Executes sp_reserve_stock.
    /// Atomically checks available stock and increments reserved_quantity.
    /// Returns true when reservation succeeded, false when insufficient stock.
    /// </summary>
    Task<bool> ReserveStockAsync(long variantId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Executes sp_deduct_stock.
    /// Decrements both stock_quantity and reserved_quantity on confirmed payment.
    /// </summary>
    Task DeductStockAsync(long variantId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Executes sp_release_reservation.
    /// Rolls back reserved_quantity only — no stock deduction (order never paid).
    /// </summary>
    Task ReleaseReservationAsync(long variantId, int quantity, CancellationToken ct = default);
}
