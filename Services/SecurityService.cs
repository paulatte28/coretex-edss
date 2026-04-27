using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace coretex_finalproj.Services
{
    public class SecurityService
    {
        private readonly IDataProtector _protector;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(IDataProtectionProvider provider, IMemoryCache cache, ILogger<SecurityService> logger)
        {
            _protector = provider.CreateProtector("Coretex.Security.V1");
            _cache = cache;
            _logger = logger;
        }

        // --- Suggestion 3: Data Encryption ---
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            return _protector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            try { return _protector.Unprotect(cipherText); }
            catch { return "[[DECRYPTION_ERROR]]"; }
        }

        // --- Suggestion 2: IP Rate Limiting ---
        public bool IsRateLimited(string ipAddress, string action, int maxAttempts = 5, int windowMinutes = 1)
        {
            var key = $"RateLimit_{action}_{ipAddress}";
            if (_cache.TryGetValue(key, out int attempts))
            {
                if (attempts >= maxAttempts) return true;
                _cache.Set(key, attempts + 1, TimeSpan.FromMinutes(windowMinutes));
            }
            else
            {
                _cache.Set(key, 1, TimeSpan.FromMinutes(windowMinutes));
            }
            return false;
        }
    }
}
