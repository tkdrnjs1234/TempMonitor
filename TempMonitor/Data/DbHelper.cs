using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

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
        public static string ConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["TempMonitorDB"];
                if (cs == null)
                    throw new ConfigurationErrorsException("App.config 에 'TempMonitorDB' 연결 문자열이 없습니다.");
                return cs.ConnectionString;
            }
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = CreateCommand(conn, sql, parameters))
            {
                await conn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task<object> ExecuteScalarAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = CreateCommand(conn, sql, parameters))
            {
                await conn.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result == DBNull.Value ? null : result;
            }
        }

        public static async Task<DataTable> QueryAsync(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = CreateCommand(conn, sql, parameters))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var table = new DataTable();
                    table.Load(reader);
                    return table;
                }
            }
        }

        public static SqlCommand CreateCommand(SqlConnection conn, string sql, params SqlParameter[] parameters)
        {
            var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd;
        }

        // ---- 파라미터 생성 헬퍼 -----------------------------------------------------------
        // AddWithValue 처럼 타입을 추측하게 두지 않고 컬럼 타입과 정확히 맞춥니다.
        // 타입이 다르면 SQL Server 가 컬럼 쪽을 형 변환해서 인덱스를 못 타는 경우가 생깁니다.

        public static SqlParameter VarChar(string name, string value, int size)
        {
            return new SqlParameter(name, SqlDbType.VarChar, size) { Value = (object)value ?? DBNull.Value };
        }

        public static SqlParameter NVarChar(string name, string value, int size)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size) { Value = (object)value ?? DBNull.Value };
        }

        public static SqlParameter Int(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        public static SqlParameter BigInt(string name, long? value)
        {
            return new SqlParameter(name, SqlDbType.BigInt) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        public static SqlParameter TinyInt(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.TinyInt) { Value = (byte)value };
        }

        public static SqlParameter Temp(string name, decimal? value)
        {
            // DECIMAL(5,1) 과 동일하게 맞춤
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = 5,
                Scale = 1,
                Value = value.HasValue ? (object)Math.Round(value.Value, 1) : DBNull.Value
            };
        }

        public static SqlParameter DateTime2(string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Scale = 0, Value = value };
        }

        public static SqlParameter Char1(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.Char, 1) { Value = value };
        }
    }
}
