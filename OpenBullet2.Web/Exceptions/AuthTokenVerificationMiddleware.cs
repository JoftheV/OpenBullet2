﻿using OpenBullet2.Core.Services;
using OpenBullet2.Web.Extensions;
using OpenBullet2.Web.Interfaces;
using OpenBullet2.Web.Models.Identity;
using System.Security.Claims;

namespace OpenBullet2.Web.Exceptions;

internal class AuthTokenVerificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthTokenVerificationMiddleware> _logger;
    private readonly OpenBulletSettingsService _obSettingsService;
    private readonly IAuthTokenService _authTokenService;

    public AuthTokenVerificationMiddleware(RequestDelegate next,
        ILogger<AuthTokenVerificationMiddleware> logger,
        OpenBulletSettingsService obSettingsService,
        IAuthTokenService authTokenService)
    {
        _next = next;
        _logger = logger;
        _obSettingsService = obSettingsService;
        _authTokenService = authTokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // If the admin user does not need any login, allow anonymous requests
        if (!_obSettingsService.Settings.SecuritySettings.RequireAdminLogin)
        {
            var apiUser = new ApiUser
            {
                Id = -1,
                Role = UserRole.Admin,
                Username = _obSettingsService.Settings.SecuritySettings.AdminUsername
            };

            context.SetApiUser(apiUser);

            await _next(context);

            return;
        }

        // If there is a token, validate it
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(' ').Last();

        if (token is not null)
        {
            var validToken = _authTokenService.ValidateToken(token);
            var apiUser = new ApiUser
            {
                Id = int.Parse(validToken.Claims.FirstOrDefault(
                    c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameidentifier")?.Value ?? "-1"),

                Username = validToken.Claims.FirstOrDefault(
                    c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value,

                Role = Enum.Parse<UserRole>(validToken.Claims.FirstOrDefault(
                    c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? "Anonymous")
            };

            context.SetApiUser(apiUser);
        }

        await _next(context);
    }
}
