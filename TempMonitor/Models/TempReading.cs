// ============================================================================================
//  Models/TempReading.cs : 센서 측정값 그릇 + 온도 상태(enum)
// --------------------------------------------------------------------------------------------
//  이 파일에는 3가지가 들어 있습니다.
//    ① enum TempStatus           : 정상 / 경고 / 센서오류 세 가지 상태
//    ② TempStatusExtensions      : 상태를 DB 코드("WARNING") 나 화면 글자("경고") 로 바꾸는 확장 메서드
//    ③ class TempReading         : 센서에서 한 번 읽어 온 값 묶음 (온도, 최저, 최고, 에러코드, 시각)
//
//  흐름
//    TempReading 을 만드는 곳 : Comm/Sig5560Client.cs  Build()  (센서 숫자 → 이 그릇)
//    TempStatus 를 정하는 곳  : MainForm.Evaluate()  (TempReading 을 보고 판단)
//    둘 다 쓰는 곳            : MainForm 화면 갱신 / 경고 판단, Data/TempLogRepository.cs (DB 저장)
// ============================================================================================

using System;   // DateTime 을 쓰기 위해

namespace TempMonitor.Models
{
    // ---- ① 온도 상태 -----------------------------------------------------------------------
    // enum(열거형) : 정해진 몇 가지 값 중 하나만 가질 수 있는 타입.
    //   상태를 "WARNING" 같은 문자열로 들고 다니면 "WARNIG" 오타를 못 잡지만,
    //   enum 은 TempStatus.Warning 말고는 쓸 수 없어서 오타가 컴파일 에러가 됨.
    //   (내부적으로는 Normal=0, Warning=1, Error=2 숫자)
    /// <summary>온도 상태. DB(TB_TEMP_LOG.STATUS_CD)에는 ToCode() 결과 문자열로 저장합니다.</summary>
    public enum TempStatus
    {
        Normal,    // 정상 : 경고 기준 온도 미만
        Warning,   // 경고 : 경고 기준 온도 이상
        Error      // 센서 오류 : 센서가 에러 코드를 보냄 (온도 값 믿을 수 없음)
    }

    // ---- ② 확장 메서드 --------------------------------------------------------------------
    // enum 안에는 메서드를 넣을 수 없어서, 바깥 static 클래스에 "this TempStatus" 매개변수를 가진 메서드를 만들면
    // 마치 enum 에 원래 있던 메서드처럼  status.ToCode()  로 부를 수 있음 = "확장 메서드".
    // (확장 메서드는 반드시 static class 안의 static 메서드여야 함)
    public static class TempStatusExtensions
    {
        // DB 저장용 코드로 변환. 쓰는 곳: Data/TempLogRepository.cs InsertAsync → TB_TEMP_LOG.STATUS_CD
        // this TempStatus status : "TempStatus 값 뒤에 .ToCode() 를 붙여 부를 수 있다" 는 표시. 점 앞의 값이 status 로 들어옴
        public static string ToCode(this TempStatus status)
        {
            switch (status)   // status 값에 따라 갈라지기
            {
                case TempStatus.Warning: return "WARNING";   // 경고
                case TempStatus.Error: return "ERROR";       // 센서 오류
                default: return "NORMAL";                    // 나머지(Normal) 전부
            }
        }

        // 화면 표시용 한글로 변환. 쓰는 곳: MainForm.UpdateCurrentPanel, MainForm.AddRealtimeRow
        public static string ToDisplayName(this TempStatus status)
        {
            switch (status)
            {
                case TempStatus.Warning: return "경고";
                case TempStatus.Error: return "센서 오류";
                default: return "정상";
            }
        }
    }

    // ---- ③ 측정값 그릇 --------------------------------------------------------------------
    /// <summary>센서에서 한 번 읽어 온 값 묶음.</summary>
    public class TempReading
    {
        public DateTime ReadTime { get; set; }         // 읽은 시각 (Sig5560Client.Build 에서 DateTime.Now)
        public decimal Temperature { get; set; }       // 현재 온도 ℃ (센서 정수 ÷ 10)
        public decimal MinTemperature { get; set; }    // 센서가 기억하는 최저 온도
        public decimal MaxTemperature { get; set; }    // 센서가 기억하는 최고 온도
        public int ErrorCode { get; set; }             // 센서 에러 코드 (0 = 정상)

        // 계산 속성: 에러가 있나? → MainForm.Evaluate, AddRealtimeRow, AddChartPoint, TempLogRepository 에서 사용
        public bool HasError
        {
            get { return ErrorCode != 0; }   // 0 이 아니면 에러
        }

        // 에러 코드 → 긴 한글 설명. 공간이 넓은 실시간 그리드(MainForm.AddRealtimeRow)에서 사용
        // (SIG-5560 설명서의 에러 코드 표를 그대로 옮긴 것)
        public string ErrorText
        {
            get
            {
                switch (ErrorCode)
                {
                    case 0: return "에러 없음";
                    case 1: return "하드웨어 초기화 에러";
                    case 2: return "하드웨어 런타임 에러";
                    case 3: return "온도센서 연결 끊김";
                    case 4: return "온도센서 측정 실패";
                    default: return "알 수 없는 에러(" + ErrorCode + ")";   // 표에 없는 코드는 숫자 그대로 보여 주기
                }
            }
        }

        /// <summary>좁은 화면(현재 온도 패널)용 짧은 오류 이름</summary>
        // 쓰는 곳: MainForm.UpdateCurrentPanel → "센서 오류 : 센서 단선"
        public string ErrorShortText
        {
            get
            {
                switch (ErrorCode)
                {
                    case 0: return "없음";
                    case 1: return "HW 초기화";
                    case 2: return "HW 동작 이상";
                    case 3: return "센서 단선";
                    case 4: return "측정 실패";
                    default: return "코드 " + ErrorCode;
                }
            }
        }
    }
}
