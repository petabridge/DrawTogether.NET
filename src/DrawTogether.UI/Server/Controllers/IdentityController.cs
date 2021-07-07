/*
 * Source inspired by https://github.com/cornflourblue/aspnet-core-3-registration-login-api/blob/master/Controllers/UsersController.cs
 * And https://jasonwatmore.com/post/2020/11/09/blazor-webassembly-user-registration-and-login-example-tutorial
 */

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DrawTogether.UI.Server.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace DrawTogether.UI.Server.Controllers
{
    public class RegisterModel
    {
        public string UserId { get; set; }
        
        public string UserName { get; set; }
        
        public string EmailAddress { get; set; }
    }

    public class AuthenticateModel
    {
        public string UserId { get; set; }
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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Autenticate([FromBody] AuthenticateModel model)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var userValues = await _userStore.FindByIdAsync(model.UserId, cts.Token);

                if (userValues.IsNotRealUser())
                {
                    return NotFound($"Was not able to find user with id [{model.UserId}]");
                }
                 
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userValues.Id),
                    new Claim(ClaimTypes.Name, userValues.UserName)
                }, "auth");
                ClaimsPrincipal claims = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claims);

                // return basic user info and authentication token
                return Ok(new
                {
                    Id = userValues.Id,
                    Username = userValues.UserName,
                });
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}