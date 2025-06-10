using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace TeraLinkaMSDocEditorApi.Application.Common.Utils;

public static class JwtUtils
{
    public static string GenerateToken(object payload, string secret, int expirationMinutes = 30)
    {
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var securityKey = new SymmetricSecurityKey(secretBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                signingCredentials: credentials
            );

            // 自定義header
            token.Header.Clear();
            token.Header["alg"] = "HS256";

            // 將 payload 添加到 token 的 payload 部分
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var payloadJson = JsonSerializer.Serialize(payload, options);
            var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson, options);
            foreach (var item in payloadDict)
            {
                token.Payload[item.Key] = item.Value;
            }

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            throw new Exception("生成 token 時發生錯誤", ex);
        }
    }
} 