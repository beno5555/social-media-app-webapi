using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.BusinessLogic.Services.Main;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
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
        return Ok();
    }
}