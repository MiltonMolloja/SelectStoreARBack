namespace SelectStoreAR.Domain.Enums;

public enum OrderStatus
{
    Sent = 0,
    Deposited = 1,
    OrderedFromSupplier = 2,
    InTransit = 3,
    ReadyForDelivery = 4,
    Delivered = 5,
    Cancelled = 6,
}
