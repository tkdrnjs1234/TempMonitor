// ============================================================================================
//  Comm/Sig5560Client.cs : SIG-5560 온도센서 "전용" 통역사
// --------------------------------------------------------------------------------------------
//  ModbusTcpClient 는 "몇 번 주소를 몇 개 읽어 줘" 같은 프로토콜만 알고, 그게 온도인지는 모릅니다.
//  이 클래스는 "SIG-5560 은 0번이 온도, 8번이 에러코드..." 라는 장비 정보(레지스터 맵)를 알고,
//  읽은 숫자를 TempReading(온도 그릇) 으로 바꿔 줍니다.
//
//  호출 흐름
//    MainForm.PollLoopAsync      → _sensor.ReadAsync()        → _modbus.ReadInputRegistersAsync()
//    MainForm.btnResetMinMax     → _sensor.ResetMinMaxAsync() → _modbus.WriteSingleCoilAsync()
//
//  MainForm 생성자에서  _sensor = new Sig5560Client(_modbus);  로 만들어짐
//  → MainForm 이 연결한 "바로 그" ModbusTcpClient 를 받아서 같이 씀
// ============================================================================================

using System;                  // DateTime
using System.Threading.Tasks;  // Task
using TempMonitor.Models;      // TempReading (Models/TempReading.cs)

namespace TempMonitor.Comm
{
    /// <summary>
    /// SIG-5560 전용 래퍼.
    /// ModbusTcpClient 는 "프로토콜"만 알고, 이 클래스는 "이 장비의 레지스터 맵"만 압니다.
    /// 나중에 다른 센서를 붙이면 이 클래스만 하나 더 만들면 됩니다.
    ///
    /// 레지스터 맵 (SIG-5560 사용자 설명서 5장)
    ///   FC04  0  : 현재 온도 x10 (int16)
    ///   FC04  8  : 에러 코드 (0 없음 / 1 HW 초기화 / 2 HW 런타임 / 3 센서 끊김 / 4 측정 실패)
    ///   FC04 16  : 최저 온도 x10
    ///   FC04 32  : 최고 온도 x10
    ///   FC05 160 : 최저/최고 온도 초기화 코일 (1 쓰면 초기화)
    /// </summary>
    public class Sig5560Client
    {
        // 레지스터(장비 메모리 칸) 번호. 설명서 주소표를 이름 붙인 상수로 옮김
        // ushort : 0~65535 부호 없는 16비트 정수 (Modbus 주소가 16비트라서)
        public const ushort RegTemperature = 0;      // 현재 온도
        public const ushort RegErrorCode = 8;        // 에러 코드
        public const ushort RegMinTemperature = 16;  // 최저 온도
        public const ushort RegMaxTemperature = 32;  // 최고 온도
        public const ushort CoilResetMinMax = 160;   // 최저/최고 초기화 스위치(코일)

        // 실제 통신을 맡길 Modbus 객체 (생성자에서 받음). readonly → 한 번 받으면 다른 객체로 못 바꿈
        private readonly ModbusTcpClient _modbus;

        // 0~32번을 한 번에 읽으면 왕복 1회로 끝납니다(4회 → 1회).
        // 단, 장비가 중간의 빈 주소를 거부(예외코드 2)하면 개별 읽기로 자동 전환합니다.
        private bool _useBlockRead = true;   // true = 한 번에 읽기 시도, false = 4번 따로 읽기 (readonly 아님: 실행 중 바뀜)

        // ---- 생성자 -----------------------------------------------------------------------------
        // 호출: MainForm 생성자  →  new Sig5560Client(_modbus)
        // 직접 new ModbusTcpClient() 를 하지 않고 밖에서 받는 이유:
        //   MainForm 이 _modbus.ConnectAsync() 로 연결한 그 객체를 써야 같은 연결로 통신이 되기 때문
        public Sig5560Client(ModbusTcpClient modbus)
        {
            _modbus = modbus;   // 받은 객체를 필드에 보관
        }

        // ---- 측정값 읽기 -------------------------------------------------------------------------
        // 호출: MainForm.PollLoopAsync (매 읽기 주기마다)
        // 반환: TempReading (온도/최저/최고/에러코드/시각)
        public async Task<TempReading> ReadAsync()
        {
            short temp, error, min, max;   // 센서에서 읽은 원래 숫자 4개 (short = 부호 있는 16비트, 음수 온도 가능)

            if (_useBlockRead)   // 한 번에 읽기 모드면
            {
                try
                {
                    // 0번부터 33개(0~32) 레지스터를 한 번에 읽음 → short[] 배열
                    short[] regs = await _modbus.ReadInputRegistersAsync(0, RegMaxTemperature + 1);
                    temp = regs[RegTemperature];      // regs[0]
                    error = regs[RegErrorCode];       // regs[8]
                    min = regs[RegMinTemperature];    // regs[16]
                    max = regs[RegMaxTemperature];    // regs[32]
                    return Build(temp, error, min, max);   // 숫자 → TempReading 으로 변환해서 반환
                }
                // catch ... when (조건) : 예외 필터. ModbusException 이면서 "예외코드 2(잘못된 주소)" 일 때만 잡음
                // 다른 예외(통신 끊김 등)는 여기서 안 잡히고 그대로 PollLoopAsync 로 올라감
                catch (ModbusException ex) when (ex.ExceptionCode == 2)
                {
                    _useBlockRead = false; // 이후로는 개별 읽기
                }
            }

            // 개별 읽기: 필요한 4개만 하나씩 (왕복 4회)
            // (await ...)[0] : 괄호로 await 결과(길이 1 배열)를 먼저 받고 그 0번째 값
            temp = (await _modbus.ReadInputRegistersAsync(RegTemperature, 1))[0];
            error = (await _modbus.ReadInputRegistersAsync(RegErrorCode, 1))[0];
            min = (await _modbus.ReadInputRegistersAsync(RegMinTemperature, 1))[0];
            max = (await _modbus.ReadInputRegistersAsync(RegMaxTemperature, 1))[0];
            return Build(temp, error, min, max);
        }

        // ---- 최저/최고 초기화 ---------------------------------------------------------------------
        // 호출: MainForm.btnResetMinMax_Click
        // 160번 코일에 ON(1) 을 쓰면 센서가 최저/최고 기록을 지움. 결과 가공이 없어서 Task 를 그대로 넘김
        public Task ResetMinMaxAsync()
        {
            return _modbus.WriteSingleCoilAsync(CoilResetMinMax, true);
        }

        // ---- 숫자 → TempReading 변환 -------------------------------------------------------------
        // 위 ReadAsync 에서만 사용. 필드를 안 쓰므로 static
        private static TempReading Build(short temp, short error, short min, short max)
        {
            // 센서는 온도를 10배 정수로 줍니다 (272 → 27.2℃).
            // decimal 로 나누면 이진 부동소수점 오차 없이 정확히 27.2 가 됩니다.
            return new TempReading
            {
                ReadTime = DateTime.Now,         // 읽은 시각 = 지금
                Temperature = temp / 10m,        // 10m : decimal 10. short ÷ decimal → decimal
                ErrorCode = error,               // short → int 자동 변환
                MinTemperature = min / 10m,
                MaxTemperature = max / 10m
            };
        }
    }
}
