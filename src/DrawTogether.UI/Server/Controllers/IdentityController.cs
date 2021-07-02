using System;
using System.Threading;
using System.Threading.Tasks;
using DrawTogether.UI.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DrawTogether.UI.Server.Controllers
{
    public class RegisterModel
    {
        public string UserId { get; set; }
        
        public string UserName { get; set; }
    }
    
    [ApiController]
    [Route("[controller]")]
    public class IdentityController : Controller
    {
        private readonly IUserStore<DTAnonymousUser> _userStore;
        
        // GET
        public IdentityController(IUserStore<DTAnonymousUser> userStore)
        {
            _userStore = userStore;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]RegisterModel model)
        {
            var userInfo = new DTAnonymousUser()
            {
                Id = model.UserId ?? Guid.NewGuid().ToString(),
                UserName = model.UserName,
            };

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                
                // create user
                var anonUser = await _userStore.CreateAsync(userInfo, cts.Token);

                if (anonUser.Succeeded)
                    return Ok();
                return BadRequest(new {message = string.Join(",", anonUser.Errors)});
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}