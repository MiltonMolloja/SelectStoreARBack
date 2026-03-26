namespace SelectStoreAR.Application.DTOs;

public sealed record SiteConfigDto(
    string WhatsAppPhone,
    decimal GlobalMarkup,
    string SiteName,
    string InstagramUrl,
    int DeliveryDays);
