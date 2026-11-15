using System;
using System.Data;
using System.Threading.Tasks;

namespace TempMonitor.Data
{
    internal class AlertRepository
    {
        /// <summary>경고를 저장하고 새로 만들어진 ALERT_ID 를 돌려줍니다.</summary>
        public async Task<long> InsertAsync(int sensorId, DateTime alertTime, decimal temp, decimal threshold, string kakaoStatus)
        {
            // OUTPUT INSERTED.xxx : INSERT 와 동시에 생성된 IDENTITY 값을 받아옵니다.
            // (SELECT @@IDENTITY 는 트리거가 있으면 엉뚱한 값을 줄 수 있어 쓰지 않습니다)
            const string sql = @"
INSERT INTO dbo.TB_ALERT_LOG (SENSOR_ID, ALERT_DT, TEMP_VALUE, THRESHOLD_VALUE, KAKAO_STATUS_CD)
OUTPUT INSERTED.ALERT_ID
VALUES (@SENSOR_ID, @ALERT_DT, @TEMP_VALUE, @THRESHOLD_VALUE, @KAKAO_STATUS_CD);";

            object id = await DbHelper.ExecuteScalarAsync(sql,
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@ALERT_DT", alertTime),
                DbHelper.Temp("@TEMP_VALUE", temp),
                DbHelper.Temp("@THRESHOLD_VALUE", threshold),
                DbHelper.VarChar("@KAKAO_STATUS_CD", kakaoStatus, 10));

            return Convert.ToInt64(id);
        }

        public Task UpdateKakaoStatusAsync(long alertId, string kakaoStatus)
        {
            const string sql = "UPDATE dbo.TB_ALERT_LOG SET KAKAO_STATUS_CD = @KAKAO_STATUS_CD WHERE ALERT_ID = @ALERT_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.VarChar("@KAKAO_STATUS_CD", kakaoStatus, 10),
                DbHelper.BigInt("@ALERT_ID", alertId));
        }

        public Task<DataTable> SearchAsync(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT A.ALERT_DT,
       A.TEMP_VALUE,
       A.THRESHOLD_VALUE,
       ISNULL(C.CODE_NM, A.KAKAO_STATUS_CD) AS KAKAO_STATUS_NM
  FROM dbo.TB_ALERT_LOG A
  LEFT JOIN dbo.TB_COMMON_CODE C
         ON C.GROUP_CD = 'KAKAO_STATUS'
        AND C.CODE     = A.KAKAO_STATUS_CD
 WHERE A.ALERT_DT >= @FROM_DT
   AND A.ALERT_DT <  @TO_DT
 ORDER BY A.ALERT_DT DESC;";

            return DbHelper.QueryAsync(sql,
                DbHelper.DateTime2("@FROM_DT", from),
                DbHelper.DateTime2("@TO_DT", to));
        }
    }
}
