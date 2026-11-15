using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using TempMonitor.Models;

namespace TempMonitor.Data
{
    internal class ConfigRepository
    {
        public async Task<Dictionary<string, string>> LoadAllAsync()
        {
            DataTable dt = await DbHelper.QueryAsync("SELECT CONFIG_KEY, CONFIG_VALUE FROM dbo.TB_CONFIG;");
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
                dict[(string)r["CONFIG_KEY"]] = (string)r["CONFIG_VALUE"];
            return dict;
        }

        public async Task<string> GetValueAsync(string key)
        {
            object v = await DbHelper.ExecuteScalarAsync(
                "SELECT CONFIG_VALUE FROM dbo.TB_CONFIG WHERE CONFIG_KEY = @KEY;",
                DbHelper.VarChar("@KEY", key, 50));
            return v as string;
        }

        /// <summary>DB 의 문자열 설정을 AppConfig 객체로 변환. 값이 없거나 잘못됐으면 기본값 유지.</summary>
        public async Task<AppConfig> LoadAppConfigAsync()
        {
            Dictionary<string, string> d = await LoadAllAsync();
            var c = new AppConfig();
            string v;

            decimal dec;
            int i;
            if (d.TryGetValue(AppConfig.KeyAlertTemp, out v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out dec))
                c.AlertTemp = dec;
            if (d.TryGetValue(AppConfig.KeyAlertIntervalMin, out v) && int.TryParse(v, out i) && i > 0)
                c.AlertIntervalMin = i;
            if (d.TryGetValue(AppConfig.KeyAlertRiseStep, out v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out dec) && dec >= 0)
                c.AlertRiseStep = dec;
            if (d.TryGetValue(AppConfig.KeyKakaoEnabled, out v))
                c.KakaoEnabled = v == "Y";
            if (d.TryGetValue(AppConfig.KeyReadCycleSec, out v) && int.TryParse(v, out i) && i > 0)
                c.ReadCycleSec = i;
            if (d.TryGetValue(AppConfig.KeySaveCycleSec, out v) && int.TryParse(v, out i) && i > 0)
                c.SaveCycleSec = i;
            if (d.TryGetValue(AppConfig.KeyKakaoReceiverName, out v) && !string.IsNullOrWhiteSpace(v))
                c.KakaoReceiverName = v;

            return c;
        }

        /// <summary>
        /// 여러 설정을 "트랜잭션" 하나로 저장합니다.
        /// 중간에 하나라도 실패하면 전부 되돌려서(Rollback) 설정이 반쯤만 바뀐 상태가 되지 않게 합니다.
        /// </summary>
        public async Task SaveAsync(IDictionary<string, string> values, string userId)
        {
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

            using (var conn = new SqlConnection(DbHelper.ConnectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (KeyValuePair<string, string> kv in values)
                        {
                            using (SqlCommand cmd = DbHelper.CreateCommand(conn, sql,
                                       DbHelper.VarChar("@KEY", kv.Key, 50),
                                       DbHelper.NVarChar("@VALUE", kv.Value ?? string.Empty, 1000),
                                       DbHelper.VarChar("@USER_ID", userId, 30)))
                            {
                                cmd.Transaction = tx;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public Task SaveAppConfigAsync(AppConfig c, string userId)
        {
            var values = new Dictionary<string, string>
            {
                { AppConfig.KeyAlertTemp, c.AlertTemp.ToString("0.0", CultureInfo.InvariantCulture) },
                { AppConfig.KeyAlertIntervalMin, c.AlertIntervalMin.ToString(CultureInfo.InvariantCulture) },
                { AppConfig.KeyAlertRiseStep, c.AlertRiseStep.ToString("0.0", CultureInfo.InvariantCulture) },
                { AppConfig.KeyKakaoEnabled, c.KakaoEnabled ? "Y" : "N" },
                { AppConfig.KeyReadCycleSec, c.ReadCycleSec.ToString(CultureInfo.InvariantCulture) },
                { AppConfig.KeySaveCycleSec, c.SaveCycleSec.ToString(CultureInfo.InvariantCulture) }
            };
            return SaveAsync(values, userId);
        }
    }
}
