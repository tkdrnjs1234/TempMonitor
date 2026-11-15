using System;
using System.Threading.Tasks;
using TempMonitor.Models;

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
        public const ushort RegTemperature = 0;
        public const ushort RegErrorCode = 8;
        public const ushort RegMinTemperature = 16;
        public const ushort RegMaxTemperature = 32;
        public const ushort CoilResetMinMax = 160;

        private readonly ModbusTcpClient _modbus;

        // 0~32번을 한 번에 읽으면 왕복 1회로 끝납니다(4회 → 1회).
        // 단, 장비가 중간의 빈 주소를 거부(예외코드 2)하면 개별 읽기로 자동 전환합니다.
        private bool _useBlockRead = true;

        public Sig5560Client(ModbusTcpClient modbus)
        {
            _modbus = modbus;
        }

        public async Task<TempReading> ReadAsync()
        {
            short temp, error, min, max;

            if (_useBlockRead)
            {
                try
                {
                    short[] regs = await _modbus.ReadInputRegistersAsync(0, RegMaxTemperature + 1);
                    temp = regs[RegTemperature];
                    error = regs[RegErrorCode];
                    min = regs[RegMinTemperature];
                    max = regs[RegMaxTemperature];
                    return Build(temp, error, min, max);
                }
                catch (ModbusException ex) when (ex.ExceptionCode == 2)
                {
                    _useBlockRead = false; // 이후로는 개별 읽기
                }
            }

            temp = (await _modbus.ReadInputRegistersAsync(RegTemperature, 1))[0];
            error = (await _modbus.ReadInputRegistersAsync(RegErrorCode, 1))[0];
            min = (await _modbus.ReadInputRegistersAsync(RegMinTemperature, 1))[0];
            max = (await _modbus.ReadInputRegistersAsync(RegMaxTemperature, 1))[0];
            return Build(temp, error, min, max);
        }

        public Task ResetMinMaxAsync()
        {
            return _modbus.WriteSingleCoilAsync(CoilResetMinMax, true);
        }

        private static TempReading Build(short temp, short error, short min, short max)
        {
            // 센서는 온도를 10배 정수로 줍니다 (272 → 27.2℃).
            // decimal 로 나누면 이진 부동소수점 오차 없이 정확히 27.2 가 됩니다.
            return new TempReading
            {
                ReadTime = DateTime.Now,
                Temperature = temp / 10m,
                ErrorCode = error,
                MinTemperature = min / 10m,
                MaxTemperature = max / 10m
            };
        }
    }
}
