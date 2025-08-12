using JwtAuthDemo.Api.Entities.Models;

namespace JwtAuthDemo.Api.Services
{
    public interface ITokenService
    {
        string CreateAccessToken(User user);
        string CreateRefreshToken();            // raw refresh token (returned to client)
        string HashToken(string token);         // store hashed token in DB
    }
}
