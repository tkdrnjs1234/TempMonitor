// ============================================================================================
//  Services/KakaoService.cs : 카카오톡 "나에게 보내기" 발송
// --------------------------------------------------------------------------------------------
//  인터넷(HTTPS)으로 카카오 서버에 메시지를 보내는 일을 맡은 클래스입니다.
//  토큰(카카오가 발급한 출입증)은 DB 의 TB_CONFIG 에 저장되어 있어서 ConfigRepository 로 읽고 씁니다.
//
//  메서드와 호출하는 곳
//    IsConfiguredAsync : 액세스 토큰이 DB 에 있나?   ← MainForm.RefreshKakaoStatusAsync ("토큰 설정됨" 표시)
//    SendToMeAsync     : 메시지 보내기              ← MainForm.SendKakaoAndLogAsync (경고/복귀/테스트)
//      → PostMessageAsync  : 실제 HTTP 요청
//      → RefreshTokenAsync : 토큰 만료(401) 시 새 토큰 받아서 DB 저장
//
//  중요한 설계: SendToMeAsync 는 실패해도 예외를 던지지 않고 KakaoSendResult(Success=false) 를 돌려줍니다.
//              카톡 하나 실패했다고 온도 모니터링 전체가 멈추면 안 되기 때문.
// ============================================================================================

using System;                              // TimeSpan, Exception
using System.Collections.Generic;          // Dictionary, List, KeyValuePair
using System.Net;                          // HttpStatusCode (401 Unauthorized 등)
using System.Net.Http;                     // HttpClient, HttpRequestMessage, FormUrlEncodedContent
using System.Net.Http.Headers;             // AuthenticationHeaderValue (Authorization 헤더)
using System.Threading.Tasks;              // Task
using System.Web.Script.Serialization;     // JavaScriptSerializer (C# 객체 ↔ JSON 문자열). 참조에 System.Web.Extensions 필요
using TempMonitor.Data;                    // ConfigRepository
using TempMonitor.Models;                  // AppConfig (키 상수들)

namespace TempMonitor.Services
{
    // ---- 발송 결과 그릇 ---------------------------------------------------------------------
    // LoginResult 와 같은 이유: "성공했나 + 실패 사유" 두 가지를 한 번에 돌려주려고
    // 받는 곳: MainForm.SendKakaoAndLogAsync → 결과를 TB_KAKAO_LOG 에 기록
    public class KakaoSendResult
    {
        public bool Success { get; set; }     // true = 발송 성공
        public string Message { get; set; }   // 성공 "OK" / 실패 사유 ("카카오 토큰 미설정", "HTTP 400: ..." 등)
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
        private const string SendUrl = "https://kapi.kakao.com/v2/api/talk/memo/default/send";   // 메시지 보내기 API 주소
        private const string TokenUrl = "https://kauth.kakao.com/oauth/token";                   // 토큰 갱신 API 주소

        // HttpClient 는 요청마다 만들면 소켓이 고갈될 수 있어 하나를 재사용하는 것이 권장 패턴입니다.
        // static readonly : 프로그램 전체에 딱 하나, 한 번 만들면 안 바뀜
        // { Timeout = 10초 } : 카카오 서버가 10초 안에 답이 없으면 포기 (예외 → SendToMeAsync 의 catch 가 결과로 바꿈)
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Data/ConfigRepository.cs 객체. 토큰이 TB_CONFIG 에 있어서 읽기(LoadAllAsync, GetValueAsync)/저장(SaveAsync) 에 사용
        // ※ MainForm 에도 _config 라는 필드가 있지만 그건 AppConfig 타입. 필드는 클래스마다 따로라서 이름이 겹쳐도 상관없음
        private readonly ConfigRepository _config = new ConfigRepository();

        // JSON 변환기. 메시지 템플릿(C# 객체) → JSON 문자열, 토큰 응답(JSON 문자열) → Dictionary
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ---- 토큰 설정 여부 ----------------------------------------------------------------------
        // 호출: MainForm.RefreshKakaoStatusAsync
        public async Task<bool> IsConfiguredAsync()
        {
            // ConfigRepository.GetValueAsync → DbHelper.ExecuteScalarAsync → TB_CONFIG 의 KAKAO_ACCESS_TOKEN 값 하나
            string token = await _config.GetValueAsync(AppConfig.KeyKakaoAccessToken);
            return !string.IsNullOrWhiteSpace(token);   // null/빈칸/공백만 있으면 false
        }

        // ---- 메시지 보내기 -----------------------------------------------------------------------
        // 호출: MainForm.SendKakaoAndLogAsync
        // text: 보낼 메시지 내용
        public async Task<KakaoSendResult> SendToMeAsync(string text)
        {
            try
            {
                // TB_CONFIG 전체를 사전으로 한 번에 읽음 (토큰들이 다 들어 있음)
                // 한 번 읽은 cfg 를 아래 RefreshTokenAsync 에도 넘겨서 DB 를 두 번 읽지 않음
                Dictionary<string, string> cfg = await _config.LoadAllAsync();
                string accessToken = Get(cfg, AppConfig.KeyKakaoAccessToken);   // 아래 Get 도우미: 키가 없으면 null

                if (string.IsNullOrWhiteSpace(accessToken))   // 토큰이 아예 없으면 보낼 수 없음
                    return new KakaoSendResult { Success = false, Message = "카카오 토큰 미설정" };

                // 실제 HTTP 요청 (아래 PostMessageAsync)
                HttpResponseMessage resp = await PostMessageAsync(accessToken, text);

                // 401 Unauthorized = 토큰 만료 → 새 토큰 받아서 한 번만 재시도
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    string newToken = await RefreshTokenAsync(cfg);   // 실패하면 null
                    if (newToken == null)
                        return new KakaoSendResult { Success = false, Message = "토큰 만료(갱신 실패)" };

                    resp = await PostMessageAsync(newToken, text);   // 새 토큰으로 다시 보내기 (resp 를 새 응답으로 교체)
                }

                string body = await resp.Content.ReadAsStringAsync();   // 응답 본문(JSON 문자열). 실패 사유를 담으려고 읽음
                if (resp.IsSuccessStatusCode)                           // 200번대 응답이면 성공
                    return new KakaoSendResult { Success = true, Message = "OK" };

                // 그 외 실패 (400 잘못된 요청 등) → 상태 코드와 본문을 사유로
                return new KakaoSendResult
                {
                    Success = false,
                    Message = string.Format("HTTP {0}: {1}", (int)resp.StatusCode, body)   // (int) : enum → 숫자 (예: 400)
                };
            }
            catch (Exception ex)   // 인터넷 끊김, 10초 시간 초과, DB 오류 등 어떤 예외든
            {
                // 알림 실패가 모니터링 전체를 멈추면 안 되므로 예외를 결과로 바꿔 돌려줍니다.
                return new KakaoSendResult { Success = false, Message = ex.Message };
            }
        }

        // ---- 실제 HTTP POST 요청 -----------------------------------------------------------------
        // 호출: 위 SendToMeAsync (처음 1번, 토큰 갱신 후 1번)
        private async Task<HttpResponseMessage> PostMessageAsync(string accessToken, string text)
        {
            // 텍스트 템플릿 (text 는 최대 200자, link 는 필수 항목)
            // new { ... } = "익명 타입": 클래스를 따로 안 만들고 즉석에서 속성 묶음을 만드는 문법.
            //   속성 이름(object_type, text, link)이 그대로 JSON 키가 되므로 카카오 API 규격대로 소문자_밑줄 이름 사용
            var template = new
            {
                object_type = "text",                                              // 텍스트형 메시지
                text = text.Length > 200 ? text.Substring(0, 200) : text,          // 200자 넘으면 잘라냄
                link = new { web_url = "https://developers.kakao.com", mobile_web_url = "https://developers.kakao.com" }   // 메시지 클릭 시 이동할 주소 (필수)
            };

            // 요청 본문: template_object=<JSON> 형태의 폼 데이터 (카카오 API 가 요구하는 형식)
            // _json.Serialize(template) : 익명 객체 → JSON 문자열 {"object_type":"text",...}
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("template_object", _json.Serialize(template))
            });

            // HttpRequestMessage : 보낼 요청 하나 (방식 POST + 주소). 다 보내면 using 으로 정리
            using (var req = new HttpRequestMessage(HttpMethod.Post, SendUrl))
            {
                // 헤더 "Authorization: Bearer <토큰>" → 카카오 서버가 누구 메시지인지 확인하는 방법
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req.Content = content;              // 본문 붙이기
                return await Http.SendAsync(req);   // static HttpClient 로 전송 → 응답 돌려줌
            }
        }

        // ---- 토큰 갱신 ---------------------------------------------------------------------------
        // 호출: 위 SendToMeAsync (401 응답일 때)
        // cfg : SendToMeAsync 가 이미 읽어 둔 설정 사전
        /// <summary>리프레시 토큰으로 새 액세스 토큰을 받아 DB 에 저장. 실패하면 null.</summary>
        private async Task<string> RefreshTokenAsync(Dictionary<string, string> cfg)
        {
            string restKey = Get(cfg, AppConfig.KeyKakaoRestApiKey);          // 카카오 앱 키
            string refreshToken = Get(cfg, AppConfig.KeyKakaoRefreshToken);   // 갱신용 토큰
            if (string.IsNullOrWhiteSpace(restKey) || string.IsNullOrWhiteSpace(refreshToken))   // 둘 중 하나라도 없으면 갱신 불가
                return null;

            // 토큰 갱신 요청에 보낼 폼 데이터 (카카오 OAuth 규격)
            // List 로 만든 이유: 아래에서 조건에 따라 client_secret 을 Add 로 추가해야 해서 (배열은 크기 고정)
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),   // "리프레시 토큰으로 갱신할게요"
                new KeyValuePair<string, string>("client_id", restKey),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            };

            // 새로 만든 카카오 앱은 "클라이언트 시크릿"이 기본 ON → 토큰 요청에 client_secret 이 필수
            string secret = Get(cfg, AppConfig.KeyKakaoClientSecret);
            if (!string.IsNullOrWhiteSpace(secret))
                form.Add(new KeyValuePair<string, string>("client_secret", secret));

            // POST 요청 보내기 (헤더 설정이 필요 없어서 SendAsync 대신 간단한 PostAsync 사용)
            HttpResponseMessage resp = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(form));
            if (!resp.IsSuccessStatusCode)   // 갱신 실패 (리프레시 토큰도 만료 등)
                return null;

            // 응답 JSON {"access_token":"...","refresh_token":"...",...} → Dictionary<string, object>
            var result = _json.Deserialize<Dictionary<string, object>>(await resp.Content.ReadAsStringAsync());

            object access;   // TryGetValue 결과 받을 변수
            if (!result.TryGetValue("access_token", out access) || access == null)   // 새 액세스 토큰이 없으면
                return null;

            // DB 에 저장할 값 사전 (새 액세스 토큰)
            var toSave = new Dictionary<string, string> { { AppConfig.KeyKakaoAccessToken, access.ToString() } };

            // 리프레시 토큰은 만료가 가까울 때만 새로 내려옵니다.
            object newRefresh;
            if (result.TryGetValue("refresh_token", out newRefresh) && newRefresh != null)
                toSave[AppConfig.KeyKakaoRefreshToken] = newRefresh.ToString();   // 있으면 같이 저장

            // 누가 갱신했는지: 로그인 사용자가 있으면 그 ID, 없으면 "SYSTEM" (Session.cs 의 static 속성 사용)
            string userId = Session.CurrentUser != null ? Session.CurrentUser.UserId : "SYSTEM";
            // ConfigRepository.SaveAsync → 트랜잭션으로 TB_CONFIG 에 UPSERT. 다음 발송부터 새 토큰 사용
            await _config.SaveAsync(toSave, userId);

            return access.ToString();   // 새 토큰을 돌려줘서 SendToMeAsync 가 바로 재전송
        }

        // ---- 사전에서 안전하게 값 꺼내기 -----------------------------------------------------------
        // d[key] 로 직접 꺼내면 키가 없을 때 KeyNotFoundException 이 나므로, TryGetValue 로 없으면 null
        private static string Get(Dictionary<string, string> d, string key)
        {
            string v;
            return d.TryGetValue(key, out v) ? v : null;
        }
    }
}
