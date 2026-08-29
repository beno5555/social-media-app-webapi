using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
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

    [HttpPost]
    [Route("{addresseeId:int}")]
    public async Task<ActionResult<ApplicationResponse<MinimalFriendshipDto>>> SendRequest(int addresseeId) 
    {
        var userId   = GetUserId();
        var response = await _friendshipService.SendRequest(userId, addresseeId);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetRelationship), new { friendId = response.Data!.AddresseeId }, response);
        }
        
        return BadRequest(response);
    }

    [HttpGet]
    [Route("{friendId:int}")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> GetRelationship(int friendId) 
    {
        var userId   = GetUserId();
        var response = await _friendshipService.GetRelationshipAsync(userId, friendId);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }

    [HttpGet]
    [Route("accepted/{friendId:int}")]
    public async Task<ActionResult<ApplicationResponse<AcceptedFriendshipDto>>> GetAcceptedFriendship(int friendId)
    {
        var userId   = GetUserId();
        var response = await _friendshipService.GetAcceptedFriendshipAsync(userId, friendId);

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
    [Route("pending")]
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
    [Route("sent")]
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
    
    [HttpPut]
    [Route("{requesterId:int}/accept")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> AcceptRequest(int requesterId) 
    {
        var userId   = GetUserId();
        var response = await _friendshipService.RespondToRequestAsync(requesterId, userId, FriendshipStatus.Accepted);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPut]
    [Route("{requesterId:int}/decline")]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> DeclineRequest(int requesterId) 
    {
        var userId   = GetUserId();
        var response = await _friendshipService.RespondToRequestAsync(requesterId, userId, FriendshipStatus.Declined);

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