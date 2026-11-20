namespace ShopElegance.Core.Enums;

public enum ProductType
{
    Physical,
    Digital,
    Both
}

public enum PaymentMethod
{
    VodafoneCash,
    InstaPay,
    CashOnDelivery
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded,
    OnHold
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded,
    PartiallyRefunded
}
