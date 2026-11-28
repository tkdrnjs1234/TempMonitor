// ============================================================================================
//  Models/UserInfo.cs : 로그인한 사용자 정보를 담는 "그릇" 클래스
// --------------------------------------------------------------------------------------------
//  Model(모델) = 기능(메서드) 없이 값만 담아서 파일 사이로 전달하는 클래스.
//
//  흐름
//    만드는 곳 : Services/AuthService.cs  LoginAsync() 에서 로그인 성공 시 new UserInfo { ... }
//    전달      : LoginResult.User → LoginForm → Session.CurrentUser 에 보관
//    쓰는 곳   : MainForm (사용자 표시, 권한), KakaoService (갱신자 ID)
//
//  ※ 비밀번호 해시/솔트는 여기 없습니다. 그건 Data/UserRepository.cs 의 UserCredential 에만 있고,
//    화면까지 올라가는 이 그릇에는 필요한 정보만 옮겨 담습니다 (보안).
// ============================================================================================

namespace TempMonitor.Models   // Models 폴더의 클래스들 이름 공간 → 다른 파일은 using TempMonitor.Models; 로 사용
{
    public class UserInfo       // public : 다른 파일 어디서나 쓸 수 있음
    {
        public string UserId { get; set; }     // 사용자 아이디   (DB TB_USER.USER_ID)
        public string UserName { get; set; }   // 사용자 이름     (DB TB_USER.USER_NM)
        public string RoleCode { get; set; }   // 권한 코드       (DB TB_USER.ROLE_CD : "ADMIN" 또는 "WORKER")

        // 계산 속성: 따로 저장하지 않고, 꺼낼 때마다 RoleCode 를 보고 계산해서 돌려줌
        // (RoleCode 가 바뀌면 자동으로 따라 바뀜. set 이 없으므로 직접 값을 넣을 수 없음)
        public bool IsAdmin
        {
            get { return RoleCode == "ADMIN"; }   // "ADMIN" 이면 true → Session.IsAdmin 이 이 값을 사용
        }

        // 화면 표시용 한글 권한 이름. MainForm_Load 에서 "(권한 : 관리자)" 로 표시할 때 사용
        public string RoleName
        {
            // 조건 ? A : B  (삼항 연산자) : IsAdmin 이 true 면 "관리자", 아니면 "작업자"
            get { return IsAdmin ? "관리자" : "작업자"; }
        }
    }
}
