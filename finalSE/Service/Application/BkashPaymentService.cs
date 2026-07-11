using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace finalSE.Service.Application
{
    /// <summary>
    /// Service to interact with the bKash Tokenized Checkout (Sandbox) API.
    /// Handles Grant Token, Create Payment, and Execute Payment flows.
    /// </summary>
    public class BkashPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BkashPaymentService> _logger;

        // Cached token
        private static string? _cachedToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;

        public BkashPaymentService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<BkashPaymentService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string BaseUrl => _configuration["BkashPGW:BaseUrl"]
            ?? "https://tokenized.sandbox.bka.sh/v1.2.0-beta";

        private string AppKey => _configuration["BkashPGW:AppKey"]
            ?? "4f6o0cjiki2rfm34kfdadl1eqq";

        private string AppSecret => _configuration["BkashPGW:AppSecret"]
            ?? "2is7hdktrekvrbljjh44ll3d9l1dtjo4pasmjvs5vl5qr3fug4b";

        private string Username => _configuration["BkashPGW:Username"]
            ?? "sandboxTokenizedUser02";

        private string Password => _configuration["BkashPGW:Password"]
            ?? "sandboxTokenizedUser02@12345";

        /// <summary>
        /// Grant Token from bKash API. Caches the token until it expires.
        /// </summary>
        public async Task<string?> GrantTokenAsync()
        {
            // Return cached token if still valid (buffer 60s before expiry)
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddSeconds(-60))
            {
                return _cachedToken;
            }

            var client = _httpClientFactory.CreateClient("bKash");
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{BaseUrl}/tokenized/checkout/token/grant";

            var requestBody = new
            {
                app_key = AppKey,
                app_secret = AppSecret
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.Add("username", Username);
            request.Headers.Add("password", Password);

            try
            {
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("bKash Grant Token Response: {Response}", responseString);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("id_token", out var tokenProp))
                    {
                        _cachedToken = tokenProp.GetString();

                        // bKash tokens are typically valid for 3600 seconds (1 hour)
                        if (root.TryGetProperty("expires_in", out var expiresProp))
                        {
                            var expiresIn = expiresProp.GetInt32();
                            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
                        }
                        else
                        {
                            _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
                        }

                        return _cachedToken;
                    }
                }

                _logger.LogError("bKash Grant Token failed. Status: {Status}, Body: {Body}",
                    response.StatusCode, responseString);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "bKash Grant Token exception.");
                return null;
            }
        }

        /// <summary>
        /// Create a bKash payment. Returns the bKashURL for redirect and paymentID.
        /// </summary>
        public async Task<BkashCreatePaymentResponse?> CreatePaymentAsync(
            decimal amount,
            string invoiceNumber,
            string callbackUrl)
        {
            var token = await GrantTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Cannot create payment — failed to obtain bKash token.");
                return null;
            }

            var client = _httpClientFactory.CreateClient("bKash");
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{BaseUrl}/tokenized/checkout/create";

            var requestBody = new
            {
                mode = "0011",
                payerReference = " ",
                callbackURL = callbackUrl,
                amount = amount.ToString("0.00"),
                currency = "BDT",
                intent = "sale",
                merchantInvoiceNumber = invoiceNumber
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-APP-Key", AppKey);

            try
            {
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("bKash Create Payment Response: {Response}", responseString);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    var result = new BkashCreatePaymentResponse();

                    if (root.TryGetProperty("bkashURL", out var bkashUrlProp))
                        result.BkashURL = bkashUrlProp.GetString();

                    if (root.TryGetProperty("paymentID", out var paymentIdProp))
                        result.PaymentID = paymentIdProp.GetString();

                    if (root.TryGetProperty("statusCode", out var statusCodeProp))
                        result.StatusCode = statusCodeProp.GetString();

                    if (root.TryGetProperty("statusMessage", out var statusMsgProp))
                        result.StatusMessage = statusMsgProp.GetString();

                    if (!string.IsNullOrEmpty(result.BkashURL) && !string.IsNullOrEmpty(result.PaymentID))
                    {
                        return result;
                    }

                    _logger.LogError("bKash Create Payment incomplete. Code: {Code}, Msg: {Msg}",
                        result.StatusCode, result.StatusMessage);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "bKash Create Payment exception.");
                return null;
            }
        }

        /// <summary>
        /// Execute a bKash payment after user completes the checkout flow.
        /// </summary>
        public async Task<BkashExecutePaymentResponse?> ExecutePaymentAsync(string paymentId)
        {
            var token = await GrantTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Cannot execute payment — failed to obtain bKash token.");
                return null;
            }

            var client = _httpClientFactory.CreateClient("bKash");
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{BaseUrl}/tokenized/checkout/execute";

            var requestBody = new { paymentID = paymentId };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-APP-Key", AppKey);

            try
            {
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("bKash Execute Payment Response: {Response}", responseString);

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                var result = new BkashExecutePaymentResponse();

                if (root.TryGetProperty("paymentID", out var pIdProp))
                    result.PaymentID = pIdProp.GetString();
                if (root.TryGetProperty("trxID", out var trxIdProp))
                    result.TrxID = trxIdProp.GetString();
                if (root.TryGetProperty("transactionStatus", out var tStatusProp))
                    result.TransactionStatus = tStatusProp.GetString();
                if (root.TryGetProperty("amount", out var amountProp))
                    result.Amount = amountProp.GetString();
                if (root.TryGetProperty("currency", out var currencyProp))
                    result.Currency = currencyProp.GetString();
                if (root.TryGetProperty("statusCode", out var statusCodeProp))
                    result.StatusCode = statusCodeProp.GetString();
                if (root.TryGetProperty("statusMessage", out var statusMsgProp))
                    result.StatusMessage = statusMsgProp.GetString();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "bKash Execute Payment exception.");
                return null;
            }
        }
    }

    // DTO Classes for bKash API responses

    public class BkashCreatePaymentResponse
    {
        public string? BkashURL { get; set; }
        public string? PaymentID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusMessage { get; set; }
    }

    public class BkashExecutePaymentResponse
    {
        public string? PaymentID { get; set; }
        public string? TrxID { get; set; }
        public string? TransactionStatus { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusMessage { get; set; }
    }
}
