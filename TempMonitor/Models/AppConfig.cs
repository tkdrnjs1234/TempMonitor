namespace TempMonitor.Models
{
    /// <summary>
    /// TB_CONFIG 의 Key-Value 를 프로그램에서 쓰기 편하게 형 변환해 둔 객체.
    /// 화면 곳곳에서 문자열 키("ALERT_TEMP")를 직접 쓰지 않도록 여기 한 곳에 모읍니다.
    /// </summary>
    public class AppConfig
    {
        public const string KeyAlertTemp = "ALERT_TEMP";
        public const string KeyAlertIntervalMin = "ALERT_INTERVAL_MIN";
        public const string KeyAlertRiseStep = "ALERT_RISE_STEP";
        public const string KeyKakaoEnabled = "KAKAO_ENABLED";
        public const string KeyReadCycleSec = "READ_CYCLE_SEC";
        public const string KeySaveCycleSec = "SAVE_CYCLE_SEC";
        public const string KeyKakaoReceiverName = "KAKAO_RECEIVER_NM";
        public const string KeyKakaoRestApiKey = "KAKAO_REST_API_KEY";
        public const string KeyKakaoClientSecret = "KAKAO_CLIENT_SECRET";
        public const string KeyKakaoAccessToken = "KAKAO_ACCESS_TOKEN";
        public const string KeyKakaoRefreshToken = "KAKAO_REFRESH_TOKEN";

        // DB 를 못 읽었을 때도 프로그램이 동작하도록 기본값을 둡니다.
        public decimal AlertTemp { get; set; } = 30.0m;
        public int AlertIntervalMin { get; set; } = 10;
        /// <summary>경고 중 직전 알림 온도보다 이만큼 오르면 간격 무시하고 즉시 재알림 (0 = 사용 안 함)</summary>
        public decimal AlertRiseStep { get; set; } = 5.0m;
        public bool KakaoEnabled { get; set; } = true;
        public int ReadCycleSec { get; set; } = 1;
        public int SaveCycleSec { get; set; } = 5;
        public string KakaoReceiverName { get; set; } = "담당자";
    }
}
