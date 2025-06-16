using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevTest.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text;


namespace RevTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly FinalTestContext _db;


        public ValuesController(FinalTestContext db)
        {
            _db = db;
        }




        [HttpPost]

        public async Task<ActionResult<User>> Register(User user)
        {
            if (user == null)
                return BadRequest();

            user.Password = HashPassword(user.Password); // Hash the password

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(user);
        }
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash); // returns uppercase hex
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Invalid credentials");

            string hashedPassword = HashPassword(user.Password);

            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username && u.Password == hashedPassword);

            if (existingUser == null)
                return Unauthorized("Incorrect username or password");

            return Ok("Login successful");
        }
    }

}
