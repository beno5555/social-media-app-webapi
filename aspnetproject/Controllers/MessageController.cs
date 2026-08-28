using aspnetproject.Common.Dtos.Common;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

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
    public async Task<ActionResult<ApplicationResponse<SentMessageDto>>> SendMessage(
        int                         receiverId, 
        [FromBody] CreateMessageDto createMessageDto
    )
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
    public async Task<ActionResult<ListResponse<StandardMessageDto>>> GetConversation(int otherUserId, [FromQuery] PageQuery query) // added
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
    public async Task<ActionResult> GetConversationFriends([FromQuery] PageQuery query) // added
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
    public async Task<ActionResult> GetNonConversationFriends([FromQuery] PageQuery query) // added
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
    [Route("conversation/unread")]
    public async Task<ActionResult<int>> Unread() // added
    {
        var userId = GetUserId();
        var response = await _messageService.GetUnreadConversationsCount(userId);
        return Ok(response);
    }

    [HttpPut]
    [Route("conversation/{otherUserId:int}/mark-as-read")]
    public async Task<ActionResult> MarkAsRead(int otherUserId)
    {
        int userId = GetUserId();
        await _messageService.MarkAsReadAsync(userId, otherUserId);
        return Ok();
    }

    [HttpPut]
    [Route("{id:int}")]
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