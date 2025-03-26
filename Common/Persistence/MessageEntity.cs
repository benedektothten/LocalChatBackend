using System;
using LocalChat.Persistence;

public class MessageEntity
{
    public int MessageId { get; set; } // Maps to chat.messages.messageid
    public int ChatRoomId { get; set; } // Maps to chat.messages.chatroomid
    public int SenderId { get; set; } // Maps to chat.messages.senderid
    public string Content { get; set; } = null!; // Maps to chat.messages."content"
    public DateTime SentAt { get; set; } = DateTime.UtcNow; // Maps to chat.messages.sentat
    public Guid UniqueId { get; set; } = Guid.NewGuid(); // Maps to chat.messages.messageguid
    
    public bool IsGif { get; set; } = false; // Maps to chat.messages.isgifs

    // Navigation properties
    public ChatRoomEntity ChatRoom { get; set; } // Foreign key relationship to ChatRoom
    public UserEntity Sender { get; set; } // Foreign key relationship to User
}