using aspnetproject.BusinessLogic.Services.Main;
using aspnetproject.Common.Dtos.Common;
using aspnetproject.Common.Dtos.Friendships;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.ProjectConstants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/friendships")]
[Authorize]
public class FriendshipController : BaseController
{
    private readonly FriendshipService _friendshipService;

    public FriendshipController(FriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> GetRelationship(int id)
    {
        var userId   = GetUserId();
        var response = await _friendshipService.GetRelationshipAsync(userId, id);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }
    
    [HttpGet]
    [Route("mine")]
    public async Task<ActionResult<ListResponse<StandardFriendshipDto>>> GetFriendships([FromQuery] PageQuery query)
    {
        int userId   = GetUserId();
        var response = await _friendshipService.GetFriendshipsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    [Route("user/{userId:int}")]
    public async Task<ActionResult<ListResponse<MinimalUserDto>>> GetFriendsOf(int userId, [FromQuery] PageQuery query)
    {
        var response = await _friendshipService.GetFriendshipsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpGet]
    [Route("pending-requests")]
    public async Task<ActionResult<ListResponse<StandardFriendshipDto>>> GetPendingRequests([FromQuery] PageQuery query)
    {
        int userId   = GetUserId();
        var response = await _friendshipService.GetPendingRequestsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    [Route("sent-requests")]
    public async Task<ActionResult<ListResponse<StandardFriendshipDto>>> GetSentRequests([FromQuery] PageQuery query)
    {
        int userId   = GetUserId();
        var response = await _friendshipService.GetSentRequestsAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpPost]
    [Route("{addresseeId:int}")]
    public async Task<ActionResult<ApplicationResponse<MinimalFriendshipDto>>> SendRequest(int addresseeId)
    {
        var userId = GetUserId();
        var response = await _friendshipService.SendRequest(userId, addresseeId);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetRelationship), new { response.Data!.AddresseeId }, response);
        }
        
        return BadRequest(response.Message);
    }

    [HttpPut]
    [Route("{requesterId:int}/accept")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> AcceptRequest(int requesterId)
    {
        return await RespondToRequest(requesterId, FriendshipStatus.Accepted);
    }

    [HttpPut]
    [Route("{requesterId:int}/decline")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> DeclineRequest(int requesterId)
    {
        return await RespondToRequest(requesterId, FriendshipStatus.Declined);
    }
    
    private async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> RespondToRequest(int requesterId, FriendshipStatus status)
    {
        var userId   = GetUserId();
        var response = await _friendshipService.RespondToRequestAsync(requesterId, userId, status);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{friendId:int}")]
    public async Task<ActionResult<ApplicationResponse>> RemoveRelationship(int friendId)
    {
        int userId = GetUserId();
        var response = await _friendshipService.RemoveRelationshipAsync(userId, friendId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}