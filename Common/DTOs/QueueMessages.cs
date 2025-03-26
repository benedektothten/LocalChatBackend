namespace Common.DTOs;

public record QueueMessages(int ChatRoomId, Guid UniqueId, int SenderId, string Content, bool IsGif, DateTime SentAt);