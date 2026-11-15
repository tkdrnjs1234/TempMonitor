using System.Threading.Tasks;
using TempMonitor.Data;
using TempMonitor.Models;

namespace TempMonitor.Services
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserInfo User { get; set; }
    }

    /// <summary>
    /// 로그인 규칙(비즈니스 로직)을 화면(LoginForm)과 분리해 둔 클래스.
    /// 화면은 "입력받고 결과 보여주기"만, 규칙은 여기서만 → 화면을 바꿔도 규칙은 그대로.
    /// </summary>
    public class AuthService
    {
        public const int MaxFailCount = 5;

        private readonly UserRepository _users = new UserRepository();

        public async Task<LoginResult> LoginAsync(string userId, string password)
        {
            UserCredential cred = await _users.GetCredentialAsync(userId);

            // 아이디가 없을 때와 비밀번호가 틀렸을 때 같은 메시지를 보여줍니다.
            // (메시지가 다르면 "이 아이디는 존재한다"는 정보를 공격자에게 주게 됨)
            const string wrongMessage = "아이디 또는 비밀번호가 올바르지 않습니다.";

            if (cred == null)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "존재하지 않는 아이디");
                return Fail(wrongMessage);
            }

            if (!cred.IsActive)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "사용 중지 계정");
                return Fail("사용이 중지된 계정입니다. 관리자에게 문의하세요.");
            }

            if (cred.FailCount >= MaxFailCount)
            {
                await _users.InsertLoginHistoryAsync(userId, false, "계정 잠김");
                return Fail(string.Format("비밀번호를 {0}회 이상 틀려 계정이 잠겼습니다. 관리자에게 문의하세요.", MaxFailCount));
            }

            if (!PasswordHasher.Verify(cred.PasswordSalt, password, cred.PasswordHash))
            {
                await _users.IncreaseFailCountAsync(userId);
                await _users.InsertLoginHistoryAsync(userId, false, "비밀번호 불일치");
                // 남은 시도 횟수 안내는 "아이디가 존재한다"는 힌트가 될 수 있지만,
                // 사내 PC 프로그램이라 잠김 예고로 인한 문의/혼란을 줄이는 편의성을 택함 (보안 ↔ 편의 트레이드오프)
                int remain = MaxFailCount - (cred.FailCount + 1);
                return Fail(remain > 0
                    ? string.Format("{0} (남은 시도 {1}회)", wrongMessage, remain)
                    : "계정이 잠겼습니다. 관리자에게 문의하세요.");
            }

            await _users.MarkLoginSuccessAsync(userId);
            await _users.InsertLoginHistoryAsync(userId, true, null);

            return new LoginResult
            {
                Success = true,
                User = new UserInfo
                {
                    UserId = cred.UserId,
                    UserName = cred.UserName,
                    RoleCode = cred.RoleCode
                }
            };
        }

        private static LoginResult Fail(string message)
        {
            return new LoginResult { Success = false, Message = message };
        }
    }
}
