using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Users.Friends;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Content;

[Route("api/messages")]
[Authorize]
public class MessageController : BaseController
{
    private readonly MessageService _messageService;
    
    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }
    
    [HttpPost]
    [Route("{receiverId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.SendMessage)]
    public async Task<ActionResult<ApplicationResponse<SentMessageDto>>> SendMessage(int receiverId, [FromBody] CreateMessageDto createMessageDto)
    {
        var userId   = GetUserId();
        var response = await _messageService.SendMessageAsync(userId, receiverId, createMessageDto);

        if (response.Succeeded)
        {
            return Created($"/api/messages/{response.Data!.Id}", response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Route("conversation/{otherUserId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadMessages)]
    public async Task<ActionResult<ListResponse<StandardMessageDto>>> GetConversation(int otherUserId, [FromQuery] PageQuery query) 
    {
        var userId = GetUserId();
        var response = await  _messageService.GetConversationAsync(userId, otherUserId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpGet]
    [Route("friends/conversation")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadMessages)]
    public async Task<ActionResult<ListResponse<ConversationFriendDto>>> GetConversationFriends([FromQuery] PageQuery query) 
    {
        var userId   = GetUserId();
        var response = await  _messageService.GetConversationFriendsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpGet]
    [Route("friends/no-conversation")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadMessages)]
    public async Task<ActionResult<ListResponse<DisplayFriendDto>>> GetNonConversationFriends([FromQuery] PageQuery query) 
    {
        var userId = GetUserId();
        var response = await _messageService.GetNonConversationFriendsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpGet]
    [Route("conversations/unread")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadMessages)]
    public async Task<ActionResult<int>> Unread() 
    {
        var userId = GetUserId();
        var response = await _messageService.GetUnreadConversationsCount(userId);
        return Ok(new { UnreadCount = response });
    }

    [HttpPut]
    [Route("conversation/{otherUserId:int}/mark-as-read")]
    [EnableRateLimiting(RateLimitConfig.Policies.MarkConversationAsRead)]
    public async Task<ActionResult> MarkAsRead(int otherUserId)
    {
        int userId = GetUserId();
        await _messageService.MarkAsReadAsync(userId, otherUserId);
        return NoContent();
    }

    [HttpPut]
    [Route("{id:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.EditMessage)]
    public async Task<ActionResult<ApplicationResponse<StandardMessageDto>>> EditMessage(int id, EditMessageDto editMessageDto)
    {
        var userId   = GetUserId();
        var response = await _messageService.EditMessage(id, editMessageDto, userId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.DeleteMessage)]
    public async Task<ActionResult<ApplicationResponse>> DeleteMessage(int id)
    {
        var userId   = GetUserId();
        var response = await _messageService.DeleteMessage(id, userId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}