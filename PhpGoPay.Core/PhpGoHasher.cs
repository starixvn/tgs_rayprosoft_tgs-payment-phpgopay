using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.PhpGoPay;

public class PhpGoHasher(ILogger<PhpGoHasher> logger)
{
	private readonly ILogger<PhpGoHasher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public string ComputeHash(string key, IEnumerable<KeyValuePair<string, string?>> dict)
	{
		var content = string.Join(
			"&",
			dict.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Key is not "sign")
				.OrderBy(kvp => kvp.Key)
				.Select(p => $"{p.Key}={p.Value}"));

		var fullText = $"{content}&key={key}";

		_logger.LogTrace("FullText: {fullText}", fullText);

		var hash = new BigInteger(
			MD5.HashData(Encoding.UTF8.GetBytes(fullText)),
			isBigEndian: true).ToString("x32");

		_logger.LogTrace("Hash: {hash}", hash);

		return hash;
	}
}
