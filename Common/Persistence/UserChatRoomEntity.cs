namespace LocalChat.Persistence;

public class UserChatRoomEntity
{
    public int UserChatRoomId { get; set; }
    public int UserId { get; set; } // Foreign key to Users
    public int ChatRoomId { get; set; } // Foreign key to ChatRooms

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public UserEntity User { get; set; } = null!;
    public ChatRoomEntity ChatRoom { get; set; } = null!;
}