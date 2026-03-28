namespace SelectStoreAR.Domain.Enums;

/// <summary>
/// Estado de disponibilidad de un producto importado desde Telegram.
///   ✅ = Available (en stock)
///   🏭 = Warehouse (en depósito)
///   🛬 = Arriving (llegando)
///   📭 = OnDemand (a pedido)
///   Unknown = no se pudo determinar
/// </summary>
public enum AvailabilityStatus
{
    Unknown = 0,
    Available = 1,
    Warehouse = 2,
    Arriving = 3,
    OnDemand = 4,
}
