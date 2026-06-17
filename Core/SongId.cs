namespace Core;

using System.Security.Cryptography;
using System.Text;

public static class SongId
{
	public static string From(string path)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
		return Convert.ToHexStringLower(hash)[..16];
	}
}
