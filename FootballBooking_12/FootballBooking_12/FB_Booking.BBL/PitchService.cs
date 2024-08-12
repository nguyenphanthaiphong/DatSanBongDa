using FB_Booking.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FB_Booking.BBL
{
    public class PitchService
    {
        private readonly FootballPitchBookingContext _dbContext;
        private readonly IConfiguration _configuration;

        public PitchService(FootballPitchBookingContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }


        //  create a football pitch
        public async Task<bool> CreatePitchAsync(string pitchName, string location, string size)
        {

            var pitch = new FootballPitch
            {
                PitchName = pitchName,
                Location = location,
                Size = size
            };

            await _dbContext.FootballPitches.AddAsync(pitch);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine("create Pitch successfully!");
            return true;
        }

        //  delete a football pitch by pitchId
        public async Task<Boolean> delete(int pitchId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                //  check null that fb pitch
                var fbPitch = await _dbContext.FootballPitches.FindAsync(pitchId);
                //  check if the pitch exists
                if (fbPitch == null)
                {
                    return false;
                }

                // Find all bookings related to the pitch
                var bookings = await _dbContext.Bookings
                                               .Where(b => b.PitchId == pitchId)
                                               .ToListAsync();
                foreach (var booking in bookings)
                {
                    // Find all bills related to each booking
                    var bills = await _dbContext.Bills
                                                .Where(b => b.BookingId == booking.BookingId)
                                                .ToListAsync();
                    // Remove all related bills
                    _dbContext.Bills.RemoveRange(bills);
                }
                // Remove all related bookings
                _dbContext.Bookings.RemoveRange(bookings);

                // Remove the football pitch
                _dbContext.FootballPitches.Remove(fbPitch);

                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                // Commit the transaction
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Rollback the transaction in case of an error
                await transaction.RollbackAsync();
                Console.WriteLine($"Error deleting pitch with ID {pitchId}: {ex.Message}");
                return false;
            }
        }

        public async Task<FootballPitch> GetPitchById(int pitchId)
        {
            Console.WriteLine($"{pitchId}");
            return await _dbContext.FootballPitches.FindAsync(pitchId);
        }

        public async Task<List<FootballPitch>> GetAllPitches()
        {
            return await _dbContext.FootballPitches.ToListAsync();
        }

        public async Task<bool> UpdatePitchAsync(int pitchId, string pitchName, string location, string size)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var fbPitch = await _dbContext.FootballPitches.FindAsync(pitchId);
                if (fbPitch == null)
                {
                    return false;
                }

                fbPitch.PitchName = pitchName;
                fbPitch.Location = location;
                fbPitch.Size = size;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine("Pitch updated successfully!");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error updating pitch with ID {pitchId}: {ex.Message}");
                return false;
            }
        }

    }
}
