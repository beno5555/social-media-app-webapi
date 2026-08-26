using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Services.Main;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
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
    public async Task<ActionResult<ApplicationResponse<DisplayUserDto>>> GetUser(int id)
    {
        return Ok("successful");
    }
}