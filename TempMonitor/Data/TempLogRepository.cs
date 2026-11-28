// ============================================================================================
//  Data/TempLogRepository.cs : 온도 기록 테이블(TB_TEMP_LOG) 담당 SQL
// --------------------------------------------------------------------------------------------
//  메서드와 호출하는 곳
//    InsertAsync : 측정값 1건 저장        ← MainForm.SaveIfNeededAsync (저장 주기마다 / 상태가 바뀔 때)
//    SearchAsync : 기간으로 기록 조회     ← MainForm.SearchHistoryAsync (기록 조회 탭)
// ============================================================================================

using System;                  // DateTime, Math
using System.Data;             // DataTable
using System.Threading.Tasks;  // Task
using TempMonitor.Models;      // TempReading, TempStatus, ToCode() 확장 메서드 (Models/TempReading.cs)

namespace TempMonitor.Data
{
    internal class TempLogRepository
    {
        // ---- 측정값 저장 --------------------------------------------------------------------------
        // reading : Sig5560Client 가 만든 측정값 그릇을 통째로 받음 → 호출하는 쪽은 값 5개를 따로 넘길 필요 없음
        // status  : MainForm.Evaluate 가 판단한 상태
        public Task InsertAsync(int sensorId, TempReading reading, TempStatus status)
        {
            // SQL 뜻: TB_TEMP_LOG 에 (센서, 측정시각, 온도, 상태코드, 에러코드) 한 행 추가
            const string sql = @"
INSERT INTO dbo.TB_TEMP_LOG (SENSOR_ID, MEASURE_DT, TEMP_VALUE, STATUS_CD, ERROR_CD)
VALUES (@SENSOR_ID, @MEASURE_DT, @TEMP_VALUE, @STATUS_CD, @ERROR_CD);";

            // 센서 오류면 온도 값을 믿을 수 없으므로 NULL 로 저장 (0℃ 와 구분)
            // decimal? : null 을 담을 수 있는 decimal
            // (decimal?)null : 삼항 연산자 양쪽 타입을 맞추려고 null 에 타입을 붙여 줌 (안 붙이면 컴파일 에러)
            decimal? temp = reading.HasError ? (decimal?)null : reading.Temperature;

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@MEASURE_DT", reading.ReadTime),
                DbHelper.Temp("@TEMP_VALUE", temp),                          // null 이면 DbHelper.Temp 가 DBNull 로 바꿈
                DbHelper.VarChar("@STATUS_CD", status.ToCode(), 10),         // 확장 메서드: TempStatus.Warning → "WARNING"
                // Math.Min(255, x) : 255 보다 크면 255 로 / Math.Max(0, ...) : 0 보다 작으면 0 으로
                // → TINYINT 범위(0~255) 를 벗어난 이상한 코드가 와도 저장 에러가 안 나게 잘라 줌
                DbHelper.TinyInt("@ERROR_CD", Math.Max(0, Math.Min(255, reading.ErrorCode))));
        }

        // ---- 기간 조회 --------------------------------------------------------------------------
        // 호출: MainForm.SearchHistoryAsync → 결과 DataTable 을 dgvHistory.DataSource 에 바로 연결
        // maxRows : MainForm.MaxHistoryRows(50000) → 너무 많은 행을 한 번에 가져오지 않게
        // 결과 컬럼 이름(MEASURE_DT, TEMP_VALUE, STATUS_NM)은 디자이너의 그리드 컬럼 DataPropertyName 과 짝이 맞아야 함
        /// <summary>
        /// 기간 조회. 시작은 포함(>=), 끝은 미포함(&lt;)인 "반열린 구간"으로 조회합니다.
        /// BETWEEN 을 쓰면 경계 시각(예: 15:00:00)의 데이터가 이전/다음 조회에 중복으로 잡힙니다.
        /// </summary>
        public Task<DataTable> SearchAsync(int sensorId, DateTime from, DateTime to, int maxRows)
        {
            // SQL 뜻:
            //   TOP (@MAX_ROWS)       : 최대 n 행까지만
            //   STATUS_CD             : 'WARNING' 같은 코드 (화면 색칠 판단용, MainForm.dgvHistory_CellFormatting)
            //   CASE ... AS STATUS_NM : 화면 표시용 이름. 오류면 '센서 오류 (온도센서 연결 끊김)' 처럼 오류 종류까지 붙임
            //   LEFT JOIN 공통코드 C  : 상태 코드 → 한글 이름 (TEMP_STATUS 그룹)
            //   LEFT JOIN 공통코드 E  : 에러 코드 → 한글 이름 (SENSOR_ERROR 그룹). ERROR_CD 는 숫자라 CAST 로 문자로 바꿔 비교
            //   ISNULL(C.CODE_NM, L.STATUS_CD) : 공통코드에 이름이 없으면 코드라도 보여 줌 (LEFT JOIN 이라 없을 수 있음)
            //   WHERE : 이 센서 + 기간(시작 이상, 끝 미만)
            //   ORDER BY ... DESC : 최신이 맨 위
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

            // 결과를 가공하지 않고 표 그대로 돌려줌 (그리드에 바로 바인딩하니까) → async 없이 Task 를 그대로 넘김
            return DbHelper.QueryAsync(sql,
                DbHelper.Int("@MAX_ROWS", maxRows),
                DbHelper.Int("@SENSOR_ID", sensorId),
                DbHelper.DateTime2("@FROM_DT", from),
                DbHelper.DateTime2("@TO_DT", to));
        }
    }
}
