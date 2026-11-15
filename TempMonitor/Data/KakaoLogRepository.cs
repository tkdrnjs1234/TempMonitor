using System;
using System.Data;
using System.Threading.Tasks;

namespace TempMonitor.Data
{
    internal class KakaoLogRepository
    {
        public Task InsertAsync(long? alertId, DateTime sendTime, string receiver, string message, bool success, string resultMessage)
        {
            const string sql = @"
INSERT INTO dbo.TB_KAKAO_LOG (ALERT_ID, SEND_DT, RECEIVER_NM, MESSAGE, RESULT_CD, RESULT_MSG)
VALUES (@ALERT_ID, @SEND_DT, @RECEIVER_NM, @MESSAGE, @RESULT_CD, @RESULT_MSG);";

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.BigInt("@ALERT_ID", alertId),
                DbHelper.DateTime2("@SEND_DT", sendTime),
                DbHelper.NVarChar("@RECEIVER_NM", Truncate(receiver, 50), 50),
                DbHelper.NVarChar("@MESSAGE", Truncate(message, 500), 500),
                DbHelper.VarChar("@RESULT_CD", success ? "SUCCESS" : "FAIL", 10),
                DbHelper.NVarChar("@RESULT_MSG", Truncate(resultMessage, 500), 500));
        }

        public Task<DataTable> SearchAsync(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT K.SEND_DT,
       K.RECEIVER_NM,
       K.MESSAGE,
       ISNULL(C.CODE_NM, K.RESULT_CD)
         + CASE WHEN K.RESULT_CD = 'FAIL' AND K.RESULT_MSG IS NOT NULL
                THEN N' (' + K.RESULT_MSG + N')' ELSE N'' END AS RESULT_NM
  FROM dbo.TB_KAKAO_LOG K
  LEFT JOIN dbo.TB_COMMON_CODE C
         ON C.GROUP_CD = 'SEND_RESULT'
        AND C.CODE     = K.RESULT_CD
 WHERE K.SEND_DT >= @FROM_DT
   AND K.SEND_DT <  @TO_DT
 ORDER BY K.SEND_DT DESC;";

            return DbHelper.QueryAsync(sql,
                DbHelper.DateTime2("@FROM_DT", from),
                DbHelper.DateTime2("@TO_DT", to));
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
