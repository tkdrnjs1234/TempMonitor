using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using TempMonitor.Data;
using TempMonitor.Models;

namespace TempMonitor.Services
{
    public class KakaoSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 카카오톡 "나에게 보내기" API 로 알림을 보냅니다.
    ///
    /// 준비 (developers.kakao.com)
    ///   1) 애플리케이션 추가 → REST API 키, 클라이언트 시크릿 확인
    ///   2) 카카오 로그인 활성화, 동의항목에서 "카카오톡 메시지 전송(talk_message)" 설정
    ///   3) 인가 코드 → 토큰 발급 (access_token, refresh_token)
    ///   4) TB_CONFIG 의 KAKAO_REST_API_KEY / KAKAO_CLIENT_SECRET / KAKAO_ACCESS_TOKEN / KAKAO_REFRESH_TOKEN 에 저장
    ///   → 3), 4) 는 Database\02_KakaoToken.ps1 스크립트가 해 줍니다.
    ///
    /// 액세스 토큰은 몇 시간 뒤 만료되므로, 401 응답을 받으면 리프레시 토큰으로 새 토큰을 받아
    /// DB 에 저장한 뒤 한 번 더 보냅니다.
    /// </summary>
    public class KakaoService
    {
        private const string SendUrl = "https://kapi.kakao.com/v2/api/talk/memo/default/send";
        private const string TokenUrl = "https://kauth.kakao.com/oauth/token";

        // HttpClient 는 요청마다 만들면 소켓이 고갈될 수 있어 하나를 재사용하는 것이 권장 패턴입니다.
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly ConfigRepository _config = new ConfigRepository();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public async Task<bool> IsConfiguredAsync()
        {
            string token = await _config.GetValueAsync(AppConfig.KeyKakaoAccessToken);
            return !string.IsNullOrWhiteSpace(token);
        }

        public async Task<KakaoSendResult> SendToMeAsync(string text)
        {
            try
            {
                Dictionary<string, string> cfg = await _config.LoadAllAsync();
                string accessToken = Get(cfg, AppConfig.KeyKakaoAccessToken);

                if (string.IsNullOrWhiteSpace(accessToken))
                    return new KakaoSendResult { Success = false, Message = "카카오 토큰 미설정" };

                HttpResponseMessage resp = await PostMessageAsync(accessToken, text);

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshTokenAsync(cfg);
                    if (newToken == null)
                        return new KakaoSendResult { Success = false, Message = "토큰 만료(갱신 실패)" };

                    resp = await PostMessageAsync(newToken, text);
                }

                string body = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode)
                    return new KakaoSendResult { Success = true, Message = "OK" };

                return new KakaoSendResult
                {
                    Success = false,
                    Message = string.Format("HTTP {0}: {1}", (int)resp.StatusCode, body)
                };
            }
            catch (Exception ex)
            {
                // 알림 실패가 모니터링 전체를 멈추면 안 되므로 예외를 결과로 바꿔 돌려줍니다.
                return new KakaoSendResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<HttpResponseMessage> PostMessageAsync(string accessToken, string text)
        {
            // 텍스트 템플릿 (text 는 최대 200자, link 는 필수 항목)
            var template = new
            {
                object_type = "text",
                text = text.Length > 200 ? text.Substring(0, 200) : text,
                link = new { web_url = "https://developers.kakao.com", mobile_web_url = "https://developers.kakao.com" }
            };

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("template_object", _json.Serialize(template))
            });

            using (var req = new HttpRequestMessage(HttpMethod.Post, SendUrl))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req.Content = content;
                return await Http.SendAsync(req);
            }
        }

        /// <summary>리프레시 토큰으로 새 액세스 토큰을 받아 DB 에 저장. 실패하면 null.</summary>
        private async Task<string> RefreshTokenAsync(Dictionary<string, string> cfg)
        {
            string restKey = Get(cfg, AppConfig.KeyKakaoRestApiKey);
            string refreshToken = Get(cfg, AppConfig.KeyKakaoRefreshToken);
            if (string.IsNullOrWhiteSpace(restKey) || string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", restKey),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            };

            // 새로 만든 카카오 앱은 "클라이언트 시크릿"이 기본 ON → 토큰 요청에 client_secret 이 필수
            string secret = Get(cfg, AppConfig.KeyKakaoClientSecret);
            if (!string.IsNullOrWhiteSpace(secret))
                form.Add(new KeyValuePair<string, string>("client_secret", secret));

            HttpResponseMessage resp = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(form));
            if (!resp.IsSuccessStatusCode)
                return null;

            var result = _json.Deserialize<Dictionary<string, object>>(await resp.Content.ReadAsStringAsync());

            object access;
            if (!result.TryGetValue("access_token", out access) || access == null)
                return null;

            var toSave = new Dictionary<string, string> { { AppConfig.KeyKakaoAccessToken, access.ToString() } };

            // 리프레시 토큰은 만료가 가까울 때만 새로 내려옵니다.
            object newRefresh;
            if (result.TryGetValue("refresh_token", out newRefresh) && newRefresh != null)
                toSave[AppConfig.KeyKakaoRefreshToken] = newRefresh.ToString();

            string userId = Session.CurrentUser != null ? Session.CurrentUser.UserId : "SYSTEM";
            await _config.SaveAsync(toSave, userId);

            return access.ToString();
        }

        private static string Get(Dictionary<string, string> d, string key)
        {
            string v;
            return d.TryGetValue(key, out v) ? v : null;
        }
    }
}
