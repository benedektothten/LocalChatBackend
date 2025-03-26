namespace LocalChat.Persistence;

public class UserEntity
{
    public int UserId { get; set; } // Maps to chat.users.userid
    public string Username { get; set; } = null!; // Maps to chat.users.username
    public string Email { get; set; } = null!; // Maps to chat.users.email
    public string PasswordHash { get; set; } = null!; // Maps to chat.users.passwordhash
    public string? DisplayName { get; set; } // Maps to chat.users.displayname
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Maps to chat.users.createdat
    
    public string? GoogleId { get; set; } // Maps to chat.users.googleid
    
    public string? AvatarUrl { get; set; } // Maps to chat.users.avatarurl

    // Navigation Property
    public ICollection<MessageEntity> Messages { get; set; } = new HashSet<MessageEntity>();
    
    // Navigation property for ChatRoom Membership
    public ICollection<UserChatRoomEntity> UserChatRooms { get; set; } = new List<UserChatRoomEntity>();

}