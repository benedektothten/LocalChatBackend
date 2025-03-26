namespace LocalChat.Persistence;

public class ChatRoomEntity
{
    public int ChatRoomId { get; set; } // Maps to chat.chatrooms.chatroomid
    public string Name { get; set; } = null!; // Maps to chat.chatrooms."name"
    public bool IsPrivate { get; set; } = false; // Maps to chat.chatrooms.isprivate
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Maps to chat.chatrooms.createdat

    // Navigation Property
    public ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
    
    // Navigation property to Users
    public ICollection<UserChatRoomEntity> UserChatRooms { get; set; } = new List<UserChatRoomEntity>();
}