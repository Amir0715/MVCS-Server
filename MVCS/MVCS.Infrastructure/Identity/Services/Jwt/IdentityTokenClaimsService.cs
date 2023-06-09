﻿using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MVCS.Core.Domain.Interfaces;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MVCS.Infrastructure.Identity.Services.Jwt;

public class IdentityTokenClaimsService : ITokenClaimsService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public IdentityTokenClaimsService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<string> GetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Name, user.UserName),
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (JwtRegisteredClaimNames.Email, user.Email),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);
        var signinCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _jwtOptions.Issuer,
            IssuedAt = DateTime.UtcNow,
            Audience = _jwtOptions.Audience,
            SigningCredentials = signinCredentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        return jwtToken;
    }
}