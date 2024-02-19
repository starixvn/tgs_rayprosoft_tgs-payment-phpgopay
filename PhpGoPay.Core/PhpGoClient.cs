using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.PhpGoPay;

internal class PhpGoClient(HttpClient httpClient, PhpGoHasher phpGoHasher, ILogger<PhpGoClient> logger)
{
	private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	private readonly PhpGoHasher _phpGoHasher = phpGoHasher ?? throw new ArgumentNullException(nameof(phpGoHasher));
	private readonly ILogger<PhpGoClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
	};

	public async Task<PhpGoClientDepositResponse> DepositAsync(
		string merchant,
		string key,
		PhpGoClientDepositOrder order,
		CancellationToken cancellationToken = default)
	{
		var requestData = new Dictionary<string, string?>
		{
			["merchant"] = merchant,
			["payment_type"] = order.PaymentType.ToTypeValue(),
			["amount"] = order.Amount.ToString(),
			["order_id"] = order.FormId.ToString(),
			["bank_code"] = order.BankCode.ToCode(),
			["callback_url"] = order.CallbackUrl.ToString(),
			["return_url"] = order.ReturnUrl.ToString()
		};
		var sign = _phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		return await _httpClient.PostAsJsonAsync(
			"/api/transfer",
			requestData,
			_jsonSerializerOptions,
			cancellationToken: cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<JsonNode>(_jsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Do(node =>
			{
				if (node is null)
					throw new ApiFailException("Deposit response can't read.");

				if (_logger.IsEnabled(LogLevel.Information))
				{
					var bodyJson = JsonSerializer.Serialize(node, _jsonSerializerOptions);

					_logger.LogInformation("Deposit Body: {bodyJson}", bodyJson);
				}

				if (node["status"]?.ToString() != "1")
					throw new ApiFailException(node["message"]?.GetValue<string>());
			})
			.Select(node => node!.Deserialize<PhpGoClientDepositResponse>(_jsonSerializerOptions)!)
			.FirstAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask WithdrawAsync(
		string merchant,
		string key,
		PhpGoClientWithdrawOrder order,
		CancellationToken cancellationToken = default)
	{
		var requestData = new Dictionary<string, string?>
		{
			["merchant"] = merchant,
			["total_amount"] = order.Amount.ToString(),
			["callback_url"] = order.CallbackUrl.ToString(),
			["order_id"] = order.FormId.ToString(),
			["bank"] = order.BankCode,
			["bank_card_name"] = order.NameOrPhone,
			["bank_card_account"] = order.AccountOrPhone,
			["bank_card_remark"] = order.BankCode switch
			{
				"gcash" => order.AccountOrPhone,
				"PMP" => order.AccountOrPhone,
				_ => "no"
			}
		};

		var sign = _phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		_ = await _httpClient.PostAsJsonAsync(
			"/api/daifu",
			requestData,
			_jsonSerializerOptions,
			cancellationToken: cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<JsonNode>(_jsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Do(node =>
			{
				if (node is null)
					throw new ApiFailException("Deposit response can't read.");

				if (_logger.IsEnabled(LogLevel.Information))
				{
					var bodyJson = JsonSerializer.Serialize(node, _jsonSerializerOptions);

					_logger.LogInformation("Deposit Body: {bodyJson}", bodyJson);
				}

				if (node["status"]?.ToString() != "1")
					throw new ApiFailException(node["message"]?.GetValue<string>());
			})
			.FirstAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<PhpGoClientQueryResponse> QueryDepositOrWithdrawFormStatusAsync(
		string merchant,
		string key,
		Guid formId,
		CancellationToken cancellationToken = default)
	{
		var requestData = new Dictionary<string, string?>
		{
			["merchant"] = merchant,
			["order_id"] = formId.ToString(),
		};

		var sign = _phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		return await _httpClient.PostAsJsonAsync(
			"/api/query",
			requestData,
			_jsonSerializerOptions,
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<PhpGoClientQueryResponse>(_jsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.Select(result => result!))
			.FirstAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
