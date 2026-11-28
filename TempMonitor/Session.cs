// ============================================================================================
//  Session.cs : "지금 로그인한 사람" 을 프로그램 어디서든 꺼내 쓰는 공용 보관함
// --------------------------------------------------------------------------------------------
//  왜 필요한가?
//    로그인은 LoginForm 에서 하는데, 그 정보를 쓰는 곳은 MainForm, KakaoService 등 다른 파일입니다.
//    LoginForm 은 로그인 직후 닫혀서 사라지므로, 폼 바깥에 "모두가 볼 수 있는 보관함" 이 필요합니다.
//
//  누가 쓰나?
//    넣기   : LoginForm.btnLogin_Click   → Session.CurrentUser = result.User;
//    읽기   : MainForm.MainForm_Load     → 상단 "사용자 : admin(권한 : 관리자)" 표시
//             MainForm.ApplyPermission   → Session.IsAdmin 으로 설정 화면 잠그기
//             MainForm.btnSaveSetting    → 저장한 사람 ID 기록
//             KakaoService.RefreshTokenAsync → 토큰 갱신한 사람 ID 기록
//    지우기 : Program.Main               → Session.Clear(); (메인 창이 닫힌 뒤)
// ============================================================================================

using TempMonitor.Models;   // UserInfo 클래스(Models/UserInfo.cs) 를 짧은 이름으로 쓰기 위해

namespace TempMonitor
{
    /// <summary>
    /// 로그인한 사용자 정보를 프로그램 전체에서 꺼내 쓰기 위한 정적 클래스.
    /// (데스크톱 프로그램은 한 PC 에 한 사용자이므로 정적 보관으로 충분합니다)
    /// </summary>
    // static class : new Session() 으로 객체를 만들 수 없고, 프로그램 전체에 딱 하나만 존재.
    //                그래서 어느 파일에서든 "Session.CurrentUser" 처럼 클래스 이름으로 바로 접근.
    public static class Session
    {
        // 지금 로그인한 사용자 정보 (Models/UserInfo.cs 객체). 로그인 전/로그아웃 후에는 null.
        // { get; set; } : 꺼내기(get)와 넣기(set) 둘 다 가능한 "자동 속성" (값 보관 칸)
        public static UserInfo CurrentUser { get; set; }

        // "지금 사용자가 관리자인가?" 를 계산해서 돌려주는 읽기 전용 속성 (set 이 없음 = 값을 넣을 수 없음)
        public static bool IsAdmin
        {
            // CurrentUser != null  : 로그인 안 된 상태면 false
            // &&                   : 왼쪽이 false 면 오른쪽을 아예 실행하지 않음(단락 평가)
            //                        → null 인데 .IsAdmin 을 꺼내다가 NullReferenceException 나는 것을 막음
            // CurrentUser.IsAdmin  : UserInfo.cs 의 계산 속성 (RoleCode == "ADMIN")
            get { return CurrentUser != null && CurrentUser.IsAdmin; }
        }

        // 로그인 정보 지우기. Program.Main 이 메인 창이 닫힌 직후 호출.
        public static void Clear()
        {
            CurrentUser = null;   // 보관함 비우기
        }
    }
}
