namespace LocalChat.DTOs;

public record CreateChatRoomRequest(string Name, bool IsPrivate, int[] membersToAdd);

public record JoinChatRoomRequest(int UserId);

public record LeaveChatRoomRequest(int UserId);

public record ChatRoomResponse(int ChatRoomId, string Name, bool IsPrivate, DateTime CreatedAt);

public record CreateMessageRequest(int ChatRoomId, string Content, bool IsGif, int SenderId) : ChatRoomAccess(ChatRoomId, SenderId);

public record ChatRoomAccess(int ChatRoomId, int SenderId);

// Message DTO
public record MessageDto(
    int MessageId,
    int ChatRoomId,
    int SenderId,
    string Content,
    DateTime SentAt,
    string SenderUsername,
    bool IsGif
);

// User DTO
public record UserDto(
    int UserId,
    string Username,
    string Email,
    string? DisplayName
);

public record LoginResponse(int UserId, string AvatarUrl, bool IsNewUser = false, string? JwtToken = null);

public record UpdateUserDto(string UserName, string DisplayName, string Password);