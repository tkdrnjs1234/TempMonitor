// ============================================================================================
//  Services/AuthService.cs : 로그인 "규칙" (비즈니스 로직)
// --------------------------------------------------------------------------------------------
//  화면(LoginForm)은 "아이디/비번 입력받고 결과 보여주기" 만 하고,
//  "이 사람을 들여보내도 되나?" 라는 규칙 판단은 전부 여기서 합니다.
//  → 화면을 바꿔도 규칙은 그대로, 규칙을 바꿔도 화면은 그대로.
//
//  호출 흐름
//    LoginForm.btnLogin_Click
//      → AuthService.LoginAsync(userId, password)
//          → UserRepository.GetCredentialAsync      (DB 에서 사용자 정보)
//          → PasswordHasher.Verify                  (비번 비교)
//          → UserRepository.IncreaseFailCountAsync / MarkLoginSuccessAsync / InsertLoginHistoryAsync
//      ← LoginResult (성공 여부 + 메시지 + 사용자 정보)
// ============================================================================================

using System.Threading.Tasks;   // Task
using TempMonitor.Data;         // UserRepository, UserCredential (Data/UserRepository.cs)
using TempMonitor.Models;       // UserInfo (Models/UserInfo.cs)

namespace TempMonitor.Services
{
    // ---- 로그인 결과 그릇 ---------------------------------------------------------------------
    // 메서드는 return 값이 하나뿐이라, "성공했나 + 실패 메시지 + 사용자 정보" 3가지를 한 번에 돌려주려고 묶은 클래스.
    // 만드는 곳: 아래 LoginAsync / Fail    받는 곳: LoginForm.btnLogin_Click
    public class LoginResult
    {
        public bool Success { get; set; }      // true = 로그인 성공
        public string Message { get; set; }    // 실패 시 화면에 보여 줄 메시지
        public UserInfo User { get; set; }     // 성공 시 사용자 정보 → LoginForm 이 Session.CurrentUser 에 보관
    }

    /// <summary>
    /// 로그인 규칙(비즈니스 로직)을 화면(LoginForm)과 분리해 둔 클래스.
    /// 화면은 "입력받고 결과 보여주기"만, 규칙은 여기서만 → 화면을 바꿔도 규칙은 그대로.
    /// </summary>
    public class AuthService
    {
        // 비밀번호를 이 횟수 이상 틀리면 계정 잠김. const 라 AuthService.MaxFailCount 로 어디서든 읽을 수 있음
        public const int MaxFailCount = 5;

        // Data/UserRepository.cs 객체. TB_USER / TB_LOGIN_HIST SQL 은 전부 이 객체에게 시킴.
        // private  : 이 클래스 안에서만 사용
        // readonly : 선언할 때(또는 생성자에서)만 값을 넣을 수 있음 → 나중에 실수로 바꿔치기 불가
        // = new UserRepository() : AuthService 가 만들어질 때(LoginForm 의 _auth 필드 초기화 시) 같이 하나 만들어 둠
        private readonly UserRepository _users = new UserRepository();

        // ---- 로그인 -----------------------------------------------------------------------------
        // 호출: LoginForm.btnLogin_Click →  LoginResult result = await _auth.LoginAsync(userId, password);
        // async Task<LoginResult> : 비동기로 실행되고 끝나면 LoginResult 를 돌려줌
        // 구조: 위에서부터 차례로 "안 되는 경우" 를 걸러내고, 끝까지 통과하면 성공 (순서 자체가 규칙)
        public async Task<LoginResult> LoginAsync(string userId, string password)
        {
            // DB 에서 이 아이디의 해시/솔트/사용여부/실패횟수 읽기. 없는 아이디면 null
            UserCredential cred = await _users.GetCredentialAsync(userId);

            // 아이디가 없을 때와 비밀번호가 틀렸을 때 같은 메시지를 보여줍니다.
            // (메시지가 다르면 "이 아이디는 존재한다"는 정보를 공격자에게 주게 됨)
            const string wrongMessage = "아이디 또는 비밀번호가 올바르지 않습니다.";   // 메서드 안의 지역 상수

            // ① 없는 아이디
            if (cred == null)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "존재하지 않는 아이디");   // 실패 이력 남기기
                return Fail(wrongMessage);                                                    // 아래 Fail 도우미로 실패 결과 만들어 반환
            }

            // ② 사용 중지된 계정 (USE_YN = 'N')
            if (!cred.IsActive)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "사용 중지 계정");
                return Fail("사용이 중지된 계정입니다. 관리자에게 문의하세요.");
            }

            // ③ 잠긴 계정 : 비번 비교보다 "먼저" 검사 → 잠긴 계정은 비번이 맞아도 못 들어감
            if (cred.FailCount >= MaxFailCount)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "계정 잠김");
                // string.Format("... {0}회 ...", 값) : {0} 자리에 MaxFailCount(5) 가 들어감
                return Fail(string.Format("비밀번호를 {0}회 이상 틀려 계정이 잠겼습니다. 관리자에게 문의하세요.", MaxFailCount));
            }

            // ④ 비밀번호 비교 : PasswordHasher.cs 의 static 메서드 (new 없이 클래스 이름으로 호출)
            //    DB 의 솔트 + 입력 비번 → 해시 → DB 의 해시와 같은지
            if (!PasswordHasher.Verify(cred.PasswordSalt, password, cred.PasswordHash))
            {
                await _users.IncreaseFailCountAsync(userId);                              // DB 의 FAIL_CNT + 1
                await _users.InsertLoginHistoryAsync(userId, false, "비밀번호 불일치");
                // 남은 시도 횟수 안내는 "아이디가 존재한다"는 힌트가 될 수 있지만,
                // 사내 PC 프로그램이라 잠김 예고로 인한 문의/혼란을 줄이는 편의성을 택함 (보안 ↔ 편의 트레이드오프)
                // cred.FailCount 는 "DB 에서 읽은 시점" 값이라, 방금 1 올린 것까지 반영하려고 + 1
                int remain = MaxFailCount - (cred.FailCount + 1);
                return Fail(remain > 0
                    ? string.Format("{0} (남은 시도 {1}회)", wrongMessage, remain)   // 아직 기회 남음
                    : "계정이 잠겼습니다. 관리자에게 문의하세요.");                     // 이번이 마지막이었음
            }

            // ⑤ 모든 검사 통과 = 로그인 성공
            await _users.MarkLoginSuccessAsync(userId);              // 실패 횟수 0, 마지막 로그인 시각 기록
            await _users.InsertLoginHistoryAsync(userId, true, null); // 성공 이력 (실패 사유 없음 → null)

            // 성공 결과 만들기
            // UserCredential(해시 포함) → UserInfo(해시 없음) 로 필요한 것만 옮겨 담아 화면에 넘김 (해시가 화면까지 안 올라가게)
            return new LoginResult        // 객체 초기화자 { 속성 = 값 }
            {
                Success = true,
                User = new UserInfo       // 초기화자 안에서 또 new + 초기화자 (중첩)
                {
                    UserId = cred.UserId,
                    UserName = cred.UserName,
                    RoleCode = cred.RoleCode
                }
            };
        }

        // ---- 실패 결과 만들기 도우미 -----------------------------------------------------------------
        // 위에서 실패 결과를 4번 만들어서, 반복을 줄이려고 작은 메서드로 뺌
        // private static : 이 클래스 안에서만, 필드를 안 쓰니 static
        private static LoginResult Fail(string message)
        {
            return new LoginResult { Success = false, Message = message };   // User 는 안 넣음 → null
        }
    }
}
