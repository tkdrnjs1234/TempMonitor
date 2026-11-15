using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sig5560Simulator
{
    /// <summary>
    /// SIG-5560 가상 장치 (Modbus/TCP 슬레이브 시뮬레이터)
    ///
    /// 실제 장치 없이 TempMonitor 를 테스트하기 위한 프로그램입니다.
    /// 실제 장치와 같은 레지스터 맵으로 응답하므로, TempMonitor 쪽 코드는 한 줄도 바꿀 필요가 없습니다.
    /// IP 만 127.0.0.1 로 입력하면 됩니다.
    ///
    ///   FC04 Read Input Registers
    ///     0  : 현재 온도 x10 (int16)       8  : 에러 코드
    ///     16 : 최저 온도 x10               32 : 최고 온도 x10
    ///     48 / 64 / 80 : 현재/최저/최고 FP32 (상위 워드 먼저)
    ///   FC05 Write Single Coil
    ///     160 : FF00 쓰면 최저/최고 리셋
    ///   그 외 Function Code → 예외 01 (Illegal Function)
    ///
    /// 실행: Sig5560Simulator.exe [포트]   (기본 502)
    /// </summary>
    internal static class Program
    {
        private static readonly SensorState State = new SensorState();
        private static int _clientCount;
        private static long _requestCount;

        private static int Main(string[] args)
        {
            int port = 502;
            if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
            {
                Console.WriteLine("사용법: Sig5560Simulator.exe [포트]");
                return 1;
            }

            // Any = 모든 네트워크 카드에서 접속 허용 (다른 PC 에서도 접속 가능).
            // 처음 실행 시 Windows 방화벽 창이 뜨면 '허용' 을 누르세요. (같은 PC 의 127.0.0.1 접속은 방화벽과 무관)
            var listener = new TcpListener(IPAddress.Any, port);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("포트 " + port + " 를 열 수 없습니다: " + ex.Message);
                Console.WriteLine("다른 프로그램이 사용 중이면 다른 포트로 실행하세요. 예) Sig5560Simulator.exe 5020");
                return 1;
            }

            Task.Run(() => AcceptLoopAsync(listener));
            Task.Run(() => SimulateLoopAsync());

            Console.WriteLine("SIG-5560 시뮬레이터 실행 중  (포트 " + port + ", Unit ID 무관)");
            Console.WriteLine("TempMonitor 에서 IP 127.0.0.1 / 포트 " + port + " 로 연결하세요.");
            PrintHelp();

            if (Console.IsInputRedirected)
            {
                // 키보드가 없는 환경(자동 테스트 등) → 그냥 계속 서비스
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                KeyLoop();
            }

            listener.Stop();
            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("  ↑ / ↓   목표 온도 +1 / -1 ℃ (온도는 목표값으로 서서히 움직임)");
            Console.WriteLine("  W       목표 35 ℃ (경고 테스트)      N  정상 복귀 (목표 25 ℃, 에러 해제)");
            Console.WriteLine("  1 ~ 4   에러 코드 발생 (1 HW초기화 / 2 HW동작 / 3 센서 단선 / 4 측정 실패)");
            Console.WriteLine("  0       에러 해제");
            Console.WriteLine("  T       무응답 모드 토글 (타임아웃 → 자동 재연결 테스트)");
            Console.WriteLine("  R       최저/최고 리셋       H  도움말       Q  종료");
            Console.WriteLine();
        }

        // ───────────────────────── 키보드 조작 ─────────────────────────

        private static void KeyLoop()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.OemPlus:
                    case ConsoleKey.Add:
                        State.ChangeTarget(+1.0); break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.OemMinus:
                    case ConsoleKey.Subtract:
                        State.ChangeTarget(-1.0); break;
                    case ConsoleKey.W: State.SetTarget(35.0); break;
                    case ConsoleKey.N: State.SetTarget(25.0); State.SetError(0); break;
                    case ConsoleKey.D0: case ConsoleKey.NumPad0: State.SetError(0); break;
                    case ConsoleKey.D1: case ConsoleKey.NumPad1: State.SetError(1); break;
                    case ConsoleKey.D2: case ConsoleKey.NumPad2: State.SetError(2); break;
                    case ConsoleKey.D3: case ConsoleKey.NumPad3: State.SetError(3); break;
                    case ConsoleKey.D4: case ConsoleKey.NumPad4: State.SetError(4); break;
                    case ConsoleKey.T: State.NoResponse = !State.NoResponse; break;
                    case ConsoleKey.R: State.ResetMinMax(); Log("최저/최고 리셋 (키보드)"); break;
                    case ConsoleKey.H: PrintHelp(); continue;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        return;
                    default:
                        continue;
                }
                Log(State.Describe());
            }
        }

        // ───────────────────────── 온도 변화 시뮬레이션 ─────────────────────────

        private static async Task SimulateLoopAsync()
        {
            while (true)
            {
                State.Step();
                // 창 제목에 현재 상태를 실시간 표시 (로그가 쌓여도 상태는 항상 보이게)
                try
                {
                    Console.Title = "SIG-5560 시뮬레이터 | " + State.Describe()
                                    + " | 접속 " + Volatile.Read(ref _clientCount)
                                    + " | 요청 " + Interlocked.Read(ref _requestCount);
                }
                catch (IOException) { }
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        // ───────────────────────── Modbus/TCP 서버 ─────────────────────────

        private static async Task AcceptLoopAsync(TcpListener listener)
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) { return; }
                catch (SocketException) { return; }

                // 접속마다 별도 Task → 여러 클라이언트 동시 접속 가능
                var _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string remote = client.Client.RemoteEndPoint.ToString();
            Interlocked.Increment(ref _clientCount);
            Log("접속: " + remote);

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    var header = new byte[7];
                    while (true)
                    {
                        // MBAP 헤더 7바이트: TID(2) PID(2) Length(2) UnitId(1)
                        if (!await ReadExactAsync(stream, header, 7)) break;

                        int length = (header[4] << 8) | header[5];   // UnitId + PDU 의 길이
                        if (length < 2 || length > 254) break;       // 비정상 프레임 → 연결 끊기

                        var pdu = new byte[length - 1];
                        if (!await ReadExactAsync(stream, pdu, pdu.Length)) break;

                        Interlocked.Increment(ref _requestCount);
                        if (State.NoResponse) continue;              // 요청은 받되 응답하지 않음 → 클라이언트 타임아웃

                        byte[] resp = Process(pdu);

                        // 응답 = 요청 헤더의 TID/PID/UnitId 그대로 + 새 Length + 응답 PDU
                        var frame = new byte[7 + resp.Length];
                        Array.Copy(header, frame, 4);
                        frame[4] = (byte)((resp.Length + 1) >> 8);
                        frame[5] = (byte)(resp.Length + 1);
                        frame[6] = header[6];
                        Array.Copy(resp, 0, frame, 7, resp.Length);

                        await stream.WriteAsync(frame, 0, frame.Length).ConfigureAwait(false);
                    }
                }
            }
            catch (IOException) { }               // 상대가 연결을 끊음
            catch (ObjectDisposedException) { }
            finally
            {
                Interlocked.Decrement(ref _clientCount);
                Log("해제: " + remote);
            }
        }

        /// <summary>TCP 는 스트림이라 한 번에 다 안 올 수 있음 → 원하는 길이만큼 반복해서 읽기. 상대가 끊으면 false.</summary>
        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, read, count - read).ConfigureAwait(false);
                if (n == 0) return false;
                read += n;
            }
            return true;
        }

        private static byte[] Process(byte[] pdu)
        {
            byte fc = pdu[0];
            switch (fc)
            {
                case 0x04: // Read Input Registers
                {
                    if (pdu.Length != 5) return ExceptionPdu(fc, 3);
                    int start = (pdu[1] << 8) | pdu[2];
                    int count = (pdu[3] << 8) | pdu[4];
                    if (count < 1 || count > 125) return ExceptionPdu(fc, 3);   // 03 Illegal Data Value

                    short[] regs = State.ReadInputRegisters(start, count);
                    if (regs == null) return ExceptionPdu(fc, 2);              // 02 Illegal Data Address

                    var resp = new byte[2 + count * 2];
                    resp[0] = fc;
                    resp[1] = (byte)(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        resp[2 + i * 2] = (byte)(regs[i] >> 8);   // Big-endian
                        resp[3 + i * 2] = (byte)regs[i];
                    }
                    return resp;
                }

                case 0x05: // Write Single Coil
                {
                    if (pdu.Length != 5) return ExceptionPdu(fc, 3);
                    int address = (pdu[1] << 8) | pdu[2];
                    int value = (pdu[3] << 8) | pdu[4];
                    if (value != 0xFF00 && value != 0x0000) return ExceptionPdu(fc, 3);
                    if (address != SensorState.CoilResetMinMax) return ExceptionPdu(fc, 2);

                    if (value == 0xFF00)
                    {
                        State.ResetMinMax();
                        Log("최저/최고 리셋 (FC05 요청)");
                    }
                    return pdu;   // 정상 응답 = 요청 그대로 에코
                }

                default:
                    return ExceptionPdu(fc, 1);   // 01 Illegal Function
            }
        }

        /// <summary>예외 응답: FC 에 0x80 비트를 켜고 예외 코드 1바이트</summary>
        private static byte[] ExceptionPdu(byte fc, byte code)
        {
            return new[] { (byte)(fc | 0x80), code };
        }

        private static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        }
    }

    /// <summary>
    /// 가상 센서 상태. 키보드 스레드 / 통신 Task / 시뮬레이션 Task 가 동시에 접근하므로 lock 으로 보호합니다.
    /// </summary>
    internal sealed class SensorState
    {
        public const int CoilResetMinMax = 160;
        private const int RegisterCount = 96;   // 0 ~ 95 (FP32 최고 온도 80,81 까지 포함)

        private readonly object _lock = new object();
        private readonly Random _random = new Random();

        private double _temp = 25.0;
        private double _target = 25.0;
        private double _min = 25.0;
        private double _max = 25.0;
        private int _error;
        private volatile bool _noResponse;

        public bool NoResponse
        {
            get { return _noResponse; }
            set { _noResponse = value; }
        }

        public void SetTarget(double target)
        {
            lock (_lock) _target = target;
        }

        public void ChangeTarget(double delta)
        {
            lock (_lock) _target += delta;
        }

        public void SetError(int code)
        {
            lock (_lock) _error = code;
        }

        public void ResetMinMax()
        {
            lock (_lock) { _min = _temp; _max = _temp; }
        }

        /// <summary>1초마다 호출: 목표값 쪽으로 20% 씩 다가가고 ±0.1 정도 노이즈 → 실제 센서처럼 부드럽게 변함</summary>
        public void Step()
        {
            lock (_lock)
            {
                if (_error != 0) return;   // 에러 중에는 측정 안 됨
                double noise = (_random.NextDouble() - 0.5) * 0.2;
                _temp = Math.Round(_temp + (_target - _temp) * 0.2 + noise, 1);
                if (_temp < _min) _min = _temp;
                if (_temp > _max) _max = _temp;
            }
        }

        /// <summary>범위를 벗어나면 null → 호출 측에서 예외 02 응답</summary>
        public short[] ReadInputRegisters(int start, int count)
        {
            if (start < 0 || start + count > RegisterCount) return null;

            var all = new short[RegisterCount];
            lock (_lock)
            {
                all[0] = _error == 0 ? ToX10(_temp) : (short)0;
                all[8] = (short)_error;
                all[16] = ToX10(_min);
                all[32] = ToX10(_max);
                WriteFloat(all, 48, _error == 0 ? (float)_temp : 0f);
                WriteFloat(all, 64, (float)_min);
                WriteFloat(all, 80, (float)_max);
            }

            var result = new short[count];
            Array.Copy(all, start, result, 0, count);
            return result;
        }

        public string Describe()
        {
            lock (_lock)
            {
                string err = _error == 0 ? "정상" : "에러 " + _error;
                return string.Format("온도 {0:0.0}℃ (목표 {1:0.0}) 최저 {2:0.0} 최고 {3:0.0} | {4}{5}",
                    _temp, _target, _min, _max, err, _noResponse ? " | 무응답 모드" : "");
            }
        }

        private static short ToX10(double value)
        {
            return (short)Math.Round(value * 10, MidpointRounding.AwayFromZero);
        }

        /// <summary>FP32 를 레지스터 2개에 저장 (상위 워드가 앞 주소)</summary>
        private static void WriteFloat(short[] regs, int address, float value)
        {
            byte[] b = BitConverter.GetBytes(value);   // x86 = little-endian
            regs[address] = (short)((b[3] << 8) | b[2]);
            regs[address + 1] = (short)((b[1] << 8) | b[0]);
        }
    }
}
