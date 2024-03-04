using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.PhpGoPay;

internal class PhpGoClient(
	HttpClient httpClient,
	PhpGoHasher phpGoHasher,
	ILogger<PhpGoClient> logger)
{
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
		var sign = phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		return await httpClient.PostAsJsonAsync(
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

				if (logger.IsEnabled(LogLevel.Information))
				{
					var bodyJson = JsonSerializer.Serialize(node, _jsonSerializerOptions);

					logger.LogInformation("Deposit Body: {bodyJson}", bodyJson);
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

		var sign = phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		_ = await httpClient.PostAsJsonAsync(
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
					throw new ApiFailException("Withdraw response can't read.");

				if (logger.IsEnabled(LogLevel.Information))
				{
					var bodyJson = JsonSerializer.Serialize(node, _jsonSerializerOptions);

					logger.LogInformation("Withdraw Body: {bodyJson}", bodyJson);
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

		var sign = phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		return await httpClient.PostAsJsonAsync(
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

	public async ValueTask<PhpGoClientBalanceResponse> GetBalanceAsync(string merchant, string key, CancellationToken cancellationToken = default)
	{
		var requestData = new Dictionary<string, string?>
		{
			["merchant"] = merchant
		};

		var sign = phpGoHasher.ComputeHash(key, requestData);

		requestData["sign"] = sign;

		return await httpClient.PostAsJsonAsync(
			"/api/me",
			requestData,
			_jsonSerializerOptions,
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<PhpGoClientBalanceResponse>(_jsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.Select(result => result!))
			.FirstAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
