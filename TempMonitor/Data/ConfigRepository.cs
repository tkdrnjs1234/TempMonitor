// ============================================================================================
//  Data/ConfigRepository.cs : 설정 테이블(TB_CONFIG) 담당 SQL
// --------------------------------------------------------------------------------------------
//  TB_CONFIG 는 (CONFIG_KEY, CONFIG_VALUE) "이름표-값" 표입니다. 값은 전부 문자열.
//
//  메서드와 호출하는 곳
//    LoadAllAsync        : 표 전체 → Dictionary(사전)       ← LoadAppConfigAsync, KakaoService.SendToMeAsync
//    GetValueAsync       : 키 하나의 값만                   ← KakaoService.IsConfiguredAsync
//    LoadAppConfigAsync  : 사전 → AppConfig 객체(형 변환)   ← MainForm.MainForm_Load
//    SaveAsync           : 여러 키를 트랜잭션으로 저장       ← SaveAppConfigAsync, KakaoService.RefreshTokenAsync
//    SaveAppConfigAsync  : AppConfig 객체 → 사전 → SaveAsync ← MainForm.btnSaveSetting_Click
// ============================================================================================

using System;                      // StringComparer
using System.Collections.Generic;  // Dictionary, KeyValuePair, IDictionary
using System.Data;                 // DataTable, DataRow
using System.Data.SqlClient;       // SqlConnection, SqlTransaction, SqlCommand (트랜잭션을 직접 다루므로)
using System.Globalization;        // CultureInfo, NumberStyles (숫자 ↔ 문자열 변환 규칙)
using System.Threading.Tasks;      // Task
using TempMonitor.Models;          // AppConfig (Models/AppConfig.cs)

namespace TempMonitor.Data
{
    internal class ConfigRepository
    {
        // ---- 설정 전체 읽기 → 사전 ------------------------------------------------------------------
        // Dictionary<string, string> : "키(string) 로 값(string) 을 찾는 사전". dict["ALERT_TEMP"] → "30.0"
        public async Task<Dictionary<string, string>> LoadAllAsync()
        {
            // DbHelper.QueryAsync : 파라미터 없는 SELECT (params 라서 0개도 OK)
            DataTable dt = await DbHelper.QueryAsync("SELECT CONFIG_KEY, CONFIG_VALUE FROM dbo.TB_CONFIG;");

            // StringComparer.OrdinalIgnoreCase : 키의 대소문자를 구분하지 않음 ("alert_temp" 로 찾아도 됨)
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)                                  // 표의 행을 하나씩 꺼내 반복
                dict[(string)r["CONFIG_KEY"]] = (string)r["CONFIG_VALUE"]; // dict[키] = 값 → 사전에 넣기 (같은 키면 덮어씀)
            return dict;
        }

        // ---- 설정값 하나 읽기 --------------------------------------------------------------------
        // 호출: KakaoService.IsConfiguredAsync (액세스 토큰이 있는지 확인)
        public async Task<string> GetValueAsync(string key)
        {
            // DbHelper.ExecuteScalarAsync : 결과 값 1개 (없으면 null)
            object v = await DbHelper.ExecuteScalarAsync(
                "SELECT CONFIG_VALUE FROM dbo.TB_CONFIG WHERE CONFIG_KEY = @KEY;",
                DbHelper.VarChar("@KEY", key, 50));
            return v as string;   // as : string 으로 바꿀 수 있으면 바꾸고, 아니면(null 포함) null. 예외가 안 남
        }

        // ---- 사전 → AppConfig 객체 --------------------------------------------------------------
        // 호출: MainForm.MainForm_Load → 결과를 MainForm._config 에 보관
        /// <summary>DB 의 문자열 설정을 AppConfig 객체로 변환. 값이 없거나 잘못됐으면 기본값 유지.</summary>
        public async Task<AppConfig> LoadAppConfigAsync()
        {
            Dictionary<string, string> d = await LoadAllAsync();   // 같은 클래스의 위 메서드 호출 → 사전
            var c = new AppConfig();                                // 기본값(30.0, 10, 5.0 ...)으로 채워진 그릇
            string v;                                               // TryGetValue 결과(문자열 값) 받을 변수

            decimal dec;    // decimal.TryParse 결과 받을 변수
            int i;          // int.TryParse 결과 받을 변수

            // 패턴:  if (① 키가 있고 && ② 숫자로 바뀌면) → 그 값으로 덮어쓰기.  하나라도 실패하면 기본값 유지
            //   d.TryGetValue(키, out v) : 키가 있으면 true + v 에 값 채움, 없으면 false (예외 안 남)
            //   decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out dec)
            //     : 문자열 → decimal. InvariantCulture = PC 지역 설정과 상관없이 소수점을 항상 "." 으로 해석
            //   && : 왼쪽이 false 면 오른쪽은 실행 안 함 → 키가 없으면 TryParse 도 안 함
            if (d.TryGetValue(AppConfig.KeyAlertTemp, out v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out dec))
                c.AlertTemp = dec;
            if (d.TryGetValue(AppConfig.KeyAlertIntervalMin, out v) && int.TryParse(v, out i) && i > 0)          // 0 이하 간격은 무시
                c.AlertIntervalMin = i;
            if (d.TryGetValue(AppConfig.KeyAlertRiseStep, out v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out dec) && dec >= 0)
                c.AlertRiseStep = dec;
            if (d.TryGetValue(AppConfig.KeyKakaoEnabled, out v))
                c.KakaoEnabled = v == "Y";                                                                       // "Y" 면 true
            if (d.TryGetValue(AppConfig.KeyReadCycleSec, out v) && int.TryParse(v, out i) && i > 0)
                c.ReadCycleSec = i;
            if (d.TryGetValue(AppConfig.KeySaveCycleSec, out v) && int.TryParse(v, out i) && i > 0)
                c.SaveCycleSec = i;
            if (d.TryGetValue(AppConfig.KeyKakaoReceiverName, out v) && !string.IsNullOrWhiteSpace(v))         // 빈 문자열이면 무시
                c.KakaoReceiverName = v;

            return c;   // 완성된 설정 그릇
        }

        // ---- 여러 설정을 한 번에 저장 (트랜잭션) ------------------------------------------------------
        // 호출: SaveAppConfigAsync (설정 탭 6개 값), KakaoService.RefreshTokenAsync (새 토큰 1~2개)
        // IDictionary<string, string> : Dictionary 같은 "사전" 이면 뭐든 받을 수 있는 인터페이스 타입
        // userId : 누가 바꿨는지 UPD_USER_ID 에 기록
        /// <summary>
        /// 여러 설정을 "트랜잭션" 하나로 저장합니다.
        /// 중간에 하나라도 실패하면 전부 되돌려서(Rollback) 설정이 반쯤만 바뀐 상태가 되지 않게 합니다.
        /// </summary>
        public async Task SaveAsync(IDictionary<string, string> values, string userId)
        {
            // SQL 뜻 (UPSERT = UPDATE + INSERT):
            //   ① 그 키가 있으면 값/수정자/수정시각을 UPDATE
            //   ② @@ROWCOUNT(방금 UPDATE 된 행 수) 가 0 이면 = 그 키가 없었다는 뜻 → INSERT 로 새로 추가
            // 있으면 UPDATE, 없으면 INSERT (UPSERT)
            const string sql = @"
UPDATE dbo.TB_CONFIG
   SET CONFIG_VALUE = @VALUE,
       UPD_USER_ID  = @USER_ID,
       UPD_DT       = SYSDATETIME()
 WHERE CONFIG_KEY = @KEY;

IF @@ROWCOUNT = 0
    INSERT INTO dbo.TB_CONFIG (CONFIG_KEY, CONFIG_VALUE, UPD_USER_ID)
    VALUES (@KEY, @VALUE, @USER_ID);";

            // DbHelper.ExecuteNonQueryAsync 를 안 쓰고 연결을 직접 여는 이유:
            //   그 메서드는 호출할 때마다 "새 연결" 을 열고 닫음. 트랜잭션은 "한 연결 안에서" 여러 명령을 묶어야 하므로
            //   연결 하나를 직접 열고, 명령만 DbHelper.CreateCommand 로 만들어 씀.
            using (var conn = new SqlConnection(DbHelper.ConnectionString))   // DbHelper 의 연결 문자열 사용
            {
                await conn.OpenAsync();                                        // 연결 열기
                using (SqlTransaction tx = conn.BeginTransaction())           // 트랜잭션 시작 (여기부터 Commit 전까지 한 묶음)
                {
                    try
                    {
                        // KeyValuePair<string, string> kv : 사전의 한 칸 (kv.Key = 키, kv.Value = 값)
                        foreach (KeyValuePair<string, string> kv in values)    // 저장할 설정 개수만큼 반복
                        {
                            // DbHelper.CreateCommand : 같은 연결(conn)로 SQL + 파라미터 3개를 묶은 명령 만들기
                            using (SqlCommand cmd = DbHelper.CreateCommand(conn, sql,
                                       DbHelper.VarChar("@KEY", kv.Key, 50),
                                       DbHelper.NVarChar("@VALUE", kv.Value ?? string.Empty, 1000),   // 값이 null 이면 빈 문자열로 (CONFIG_VALUE 가 NOT NULL)
                                       DbHelper.VarChar("@USER_ID", userId, 30)))
                            {
                                cmd.Transaction = tx;              // 이 명령을 트랜잭션에 포함 (빠뜨리면 실행 시 예외)
                                await cmd.ExecuteNonQueryAsync();  // 실행 (아직 확정 아님)
                            }
                        }
                        tx.Commit();     // 전부 성공 → 확정 (이제 DB 에 실제로 반영)
                    }
                    catch                // 하나라도 실패하면
                    {
                        tx.Rollback();   // 지금까지 실행한 것 전부 취소 → 설정이 반쯤만 바뀌는 일 없음
                        throw;           // 잡은 예외를 그대로 다시 던짐 → 호출한 쪽(btnSaveSetting_Click)의 catch 가 "저장 실패" 표시
                    }
                }
            }
        }

        // ---- AppConfig 객체 → 사전 → 저장 ----------------------------------------------------------
        // 호출: MainForm.btnSaveSetting_Click
        // LoadAppConfigAsync 의 정반대 방향 (객체 → 문자열)
        public Task SaveAppConfigAsync(AppConfig c, string userId)
        {
            // 컬렉션 초기화자 { { 키, 값 }, ... } : new 하면서 사전에 여러 칸을 한 번에 넣음
            var values = new Dictionary<string, string>
            {
                { AppConfig.KeyAlertTemp, c.AlertTemp.ToString("0.0", CultureInfo.InvariantCulture) },        // 30 → "30.0"
                { AppConfig.KeyAlertIntervalMin, c.AlertIntervalMin.ToString(CultureInfo.InvariantCulture) },  // 10 → "10"
                { AppConfig.KeyAlertRiseStep, c.AlertRiseStep.ToString("0.0", CultureInfo.InvariantCulture) },
                { AppConfig.KeyKakaoEnabled, c.KakaoEnabled ? "Y" : "N" },                                     // bool → "Y"/"N"
                { AppConfig.KeyReadCycleSec, c.ReadCycleSec.ToString(CultureInfo.InvariantCulture) },
                { AppConfig.KeySaveCycleSec, c.SaveCycleSec.ToString(CultureInfo.InvariantCulture) }
            };
            return SaveAsync(values, userId);   // 위의 트랜잭션 저장 메서드 재사용. Task 를 그대로 넘김
        }
    }
}
