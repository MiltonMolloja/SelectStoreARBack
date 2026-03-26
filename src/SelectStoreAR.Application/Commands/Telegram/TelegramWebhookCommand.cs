using MediatR;

namespace SelectStoreAR.Application.Commands.Telegram;

public sealed record TelegramWebhookCommand(
    TelegramUpdate Update) : IRequest<TelegramWebhookResult>;

public sealed record TelegramWebhookResult(
    string Status,
    string Action,
    string? Reason = null,
    Guid? ProductId = null,
    string? ProductName = null);

// Telegram API models
public sealed record TelegramUpdate(
    long UpdateId,
    TelegramMessage? ChannelPost,
    TelegramMessage? EditedChannelPost);

public sealed record TelegramMessage(
    long MessageId,
    TelegramChat Chat,
    long Date,
    string? Text,
    string? Caption,
    IReadOnlyList<TelegramPhotoSize>? Photo);

public sealed record TelegramChat(
    long Id,
    string? Title,
    string? Type);

public sealed record TelegramPhotoSize(
    string FileId,
    string FileUniqueId,
    int Width,
    int Height,
    long? FileSize);
