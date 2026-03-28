namespace SelectStoreAR.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida de un cambio pendiente.
/// </summary>
public enum PendingChangeStatus
{
    /// <summary>Esperando revisión del admin.</summary>
    Pending = 0,

    /// <summary>Aprobado — el cambio fue aplicado al producto.</summary>
    Approved = 1,

    /// <summary>Rechazado — el producto no fue modificado.</summary>
    Rejected = 2,
}
