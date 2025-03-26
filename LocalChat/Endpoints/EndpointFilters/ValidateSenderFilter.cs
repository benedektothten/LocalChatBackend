using LocalChat.DTOs;
using LocalChat.Validators;
using LocalChat.Validators.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LocalChat.Endpoints.EndpointFilters;

public class ValidateSenderFilter(ISenderValidator senderValidator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Check the type of the first argument
        var request = context.Arguments[0] switch
        {
            // If the first argument is already a ChatRoomAccess instance, use it directly
            ChatRoomAccess instance => instance,

            // If the first argument is an int, assume it's `chatRoomId` and the second is `senderId`
            int chatRoomId when context.Arguments.Count > 1 && context.Arguments[1] is int senderId =>
                new ChatRoomAccess(chatRoomId, senderId),

            // If neither match, throw an invalid operation exception (unexpected situation)
            _ => throw new InvalidOperationException("Invalid endpoint arguments provided to ValidateSenderFilter.")
        };
        
        if (!await senderValidator.IsSenderPartOfChatroomAsync(request.ChatRoomId, request.SenderId))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}