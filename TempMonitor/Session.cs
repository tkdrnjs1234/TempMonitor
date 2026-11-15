using TempMonitor.Models;

namespace TempMonitor
{
    /// <summary>
    /// 로그인한 사용자 정보를 프로그램 전체에서 꺼내 쓰기 위한 정적 클래스.
    /// (데스크톱 프로그램은 한 PC 에 한 사용자이므로 정적 보관으로 충분합니다)
    /// </summary>
    public static class Session
    {
        public static UserInfo CurrentUser { get; set; }

        public static bool IsAdmin
        {
            get { return CurrentUser != null && CurrentUser.IsAdmin; }
        }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
