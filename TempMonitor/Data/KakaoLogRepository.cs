// ============================================================================================
//  Data/KakaoLogRepository.cs : 카카오톡 발송 이력 테이블(TB_KAKAO_LOG) 담당 SQL
// --------------------------------------------------------------------------------------------
//  메서드와 호출하는 곳
//    InsertAsync : 발송 1건 기록       ← MainForm.SendKakaoAndLogAsync (경고 / 정상 복귀 / 테스트 발송 모두)
//    SearchAsync : 기간으로 이력 조회  ← MainForm.SearchKakaoAsync (카톡 이력 탭)
// ============================================================================================

using System;                  // DateTime
using System.Data;             // DataTable
using System.Threading.Tasks;  // Task

namespace TempMonitor.Data
{
    internal class KakaoLogRepository
    {
        // ---- 발송 이력 저장 ---------------------------------------------------------------------
        // alertId       : 연결할 경고 ID. 테스트 발송은 경고가 없으므로 null → long? (null 허용)
        // receiver      : 받는 사람 이름 (AppConfig.KakaoReceiverName)
        // message       : 보낸 메시지 내용
        // success       : 성공 여부 → "SUCCESS"/"FAIL" 코드로 바꿔 저장
        // resultMessage : 실패 사유 (성공이면 null)
        public Task InsertAsync(long? alertId, DateTime sendTime, string receiver, string message, bool success, string resultMessage)
        {
            const string sql = @"
INSERT INTO dbo.TB_KAKAO_LOG (ALERT_ID, SEND_DT, RECEIVER_NM, MESSAGE, RESULT_CD, RESULT_MSG)
VALUES (@ALERT_ID, @SEND_DT, @RECEIVER_NM, @MESSAGE, @RESULT_CD, @RESULT_MSG);";

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.BigInt("@ALERT_ID", alertId),                                 // null 이면 DB NULL
                DbHelper.DateTime2("@SEND_DT", sendTime),
                DbHelper.NVarChar("@RECEIVER_NM", Truncate(receiver, 50), 50),         // 컬럼 길이 50 에 맞게 자르기
                DbHelper.NVarChar("@MESSAGE", Truncate(message, 500), 500),            // 컬럼 길이 500 에 맞게 자르기
                DbHelper.VarChar("@RESULT_CD", success ? "SUCCESS" : "FAIL", 10),      // bool → DB 코드 (DB 표현은 데이터 층이 책임)
                // 성공이면 사유가 없으므로 NULL 그대로 (Truncate 는 null 을 "" 로 바꾸므로 거치지 않음)
                DbHelper.NVarChar("@RESULT_MSG", resultMessage == null ? null : Truncate(resultMessage, 500), 500));
        }

        // ---- 발송 이력 조회 ---------------------------------------------------------------------
        // 호출: MainForm.SearchKakaoAsync → dgvKakao.DataSource 에 연결
        public Task<DataTable> SearchAsync(DateTime from, DateTime to)
        {
            // SQL 뜻:
            //   RESULT_NM = 결과 코드의 한글 이름(공통코드 SEND_RESULT, 없으면 코드)
            //             + 실패이고 사유가 있으면 " (사유)" 를 뒤에 붙임  → 예: '실패 (HTTP 401: ...)'
            //   N'...' : 한글이 들어간 유니코드 문자열 표시
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

        // ---- 문자열 자르기 도우미 ---------------------------------------------------------------
        // 컬럼 길이보다 긴 문자열을 넣으면 "String or binary data would be truncated" 에러가 나므로 미리 자름
        // static : 필드를 안 쓰고 매개변수만으로 계산하는 메서드
        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;          // null 이면 "" , 빈 문자열이면 그대로
            return s.Length <= max ? s : s.Substring(0, max);             // 짧으면 그대로, 길면 앞에서 max 글자만
        }
    }
}
