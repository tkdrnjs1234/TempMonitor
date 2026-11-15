using System;
using System.Data;
using System.Threading.Tasks;
using TempMonitor.Models;

namespace TempMonitor.Data
{
    internal class TempLogRepository
    {
        public Task InsertAsync(int sensorId, TempReading reading, TempStatus status)
        {
            const string sql = @"
INSERT INTO dbo.TB_TEMP_LOG (SENSOR_ID, MEASURE_DT, TEMP_VALUE, STATUS_CD, ERROR_CD)
VALUES (@SENSOR_ID, @MEASURE_DT, @TEMP_VALUE, @STATUS_CD, @ERROR_CD);";

            // 센서 오류면 온도 값을 믿을 수 없으므로 NULL 로 저장 (0℃ 와 구분)
            decimal? temp = reading.HasError ? (decimal?)null : reading.Temperature;

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@MEASURE_DT", reading.ReadTime),
                DbHelper.Temp("@TEMP_VALUE", temp),
                DbHelper.VarChar("@STATUS_CD", status.ToCode(), 10),
                DbHelper.TinyInt("@ERROR_CD", Math.Max(0, Math.Min(255, reading.ErrorCode))));
        }

        /// <summary>
        /// 기간 조회. 시작은 포함(>=), 끝은 미포함(&lt;)인 "반열린 구간"으로 조회합니다.
        /// BETWEEN 을 쓰면 경계 시각(예: 15:00:00)의 데이터가 이전/다음 조회에 중복으로 잡힙니다.
        /// </summary>
        public Task<DataTable> SearchAsync(int sensorId, DateTime from, DateTime to, int maxRows)
        {
            const string sql = @"
SELECT TOP (@MAX_ROWS)
       L.MEASURE_DT,
       L.TEMP_VALUE,
       L.STATUS_CD,
       -- 센서 오류면 오류 종류까지 붙여서 표시: '센서 오류 (온도센서 연결 끊김)'
       CASE WHEN L.STATUS_CD = 'ERROR' AND E.CODE_NM IS NOT NULL
            THEN ISNULL(C.CODE_NM, L.STATUS_CD) + N' (' + E.CODE_NM + N')'
            ELSE ISNULL(C.CODE_NM, L.STATUS_CD)
       END AS STATUS_NM
  FROM dbo.TB_TEMP_LOG L
  LEFT JOIN dbo.TB_COMMON_CODE C                  -- 상태 이름 (정상/경고/센서 오류)
         ON C.GROUP_CD = 'TEMP_STATUS'
        AND C.CODE     = L.STATUS_CD
  LEFT JOIN dbo.TB_COMMON_CODE E                  -- 오류 이름 (공통코드 SENSOR_ERROR)
         ON E.GROUP_CD = 'SENSOR_ERROR'
        AND E.CODE     = CAST(L.ERROR_CD AS VARCHAR(10))
 WHERE L.SENSOR_ID  = @SENSOR_ID
   AND L.MEASURE_DT >= @FROM_DT
   AND L.MEASURE_DT <  @TO_DT
 ORDER BY L.MEASURE_DT DESC;";

            return DbHelper.QueryAsync(sql,
                DbHelper.Int("@MAX_ROWS", maxRows),
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@FROM_DT", from),
                DbHelper.DateTime2("@TO_DT", to));
        }
    }
}
