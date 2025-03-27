using LocalChat.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Common.Persistence;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    // DbSets for entities
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ChatRoomEntity> ChatRooms { get; set; }
    public DbSet<UserChatRoomEntity> UserChatRooms { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly map entities to the "chat" schema
        modelBuilder.HasDefaultSchema("chat");
    
        // Users Mapping
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
    
            entity.HasKey(e => e.UserId)
                .HasName("users_pkey");
    
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("passwordhash").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("displayname");
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.GoogleId).HasColumnName("googleuserid");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatarurl");
    
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("users_email_key");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("users_username_key");
            
            // Relationship to Messages
            entity.HasMany(u => u.Messages) // One User can send many Messages
                .WithOne(m => m.Sender)     // Each Message has one Sender
                .HasForeignKey(m => m.SenderId) // Use SenderId as the foreign key
                .HasConstraintName("messages_senderid_fkey")
                .OnDelete(DeleteBehavior.Cascade); // Enforce cascading deletes
    
    
        });
    
        // ChatRooms Mapping
        modelBuilder.Entity<ChatRoomEntity>(entity =>
        {
            entity.ToTable("chatrooms");
    
            entity.HasKey(e => e.ChatRoomId)
                .HasName("chatrooms_pkey");
    
            entity.Property(e => e.ChatRoomId).HasColumnName("chatroomid");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsPrivate).HasColumnName("isprivate").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    
        // Messages Mapping
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.ToTable("messages");
    
            entity.HasKey(e => e.MessageId)
                .HasName("messages_pkey");
    
            entity.Property(e => e.MessageId).HasColumnName("messageid");
            entity.Property(e => e.ChatRoomId).HasColumnName("chatroomid").IsRequired();
            entity.Property(e => e.SenderId).HasColumnName("senderid").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.SentAt).HasColumnName("sentat").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UniqueId).HasColumnName("uniqueid").IsRequired();
            entity.Property(e => e.IsGif).HasColumnName("isgif").HasDefaultValue(false);
    
            // Relationship to ChatRoom
            entity.HasOne(m => m.ChatRoom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .HasConstraintName("messages_chatroomid_fkey")
                .OnDelete(DeleteBehavior.Cascade);
    
            // Relationship to UserEntity (Sender)
            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages) // Specify that Sender has many Messages
                .HasForeignKey(m => m.SenderId)
                .HasPrincipalKey(u => u.UserId) // Map explicitly to UserId
                .HasConstraintName("messages_senderid_fkey")
                .OnDelete(DeleteBehavior.Cascade);
    
        });
        
        modelBuilder.Entity<UserChatRoomEntity>(entity =>
        {
            entity.ToTable("userchatrooms");
    
            entity.HasKey(e => e.UserChatRoomId) // Primary key
                .HasName("userchatrooms_pkey");
    
            entity.Property(e => e.UserChatRoomId).HasColumnName("userchatroomid");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.ChatRoomId).HasColumnName("chatroomid").IsRequired();
            
            entity.Property(e => e.JoinedAt)
                .HasColumnName("joinedat")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
    
            // Foreign Key to Users
            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserChatRooms)
                .HasForeignKey(uc => uc.UserId)
                .HasConstraintName("userchatrooms_userid_fkey")
                .OnDelete(DeleteBehavior.Cascade);
    
            // Foreign Key to ChatRooms
            entity.HasOne(uc => uc.ChatRoom)
                .WithMany(cr => cr.UserChatRooms)
                .HasForeignKey(uc => uc.ChatRoomId)
                .HasConstraintName("userchatrooms_chatroomid_fkey")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}