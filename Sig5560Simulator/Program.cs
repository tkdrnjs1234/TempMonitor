// ============================================================================================
//  Sig5560Simulator/Program.cs : 진짜 센서 없이 테스트하기 위한 "가짜 SIG-5560" 콘솔 프로그램
// --------------------------------------------------------------------------------------------
//  TempMonitor 는 "Modbus 클라이언트(마스터)" = 요청하는 쪽
//  이 시뮬레이터는 "Modbus 서버(슬레이브)"   = 요청을 받고 답하는 쪽 (진짜 센서 역할)
//
//  TempMonitor 의 ModbusTcpClient 가 보낸 요청 바이트를 받아서, 진짜 SIG-5560 과 똑같은 모양으로 답합니다.
//  그래서 TempMonitor 코드는 그대로 두고 IP 만 127.0.0.1(내 PC) 로 바꾸면 테스트 가능.
//
//  이 파일의 구성
//    class Program     : 프로그램 시작, 키보드 조작, TCP 서버(접속 받기, 요청 처리)
//    class SensorState : 가상 센서의 상태(온도, 목표, 최저, 최고, 에러)와 레지스터 값 만들기
//
//  동시에 도는 작업 3개
//    ① KeyLoop          (메인 스레드)   : 키보드 입력 → 상태 바꾸기
//    ② SimulateLoopAsync (Task)         : 1초마다 온도를 목표값 쪽으로 조금씩 움직이기
//    ③ AcceptLoopAsync   (Task)         : 접속 받기 → 접속마다 HandleClientAsync (Task) 하나씩
//  → 셋이 같은 SensorState 를 만지므로 SensorState 안에서 lock 으로 보호
// ============================================================================================

using System;                  // Console, Math, Random, Array, BitConverter
using System.IO;               // IOException
using System.Net;              // IPAddress
using System.Net.Sockets;      // TcpListener(접속 받는 서버), TcpClient, NetworkStream
using System.Threading;        // Thread, Interlocked, Volatile, Timeout
using System.Threading.Tasks;  // Task

namespace Sig5560Simulator   // TempMonitor 와 다른 별도 프로젝트(exe) 의 이름 공간
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
        // 가상 센서 상태 객체 (아래 SensorState 클래스). 프로그램 전체가 공유하므로 static, 한 번 만들면 안 바뀌니 readonly
        private static readonly SensorState State = new SensorState();
        private static int _clientCount;      // 지금 접속 중인 클라이언트 수 (창 제목에 표시)
        private static long _requestCount;    // 지금까지 받은 요청 수 (창 제목에 표시)

        // ---- 시작점 ---------------------------------------------------------------------------
        // int Main : 종료 코드를 돌려줌 (0 = 정상, 1 = 오류)
        // string[] args : 실행할 때 뒤에 붙인 인수들. 예) Sig5560Simulator.exe 5020 → args[0] = "5020"
        private static int Main(string[] args)
        {
            int port = 502;   // Modbus TCP 기본 포트
            // 인수가 있는데 && (숫자가 아니거나 || 범위 밖) 이면 사용법 출력 후 종료
            // TryParse 성공하면 port 에 그 값이 들어감
            if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
            {
                Console.WriteLine("사용법: Sig5560Simulator.exe [포트]");
                return 1;
            }

            // Any = 모든 네트워크 카드에서 접속 허용 (다른 PC 에서도 접속 가능).
            // 처음 실행 시 Windows 방화벽 창이 뜨면 '허용' 을 누르세요. (같은 PC 의 127.0.0.1 접속은 방화벽과 무관)
            var listener = new TcpListener(IPAddress.Any, port);   // 접속을 기다리는 서버 객체
            try
            {
                listener.Start();   // 포트 열고 접속 대기 시작
            }
            catch (SocketException ex)   // 포트가 이미 사용 중 등
            {
                Console.WriteLine("포트 " + port + " 를 열 수 없습니다: " + ex.Message);
                Console.WriteLine("다른 프로그램이 사용 중이면 다른 포트로 실행하세요. 예) Sig5560Simulator.exe 5020");
                return 1;
            }

            // Task.Run(() => 메서드()) : 그 메서드를 백그라운드에서 따로 실행 (기다리지 않고 바로 다음 줄로)
            Task.Run(() => AcceptLoopAsync(listener));   // ③ 접속 받기 루프
            Task.Run(() => SimulateLoopAsync());         // ② 온도 변화 루프

            Console.WriteLine("SIG-5560 시뮬레이터 실행 중  (포트 " + port + ", Unit ID 무관)");
            Console.WriteLine("TempMonitor 에서 IP 127.0.0.1 / 포트 " + port + " 로 연결하세요.");
            PrintHelp();   // 키 사용법 출력 (아래 메서드)

            if (Console.IsInputRedirected)   // 키보드 입력이 없는 환경이면 (자동 테스트에서 실행 등)
            {
                // 키보드가 없는 환경(자동 테스트 등) → 그냥 계속 서비스
                Thread.Sleep(Timeout.Infinite);   // 영원히 대기 (백그라운드 Task 들은 계속 돎)
            }
            else
            {
                KeyLoop();   // ① 키보드 루프. Q/ESC 를 누르면 돌아옴
            }

            listener.Stop();   // 서버 닫기
            return 0;          // 정상 종료
        }

        // 키 사용법 출력. 부르는 곳: Main, KeyLoop(H 키)
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

        // ① 키보드 입력 처리 루프. 키에 따라 SensorState 의 메서드를 호출
        private static void KeyLoop()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);   // 키 하나 누를 때까지 대기. true = 화면에 글자 안 찍음
                switch (key.Key)
                {
                    // case 여러 개를 붙여 쓰면 "이 중 하나면" 같은 처리
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.OemPlus:
                    case ConsoleKey.Add:
                        State.ChangeTarget(+1.0); break;      // 목표 온도 +1
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.OemMinus:
                    case ConsoleKey.Subtract:
                        State.ChangeTarget(-1.0); break;      // 목표 온도 -1
                    case ConsoleKey.W: State.SetTarget(35.0); break;                        // 경고 테스트 (기준 30℃ 넘김)
                    case ConsoleKey.N: State.SetTarget(25.0); State.SetError(0); break;     // 정상 복귀
                    case ConsoleKey.D0: case ConsoleKey.NumPad0: State.SetError(0); break;  // D0 = 숫자 0 키, NumPad0 = 키패드 0
                    case ConsoleKey.D1: case ConsoleKey.NumPad1: State.SetError(1); break;  // 에러 1
                    case ConsoleKey.D2: case ConsoleKey.NumPad2: State.SetError(2); break;
                    case ConsoleKey.D3: case ConsoleKey.NumPad3: State.SetError(3); break;
                    case ConsoleKey.D4: case ConsoleKey.NumPad4: State.SetError(4); break;
                    case ConsoleKey.T: State.NoResponse = !State.NoResponse; break;          // 무응답 모드 켜기/끄기 (! = 반대로)
                    case ConsoleKey.R: State.ResetMinMax(); Log("최저/최고 리셋 (키보드)"); break;
                    case ConsoleKey.H: PrintHelp(); continue;   // continue : 아래 Log 를 건너뛰고 다음 키 대기
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        return;                                 // KeyLoop 종료 → Main 으로 돌아가 프로그램 끝
                    default:
                        continue;                               // 모르는 키는 무시
                }
                Log(State.Describe());   // 바뀐 상태를 한 줄 출력
            }
        }

        // ───────────────────────── 온도 변화 시뮬레이션 ─────────────────────────

        // ② 1초마다 온도를 조금씩 움직이는 루프
        private static async Task SimulateLoopAsync()
        {
            while (true)
            {
                State.Step();   // 온도를 목표값 쪽으로 한 걸음 (SensorState.Step)
                // 창 제목에 현재 상태를 실시간 표시 (로그가 쌓여도 상태는 항상 보이게)
                try
                {
                    // Volatile.Read / Interlocked.Read : 다른 스레드가 바꾸는 값을 안전하게 읽기
                    Console.Title = "SIG-5560 시뮬레이터 | " + State.Describe()
                                    + " | 접속 " + Volatile.Read(ref _clientCount)
                                    + " | 요청 " + Interlocked.Read(ref _requestCount);
                }
                catch (IOException) { }   // 콘솔 창이 없는 환경이면 제목 설정 실패 → 무시
                await Task.Delay(1000).ConfigureAwait(false);   // 1초 대기
            }
        }

        // ───────────────────────── Modbus/TCP 서버 ─────────────────────────

        // ③ 접속 받기 루프. 부르는 곳: Main (Task.Run)
        private static async Task AcceptLoopAsync(TcpListener listener)
        {
            while (true)
            {
                TcpClient client;   // 접속해 온 상대 (TempMonitor)
                try
                {
                    // 누군가 접속할 때까지 기다렸다가 그 연결을 돌려줌 (TempMonitor 의 ConnectAsync 가 여기에 도착)
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException) { return; }   // 서버가 닫힘 → 루프 종료
                catch (SocketException) { return; }

                // 접속마다 별도 Task → 여러 클라이언트 동시 접속 가능
                var _ = Task.Run(() => HandleClientAsync(client));   // _ : 결과를 기다리지 않음
            }
        }

        // 접속 하나를 담당: 요청 받기 → 처리 → 응답 보내기를 연결이 끊길 때까지 반복
        private static async Task HandleClientAsync(TcpClient client)
        {
            string remote = client.Client.RemoteEndPoint.ToString();   // 상대 주소 "127.0.0.1:54321" (로그용)
            Interlocked.Increment(ref _clientCount);                    // 접속 수 +1 (여러 스레드가 동시에 바꿔도 안전하게)
            Log("접속: " + remote);

            try
            {
                using (client)                                          // 끝나면 연결 닫기
                using (NetworkStream stream = client.GetStream())       // 바이트를 주고받을 통로
                {
                    var header = new byte[7];   // 요청 헤더를 담을 배열 (매번 재사용)
                    while (true)
                    {
                        // MBAP 헤더 7바이트: TID(2) PID(2) Length(2) UnitId(1)
                        if (!await ReadExactAsync(stream, header, 7)) break;   // 상대가 끊었으면 루프 종료

                        int length = (header[4] << 8) | header[5];   // UnitId + PDU 의 길이
                        if (length < 2 || length > 254) break;       // 비정상 프레임 → 연결 끊기

                        var pdu = new byte[length - 1];              // 본문(PDU) 길이 = Length - UnitId 1바이트
                        if (!await ReadExactAsync(stream, pdu, pdu.Length)) break;

                        Interlocked.Increment(ref _requestCount);    // 요청 수 +1
                        if (State.NoResponse) continue;              // 요청은 받되 응답하지 않음 → 클라이언트 타임아웃

                        byte[] resp = Process(pdu);                  // 요청 처리 → 응답 PDU (아래 메서드)

                        // 응답 = 요청 헤더의 TID/PID/UnitId 그대로 + 새 Length + 응답 PDU
                        var frame = new byte[7 + resp.Length];
                        Array.Copy(header, frame, 4);                // TID(2) + PID(2) 4바이트 복사 → TempMonitor 가 요청/응답 짝을 확인
                        frame[4] = (byte)((resp.Length + 1) >> 8);   // 새 Length 상위 바이트
                        frame[5] = (byte)(resp.Length + 1);          // 새 Length 하위 바이트
                        frame[6] = header[6];                        // UnitId 그대로
                        Array.Copy(resp, 0, frame, 7, resp.Length);  // 7번 자리부터 응답 PDU

                        await stream.WriteAsync(frame, 0, frame.Length).ConfigureAwait(false);   // 응답 보내기
                    }
                }
            }
            catch (IOException) { }               // 상대가 연결을 끊음
            catch (ObjectDisposedException) { }
            finally
            {
                Interlocked.Decrement(ref _clientCount);   // 접속 수 -1
                Log("해제: " + remote);
            }
        }

        // TempMonitor 의 ModbusTcpClient.ReadExactAsync 와 같은 역할 (여기는 시간 제한 없음)
        /// <summary>TCP 는 스트림이라 한 번에 다 안 올 수 있음 → 원하는 길이만큼 반복해서 읽기. 상대가 끊으면 false.</summary>
        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int read = 0;                  // 지금까지 읽은 바이트 수
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, read, count - read).ConfigureAwait(false);   // 남은 만큼 읽기
                if (n == 0) return false;  // 0 = 상대가 연결을 닫음
                read += n;
            }
            return true;
        }

        // 요청 PDU → 응답 PDU. 부르는 곳: HandleClientAsync
        private static byte[] Process(byte[] pdu)
        {
            byte fc = pdu[0];   // 함수 코드
            switch (fc)
            {
                case 0x04: // Read Input Registers
                {
                    if (pdu.Length != 5) return ExceptionPdu(fc, 3);   // FC04 요청은 항상 5바이트
                    int start = (pdu[1] << 8) | pdu[2];                // 시작 주소 (두 바이트 합치기)
                    int count = (pdu[3] << 8) | pdu[4];                // 개수
                    if (count < 1 || count > 125) return ExceptionPdu(fc, 3);   // 03 Illegal Data Value

                    short[] regs = State.ReadInputRegisters(start, count);    // 가상 센서의 레지스터 값 (범위 밖이면 null)
                    if (regs == null) return ExceptionPdu(fc, 2);              // 02 Illegal Data Address

                    // 응답 PDU: [FC][바이트 수][값1 상위][값1 하위]...
                    var resp = new byte[2 + count * 2];
                    resp[0] = fc;
                    resp[1] = (byte)(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        resp[2 + i * 2] = (byte)(regs[i] >> 8);   // Big-endian (상위 바이트 먼저)
                        resp[3 + i * 2] = (byte)regs[i];          // 하위 바이트 (byte 로 바꾸면 아래 8비트만 남음)
                    }
                    return resp;
                }

                case 0x05: // Write Single Coil
                {
                    if (pdu.Length != 5) return ExceptionPdu(fc, 3);
                    int address = (pdu[1] << 8) | pdu[2];   // 코일 주소
                    int value = (pdu[3] << 8) | pdu[4];     // 값 (ON = 0xFF00, OFF = 0x0000)
                    if (value != 0xFF00 && value != 0x0000) return ExceptionPdu(fc, 3);       // 규격 밖의 값
                    if (address != SensorState.CoilResetMinMax) return ExceptionPdu(fc, 2);   // 160번 말고는 없는 주소

                    if (value == 0xFF00)   // ON 이면 리셋
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

        // TempMonitor 쪽에서는 ModbusTcpClient.SendAsync 가 이 응답을 보고 ModbusException 을 던짐
        /// <summary>예외 응답: FC 에 0x80 비트를 켜고 예외 코드 1바이트</summary>
        private static byte[] ExceptionPdu(byte fc, byte code)
        {
            return new[] { (byte)(fc | 0x80), code };   // | 0x80 : 최상위 비트 켜기 (0x04 → 0x84)
        }

        // 시각과 함께 한 줄 출력
        private static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        }
    }

    /// <summary>
    /// 가상 센서 상태. 키보드 스레드 / 통신 Task / 시뮬레이션 Task 가 동시에 접근하므로 lock 으로 보호합니다.
    /// </summary>
    // sealed : 상속 금지
    internal sealed class SensorState
    {
        public const int CoilResetMinMax = 160;   // 최저/최고 리셋 코일 주소 (Program.Process 에서 비교)
        private const int RegisterCount = 96;   // 0 ~ 95 (FP32 최고 온도 80,81 까지 포함)

        // lock 에 쓸 열쇠 객체. lock (_lock) { ... } 안은 한 번에 한 스레드만 들어갈 수 있음
        private readonly object _lock = new object();
        private readonly Random _random = new Random();   // 노이즈용 난수 생성기

        private double _temp = 25.0;     // 현재 온도
        private double _target = 25.0;   // 목표 온도 (키보드로 바꿈)
        private double _min = 25.0;      // 최저
        private double _max = 25.0;      // 최고
        private int _error;              // 에러 코드 (0 = 정상)
        private volatile bool _noResponse;   // 무응답 모드. volatile : 다른 스레드가 바꾼 값을 항상 최신으로 읽게

        // 무응답 모드 속성 (get/set 을 직접 구현해 _noResponse 필드에 연결)
        public bool NoResponse
        {
            get { return _noResponse; }
            set { _noResponse = value; }   // value = 대입한 값
        }

        // 목표 온도 지정 (W, N 키)
        public void SetTarget(double target)
        {
            lock (_lock) _target = target;   // lock 뒤 문장 하나는 중괄호 생략 가능
        }

        // 목표 온도 더하기/빼기 (↑ ↓ 키)
        public void ChangeTarget(double delta)
        {
            lock (_lock) _target += delta;
        }

        // 에러 코드 설정 (0~4 키)
        public void SetError(int code)
        {
            lock (_lock) _error = code;
        }

        // 최저/최고를 현재 온도로 (R 키, FC05 요청)
        public void ResetMinMax()
        {
            lock (_lock) { _min = _temp; _max = _temp; }
        }

        // 부르는 곳: Program.SimulateLoopAsync
        /// <summary>1초마다 호출: 목표값 쪽으로 20% 씩 다가가고 ±0.1 정도 노이즈 → 실제 센서처럼 부드럽게 변함</summary>
        public void Step()
        {
            lock (_lock)
            {
                if (_error != 0) return;   // 에러 중에는 측정 안 됨
                // NextDouble() : 0~1 난수 → -0.5 하면 -0.5~0.5 → × 0.2 하면 -0.1~0.1
                double noise = (_random.NextDouble() - 0.5) * 0.2;
                // (목표 - 현재) × 0.2 만큼 다가감 + 노이즈, 소수 1자리로 반올림
                _temp = Math.Round(_temp + (_target - _temp) * 0.2 + noise, 1);
                if (_temp < _min) _min = _temp;   // 최저 갱신
                if (_temp > _max) _max = _temp;   // 최고 갱신
            }
        }

        // 부르는 곳: Program.Process (FC04)
        /// <summary>범위를 벗어나면 null → 호출 측에서 예외 02 응답</summary>
        public short[] ReadInputRegisters(int start, int count)
        {
            if (start < 0 || start + count > RegisterCount) return null;   // 0~95 밖을 요청하면 null

            var all = new short[RegisterCount];   // 레지스터 96칸 전부 (나머지 칸은 0)
            lock (_lock)
            {
                all[0] = _error == 0 ? ToX10(_temp) : (short)0;   // 0번: 현재 온도 ×10 (에러면 0)
                all[8] = (short)_error;                           // 8번: 에러 코드
                all[16] = ToX10(_min);                            // 16번: 최저 ×10
                all[32] = ToX10(_max);                            // 32번: 최고 ×10
                WriteFloat(all, 48, _error == 0 ? (float)_temp : 0f);   // 48,49번: 현재 온도 실수(FP32)
                WriteFloat(all, 64, (float)_min);                       // 64,65번
                WriteFloat(all, 80, (float)_max);                       // 80,81번
            }

            var result = new short[count];
            Array.Copy(all, start, result, 0, count);   // 요청한 범위(start 부터 count 개)만 잘라서
            return result;
        }

        // 현재 상태 한 줄 설명 (로그, 창 제목에 사용)
        public string Describe()
        {
            lock (_lock)
            {
                string err = _error == 0 ? "정상" : "에러 " + _error;
                return string.Format("온도 {0:0.0}℃ (목표 {1:0.0}) 최저 {2:0.0} 최고 {3:0.0} | {4}{5}",
                    _temp, _target, _min, _max, err, _noResponse ? " | 무응답 모드" : "");
            }
        }

        // 27.2 → 272 (센서는 온도를 10배 정수로 줌). AwayFromZero : 0.5 는 항상 올림 (반올림 규칙)
        private static short ToX10(double value)
        {
            return (short)Math.Round(value * 10, MidpointRounding.AwayFromZero);
        }

        /// <summary>FP32 를 레지스터 2개에 저장 (상위 워드가 앞 주소)</summary>
        private static void WriteFloat(short[] regs, int address, float value)
        {
            byte[] b = BitConverter.GetBytes(value);   // x86 = little-endian
            // float 4바이트를 16비트 2칸으로: 상위 2바이트(b[3], b[2]) → 앞 주소, 하위 2바이트(b[1], b[0]) → 다음 주소
            regs[address] = (short)((b[3] << 8) | b[2]);
            regs[address + 1] = (short)((b[1] << 8) | b[0]);
        }
    }
}
