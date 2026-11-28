// ============================================================================================
//  Models/AppConfig.cs : 프로그램 설정값 그릇
// --------------------------------------------------------------------------------------------
//  DB 의 TB_CONFIG 테이블은 (CONFIG_KEY, CONFIG_VALUE) 두 칸짜리 "이름표-값" 표이고, 값은 전부 문자열입니다.
//     예)  ALERT_TEMP     | "30.0"
//          READ_CYCLE_SEC | "1"
//  이 클래스는 그 문자열들을 decimal / int / bool 로 바꿔서 "쓰기 편하게" 담아 두는 그릇입니다.
//
//  흐름
//    DB → 그릇 : Data/ConfigRepository.cs  LoadAppConfigAsync()  (MainForm_Load 가 호출)
//    그릇 → DB : Data/ConfigRepository.cs  SaveAppConfigAsync()  (MainForm.btnSaveSetting_Click 이 호출)
//    사용      : MainForm 의 _config 필드에 보관 → 경고 판단, 읽기/저장 주기, 카톡 사용 여부 등
// ============================================================================================

namespace TempMonitor.Models
{
    /// <summary>
    /// TB_CONFIG 의 Key-Value 를 프로그램에서 쓰기 편하게 형 변환해 둔 객체.
    /// 화면 곳곳에서 문자열 키("ALERT_TEMP")를 직접 쓰지 않도록 여기 한 곳에 모읍니다.
    /// </summary>
    public class AppConfig
    {
        // ---- ① TB_CONFIG.CONFIG_KEY 이름표들 -------------------------------------------------
        // const : 절대 안 바뀌는 고정값. "ALERT_TEMP" 를 코드 여기저기 직접 쓰면 오타를 컴파일러가 못 잡지만,
        //         AppConfig.KeyAlertTemp 로 쓰면 오타 시 빨간 줄이 생김.
        // const 는 자동으로 static 이라 객체 없이 AppConfig.KeyAlertTemp 로 바로 씀.
        // 쓰는 곳: ConfigRepository(읽기/저장), KakaoService(토큰 키 4개)
        public const string KeyAlertTemp = "ALERT_TEMP";                    // 경고 기준 온도
        public const string KeyAlertIntervalMin = "ALERT_INTERVAL_MIN";     // 같은 경고 재알림 최소 간격(분)
        public const string KeyAlertRiseStep = "ALERT_RISE_STEP";           // 이만큼 더 오르면 즉시 재알림(℃)
        public const string KeyKakaoEnabled = "KAKAO_ENABLED";              // 카톡 알림 사용 여부 ("Y"/"N")
        public const string KeyReadCycleSec = "READ_CYCLE_SEC";             // 센서 읽기 주기(초)
        public const string KeySaveCycleSec = "SAVE_CYCLE_SEC";             // DB 저장 주기(초)
        public const string KeyKakaoReceiverName = "KAKAO_RECEIVER_NM";     // 카톡 이력에 남길 받는 사람 이름
        public const string KeyKakaoRestApiKey = "KAKAO_REST_API_KEY";      // 카카오 앱 REST API 키 (토큰 갱신용)
        public const string KeyKakaoClientSecret = "KAKAO_CLIENT_SECRET";   // 카카오 클라이언트 시크릿 (토큰 갱신용)
        public const string KeyKakaoAccessToken = "KAKAO_ACCESS_TOKEN";     // 메시지 보낼 때 쓰는 토큰 (몇 시간 뒤 만료)
        public const string KeyKakaoRefreshToken = "KAKAO_REFRESH_TOKEN";   // 액세스 토큰을 새로 받을 때 쓰는 토큰

        // ---- ② 형 변환된 설정값 (속성 + 기본값) ----------------------------------------------
        // DB 를 못 읽었을 때도 프로그램이 동작하도록 기본값을 둡니다.
        // "= 30.0m" : 속성 초기값. new AppConfig() 만 해도 이 값들로 채워짐.
        // m 접미사  : decimal 리터럴이라는 표시 (안 붙이면 double 로 취급되어 컴파일 에러)
        public decimal AlertTemp { get; set; } = 30.0m;        // 이 온도 "이상" 이면 경고 (MainForm.Evaluate 에서 비교)
        public int AlertIntervalMin { get; set; } = 10;        // 경고 지속 알림 간격(분) (MainForm.CheckAlertAsync)
        /// <summary>경고 중 직전 알림 온도보다 이만큼 오르면 간격 무시하고 즉시 재알림 (0 = 사용 안 함)</summary>
        public decimal AlertRiseStep { get; set; } = 5.0m;
        public bool KakaoEnabled { get; set; } = true;         // false 면 경고는 기록만 하고 카톡은 안 보냄
        public int ReadCycleSec { get; set; } = 1;             // MainForm.PollLoopAsync 의 Task.Delay 대기 시간
        public int SaveCycleSec { get; set; } = 5;             // MainForm.SaveIfNeededAsync 의 저장 간격
        public string KakaoReceiverName { get; set; } = "담당자"; // KakaoLogRepository.InsertAsync 의 RECEIVER_NM 으로 저장
    }
}
