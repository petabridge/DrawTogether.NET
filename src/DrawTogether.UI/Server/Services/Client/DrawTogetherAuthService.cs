using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using DrawTogether.UI.Server.Controllers;
using DrawTogether.UI.Server.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace DrawTogether.UI.Server.Services.Client
{
    public interface IDrawTogetherClientAuthService
    {
        Task<IdentityProtocol.UserInfo> Register(string userName);
        Task<bool> LoginById(string userId);
        Task<bool> LoginByName(string userName);
    }
    
    public class DrawTogetherAuthService : IDrawTogetherClientAuthService
    {
        private class UserIdResponse
        {
            public string Id { get; set; }
            
            public string UserName { get; set; }
        }
        
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public DrawTogetherAuthService(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }

        public async Task<IdentityProtocol.UserInfo> Register(string userName)
        {
            var registerModel = new RegisterModel() {UserName = userName};
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, registerModel);
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var response = await _httpClient.PostAsync("/identity/register", content);

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"[{response.StatusCode}] {response.ReasonPhrase}");

            var deserialized = JsonSerializer.Deserialize<UserIdResponse>(await response.Content.ReadAsStringAsync());

            Debug.Assert(deserialized != null, nameof(deserialized) + " != null");
            return new IdentityProtocol.UserInfo(deserialized.Id, UserName: deserialized.UserName);
        }

        public async Task<bool> LoginById(string userId)
        {
            var authenticateModel = new AuthenticateModel() { UserId = userId };
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, authenticateModel);
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var response = await _httpClient.PostAsync("/identity/login", content);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            
            var deserialized = JsonSerializer.Deserialize<UserIdResponse>(await response.Content.ReadAsStringAsync());

            await _localStorage.SetItemAsync("userName", deserialized.UserName);
            await _localStorage.SetItemAsync("userId", deserialized.Id);
            
            _authenticationStateProvider.
        }

        public Task<bool> LoginByName(string userName)
        {
            throw new System.NotImplementedException();
        }
    }
}