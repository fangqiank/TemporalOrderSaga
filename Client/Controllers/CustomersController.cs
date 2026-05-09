using Client.Data;
using Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Client.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll()
        => Ok(await db.Customers.OrderBy(c => c.Name).ToListAsync());
}
