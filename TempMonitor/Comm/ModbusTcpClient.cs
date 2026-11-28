// ============================================================================================
//  Comm/ModbusTcpClient.cs : Modbus TCP 통신 담당 (바이트를 만들어 보내고, 응답 바이트를 받기)
// --------------------------------------------------------------------------------------------
//  Modbus = 공장 장비들이 많이 쓰는 통신 규칙. "몇 번 주소 값을 읽어 줘 / 써 줘" 를 정해진 바이트 모양으로 주고받음.
//  이 클래스는 "온도" 라는 개념을 모릅니다. 주소와 숫자만 다룹니다. (온도 해석은 Sig5560Client 가 함)
//
//  누가 부르나
//    MainForm         : ConnectAsync (연결), Disconnect, IsConnected (끊겼나 확인), UnitId (장비 번호), Dispose
//    Sig5560Client    : ReadInputRegistersAsync (값 읽기), WriteSingleCoilAsync (초기화 쓰기)
//
//  내부 호출 흐름
//    ReadInputRegistersAsync / WriteSingleCoilAsync
//      → SendAsync        (요청 프레임 만들어 보내고 응답 받기, 한 번에 하나씩만)
//          → ReadExactAsync (원하는 바이트 수를 다 받을 때까지 읽기)
//
//  ConfigureAwait(false) : await 가 끝난 뒤 "UI 스레드로 돌아올 필요 없다" 는 뜻.
//    이 파일은 화면 컨트롤을 만지지 않으므로 굳이 UI 스레드를 기다리지 않게 해서 효율적으로.
//    (반대로 MainForm 에서는 await 뒤에 라벨을 바꿔야 하므로 쓰면 안 됨)
// ============================================================================================

using System;                      // Exception, TimeoutException, ArgumentOutOfRangeException, Buffer, IDisposable
using System.IO;                   // IOException
using System.Net.Sockets;          // TcpClient, NetworkStream (TCP 통신)
using System.Threading;            // SemaphoreSlim (동시 접근 막는 잠금)
using System.Threading.Tasks;      // Task

namespace TempMonitor.Comm
{
    // ---- Modbus 예외 --------------------------------------------------------------------------
    // 장비가 "그 요청은 못 해요" 라고 정상적으로 답한 경우를 나타내는 우리만의 예외 클래스.
    // : Exception → .NET 기본 예외를 상속. 그래서 throw / catch 할 수 있음
    // 따로 만든 이유: catch (ModbusException ex) when (ex.ExceptionCode == 2) 처럼 "종류별로" 골라 잡으려고
    //   (Sig5560Client.ReadAsync 가 코드 2 를 잡아 개별 읽기로 전환)
    /// <summary>
    /// 장비가 Modbus 예외 응답(함수코드 + 0x80)을 돌려줬을 때 발생시키는 예외.
    /// </summary>
    public class ModbusException : Exception
    {
        // { get; } 만 있는 속성 = 생성자에서만 값을 넣을 수 있는 읽기 전용
        public byte FunctionCode { get; }    // 어떤 요청(함수코드)에 대한 에러인지 (예: 0x04)
        public byte ExceptionCode { get; }   // 장비가 알려 준 에러 번호 (1~4)

        // 생성자. : base(...) → 부모(Exception) 생성자에 메시지를 넘김 → ex.Message 로 꺼낼 수 있게 됨
        // {0:X2} : 16진수 2자리 (4 → "04")
        public ModbusException(byte functionCode, byte exceptionCode)
            : base(string.Format("Modbus 예외 응답 (FC {0:X2}, 코드 {1}: {2})",
                                 functionCode, exceptionCode, Describe(exceptionCode)))
        {
            FunctionCode = functionCode;
            ExceptionCode = exceptionCode;
        }

        // 에러 번호 → 한글 설명 (Modbus 표준 예외 코드표). 생성자의 base(...) 안에서 쓰므로 static 이어야 함
        private static string Describe(byte code)
        {
            switch (code)
            {
                case 1: return "지원하지 않는 함수 코드";
                case 2: return "잘못된 데이터 주소";     // ← Sig5560Client 가 이 경우를 특별 처리
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
    // sealed      : 이 클래스를 상속 못 하게 막음
    // IDisposable : "다 쓰면 Dispose() 로 정리해야 하는 클래스" 라는 약속. MainForm.FormClosing 에서 _modbus.Dispose() 호출
    public sealed class ModbusTcpClient : IDisposable
    {
        private TcpClient _client;          // .NET 의 TCP 연결 객체 (연결 안 됐으면 null)
        private NetworkStream _stream;      // 그 연결로 바이트를 쓰고/읽는 통로 (연결 안 됐으면 null)
        private ushort _transactionId;      // 요청 번호. 요청마다 +1 해서 응답이 내 요청의 답인지 확인

        // 한 소켓으로 요청을 동시에 두 개 보내면 응답이 섞입니다.
        // (예: 폴링 중에 사용자가 '초기화' 버튼 클릭) → 한 번에 하나의 요청만 보내도록 잠금.
        // SemaphoreSlim(1, 1) : 입장권이 1장뿐인 문. WaitAsync 로 입장권을 받고 Release 로 반납
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);

        public byte UnitId { get; set; } = 1;        // 장비 번호 (기본 1). MainForm_Load 가 DB 의 UNIT_ID 로 바꿈 → 프레임 [6] 에 들어감
        public int TimeoutMs { get; set; } = 3000;   // 연결/응답 제한 시간 3초

        // 연결되어 있나? → MainForm.PollLoopAsync 가 매 회차 확인해서 false 면 재연결
        public bool IsConnected
        {
            // 객체가 있고 && 소켓이 연결 상태이고 && 통로가 있으면 true
            get { return _client != null && _client.Connected && _stream != null; }
        }

        // ---- 연결 -------------------------------------------------------------------------------
        // 호출: MainForm.btnConnect_Click (처음 연결), MainForm.PollLoopAsync (끊겼을 때 재연결)
        // 실패하면 예외(TimeoutException / SocketException)를 던짐 → 호출한 쪽 catch 가 처리
        public async Task ConnectAsync(string host, int port)
        {
            Disconnect();   // 혹시 남아 있는 이전 연결이 있으면 먼저 정리 (아래 Disconnect 메서드)

            var client = new TcpClient();                          // 새 TCP 객체
            Task connectTask = client.ConnectAsync(host, port);    // 연결 "시작" (await 안 하고 Task 만 받아 둠)

            // TcpClient.ConnectAsync 는 자체 타임아웃이 없어(윈도우 기본 약 20초) 직접 제한합니다.
            // Task.WhenAny(A, B) : A, B 중 "먼저 끝난 것" 을 돌려줌 → 연결 vs 3초 타이머 경주
            // != connectTask : 먼저 끝난 게 연결이 아니라 타이머면 = 3초 안에 연결 못 함
            if (await Task.WhenAny(connectTask, Task.Delay(TimeoutMs)).ConfigureAwait(false) != connectTask)
            {
                client.Close();   // 연결 시도 포기
                // 버려진 Task 의 예외를 관찰 처리해 둠 (UnobservedTaskException 방지)
                // ContinueWith : connectTask 가 나중에 실패하면 실행할 작업. 예외를 한 번 꺼내 봐서 "처리됨" 으로 만듦
                var ignored = connectTask.ContinueWith(t => { var e = t.Exception; },
                                                       TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException(string.Format("{0}:{1} 연결 시간 초과", host, port));   // → btnConnect_Click 의 catch 로
            }

            try
            {
                await connectTask.ConfigureAwait(false); // 연결 실패 예외가 있으면 여기서 던져짐
            }
            catch
            {
                client.Close();   // 실패한 객체 정리하고
                throw;            // 예외는 그대로 위로 (호출한 쪽에서 메시지 표시)
            }

            client.NoDelay = true; // 작은 패킷을 모아 보내지 않고 즉시 전송 (요청-응답형 통신에 유리)
            _client = client;               // 성공했을 때만 필드에 보관 → 실패하면 IsConnected 는 계속 false
            _stream = client.GetStream();   // 바이트를 주고받을 통로 꺼내기
        }

        // ---- 연결 끊기 ---------------------------------------------------------------------------
        // 호출: ConnectAsync(처음에), SendAsync(통신 오류 시), Dispose, MainForm.StopPolling, PollLoopAsync
        public void Disconnect()
        {
            if (_stream != null) { _stream.Dispose(); _stream = null; }   // 통로 닫고 null 로 (IsConnected 가 false 가 됨)
            if (_client != null) { _client.Close(); _client = null; }     // 소켓 닫고 null 로
        }

        // ---- FC04 : 입력 레지스터 읽기 -------------------------------------------------------------
        // 호출: Sig5560Client.ReadAsync
        // startAddress : 시작 주소 / count : 몇 개 / 반환 : 읽은 값 배열 (short = 부호 있는 16비트)
        /// <summary>FC 04 : Read Input Registers (읽기 전용 16비트 레지스터)</summary>
        public async Task<short[]> ReadInputRegistersAsync(ushort startAddress, ushort count)
        {
            if (count < 1 || count > 125)   // Modbus 규격상 한 번에 최대 125개
                // nameof(count) : 변수 이름을 문자열 "count" 로 (이름을 바꾸면 같이 바뀜)
                throw new ArgumentOutOfRangeException(nameof(count), "한 번에 1~125개까지 읽을 수 있습니다.");

            // 요청 PDU 5바이트: [함수코드 0x04][시작주소 상위][하위][개수 상위][하위]
            byte[] pdu =
            {
                0x04,
                Hi(startAddress), Lo(startAddress),   // Hi/Lo : 16비트를 상위/하위 바이트로 쪼개는 도우미 (파일 맨 아래)
                Hi(count),        Lo(count)
            };

            byte[] resp = await SendAsync(pdu).ConfigureAwait(false);   // 보내고 응답 PDU 받기 (아래 SendAsync)

            // 응답 PDU: [FC][바이트 수][데이터...]
            int byteCount = resp[1];                                     // 데이터 바이트 수 (값 1개 = 2바이트)
            if (byteCount != count * 2 || resp.Length < 2 + byteCount)   // 요청한 개수만큼 안 왔으면
                throw new IOException("응답 데이터 길이가 올바르지 않습니다.");

            var values = new short[count];   // 결과 담을 배열
            for (int i = 0; i < count; i++)
            {
                // Big-endian 두 바이트 → 부호 있는 16비트 정수 (음수 온도는 2의 보수로 들어옴: 0xFFB9 = -71)
                // << 8 : 왼쪽으로 8비트 이동 (= ×256, 상위 바이트 자리로) / | : 하위 바이트를 합치기 / (short) : 부호 있는 수로
                values[i] = (short)((resp[2 + i * 2] << 8) | resp[3 + i * 2]);
            }
            return values;
        }

        // ---- FC05 : 코일 1개 쓰기 ---------------------------------------------------------------
        // 호출: Sig5560Client.ResetMinMaxAsync (160번 코일 ON = 최저/최고 초기화)
        /// <summary>FC 05 : Write Single Coil (1비트 쓰기, ON = 0xFF00 / OFF = 0x0000)</summary>
        public async Task WriteSingleCoilAsync(ushort address, bool on)
        {
            // 요청 PDU: [0x05][주소 상위][하위][값 상위: ON=0xFF, OFF=0x00][값 하위: 0x00]
            byte[] pdu =
            {
                0x05,
                Hi(address), Lo(address),
                (byte)(on ? 0xFF : 0x00), 0x00
            };
            await SendAsync(pdu).ConfigureAwait(false); // 정상 응답은 요청을 그대로 되돌려줌(echo)
        }

        // ---- 송수신 핵심 -------------------------------------------------------------------------
        // 호출: 위 ReadInputRegistersAsync, WriteSingleCoilAsync
        // pdu → 앞에 7바이트 헤더를 붙여 보내고 → 응답 헤더/본문을 받아 검사 → 응답 PDU 돌려줌
        /// <summary>요청 PDU 를 MBAP 헤더로 감싸 보내고, 응답 PDU 를 돌려줍니다.</summary>
        private async Task<byte[]> SendAsync(byte[] pdu)
        {
            await _ioLock.WaitAsync().ConfigureAwait(false);   // 입장권 받기. 다른 요청이 진행 중이면 끝날 때까지 기다림
            try
            {
                if (!IsConnected)
                    throw new IOException("센서에 연결되어 있지 않습니다.");

                // unchecked : 65535 에서 +1 하면 0 으로 넘어가도 오류 내지 말라는 뜻 / ++x : 먼저 1 더하고 그 값을 사용
                ushort tid = unchecked(++_transactionId);
                ushort length = (ushort)(pdu.Length + 1);   // 헤더의 Length = UnitId(1) + PDU 길이

                // 프레임 = 헤더 7바이트 + PDU
                var frame = new byte[7 + pdu.Length];
                frame[0] = Hi(tid);    frame[1] = Lo(tid);      // Transaction ID
                frame[2] = 0;          frame[3] = 0;           // Protocol ID
                frame[4] = Hi(length); frame[5] = Lo(length);   // Length
                frame[6] = UnitId;                              // 장비 번호
                Buffer.BlockCopy(pdu, 0, frame, 7, pdu.Length); // pdu 의 0번부터 전부를 frame 의 7번 자리부터 복사

                await _stream.WriteAsync(frame, 0, frame.Length).ConfigureAwait(false);   // 센서로 전송

                byte[] header = await ReadExactAsync(7).ConfigureAwait(false);      // 응답 헤더 7바이트 받기
                ushort respTid = (ushort)((header[0] << 8) | header[1]);            // 응답의 Transaction ID
                ushort respLen = (ushort)((header[4] << 8) | header[5]);            // 응답의 Length

                if (respTid != tid)   // 내가 보낸 요청의 답이 아니면 (이전 응답이 늦게 도착 등)
                    throw new IOException(string.Format("Transaction ID 불일치 (보냄 {0}, 받음 {1})", tid, respTid));
                if (respLen < 2 || respLen > 254)   // 말이 안 되는 길이면
                    throw new IOException("응답 길이가 올바르지 않습니다: " + respLen);

                byte[] respPdu = await ReadExactAsync(respLen - 1).ConfigureAwait(false);   // 본문 받기 (Length 에서 UnitId 1바이트 빼기)

                byte fc = respPdu[0];                          // 응답의 함수 코드
                if ((fc & 0x80) != 0)                          // 최상위 비트가 켜져 있으면 예외 응답
                    // fc & 0x7F : 최상위 비트를 끄면 원래 함수 코드 / respPdu[1] : 예외 코드
                    throw new ModbusException((byte)(fc & 0x7F), respPdu.Length > 1 ? respPdu[1] : (byte)0);
                if (fc != pdu[0])                              // 보낸 함수 코드와 다른 응답이면
                    throw new IOException("요청과 다른 함수 코드의 응답을 받았습니다.");

                return respPdu;   // 정상 응답 PDU
            }
            // 예외 필터: ModbusException 이 "아닌" 예외만 잡음
            //   ModbusException 은 장비가 정상적으로 "안 돼요" 라고 답한 것 → 연결은 멀쩡하니 끊지 않음
            catch (Exception ex) when (!(ex is ModbusException))
            {
                // 통신 중 오류가 나면 스트림 상태를 믿을 수 없으므로 연결을 끊어 둡니다.
                // (남은 쓰레기 바이트가 다음 응답과 섞이는 것을 방지 → 폴링 루프에서 재연결)
                Disconnect();
                throw;   // 예외는 그대로 위로 → Sig5560Client → MainForm.PollLoopAsync 의 catch
            }
            finally
            {
                _ioLock.Release();   // 성공이든 실패든 입장권 반납 (안 하면 다음 요청이 영원히 기다림)
            }
        }

        // ---- 정확히 n 바이트 읽기 -------------------------------------------------------------------
        // 호출: 위 SendAsync (헤더 7바이트, 본문 n바이트)
        /// <summary>
        /// TCP 는 "스트림"이라 한 번의 Read 로 원하는 바이트가 다 온다는 보장이 없습니다.
        /// 원하는 길이를 다 받을 때까지 반복해서 읽습니다.
        /// </summary>
        private async Task<byte[]> ReadExactAsync(int length)
        {
            var buffer = new byte[length];   // 받을 바이트를 담을 배열
            int offset = 0;                  // 지금까지 받은 바이트 수 (= 다음에 채울 위치)
            while (offset < length)          // 다 받을 때까지 반복
            {
                // 남은 만큼(length - offset) 읽기 시작. 실제로는 그보다 적게 읽힐 수도 있음
                Task<int> readTask = _stream.ReadAsync(buffer, offset, length - offset);
                // ConnectAsync 와 같은 방식으로 3초 타이머와 경주 → 센서가 답이 없으면 시간 초과
                if (await Task.WhenAny(readTask, Task.Delay(TimeoutMs)).ConfigureAwait(false) != readTask)
                {
                    var ignored = readTask.ContinueWith(t => { var e = t.Exception; },
                                                        TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException("센서 응답 시간 초과");
                }

                int read = await readTask.ConfigureAwait(false);   // 이번에 실제로 읽은 바이트 수
                if (read == 0)                                     // 0 = 상대방이 연결을 닫음
                    throw new IOException("센서가 연결을 종료했습니다.");
                offset += read;                                    // 받은 만큼 위치 이동
            }
            return buffer;
        }

        // 16비트 값 → 상위 바이트 (>> 8 : 오른쪽으로 8비트 이동)
        private static byte Hi(ushort v) { return (byte)(v >> 8); }
        // 16비트 값 → 하위 바이트 (& 0xFF : 아래 8비트만 남김)
        private static byte Lo(ushort v) { return (byte)(v & 0xFF); }

        // ---- 정리 --------------------------------------------------------------------------------
        // IDisposable 약속 구현. 호출: MainForm.MainForm_FormClosing
        public void Dispose()
        {
            Disconnect();        // 소켓 닫기
            _ioLock.Dispose();   // 잠금 객체 정리
        }
    }
}
