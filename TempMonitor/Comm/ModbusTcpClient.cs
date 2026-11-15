using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TempMonitor.Comm
{
    /// <summary>
    /// 장비가 Modbus 예외 응답(함수코드 + 0x80)을 돌려줬을 때 발생시키는 예외.
    /// </summary>
    public class ModbusException : Exception
    {
        public byte FunctionCode { get; }
        public byte ExceptionCode { get; }

        public ModbusException(byte functionCode, byte exceptionCode)
            : base(string.Format("Modbus 예외 응답 (FC {0:X2}, 코드 {1}: {2})",
                                 functionCode, exceptionCode, Describe(exceptionCode)))
        {
            FunctionCode = functionCode;
            ExceptionCode = exceptionCode;
        }

        private static string Describe(byte code)
        {
            switch (code)
            {
                case 1: return "지원하지 않는 함수 코드";
                case 2: return "잘못된 데이터 주소";
                case 3: return "잘못된 데이터 값";
                case 4: return "장비 내부 오류";
                default: return "알 수 없는 오류";
            }
        }
    }

    /// <summary>
    /// 최소한의 Modbus/TCP 마스터(클라이언트) 구현.
    ///
    /// 프레임 구조 (MBAP 헤더 7바이트 + PDU)
    ///   [0-1] Transaction ID : 요청/응답 짝을 맞추는 번호 (요청마다 1씩 증가)
    ///   [2-3] Protocol ID    : Modbus 는 항상 0
    ///   [4-5] Length         : 이 뒤에 오는 바이트 수 (Unit ID 1 + PDU 길이)
    ///   [6]   Unit ID        : 장비 번호 (TCP 에서는 보통 1)
    ///   [7~]  PDU            : 함수 코드 1바이트 + 데이터
    /// 모든 16비트 값은 Big-endian (상위 바이트 먼저).
    /// </summary>
    public sealed class ModbusTcpClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private ushort _transactionId;

        // 한 소켓으로 요청을 동시에 두 개 보내면 응답이 섞입니다.
        // (예: 폴링 중에 사용자가 '초기화' 버튼 클릭) → 한 번에 하나의 요청만 보내도록 잠금.
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);

        public byte UnitId { get; set; } = 1;
        public int TimeoutMs { get; set; } = 3000;

        public bool IsConnected
        {
            get { return _client != null && _client.Connected && _stream != null; }
        }

        public async Task ConnectAsync(string host, int port)
        {
            Disconnect();

            var client = new TcpClient();
            Task connectTask = client.ConnectAsync(host, port);

            // TcpClient.ConnectAsync 는 자체 타임아웃이 없어(윈도우 기본 약 20초) 직접 제한합니다.
            if (await Task.WhenAny(connectTask, Task.Delay(TimeoutMs)).ConfigureAwait(false) != connectTask)
            {
                client.Close();
                // 버려진 Task 의 예외를 관찰 처리해 둠 (UnobservedTaskException 방지)
                var ignored = connectTask.ContinueWith(t => { var e = t.Exception; },
                                                       TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException(string.Format("{0}:{1} 연결 시간 초과", host, port));
            }

            try
            {
                await connectTask.ConfigureAwait(false); // 연결 실패 예외가 있으면 여기서 던져짐
            }
            catch
            {
                client.Close();
                throw;
            }

            client.NoDelay = true; // 작은 패킷을 모아 보내지 않고 즉시 전송 (요청-응답형 통신에 유리)
            _client = client;
            _stream = client.GetStream();
        }

        public void Disconnect()
        {
            if (_stream != null) { _stream.Dispose(); _stream = null; }
            if (_client != null) { _client.Close(); _client = null; }
        }

        /// <summary>FC 04 : Read Input Registers (읽기 전용 16비트 레지스터)</summary>
        public async Task<short[]> ReadInputRegistersAsync(ushort startAddress, ushort count)
        {
            if (count < 1 || count > 125)
                throw new ArgumentOutOfRangeException(nameof(count), "한 번에 1~125개까지 읽을 수 있습니다.");

            byte[] pdu =
            {
                0x04,
                Hi(startAddress), Lo(startAddress),
                Hi(count),        Lo(count)
            };

            byte[] resp = await SendAsync(pdu).ConfigureAwait(false);

            // 응답 PDU: [FC][바이트 수][데이터...]
            int byteCount = resp[1];
            if (byteCount != count * 2 || resp.Length < 2 + byteCount)
                throw new IOException("응답 데이터 길이가 올바르지 않습니다.");

            var values = new short[count];
            for (int i = 0; i < count; i++)
            {
                // Big-endian 두 바이트 → 부호 있는 16비트 정수 (음수 온도는 2의 보수로 들어옴: 0xFFB9 = -71)
                values[i] = (short)((resp[2 + i * 2] << 8) | resp[3 + i * 2]);
            }
            return values;
        }

        /// <summary>FC 05 : Write Single Coil (1비트 쓰기, ON = 0xFF00 / OFF = 0x0000)</summary>
        public async Task WriteSingleCoilAsync(ushort address, bool on)
        {
            byte[] pdu =
            {
                0x05,
                Hi(address), Lo(address),
                (byte)(on ? 0xFF : 0x00), 0x00
            };
            await SendAsync(pdu).ConfigureAwait(false); // 정상 응답은 요청을 그대로 되돌려줌(echo)
        }

        /// <summary>요청 PDU 를 MBAP 헤더로 감싸 보내고, 응답 PDU 를 돌려줍니다.</summary>
        private async Task<byte[]> SendAsync(byte[] pdu)
        {
            await _ioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsConnected)
                    throw new IOException("센서에 연결되어 있지 않습니다.");

                ushort tid = unchecked(++_transactionId);
                ushort length = (ushort)(pdu.Length + 1);

                var frame = new byte[7 + pdu.Length];
                frame[0] = Hi(tid);    frame[1] = Lo(tid);
                frame[2] = 0;          frame[3] = 0;           // Protocol ID
                frame[4] = Hi(length); frame[5] = Lo(length);
                frame[6] = UnitId;
                Buffer.BlockCopy(pdu, 0, frame, 7, pdu.Length);

                await _stream.WriteAsync(frame, 0, frame.Length).ConfigureAwait(false);

                byte[] header = await ReadExactAsync(7).ConfigureAwait(false);
                ushort respTid = (ushort)((header[0] << 8) | header[1]);
                ushort respLen = (ushort)((header[4] << 8) | header[5]);

                if (respTid != tid)
                    throw new IOException(string.Format("Transaction ID 불일치 (보냄 {0}, 받음 {1})", tid, respTid));
                if (respLen < 2 || respLen > 254)
                    throw new IOException("응답 길이가 올바르지 않습니다: " + respLen);

                byte[] respPdu = await ReadExactAsync(respLen - 1).ConfigureAwait(false);

                byte fc = respPdu[0];
                if ((fc & 0x80) != 0)                          // 최상위 비트가 켜져 있으면 예외 응답
                    throw new ModbusException((byte)(fc & 0x7F), respPdu.Length > 1 ? respPdu[1] : (byte)0);
                if (fc != pdu[0])
                    throw new IOException("요청과 다른 함수 코드의 응답을 받았습니다.");

                return respPdu;
            }
            catch (Exception ex) when (!(ex is ModbusException))
            {
                // 통신 중 오류가 나면 스트림 상태를 믿을 수 없으므로 연결을 끊어 둡니다.
                // (남은 쓰레기 바이트가 다음 응답과 섞이는 것을 방지 → 폴링 루프에서 재연결)
                Disconnect();
                throw;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        /// <summary>
        /// TCP 는 "스트림"이라 한 번의 Read 로 원하는 바이트가 다 온다는 보장이 없습니다.
        /// 원하는 길이를 다 받을 때까지 반복해서 읽습니다.
        /// </summary>
        private async Task<byte[]> ReadExactAsync(int length)
        {
            var buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                Task<int> readTask = _stream.ReadAsync(buffer, offset, length - offset);
                if (await Task.WhenAny(readTask, Task.Delay(TimeoutMs)).ConfigureAwait(false) != readTask)
                {
                    var ignored = readTask.ContinueWith(t => { var e = t.Exception; },
                                                        TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException("센서 응답 시간 초과");
                }

                int read = await readTask.ConfigureAwait(false);
                if (read == 0)
                    throw new IOException("센서가 연결을 종료했습니다.");
                offset += read;
            }
            return buffer;
        }

        private static byte Hi(ushort v) { return (byte)(v >> 8); }
        private static byte Lo(ushort v) { return (byte)(v & 0xFF); }

        public void Dispose()
        {
            Disconnect();
            _ioLock.Dispose();
        }
    }
}
