using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/users")]
[Authorize]
public class UserController : BaseController
{
    private readonly AccountService _accountService;

    public UserController(AccountService accountService)
    {
        _accountService = accountService;
    }
        
    [HttpGet]
    [Route("{id:int}")]
    public async Task<ActionResult<ApplicationResponse<StandardUserDto>>> GetUser(int id)
    {
        return Ok("successful");
    }
}