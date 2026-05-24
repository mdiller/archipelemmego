using ArchipeLemmeGo.Bot;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace ArchipeLemmeGo.Web
{
    public static class AuthEndpoints
    {
        private const string DiscordAuthUrl = "https://discord.com/oauth2/authorize";
        private const string DiscordTokenUrl = "https://discord.com/api/oauth2/token";
        private const string DiscordUserUrl = "https://discord.com/api/users/@me";

        public static void Map(WebApplication app)
        {
            app.MapGet("/auth/login", Login);
            app.MapGet("/auth/callback", Callback);
            app.MapGet("/auth/me", Me);
            app.MapPost("/auth/logout", (Delegate)Logout);
        }

        private static IResult Login(HttpContext ctx, IWebHostEnvironment env, string? returnUrl = null)
        {
            var state = Guid.NewGuid().ToString("N");
            var cookieOpts = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
                Secure = !env.IsDevelopment()
            };
            ctx.Response.Cookies.Append("auth_state", state, cookieOpts);

            // Only store returnUrl if it's a safe destination — prevents open redirect.
            // Allows relative paths, localhost (dev), and the configured BaseUrl host (prod).
            if (returnUrl != null && IsSafeReturnUrl(returnUrl))
                ctx.Response.Cookies.Append("auth_return", returnUrl, cookieOpts);

            var callbackUri = Uri.EscapeDataString(GetCallbackUri(ctx));
            var url = $"{DiscordAuthUrl}?client_id={BotInfo.DiscordClientId}&redirect_uri={callbackUri}&response_type=code&scope=identify&state={state}";
            return Results.Redirect(url);
        }

        private static async Task<IResult> Callback(HttpContext ctx, IHttpClientFactory httpClientFactory)
        {
            var code = ctx.Request.Query["code"].ToString();
            var state = ctx.Request.Query["state"].ToString();
            var savedState = ctx.Request.Cookies["auth_state"];
            var returnUrl = ctx.Request.Cookies["auth_return"] ?? "/";

            ctx.Response.Cookies.Delete("auth_state");
            ctx.Response.Cookies.Delete("auth_return");

            if (string.IsNullOrEmpty(code)
                || string.IsNullOrEmpty(state)
                || string.IsNullOrEmpty(savedState)
                || state != savedState)
                return Results.Redirect("/?auth_error=invalid_state");

            var client = httpClientFactory.CreateClient();

            var tokenResp = await client.PostAsync(DiscordTokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = BotInfo.DiscordClientId,
                ["client_secret"] = BotInfo.DiscordClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = GetCallbackUri(ctx)
            }));

            if (!tokenResp.IsSuccessStatusCode)
                return Results.Redirect("/?auth_error=token_failed");

            var tokenJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenJson.GetProperty("access_token").GetString()!;

            var userReq = new HttpRequestMessage(HttpMethod.Get, DiscordUserUrl);
            userReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userResp = await client.SendAsync(userReq);

            if (!userResp.IsSuccessStatusCode)
                return Results.Redirect("/?auth_error=user_failed");

            var userJson = await userResp.Content.ReadFromJsonAsync<JsonElement>();
            var userId = userJson.GetProperty("id").GetString()!;
            var username = userJson.GetProperty("username").GetString()!;
            var avatarHash = userJson.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null
                ? av.GetString() ?? ""
                : "";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new("avatar", avatarHash)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Results.Redirect(returnUrl);
        }

        private static IResult Me(HttpContext ctx)
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var username = ctx.User.FindFirstValue(ClaimTypes.Name)!;
            var avatarHash = ctx.User.FindFirstValue("avatar") ?? "";
            var avatarUrl = avatarHash.Length > 0
                ? $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png"
                : $"https://cdn.discordapp.com/embed/avatars/0.png";

            return Results.Ok(new { id = userId, username, avatarUrl });
        }

        private static async Task<IResult> Logout(HttpContext ctx)
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }

        // Prefers the hardcoded BaseUrl from config to avoid Host-header injection.
        // Falls back to the request host (fine for local dev where BaseUrl is unset).
        private static string GetCallbackUri(HttpContext ctx)
        {
            var baseUrl = BotInfo.BaseUrl;
            return string.IsNullOrEmpty(baseUrl)
                ? $"{ctx.Request.Scheme}://{ctx.Request.Host}/auth/callback"
                : $"{baseUrl}/auth/callback";
        }

        private static bool IsSafeReturnUrl(string returnUrl)
        {
            if (Uri.TryCreate(returnUrl, UriKind.Relative, out _))
                return true;

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                return false;

            // Allow localhost on any port (dev)
            if (uri.Host is "localhost" or "127.0.0.1")
                return true;

            // Allow the configured production origin
            var baseUrl = BotInfo.BaseUrl;
            if (!string.IsNullOrEmpty(baseUrl)
                && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
                && uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
