// ============================================================================================
//  Data/AlertRepository.cs : 경고 이력 테이블(TB_ALERT_LOG) 담당 SQL
// --------------------------------------------------------------------------------------------
//  메서드와 호출하는 곳
//    InsertAsync           : 경고 1건 저장 + 새 ALERT_ID 돌려받기   ← MainForm.CheckAlertAsync
//    UpdateKakaoStatusAsync: 카톡 발송 결과(SENT/FAILED) 반영        ← MainForm.SendAlertKakaoAsync
//    SearchAsync           : 기간으로 경고 조회                     ← MainForm.btnAlertSearch_Click (경고 조회 탭)
//
//  KAKAO_STATUS_CD 값 : DISABLED(카톡 꺼짐) / SKIPPED(간격 안이라 생략) / PENDING(보내는 중) / SENT / FAILED
// ============================================================================================

using System;                  // DateTime, Convert
using System.Data;             // DataTable
using System.Threading.Tasks;  // Task

namespace TempMonitor.Data
{
    internal class AlertRepository
    {
        // ---- 경고 저장 --------------------------------------------------------------------------
        // 반환: 방금 만들어진 ALERT_ID (long)
        //   MainForm 은 이 ID 로 ① 나중에 카톡 결과를 이 행에 업데이트하고 ② 카톡 이력(TB_KAKAO_LOG.ALERT_ID)을 이 경고에 연결함
        /// <summary>경고를 저장하고 새로 만들어진 ALERT_ID 를 돌려줍니다.</summary>
        public async Task<long> InsertAsync(int sensorId, DateTime alertTime, decimal temp, decimal threshold, string kakaoStatus)
        {
            // OUTPUT INSERTED.xxx : INSERT 와 동시에 생성된 IDENTITY 값을 받아옵니다.
            // (SELECT @@IDENTITY 는 트리거가 있으면 엉뚱한 값을 줄 수 있어 쓰지 않습니다)
            const string sql = @"
INSERT INTO dbo.TB_ALERT_LOG (SENSOR_ID, ALERT_DT, TEMP_VALUE, THRESHOLD_VALUE, KAKAO_STATUS_CD)
OUTPUT INSERTED.ALERT_ID
VALUES (@SENSOR_ID, @ALERT_DT, @TEMP_VALUE, @THRESHOLD_VALUE, @KAKAO_STATUS_CD);";

            // 결과가 "값 1개(새 ID)" 라서 ExecuteScalarAsync 사용
            object id = await DbHelper.ExecuteScalarAsync(sql,
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@ALERT_DT", alertTime),
                DbHelper.Temp("@TEMP_VALUE", temp),               // 그때 온도
                DbHelper.Temp("@THRESHOLD_VALUE", threshold),     // 그때 경고 기준 (나중에 기준이 바뀌어도 기록이 남게)
                DbHelper.VarChar("@KAKAO_STATUS_CD", kakaoStatus, 10));

            // ExecuteScalar 는 타입을 몰라서 object 로 줌 → Convert.ToInt64 로 long 변환
            // (결과를 가공해야 하므로 이 메서드는 async + await)
            return Convert.ToInt64(id);
        }

        // ---- 카톡 결과 반영 --------------------------------------------------------------------
        // 호출: MainForm.SendAlertKakaoAsync (카톡을 보낸 뒤 PENDING → SENT 또는 FAILED)
        public Task UpdateKakaoStatusAsync(long alertId, string kakaoStatus)
        {
            const string sql = "UPDATE dbo.TB_ALERT_LOG SET KAKAO_STATUS_CD = @KAKAO_STATUS_CD WHERE ALERT_ID = @ALERT_ID;";
            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.VarChar("@KAKAO_STATUS_CD", kakaoStatus, 10),
                DbHelper.BigInt("@ALERT_ID", alertId));   // long 을 long? 매개변수에 넘기면 자동 변환됨
        }

        // ---- 경고 조회 --------------------------------------------------------------------------
        // 호출: MainForm.btnAlertSearch_Click → dgvAlert.DataSource 에 연결
        public Task<DataTable> SearchAsync(DateTime from, DateTime to)
        {
            // SQL 뜻: 기간 안의 경고들 + 카톡 상태 코드를 공통코드(KAKAO_STATUS) 한글 이름으로 (없으면 코드 그대로), 최신순
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
