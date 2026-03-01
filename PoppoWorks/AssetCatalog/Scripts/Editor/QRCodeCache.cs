// SPDX-License-Identifier: CC0-1.0

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetCatalog.Editor
{
    public static class QRCodeCache
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static int _cacheHitCount;
        private static int _cacheMissCount;
        private static int _apiCallCount;

        public static void ResetStats()
        {
            _cacheHitCount = 0;
            _cacheMissCount = 0;
            _apiCallCount = 0;
        }

        public static string GetStatsSummary()
        {
            return $"QR cache stats: hits={_cacheHitCount}, misses={_cacheMissCount}, apiCalls={_apiCallCount}";
        }

        public static void LogStatsSummary()
        {
            Log(GetStatsSummary());
        }

        public static Task<byte[]> GetOrFetchPngAsync(
            string url,
            int size = 150,
            int timeoutSeconds = 10,
            int retryCount = 1,
            float retryDelaySeconds = 1f)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Task.FromResult<byte[]>(null);
            }

            string cachePath = GetCachePath(url, size);
            if (File.Exists(cachePath))
            {
                try
                {
                    _cacheHitCount++;
                    Log($"[QRCache] HIT {Path.GetFileName(cachePath)}");
                    // Cache hit: bypass network and async continuation overhead.
                    return Task.FromResult(File.ReadAllBytes(cachePath));
                }
                catch (Exception ex)
                {
                    Log($"[QRCache] HIT read failed, refetching: {ex.Message}");
                }
            }

            _cacheMissCount++;
            Log($"[QRCache] MISS {Path.GetFileName(cachePath)}");
            timeoutSeconds = Math.Max(1, timeoutSeconds);
            retryCount = Math.Max(0, retryCount);
            retryDelaySeconds = Math.Max(0f, retryDelaySeconds);

            return FetchAndStoreAsync(url, size, cachePath, timeoutSeconds, retryCount, retryDelaySeconds);
        }

        private static async Task<byte[]> FetchAndStoreAsync(
            string url,
            int size,
            string cachePath,
            int timeoutSeconds,
            int retryCount,
            float retryDelaySeconds)
        {
            string apiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size={size}x{size}&data={UnityEngine.Networking.UnityWebRequest.EscapeURL(url)}&format=png";
            int attempts = retryCount + 1;
            Exception lastException = null;
            int apiDelayMs = Mathf.CeilToInt(retryDelaySeconds * 1000f);

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    _apiCallCount++;
                    Log($"[QRCache] API request attempt={attempt}/{attempts} timeout={timeoutSeconds}s delay={retryDelaySeconds:0.###}s");
                    if (apiDelayMs > 0)
                    {
                        await Task.Delay(apiDelayMs);
                    }
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        var response = await HttpClient.GetAsync(apiUrl, cts.Token);
                        response.EnsureSuccessStatusCode();
                        var responseBytes = await response.Content.ReadAsByteArrayAsync();

                        Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                        await File.WriteAllBytesAsync(cachePath, responseBytes);
                        return responseBytes;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt == attempts)
                    {
                        break;
                    }
                }
            }

            throw new TimeoutException(
                $"QR API request failed after {attempts} attempt(s). timeout={timeoutSeconds}s, retry={retryCount}, retryDelay={retryDelaySeconds}s",
                lastException);
        }

        private static string GetCachePath(string url, int size)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string cacheDir = Path.Combine(projectRoot, "Library", "AssetCatalog", "QrCache");
            string cacheKey = $"{size}:{url.Trim()}";
            string hash = ComputeSha256(cacheKey);
            return Path.Combine(cacheDir, $"{hash}.png");
        }

        private static string ComputeSha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static void Log(string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
            Debug.Log(line);
        }
    }
}
