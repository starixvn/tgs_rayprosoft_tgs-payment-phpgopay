using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Tgs.Payment.PhpGoPay;

namespace PhpGoPay.Core.UnitTests;

public class JsonDeserializeTests
{
	[Fact]
	public void 存款申請返回Remark為數字會解讀成字串()
	{
		var json = JsonNode.Parse("""
			{
			    "redirect_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "qrcode_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "gcash_qr_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d?g_type=1",
			    "order_id": "a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "remark": 0,
			    "amount": "100",
			    "status": "1",
			    "message": "success"
			}
			""");

		var actual = json.Deserialize<PhpGoClientDepositResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		});

		Assert.NotNull(actual);
		Assert.Equal("0", actual.Remark);
	}

	[Fact]
	public void 存款申請返回Remark字串也會正常解讀()
	{
		var json = JsonNode.Parse("""
			{
			    "redirect_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "qrcode_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "gcash_qr_url": "https:\/\/wpay.galaxyhub.cc\/api\/payment_direct\/redirect\/a3303a1b-f627-4cc0-9f5d-65b0ed50092d?g_type=1",
			    "order_id": "a3303a1b-f627-4cc0-9f5d-65b0ed50092d",
			    "remark": "0",
			    "amount": "100",
			    "status": "1",
			    "message": "success"
			}
			""");

		var actual = json.Deserialize<PhpGoClientDepositResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
		});

		Assert.NotNull(actual);
		Assert.Equal("0", actual.Remark);
	}
}