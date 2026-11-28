// ============================================================================================
//  Services/PasswordHasher.cs : 비밀번호 해시 계산 / 비교
// --------------------------------------------------------------------------------------------
//  해시(Hash) = 비밀번호를 되돌릴 수 없는 고정 길이 숫자 덩어리로 바꾸는 것.
//  DB 에는 비밀번호 원문이 아니라 "해시" 만 저장되어 있고, 로그인할 때 입력한 비밀번호를 같은 방법으로
//  해시해서 DB 의 해시와 같은지 비교합니다.
//
//  솔트(Salt) = 사용자마다 다른 랜덤 문자열. 비번 앞에 붙여서 해시하면, 같은 비번이라도 사람마다 해시가 달라짐.
//
//  누가 부르나? → Services/AuthService.cs  LoginAsync()  에서  PasswordHasher.Verify(...)
//  static class 라서 new 없이 PasswordHasher.Verify(...) 로 바로 사용 (기억할 상태가 없는 계산 도구)
// ============================================================================================

using System.Security.Cryptography;   // SHA256 (해시 알고리즘)
using System.Text;                    // Encoding (문자열 → 바이트 변환)

namespace TempMonitor.Services
{
    /// <summary>
    /// 비밀번호 해시.
    /// SQL 의 HASHBYTES('SHA2_256', N'솔트' + N'비번') 과 같은 결과가 나오도록
    /// UTF-16(Encoding.Unicode = NVARCHAR 의 인코딩)으로 바이트를 만듭니다.
    ///
    /// ※ 학습용으로 이해하기 쉬운 "솔트 + SHA-256" 을 썼습니다.
    ///   실무에서는 일부러 느리게 만든 PBKDF2(Rfc2898DeriveBytes) / bcrypt 를 권장합니다.
    /// </summary>
    public static class PasswordHasher
    {
        // ---- 해시 계산 ----------------------------------------------------------------------------
        // 호출: 아래 Verify 에서
        // 반환: 32바이트 배열 (SHA-256 결과는 항상 32바이트)
        public static byte[] Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())   // SHA-256 계산기 만들기 (using → 다 쓰면 자동 정리)
            {
                // salt + password             : 두 문자열 이어 붙이기
                // Encoding.Unicode.GetBytes() : 문자열 → UTF-16 바이트 배열
                //   (DB 초기 계정이 NVARCHAR(=UTF-16) 로 해시를 만들었기 때문에 같은 인코딩이어야 같은 결과가 나옴)
                // sha.ComputeHash(바이트)      : 해시 계산 → 32바이트
                return sha.ComputeHash(Encoding.Unicode.GetBytes(salt + password));
            }
        }

        // ---- 비밀번호 확인 -------------------------------------------------------------------------
        // 호출: AuthService.LoginAsync (④ 비밀번호 비교 단계)
        // salt         : DB 의 PASSWORD_SALT
        // password     : 사용자가 입력한 비밀번호
        // expectedHash : DB 의 PASSWORD_HASH
        // 반환         : 맞으면 true
        /// <summary>
        /// 길이가 같으면 끝까지 다 비교하는 "고정 시간 비교".
        /// 첫 바이트가 다를 때 바로 false 를 반환하면, 응답 시간 차이로 정답을 추측하는 공격이 가능해집니다.
        /// </summary>
        public static bool Verify(string salt, string password, byte[] expectedHash)
        {
            byte[] actual = Hash(salt, password);   // 입력한 비번으로 해시 계산 (위의 Hash 호출)
            if (expectedHash == null || actual.Length != expectedHash.Length)   // DB 값이 없거나 길이가 다르면
                return false;                                                   // 무조건 불일치

            int diff = 0;   // 다른 비트를 모아 둘 변수. 끝까지 0 이면 전부 같다는 뜻
            for (int i = 0; i < actual.Length; i++)       // 0번 바이트부터 마지막 바이트까지
                // ^  (XOR) : 두 바이트의 같은 자리 비트가 같으면 0, 다르면 1
                // |= (OR 대입) : 한 번이라도 1 이 생기면 diff 에 계속 남음
                diff |= actual[i] ^ expectedHash[i];
            return diff == 0;   // 하나도 안 달랐으면 true
        }
    }
}
