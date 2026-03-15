namespace Ecommerce.API.Middleware;

/// <summary>
/// Appends security-related HTTP response headers to every response.
///
/// Headers added
/// ─────────────
/// Content-Security-Policy
///   Restricts which origins the browser may load resources from.
///   API-only service: default-src 'none' is safe — no HTML, no scripts, no images
///   are served from this origin. Adjust if you ever serve Swagger in production
///   (add script-src 'self' 'unsafe-inline' and style-src 'self' 'unsafe-inline').
///
/// X-Content-Type-Options: nosniff
///   Prevents browsers from MIME-sniffing a response away from the declared
///   Content-Type. Without this, a browser that receives "text/plain" containing
///   script may execute it as JavaScript.
///
/// X-Frame-Options: DENY
///   Prevents this API's responses from being embedded in an &lt;iframe&gt;.
///   Mitigates clickjacking. DENY is correct for a pure API; use SAMEORIGIN
///   only if you serve HTML pages that embed other pages from this origin.
///
/// Referrer-Policy: no-referrer
///   Instructs the browser not to send a Referer header on outbound requests
///   originating from this origin. Prevents leaking URL structure (which may
///   contain IDs, tokens, or query parameters) to third-party origins.
///
/// Permissions-Policy
///   Explicitly disables browser features (camera, microphone, geolocation, etc.)
///   that an API service never needs. Reduces the attack surface if a future
///   response ever accidentally includes HTML or JavaScript.
///
/// Strict-Transport-Security (HSTS)
///   Tells browsers to connect only over HTTPS for the next year.
///   includeSubDomains is included so sub-domains can't be used as a downgrade
///   vector. preload is NOT included — submit to the HSTS preload list manually
///   once you are certain the domain and all sub-domains are HTTPS-only.
///   Only sent over HTTPS connections (skipped for HTTP to avoid bricking
///   local development over plain HTTP).
///
/// Cross-Origin-Resource-Policy: same-origin
///   Blocks other origins from reading this API's responses via no-cors fetch
///   or &lt;img src&gt;. Protects against Spectre-style cross-origin data leaks.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Headers are added before the response starts so they are always present,
        // including on error responses from ExceptionHandlingMiddleware.
        // OnStarting fires just before the first byte is written.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // ── Content-Security-Policy ───────────────────────────────────────
            // Pure REST API: nothing is ever rendered in a browser from this origin.
            // 'none' for every directive is the strictest possible policy.
            // If you expose Swagger in production, change to:
            //   default-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:
            headers["Content-Security-Policy"] =
                "default-src 'none'; frame-ancestors 'none'";

            // ── X-Content-Type-Options ────────────────────────────────────────
            headers["X-Content-Type-Options"] = "nosniff";

            // ── X-Frame-Options ───────────────────────────────────────────────
            // Redundant with CSP frame-ancestors 'none' but kept for older browsers
            // that understand X-Frame-Options but not CSP.
            headers["X-Frame-Options"] = "DENY";

            // ── Referrer-Policy ───────────────────────────────────────────────
            headers["Referrer-Policy"] = "no-referrer";

            // ── Permissions-Policy ────────────────────────────────────────────
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), " +
                "interest-cohort=()";

            // ── Cross-Origin-Resource-Policy ──────────────────────────────────
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // ── Strict-Transport-Security (HSTS) ──────────────────────────────
            // Only send over HTTPS — sending HSTS over HTTP locks out HTTP clients
            // permanently and breaks local development.
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains";
            }

            // ── Remove headers that leak implementation details ───────────────
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}