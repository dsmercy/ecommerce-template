namespace Ecommerce.Domain.Enums;

public enum UserRole
{
    ADMIN,
    CUSTOMER
}

public enum OrderStatus
{
    PENDING,
    PAID,
    SHIPPED,
    DELIVERED,
    CANCELLED
}

public enum PaymentStatus
{
    PENDING,
    COMPLETED,
    FAILED,
    REFUNDED
}

public enum DiscountType
{
    PERCENTAGE,
    FLAT
}
