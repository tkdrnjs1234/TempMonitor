using System;

namespace TempMonitor.Models
{
    /// <summary>온도 상태. DB(TB_TEMP_LOG.STATUS_CD)에는 ToCode() 결과 문자열로 저장합니다.</summary>
    public enum TempStatus
    {
        Normal,
        Warning,
        Error
    }

    public static class TempStatusExtensions
    {
        public static string ToCode(this TempStatus status)
        {
            switch (status)
            {
                case TempStatus.Warning: return "WARNING";
                case TempStatus.Error: return "ERROR";
                default: return "NORMAL";
            }
        }

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

    /// <summary>센서에서 한 번 읽어 온 값 묶음.</summary>
    public class TempReading
    {
        public DateTime ReadTime { get; set; }
        public decimal Temperature { get; set; }
        public decimal MinTemperature { get; set; }
        public decimal MaxTemperature { get; set; }
        public int ErrorCode { get; set; }

        public bool HasError
        {
            get { return ErrorCode != 0; }
        }

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
                    default: return "알 수 없는 에러(" + ErrorCode + ")";
                }
            }
        }

        /// <summary>좁은 화면(현재 온도 패널)용 짧은 오류 이름</summary>
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
