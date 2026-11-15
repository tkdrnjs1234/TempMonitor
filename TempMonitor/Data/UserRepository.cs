using System;
using System.Data;
using System.Threading.Tasks;

namespace TempMonitor.Data
{
    /// <summary>로그인에 필요한 사용자 정보 (해시/솔트 포함, 화면에는 넘기지 않음)</summary>
    internal class UserCredential
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RoleCode { get; set; }
        public byte[] PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsActive { get; set; }
        public int FailCount { get; set; }
    }

    internal class UserRepository
    {
        public async Task<UserCredential> GetCredentialAsync(string userId)
        {
            const string sql = @"
SELECT USER_ID, USER_NM, ROLE_CD, PASSWORD_HASH, PASSWORD_SALT, USE_YN, FAIL_CNT
  FROM dbo.TB_USER
 WHERE USER_ID = @USER_ID;";

            DataTable dt = await DbHelper.QueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new UserCredential
            {
                UserId = (string)r["USER_ID"],
                UserName = (string)r["USER_NM"],
                RoleCode = (string)r["ROLE_CD"],
                PasswordHash = (byte[])r["PASSWORD_HASH"],
                PasswordSalt = (string)r["PASSWORD_SALT"],
                IsActive = (string)r["USE_YN"] == "Y",
                FailCount = (int)r["FAIL_CNT"]
            };
        }

        public Task IncreaseFailCountAsync(string userId)
        {
            const string sql = "UPDATE dbo.TB_USER SET FAIL_CNT = FAIL_CNT + 1 WHERE USER_ID = @USER_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
        }

        public Task MarkLoginSuccessAsync(string userId)
        {
            const string sql = @"
UPDATE dbo.TB_USER
   SET FAIL_CNT = 0,
       LAST_LOGIN_DT = SYSDATETIME()
 WHERE USER_ID = @USER_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
        }

        public Task InsertLoginHistoryAsync(string userId, bool success, string failReason)
        {
            const string sql = @"
INSERT INTO dbo.TB_LOGIN_HIST (USER_ID, SUCCESS_YN, FAIL_REASON, CLIENT_PC)
VALUES (@USER_ID, @SUCCESS_YN, @FAIL_REASON, @CLIENT_PC);";

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.VarChar("@USER_ID", userId, 30),
                DbHelper.Char1("@SUCCESS_YN", success ? "Y" : "N"),
                DbHelper.NVarChar("@FAIL_REASON", failReason, 100),
                DbHelper.NVarChar("@CLIENT_PC", Environment.MachineName, 100));
        }
    }
}
