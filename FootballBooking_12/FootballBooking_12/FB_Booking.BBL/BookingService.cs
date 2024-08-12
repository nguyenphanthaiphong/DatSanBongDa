using FB_Booking.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FB_Booking.BBL
{
    public class BookingService
    {
        private readonly FootballPitchBookingContext _dbContext;
        private readonly IConfiguration _configuration;


        public BookingService(FootballPitchBookingContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> CheckAndCreateBooking(int userId, int pitchId, DateTime timeStart, float hoursToRent, string paymentMethod, string paymentState)
        {
            var endTime = timeStart.AddHours(hoursToRent);
            var cost = hoursToRent * 100;  // Assuming the cost is 100 per hour
            var existingBooking = await _dbContext.Bookings
                .AnyAsync(b => b.PitchId == pitchId && timeStart < b.TimeStart.AddHours(b.HoursToRent) && endTime > b.TimeStart);

            if (!existingBooking)
            {
                var booking = new Booking
                {
                    UserId = userId,
                    PitchId = pitchId,
                    TimeStart = timeStart,
                    HoursToRent = hoursToRent
                };
                await _dbContext.Bookings.AddAsync(booking);
                await _dbContext.SaveChangesAsync();

                var bill = new Bill
                {
                    BookingId = booking.BookingId,
                    Cost = (decimal)cost,
                    PaymentMethod = paymentMethod,
                    PaymentState = paymentState
                };
                await _dbContext.Bills.AddAsync(bill);
                await _dbContext.SaveChangesAsync();

                return true;
            }

            return false;

        }

        //  get all user's bookings by user id
        public async Task<List<get_booking>> get_by_userID(int userId)
        {
            var bookings = await _dbContext.Bookings
                .Where(b => b.UserId == userId)
                .Join(
                    _dbContext.Bills,
                    booking => booking.BookingId,
                    bill => bill.BookingId,
                    (booking, bill) => new { booking, bill }
                )
                .Join(
                    _dbContext.FootballPitches,
                    bookingBill => bookingBill.booking.PitchId,
                    pitch => pitch.PitchId,
                    (bookingBill, pitch) => new get_booking
                    {
                        bookingID = bookingBill.booking.BookingId,
                        UserId = bookingBill.booking.UserId,
                        PitchId = bookingBill.booking.PitchId,
                        TimeStart = bookingBill.booking.TimeStart,
                        HoursToRent = bookingBill.booking.HoursToRent,
                        Cost = bookingBill.bill.Cost,
                        PaymentMethod = bookingBill.bill.PaymentMethod,
                        PaymentState = bookingBill.bill.PaymentState,
                        PitchName = pitch.PitchName, // Assuming PitchName is the field name
                        Location = pitch.Location,
                    }
                )
                .OrderBy(booking => booking.TimeStart)
                .ToListAsync();
            return bookings;
        }

        public async Task<List<get_booking>> GetAllBookingsSortedByDay()
        {
            var bookings = await _dbContext.Bookings
                .Join(
                    _dbContext.Bills,
                    booking => booking.BookingId,
                    bill => bill.BookingId,
                    (booking, bill) => new { booking, bill }
                )
                .Join(
                    _dbContext.FootballPitches,
                    bookingBill => bookingBill.booking.PitchId,
                    pitch => pitch.PitchId,
                    (bookingBill, pitch) => new get_booking
                    {
                        bookingID = bookingBill.booking.BookingId,
                        UserId = bookingBill.booking.UserId,
                        PitchId = bookingBill.booking.PitchId,
                        TimeStart = bookingBill.booking.TimeStart,
                        HoursToRent = bookingBill.booking.HoursToRent,
                        Cost = bookingBill.bill.Cost,
                        PaymentMethod = bookingBill.bill.PaymentMethod,
                        PaymentState = bookingBill.bill.PaymentState,
                        PitchName = pitch.PitchName,
                        Location = pitch.Location,
                    }
                )
                .OrderByDescending(booking => booking.TimeStart) // Sorting by TimeStart
                .ToListAsync();

            return bookings;
        }


        //  delete a booking by booking id
        public async Task<Boolean> delete(int bookingID)
        {
            try
            {
                var booking = await _dbContext.Bookings.FindAsync(bookingID);
                //  check if the booking exists
                if (booking == null)
                {
                    return false;
                }

                var bill = await _dbContext.Bills.FirstOrDefaultAsync(b => b.BookingId == bookingID);
                //  check if the booking exists
                if (bill != null)
                {
                    _dbContext.Bills.Remove(bill);
                    await _dbContext.SaveChangesAsync();
                }
                _dbContext.Bookings.Remove(booking);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<get_booking>> search(string username, DateTime? date)
        {
            var query = from u in _dbContext.Users
                        join b in _dbContext.Bookings on u.UserId equals b.UserId
                        join bi in _dbContext.Bills on b.BookingId equals bi.BookingId
                        join p in _dbContext.FootballPitches on b.PitchId equals p.PitchId
                        where (date == null || b.TimeStart == date) &&
                              (username == null || u.UserName == username)
                        select new get_booking
                        {
                            bookingID = b.BookingId,
                            UserId = b.UserId,
                            PitchId = b.PitchId,
                            TimeStart = b.TimeStart,
                            HoursToRent = b.HoursToRent,
                            Cost = bi.Cost,
                            PaymentMethod = bi.PaymentMethod,
                            PaymentState = bi.PaymentState,
                            Location = p.Location,
                            PitchName = p.PitchName
                        };
            return await query.ToListAsync();
        }

        public async Task<decimal?> GetTotalRevenueForToday(int? year, int? month, int? day)
        {
            try
            {
                IQueryable<Booking> bookingsQuery = _dbContext.Bookings;

                if (year != 0)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.TimeStart.Year == year);
                }
                else if (month != 0)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.TimeStart.Month == month);
                }
                else if (day != 0)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.TimeStart.Day == day);
                }

                var totalRevenue = await bookingsQuery
                    .Join(_dbContext.Bills, b => b.BookingId, bi => bi.BookingId, (b, bi) => bi.Cost)
                    .SumAsync();

                return totalRevenue;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu cần
                throw;
            }
        }


    }

    public class get_booking
    {
        public int bookingID { get; set; }
        public int? UserId { get; set; }
        public int? PitchId { get; set; }

        public string PitchName { get; set; }
        public string Location { get; set; }
        public DateTime TimeStart { get; set; }
        public double HoursToRent { get; set; }
        public decimal Cost { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentState { get; set; }
    }
}
