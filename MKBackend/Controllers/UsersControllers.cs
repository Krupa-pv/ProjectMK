using Microsoft.AspNetCore.Mvc;
using ModelsUser = MKBackend.Models.User;
using MKBackend.Services;

namespace MKBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CosmosDBService _cosmosDbService;

    public UsersController(CosmosDBService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] ModelsUser user)
    {
        await _cosmosDbService.AddUpdateUserAsync(user);
        return Ok();
    }



    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _cosmosDbService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }
}
