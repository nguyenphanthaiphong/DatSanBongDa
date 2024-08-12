using FB_Booking.BBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballBooking_12.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]  //[ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingController(BookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookPitch([FromBody] BookingRequest request)
        {
            // Get the userId from the token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized(new { success = false, message = "User ID not found in token." });
            }

            int userId = int.Parse(userIdClaim.Value);
            Console.WriteLine(userId);

            var result = await _bookingService.CheckAndCreateBooking(userId, request.PitchId, request.TimeStart, request.HoursToRent, request.PaymentMethod, request.PaymentState);
            if (result)
            {
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false, message = "Pitch is not available for the selected time." });
        }

        [HttpGet("bookings/{userID}")]
        [Authorize]
        public async Task<IActionResult> GetBookings(int userID)
        {
            try
            {
                var bookings = await _bookingService.get_by_userID(userID);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete/{bookingID}")]
        public async Task<IActionResult> Delete_Booking(int bookingID)
        {
            try
            {
                var result = await _bookingService.delete(bookingID);
                if (result)
                    return NoContent();
                else
                    return StatusCode(500, "An error occurred while deleting the booking.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the booking.");
            }
        }

        [HttpPost("search")]
        [Authorize]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            try
            {
                var bookings = await _bookingService.search(request.Username, request.date);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("search-all-bookings")]
        [Authorize]
        public async Task<IActionResult> SearchAllBookings()
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsSortedByDay();
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("revenue")]
        public async Task<ActionResult<decimal>> GetTotalRevenueForToday([FromBody] RevenueRequest request)
        {
            try
            {
                var totalRevenue = await _bookingService.GetTotalRevenueForToday(request.year, request.month, request.day);
                return Ok(totalRevenue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }

    public class BookingRequest
    {
        public int PitchId { get; set; }
        public DateTime TimeStart { get; set; } // Renamed from TimeStart to BookingTime
        public float HoursToRent { get; set; } // You might need this instead of TimeSlotId, assuming you want to specify the duration directly
        public string PaymentMethod { get; set; }
        public string PaymentState { get; set; }
    }

    public class SearchRequest
    {
        public string Username { get; set; }
        public DateTime? date { get; set; }
    }

    public class RevenueRequest
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? day { get; set; }
    }
}
