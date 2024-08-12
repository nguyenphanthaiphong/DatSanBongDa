using FB_Booking.BBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballBooking_12.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class PitchController : ControllerBase
    {
        private readonly PitchService _pitchService;

        public PitchController(PitchService pitchService)
        {
            _pitchService = pitchService;
        }

        [HttpPost("create-pitch")]
        public async Task<IActionResult> Signup([FromBody] PitchRequest request)
        {
            var success = await _pitchService.CreatePitchAsync(request.pitchName, request.location, request.size);

            if (success)
            {
                return Ok(new { message = "Pitch created successfully" });
            }

            return BadRequest(new { message = "Is there something wrong!" });
        }

        [HttpDelete("delete-pitch/{pitchId}")]
        public async Task<IActionResult> Delete_FBPitch(int pitchId)
        {
            try
            {
                var result = await _pitchService.delete(pitchId);
                if (result)
                    return NoContent();
                else
                    return StatusCode(500, "An error occurred while deleting the fb pitch.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the fb pitch.");
            }
        }

        [HttpGet("get-pitch/{pitchId}")]
        public async Task<IActionResult> GetPitchById(int pitchId)
        {
            var pitch = await _pitchService.GetPitchById(pitchId);
            if (pitch == null)
            {
                return NotFound(new { message = "Pitch not found" });
            }
            return Ok(pitch);
        }

        [HttpGet("get-all-pitches")]
        public async Task<IActionResult> GetAllPitches()
        {
            var pitches = await _pitchService.GetAllPitches();
            return Ok(pitches);
        }

        [HttpPut("update-pitch/{pitchId}")]
        public async Task<IActionResult> UpdatePitch(int pitchId, [FromBody] PitchRequest request)
        {
            var success = await _pitchService.UpdatePitchAsync(pitchId, request.pitchName, request.location, request.size);

            if (success)
            {
                return Ok(new { message = "Pitch updated successfully" });
            }

            return NotFound(new { message = "Pitch not found or update failed" });
        }

    }

    public class PitchRequest
    {
        public string pitchName { get; set; }
        public string location { get; set; }
        public string size { get; set; }
    }
}
