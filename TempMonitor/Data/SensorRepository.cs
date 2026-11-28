// ============================================================================================
//  Data/SensorRepository.cs : 센서 정보 테이블(TB_SENSOR) 담당 SQL
// --------------------------------------------------------------------------------------------
//  메서드와 호출하는 곳
//    GetDefaultSensorAsync : 사용 중인 첫 센서 정보 읽기    ← MainForm.MainForm_Load (IP/포트 자동 채우기)
//    UpdateAddressAsync    : 연결 성공한 IP/포트 저장       ← MainForm.btnConnect_Click
//  → 그래서 한 번 연결에 성공하면, 다음 실행 때 그 IP 가 자동으로 입력칸에 들어가 있음
// ============================================================================================

using System.Data;             // DataTable, DataRow
using System.Threading.Tasks;  // Task

namespace TempMonitor.Data
{
    // ---- 센서 정보 그릇 ----------------------------------------------------------------------
    // GetDefaultSensorAsync 가 만들어서 → MainForm_Load 가 받아 필드/입력칸에 나눠 담음
    internal class SensorInfo
    {
        public int SensorId { get; set; }        // SENSOR_ID → MainForm._sensorId (모든 저장/조회의 @SENSOR_ID)
        public string SensorName { get; set; }   // SENSOR_NM → MainForm._sensorName (카톡 메시지에 표시)
        public string IpAddress { get; set; }    // IP_ADDR   → MainForm.txtIp.Text
        public int Port { get; set; }            // PORT_NO   → MainForm.txtPort.Text
        public byte UnitId { get; set; }         // UNIT_ID   → MainForm._modbus.UnitId (Modbus 장비 번호)
    }

    internal class SensorRepository
    {
        // ---- 기본 센서 읽기 ----------------------------------------------------------------------
        /// <summary>사용 중인 첫 번째 센서를 가져옵니다. (현재 화면은 센서 1대 기준)</summary>
        public async Task<SensorInfo> GetDefaultSensorAsync()
        {
            // SQL 뜻: 사용 중(USE_YN='Y') 센서를 SENSOR_ID 순으로 정렬해서 맨 위 1개(TOP 1)만
            const string sql = @"
SELECT TOP (1) SENSOR_ID, SENSOR_NM, IP_ADDR, PORT_NO, UNIT_ID
  FROM dbo.TB_SENSOR
 WHERE USE_YN = 'Y'
 ORDER BY SENSOR_ID;";

            DataTable dt = await DbHelper.QueryAsync(sql);   // 파라미터 없는 조회
            if (dt.Rows.Count == 0)                          // 등록된 센서가 없으면
                return null;                                 // MainForm_Load 가 null 이면 기본값(_sensorId = 1) 그대로 둠

            DataRow r = dt.Rows[0];
            return new SensorInfo   // 행 → 객체 번역 (UserRepository.GetCredentialAsync 와 같은 패턴)
            {
                SensorId = (int)r["SENSOR_ID"],
                SensorName = (string)r["SENSOR_NM"],
                IpAddress = (string)r["IP_ADDR"],
                Port = (int)r["PORT_NO"],
                UnitId = (byte)r["UNIT_ID"]   // DB 가 TINYINT 라 C# 에서는 byte 로 꺼내야 함 (int 로 캐스팅하면 InvalidCastException)
            };
        }

        // ---- 접속 주소 저장 ---------------------------------------------------------------------
        // 호출: MainForm.btnConnect_Click (연결 성공 직후). 결과를 가공할 게 없어서 Task 를 그대로 넘김
        public Task UpdateAddressAsync(int sensorId, string ip, int port)
        {
            // SQL 뜻: 그 센서의 IP, 포트, 수정시각을 바꿔라
            const string sql = @"
UPDATE dbo.TB_SENSOR
   SET IP_ADDR = @IP_ADDR,
       PORT_NO = @PORT_NO,
       UPD_DT  = SYSDATETIME()
 WHERE SENSOR_ID = @SENSOR_ID;";

            return DbHelper.ExecuteNonQueryAsync(sql,
                DbHelper.VarChar("@IP_ADDR", ip, 50),
                DbHelper.Int("@PORT_NO", port),
                DbHelper.Int("@SENSOR_ID", sensorId));
        }
    }
}
