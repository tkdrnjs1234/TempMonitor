// ============================================================================================
//  Data/UserRepository.cs : 사용자 테이블(TB_USER) 과 로그인 이력(TB_LOGIN_HIST) 담당 SQL
// --------------------------------------------------------------------------------------------
//  Repository(저장소) = "테이블 하나를 맡아서 SQL 을 실행하는 클래스".
//  SQL 을 만들고 → DbHelper 에 실행을 맡기고 → 결과(DataTable)를 C# 객체로 바꿔서 돌려줍니다.
//
//  누가 부르나? → Services/AuthService.cs 의 LoginAsync() 만 부릅니다.
//    GetCredentialAsync      : 아이디로 사용자 정보(해시/솔트/실패횟수...) 읽기
//    IncreaseFailCountAsync  : 비밀번호 틀림 → 실패 횟수 +1
//    MarkLoginSuccessAsync   : 로그인 성공 → 실패 횟수 0, 마지막 로그인 시각 기록
//    InsertLoginHistoryAsync : 성공/실패 이력 한 줄 추가
// ============================================================================================

using System;                  // Environment (PC 이름)
using System.Data;             // DataTable, DataRow
using System.Threading.Tasks;  // Task

namespace TempMonitor.Data
{
    // ---- 로그인 검사용 그릇 ------------------------------------------------------------------
    // GetCredentialAsync 가 만들어서 → AuthService.LoginAsync 가 받아 검사에만 사용.
    // 비밀번호 해시가 들어 있으므로 internal(이 프로젝트 안에서만) 로 범위를 좁히고, 화면에는 넘기지 않음.
    // (화면에는 해시를 뺀 Models/UserInfo 로 옮겨 담아 넘김)
    /// <summary>로그인에 필요한 사용자 정보 (해시/솔트 포함, 화면에는 넘기지 않음)</summary>
    internal class UserCredential
    {
        public string UserId { get; set; }          // USER_ID
        public string UserName { get; set; }        // USER_NM
        public string RoleCode { get; set; }        // ROLE_CD ("ADMIN"/"WORKER")
        public byte[] PasswordHash { get; set; }    // PASSWORD_HASH (VARBINARY → byte 배열, SHA-256 이면 32바이트)
        public string PasswordSalt { get; set; }    // PASSWORD_SALT (해시 계산 때 비번 앞에 붙이는 랜덤 문자열)
        public bool IsActive { get; set; }          // USE_YN 이 'Y' 면 true (사용 중지 계정 확인용)
        public int FailCount { get; set; }          // FAIL_CNT (비번 연속 실패 횟수, 잠김 판단용)
    }

    internal class UserRepository
    {
        // ---- 아이디로 사용자 정보 읽기 -------------------------------------------------------------
        // 호출: AuthService.LoginAsync 첫 줄
        // 반환: UserCredential 객체, 그런 아이디가 없으면 null
        // async + await 를 쓰는 이유: DB 결과(DataTable)를 받아서 UserCredential 로 "가공" 해야 하므로
        public async Task<UserCredential> GetCredentialAsync(string userId)
        {
            // const string : 바뀌지 않는 SQL 문자열
            // @"..."       : 여러 줄 문자열 (줄바꿈을 그대로 포함, \ 를 특수문자로 안 봄)
            // SQL 뜻: TB_USER 에서 USER_ID 가 @USER_ID(파라미터) 인 행의 7개 컬럼을 가져와라
            const string sql = @"
SELECT USER_ID, USER_NM, ROLE_CD, PASSWORD_HASH, PASSWORD_SALT, USE_YN, FAIL_CNT
  FROM dbo.TB_USER
 WHERE USER_ID = @USER_ID;";

            // DbHelper.QueryAsync (Data/DbHelper.cs) : SQL 실행 → 결과 표
            // DbHelper.VarChar("@USER_ID", userId, 30) : SQL 의 @USER_ID 자리에 userId 값을 VARCHAR(30) 타입으로 넣음
            DataTable dt = await DbHelper.QueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
            if (dt.Rows.Count == 0)   // 결과 행이 0개 = 그런 아이디 없음
                return null;          // AuthService 가 null 을 보고 "존재하지 않는 아이디" 처리

            DataRow r = dt.Rows[0];   // 첫 번째 행 (USER_ID 는 PK 라 최대 1행)

            // DataTable 의 행(r["컬럼명"]) → C# 객체 속성으로 "번역". 이게 Repository 의 핵심 일.
            // r["..."] 는 object 타입이라 (string), (int), (byte[]) 처럼 실제 타입으로 형변환(캐스팅) 필요.
            // 이 컬럼들은 NOT NULL 이라 DBNull 걱정 없이 바로 캐스팅.
            return new UserCredential   // 객체 초기화자: new 하면서 속성들을 한 번에 채움
            {
                UserId = (string)r["USER_ID"],
                UserName = (string)r["USER_NM"],
                RoleCode = (string)r["ROLE_CD"],
                PasswordHash = (byte[])r["PASSWORD_HASH"],
                PasswordSalt = (string)r["PASSWORD_SALT"],
                IsActive = (string)r["USE_YN"] == "Y",   // 문자열 비교 결과(true/false)를 bool 속성에 넣음
                FailCount = (int)r["FAIL_CNT"]
            };
        }

        // ---- 비밀번호 실패 횟수 +1 -------------------------------------------------------------------
        // 호출: AuthService.LoginAsync (비밀번호 불일치일 때)
        // async 가 없는 이유: 결과를 가공할 게 없어서 DbHelper 가 돌려준 Task 를 그대로 호출한 쪽에 넘김.
        //                    AuthService 가 await 하면 똑같이 기다려짐.
        public Task IncreaseFailCountAsync(string userId)
        {
            // FAIL_CNT = FAIL_CNT + 1 을 SQL 에서 계산하는 이유:
            //  C# 에서 읽고 +1 해서 다시 쓰면, 그 사이 다른 PC 가 바꾼 값을 덮어쓸 수 있음. DB 가 직접 더하면 안전.
            const string sql = "UPDATE dbo.TB_USER SET FAIL_CNT = FAIL_CNT + 1 WHERE USER_ID = @USER_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
        }

        // ---- 로그인 성공 기록 -------------------------------------------------------------------------
        // 호출: AuthService.LoginAsync (모든 검사 통과 시)
        // 실패 횟수를 0 으로 되돌리고, 마지막 로그인 시각을 DB 현재 시각(SYSDATETIME())으로
        public Task MarkLoginSuccessAsync(string userId)
        {
            const string sql = @"
UPDATE dbo.TB_USER
   SET FAIL_CNT = 0,
       LAST_LOGIN_DT = SYSDATETIME()
 WHERE USER_ID = @USER_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql, DbHelper.VarChar("@USER_ID", userId, 30));
        }

        // ---- 로그인 이력 한 줄 추가 -------------------------------------------------------------------
        // 호출: AuthService.LoginAsync (성공이든 실패든 매번)
        // success    : true → 'Y', false → 'N'
        // failReason : 실패 이유 ("비밀번호 불일치" 등), 성공이면 null → DB NULL
        public Task InsertLoginHistoryAsync(string userId, bool success, string failReason)
        {
            // SQL 뜻: TB_LOGIN_HIST 에 (아이디, 성공여부, 실패사유, PC이름) 한 행 추가 (일시는 DB 기본값으로 자동)
            const string sql = @"
INSERT INTO dbo.TB_LOGIN_HIST (USER_ID, SUCCESS_YN, FAIL_REASON, CLIENT_PC)
VALUES (@USER_ID, @SUCCESS_YN, @FAIL_REASON, @CLIENT_PC);";

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.VarChar("@USER_ID", userId, 30),
                DbHelper.Char1("@SUCCESS_YN", success ? "Y" : "N"),          // bool → "Y"/"N"
                DbHelper.NVarChar("@FAIL_REASON", failReason, 100),          // null 이면 DbHelper 가 DBNull 로 바꿔 줌
                DbHelper.NVarChar("@CLIENT_PC", Environment.MachineName, 100)); // Environment.MachineName : 이 PC 의 컴퓨터 이름
        }
    }
}
