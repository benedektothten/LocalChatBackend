namespace LocalChat.Validators.Interfaces;

public interface ISenderValidator
{
    Task<bool> IsSenderPartOfChatroomAsync(int chatRoomId, int senderId);
}