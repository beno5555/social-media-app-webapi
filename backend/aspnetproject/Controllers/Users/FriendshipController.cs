using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Users;

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
    [EnableRateLimiting(RateLimitConfig.Policies.SendFriendRequest)]
    public async Task<ActionResult<ApplicationResponse<MinimalFriendshipDto>>> SendRequest(int addresseeId) 
    {
        int userId   = GetUserId();
        var response = await _friendshipService.SendRequest(userId, addresseeId);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetRelationship), new { friendId = response.Data!.AddresseeId }, response);
        }
        
        return BadRequest(response);
    }

    [HttpGet]
    [Route("{friendId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> GetRelationship(int friendId) 
    {
        int userId   = GetUserId();
        var response = await _friendshipService.GetRelationshipAsync(userId, friendId);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }

    [HttpGet]
    [Route("accepted/{friendId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
    public async Task<ActionResult<ApplicationResponse<AcceptedFriendshipDto>>> GetAcceptedFriendship(int friendId)
    {
        int userId   = GetUserId();
        var response = await _friendshipService.GetAcceptedFriendshipAsync(userId, friendId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
    
    [HttpGet]
    [Route("accepted")]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
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
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
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
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
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
    [EnableRateLimiting(RateLimitConfig.Policies.ReadFriendships)]
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
    [EnableRateLimiting(RateLimitConfig.Policies.RespondFriendRequest)]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> AcceptRequest(int requesterId) 
    {
        int userId   = GetUserId();
        var response = await _friendshipService.RespondToRequestAsync(requesterId, userId, FriendshipStatus.Accepted);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPut]
    [Route("{requesterId:int}/decline")]
    [EnableRateLimiting(RateLimitConfig.Policies.RespondFriendRequest)]
    public async Task<ActionResult<ApplicationResponse<StandardFriendshipDto>>> DeclineRequest(int requesterId) 
    {
        int userId   = GetUserId();
        var response = await _friendshipService.RespondToRequestAsync(requesterId, userId, FriendshipStatus.Declined);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{friendId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.RemoveFriend)]
    public async Task<ActionResult<ApplicationResponse>> RemoveRelationship(int friendId) 
    {
        int userId   = GetUserId();
        var response = await _friendshipService.RemoveRelationshipAsync(userId, friendId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}