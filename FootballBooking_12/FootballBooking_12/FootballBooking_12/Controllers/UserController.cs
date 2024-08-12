using FB_Booking.BBL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballBooking_12.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            var success = await _userService.CreateUserAsync(request.Username, request.Password, request.Email);

            if (success)
            {
                return Ok(new { message = "User created successfully" });
            }

            return BadRequest(new { message = "Username or email already exists" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _userService.ValidateUserAsync(request.Username, request.Password);

            if (token != null)
            {
                return Ok(new { token });
            }

            return Unauthorized(new { message = "Invalid username or password" });
        }

        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> Delete_User(int userId)
        {
            // Get the roleId from the token
            var roleIdClaim = User.Claims.FirstOrDefault(c => c.Type == "RoleId");
            if(roleIdClaim != null && roleIdClaim.Value == "1")
            {
                try
                {
                    var result = await _userService.delete(userId);
                    if (result)
                        return NoContent();
                    else
                        return StatusCode(500, "An error occurred while deleting the user.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "An error occurred while deleting the user.");
                }
            }
            else
            {
                return StatusCode(403, "You're not authorized to delete users.");
            }
        }


        [HttpGet("get-all-users")]
        public async Task<IActionResult> get_all()
        {
            try
            {
                var users = await _userService.get_all_users();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }

    // DTO for login request
    public class SignupRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
