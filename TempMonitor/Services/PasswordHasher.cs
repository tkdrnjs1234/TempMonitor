using System.Security.Cryptography;
using System.Text;

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
        public static byte[] Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.Unicode.GetBytes(salt + password));
            }
        }

        /// <summary>
        /// 길이가 같으면 끝까지 다 비교하는 "고정 시간 비교".
        /// 첫 바이트가 다를 때 바로 false 를 반환하면, 응답 시간 차이로 정답을 추측하는 공격이 가능해집니다.
        /// </summary>
        public static bool Verify(string salt, string password, byte[] expectedHash)
        {
            byte[] actual = Hash(salt, password);
            if (expectedHash == null || actual.Length != expectedHash.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < actual.Length; i++)
                diff |= actual[i] ^ expectedHash[i];
            return diff == 0;
        }
    }
}
