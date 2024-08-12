using FB_Booking.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FB_Booking.BBL
{
    public class UserService
    {
        private readonly FootballPitchBookingContext _dbContext;
        private readonly IConfiguration _configuration;


        public UserService(FootballPitchBookingContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> CreateUserAsync(string username, string password, string email)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == username))
            {
                Console.WriteLine("Validate User!");
                return false; // Username or email already exists
            }

            var user = new User
            {
                UserName = username,
                Email = email,
                PasswordHash = password,
                RoleId = 2
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine("create User successfully!");
            return true;
        }

        public async Task<string> ValidateUserAsync(string username, string password)
        {
            var user = await _dbContext.Users
                .Where(u => u.UserName == username)
                .FirstOrDefaultAsync();

            if (user == null || user.PasswordHash != password)
            {
                return null; // Invalid username or password
            }

            return GenerateJwtToken(user.UserName, user.UserId, (int)user.RoleId); // Return the JWT token
        }
        private string GenerateJwtToken(string username, int userID, int roleId)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("UserId", userID.ToString()),
                    new Claim("RoleId", roleId.ToString())
                }),
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        //  delete user
        public async Task<Boolean> delete(int userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                //  check if the booking exists
                if (user == null)
                {
                    return false;
                }

                //  find all booking related to the user
                var bookings = await _dbContext.Bookings.Where(b => b.UserId == userId).ToListAsync();
                //  check if the booking exists
                foreach(var booking in bookings)
                {
                    var bills = await _dbContext.Bills.Where(b => b.BookingId == booking.BookingId)
                        .ToListAsync();

                    //remove bills
                    _dbContext.Bills.RemoveRange(bills);
                }

                // Remove all related bookings
                _dbContext.Bookings.RemoveRange(bookings);

                // Remove the football pitch
                _dbContext.Users.Remove(user);

                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                // Commit the transaction
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error deleting user with ID {userId}: {ex.Message}");
                return false;
            }
        }

        //  get all users
        public async Task<List<User>> get_all_users()
        {
            var users = await _dbContext.Users.Where(u => u.RoleId == '2').ToListAsync();
            return users;
        }
    }
}



