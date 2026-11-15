using System.Data;
using System.Threading.Tasks;

namespace TempMonitor.Data
{
    internal class SensorInfo
    {
        public int SensorId { get; set; }
        public string SensorName { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public byte UnitId { get; set; }
    }

    internal class SensorRepository
    {
        /// <summary>사용 중인 첫 번째 센서를 가져옵니다. (현재 화면은 센서 1대 기준)</summary>
        public async Task<SensorInfo> GetDefaultSensorAsync()
        {
            const string sql = @"
SELECT TOP (1) SENSOR_ID, SENSOR_NM, IP_ADDR, PORT_NO, UNIT_ID
  FROM dbo.TB_SENSOR
 WHERE USE_YN = 'Y'
 ORDER BY SENSOR_ID;";

            DataTable dt = await DbHelper.QueryAsync(sql);
            if (dt.Rows.Count == 0)
                return null;

            DataRow r = dt.Rows[0];
            return new SensorInfo
            {
                SensorId = (int)r["SENSOR_ID"],
                SensorName = (string)r["SENSOR_NM"],
                IpAddress = (string)r["IP_ADDR"],
                Port = (int)r["PORT_NO"],
                UnitId = (byte)r["UNIT_ID"]
            };
        }

        public Task UpdateAddressAsync(int sensorId, string ip, int port)
        {
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
