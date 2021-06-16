using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DrawTogether.UI.Shared;

namespace DrawTogether.UI.Client.Auth
{
    public class RandomAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var userName = new ClaimsIdentity(new Claim[] {new Claim(ClaimTypes.Name, UserNamingService.GenerateRandomName())});
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(userName)));
        }
    }
}
