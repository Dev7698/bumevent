using bumevent.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BmuEvent_API.Controllers
{
    [Route("api/generate-description")]
    [ApiController]
    public class AIController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> GenerateDescription([FromBody] UserInputModel input)
        {
            string userText = input.UserInput;


            string aiGeneratedText = $"Enhanced AI Description: {userText} with additional insights.";
                   
            return Ok(new { generatedText = aiGeneratedText });
        }
    }
}
