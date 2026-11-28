// ============================================================================================
//  Data/DbHelper.cs : DB(SQL Server) 연결과 SQL 실행을 대신해 주는 "공통 도구함"
// --------------------------------------------------------------------------------------------
//  모든 Repository(UserRepository, ConfigRepository, SensorRepository, TempLogRepository,
//  AlertRepository, KakaoLogRepository) 가 SQL 을 실행할 때 이 파일의 메서드를 부릅니다.
//  Repository 는 "어떤 SQL 을 실행할지" 만 정하고, "어떻게 연결해서 실행하는지" 는 여기서 처리.
//
//  제공하는 기능
//    ConnectionString        : App.config 에서 DB 접속 문자열 읽기
//    TestConnectionAsync     : 연결되는지 확인만 (MainForm_Load)
//    ExecuteNonQueryAsync    : INSERT / UPDATE / DELETE 실행 → 영향받은 행 수
//    ExecuteScalarAsync      : 결과 "값 1개" (OUTPUT INSERTED.ID, 설정값 하나)
//    QueryAsync              : SELECT → DataTable(표)
//    CreateCommand           : SQL + 연결 + 파라미터를 묶은 SqlCommand 만들기 (위 3개의 공통 부품)
//    VarChar / Int / Temp ...: SQL 의 @이름 자리에 넣을 SqlParameter 만들기
//
//  internal static class
//    internal : 이 프로젝트 안에서만 사용 (DB 접근은 외부에 공개할 이유가 없음)
//    static   : 기억할 상태가 없는 도구라서 객체 없이 DbHelper.QueryAsync(...) 로 바로 씀
// ============================================================================================

using System;                      // DBNull, Math, DateTime
using System.Configuration;        // ConfigurationManager (App.config 읽기). 참조에 System.Configuration 이 추가돼 있어야 함
using System.Data;                 // DataTable, SqlDbType
using System.Data.SqlClient;       // SqlConnection, SqlCommand, SqlParameter (SQL Server 전용 ADO.NET 클래스)
using System.Threading.Tasks;      // Task (비동기)

namespace TempMonitor.Data
{
    /// <summary>
    /// ADO.NET 공통 처리.
    /// - 연결 문자열은 App.config 에서 읽습니다 (코드에 서버 주소/계정을 박아 넣지 않기 위해).
    /// - 모든 SQL 은 반드시 SqlParameter 로 값을 넘깁니다. 문자열 연결로 SQL 을 만들면 SQL Injection 위험.
    /// - SqlConnection 은 using 으로 감싸 매번 열고 닫습니다. 실제로는 "커넥션 풀"이 재사용하므로 비용이 거의 없습니다.
    /// </summary>
    internal static class DbHelper
    {
        // ---- 연결 문자열 -------------------------------------------------------------------------
        // 계산 속성 (get 안에 코드가 있음) → DbHelper.ConnectionString 을 읽을 때마다 아래 코드가 실행됨
        // 쓰는 곳: 이 파일의 모든 메서드 + ConfigRepository.SaveAsync (트랜잭션 때문에 직접 연결)
        public static string ConnectionString
        {
            get
            {
                // App.config 파일의 <connectionStrings> 안에서 name="TempMonitorDB" 항목을 찾음
                var cs = ConfigurationManager.ConnectionStrings["TempMonitorDB"];
                if (cs == null)   // 그런 이름이 없으면
                    // throw : 예외를 던져 실행을 멈추고 호출한 쪽으로 오류를 올려 보냄.
                    // null 을 그냥 돌려주면 나중에 엉뚱한 곳에서 터져 원인 찾기가 어려우므로 명확한 메시지로 바로 알림
                    throw new ConfigurationErrorsException("App.config 에 'TempMonitorDB' 연결 문자열이 없습니다.");
                return cs.ConnectionString;   // 실제 접속 문자열 (예: Data Source=...;Initial Catalog=TempMonitorDB;...)
            }
        }

        // ---- 연결 테스트 -------------------------------------------------------------------------
        // 호출: MainForm.MainForm_Load → 결과로 상태 표시줄 "DB: 연결됨 / 연결 실패"
        // async Task<bool> : 비동기로 실행되고, 끝나면 bool 을 돌려주는 메서드
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                // using : 블록이 끝나면 conn.Dispose() 자동 호출 → 연결 닫기(커넥션 풀에 반납)
                using (var conn = new SqlConnection(ConnectionString))   // 연결 객체 만들기 (아직 연결 안 됨)
                {
                    await conn.OpenAsync();   // 실제로 DB 에 연결 시도. await : 연결될 때까지 기다리되 화면은 멈추지 않음
                    return true;              // 여기까지 오면 연결 성공
                }
            }
            catch      // 어떤 예외든 (서버 꺼짐, 이름 틀림, 권한 없음 ...)
            {
                return false;   // 예외를 bool 로 바꿔서 돌려줌. Load 에서는 "되나 안 되나" 만 알면 되니까
            }
        }

        // ---- INSERT / UPDATE / DELETE ------------------------------------------------------------
        // sql        : 실행할 SQL 문 (값 자리는 @이름)
        // params ... : 파라미터를 배열로 안 묶고 쉼표로 여러 개 넘길 수 있게 해 주는 키워드
        //              예) ExecuteNonQueryAsync(sql, DbHelper.Int("@A", 1), DbHelper.VarChar("@B", "x", 10))
        // 반환       : 영향받은 행 수 (int)
        // 호출: UserRepository, SensorRepository, TempLogRepository, AlertRepository, KakaoLogRepository
        public static async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))      // ① 연결 객체
            using (var cmd = CreateCommand(conn, sql, parameters))       // ② 명령 객체 (아래 CreateCommand 호출) - using 두 개를 겹쳐 씀
            {
                await conn.OpenAsync();                                  // ③ 연결 열기
                return await cmd.ExecuteNonQueryAsync();                 // ④ 실행 → 행 수 돌려줌
            }   // ⑤ cmd → conn 순서로 자동 정리
        }

        // ---- 값 1개 조회 -------------------------------------------------------------------------
        // 결과 표의 "첫 번째 행, 첫 번째 칸" 하나만 돌려줌. 타입을 모르니 object.
        // 호출: AlertRepository.InsertAsync (OUTPUT INSERTED.ALERT_ID 로 새 ID 받기)
        //       ConfigRepository.GetValueAsync (설정값 하나 읽기)
        public static async Task<object> ExecuteScalarAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = CreateCommand(conn, sql, parameters))
            {
                await conn.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();          // 값 1개 (행이 없으면 null, DB NULL 이면 DBNull.Value)
                // DB 의 NULL(DBNull.Value) 을 C# 의 null 로 바꿔서 돌려줌
                // → 부르는 쪽에서 "v as string" 처럼 null 하나만 신경 쓰면 되게
                return result == DBNull.Value ? null : result;
            }
        }

        // ---- SELECT → 표 -------------------------------------------------------------------------
        // 반환: DataTable (메모리 위의 표. Rows[행번호]["컬럼명"] 으로 값 꺼냄)
        // 호출: UserRepository.GetCredentialAsync, ConfigRepository.LoadAllAsync,
        //       SensorRepository.GetDefaultSensorAsync, 각 Repository 의 SearchAsync (조회 탭)
        public static async Task<DataTable> QueryAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = CreateCommand(conn, sql, parameters))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())     // SQL 실행 → 결과를 한 줄씩 읽는 reader
                {
                    var table = new DataTable();                         // 빈 표 만들기
                    table.Load(reader);                                  // reader 의 모든 행/컬럼을 표에 옮겨 담기
                    return table;                                        // 표 돌려주기 (연결이 닫혀도 표는 메모리에 남음)
                }
            }
        }

        // ---- 명령 객체 만들기 (공통 부품) ----------------------------------------------------------
        // 위 3개 메서드가 공통으로 부르고, ConfigRepository.SaveAsync 도 트랜잭션 때문에 직접 부름 (그래서 public)
        public static SqlCommand CreateCommand(SqlConnection conn, string sql, params SqlParameter[] parameters)
        {
            // new SqlCommand(sql, conn) : "이 연결로 이 SQL 을 실행할 명령"
            // { CommandTimeout = 30 }   : 객체 초기화자. 만들면서 속성도 설정. 30초 넘게 걸리면 시간 초과 예외
            var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            if (parameters != null)                   // 파라미터가 있으면
                cmd.Parameters.AddRange(parameters);  // 배열에 든 파라미터를 한 번에 전부 붙이기
            return cmd;
        }

        // ---- 파라미터 생성 헬퍼 -----------------------------------------------------------
        // AddWithValue 처럼 타입을 추측하게 두지 않고 컬럼 타입과 정확히 맞춥니다.
        // 타입이 다르면 SQL Server 가 컬럼 쪽을 형 변환해서 인덱스를 못 타는 경우가 생깁니다.
        //
        // 공통 모양:  new SqlParameter(이름, DB타입, 길이) { Value = 값 }
        //   이름 : SQL 문 안의 @이름 과 똑같아야 함
        //   값   : C# 의 null 은 DB 에 그대로 못 넘기므로 DBNull.Value 로 바꿔서 넣어야 함

        // VARCHAR(영문/숫자 문자열) 컬럼용. 예) USER_ID, STATUS_CD, CONFIG_KEY, IP_ADDR
        public static SqlParameter VarChar(string name, string value, int size)
        {
            // (object)value ?? DBNull.Value
            //   ?? : 왼쪽이 null 이면 오른쪽 값을 씀
            //   (object) : ?? 양쪽 타입이 같아야 하는데 string 과 DBNull 은 공통 타입이 object 라서 맞춰 줌
            return new SqlParameter(name, SqlDbType.VarChar, size) { Value = (object)value ?? DBNull.Value };
        }

        // NVARCHAR(한글 가능 문자열) 컬럼용. 예) FAIL_REASON, MESSAGE, CONFIG_VALUE, RECEIVER_NM
        public static SqlParameter NVarChar(string name, string value, int size)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size) { Value = (object)value ?? DBNull.Value };
        }

        // INT 컬럼용. 예) SENSOR_ID, PORT_NO, MAX_ROWS (int 는 null 이 될 수 없으니 DBNull 처리 없음)
        public static SqlParameter Int(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        // BIGINT 컬럼용. 예) ALERT_ID
        // long? : null 허용 long. 카톡 테스트 발송처럼 연결할 경고가 없으면 null → DB 에 NULL 저장
        public static SqlParameter BigInt(string name, long? value)
        {
            // value.HasValue : 값이 있나?  value.Value : 실제 값 꺼내기
            return new SqlParameter(name, SqlDbType.BigInt) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        // TINYINT(0~255) 컬럼용. 예) ERROR_CD
        public static SqlParameter TinyInt(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.TinyInt) { Value = (byte)value };   // TINYINT 는 C# 의 byte 와 짝
        }

        // 온도 DECIMAL(5,1) 컬럼용. 예) TEMP_VALUE, THRESHOLD_VALUE
        // decimal? : 센서 오류일 때 온도를 NULL 로 저장하려고 null 허용 (TempLogRepository.InsertAsync)
        public static SqlParameter Temp(string name, decimal? value)
        {
            // DECIMAL(5,1) 과 동일하게 맞춤
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = 5,   // 전체 자릿수 5 (예: 9999.9)
                Scale = 1,       // 소수점 아래 1자리
                Value = value.HasValue ? (object)Math.Round(value.Value, 1) : DBNull.Value   // 소수 1자리로 반올림, 없으면 NULL
            };
        }

        // DATETIME2(0) 컬럼용 (초 단위까지). 예) MEASURE_DT, ALERT_DT, SEND_DT, 조회 FROM_DT/TO_DT
        public static SqlParameter DateTime2(string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Scale = 0, Value = value };   // Scale 0 = 소수 초 없음
        }

        // CHAR(1) 컬럼용. 예) SUCCESS_YN ("Y"/"N")
        public static SqlParameter Char1(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.Char, 1) { Value = value };
        }
    }
}
