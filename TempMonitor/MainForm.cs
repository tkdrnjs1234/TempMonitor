// ============================================================================================
//  MainForm.cs : 메인 화면의 "동작" 코드 (모니터링 / 조회 / 설정 / 로그아웃)
// --------------------------------------------------------------------------------------------
//  partial class 라서 두 파일에 나뉘어 있습니다.
//    MainForm.cs          (이 파일) : 실행 중에 바뀌는 값과 동작
//    MainForm.Designer.cs (자동 생성) : 컨트롤 선언(txtIp, dgvHistory, chartTemp, tmrClock ...),
//                                      글자/색/그리드 컬럼/차트 시리즈, 이벤트 연결(Click, Load, FormClosing ...)
//
//  누가 띄우나? → Program.Main 에서  new MainForm() → Application.Run(main)
//
//  이 파일이 부르는 다른 파일들 (필드로 객체를 하나씩 만들어 보관)
//    통신   : Comm/ModbusTcpClient.cs (_modbus), Comm/Sig5560Client.cs (_sensor)
//    DB     : Data/SensorRepository, TempLogRepository, AlertRepository, KakaoLogRepository, ConfigRepository
//    서비스 : Services/KakaoService.cs (_kakao)
//    공용   : Session.cs (로그인 사용자), Models (AppConfig, TempReading, TempStatus, UserInfo), Data/DbHelper.cs
//
//  큰 흐름
//    ① 화면 뜸       MainForm_Load    → DB 연결 확인, 설정/센서 정보 읽기
//    ② [연결]        btnConnect_Click → 센서 연결 → PollLoopAsync 시작
//    ③ 폴링 루프     PollLoopAsync    → 읽기 → HandleReadingAsync(화면/차트/경고/저장) → 대기 → 반복
//    ④ 조회 탭       btnSearch / btnAlertSearch / btnKakaoSearch
//    ⑤ 설정 탭       btnSaveSetting_Click
//    ⑥ 로그아웃/종료 btnLogout_Click, MainForm_FormClosing
// ============================================================================================

using System;                                       // DateTime, TimeSpan, Math, Exception, EventArgs, Func
using System.Data;                                  // DataTable, DataRowView
using System.Drawing;                               // Color, Region, SystemColors
using System.Drawing.Drawing2D;                     // GraphicsPath (램프를 동그랗게 자를 때)
using System.Threading;                             // CancellationTokenSource, CancellationToken
using System.Threading.Tasks;                       // Task
using System.Windows.Forms;                         // Form, DataGridView, MessageBox, Cursors ...
using System.Windows.Forms.DataVisualization.Charting;   // Series (차트)
using TempMonitor.Comm;                             // ModbusTcpClient, Sig5560Client
using TempMonitor.Data;                             // DbHelper, 각종 Repository, SensorInfo
using TempMonitor.Models;                           // AppConfig, TempReading, TempStatus, UserInfo
using TempMonitor.Services;                         // KakaoService, KakaoSendResult

namespace TempMonitor
{
    public partial class MainForm : Form   // partial: 나머지는 MainForm.Designer.cs / : Form → 창 기능 상속
    {
        // ---- 화면 표시 한도 -------------------------------------------------------------
        // const : 고정값에 이름 붙이기. 숫자를 코드 중간에 직접 쓰지 않으려고
        private const int MaxChartPoints = 120;      // 차트에 유지할 점 개수 (1초 주기면 최근 2분)       → AddChartPoint
        private const int MaxRealtimeRows = 200;     // 실시간 그리드에 유지할 행 수                       → AddRealtimeRow
        private const int MaxHistoryRows = 50000;    // 기록 조회 최대 행 수 (메모리/화면 보호)             → SearchHistoryAsync → TempLogRepository 의 TOP (@MAX_ROWS)

        // ---- 상태별 색상 (실행 중에 상태가 바뀔 때마다 코드에서 바꿔 칠하는 색) ------------
        // static readonly : Color 는 구조체(객체)라 const 가 안 되므로 "한 번 정하면 안 바뀌는" static readonly 로
        // 쓰는 곳: UpdateCurrentPanel, AddRealtimeRow, PollLoopAsync, btnDisconnect_Click, dgvHistory_CellFormatting, SetDbStatus, RefreshKakaoStatusAsync
        private static readonly Color ColorNormal = Color.SeaGreen;    // 정상 = 초록
        private static readonly Color ColorWarning = Color.Crimson;    // 경고 = 빨강
        private static readonly Color ColorError = Color.DarkOrange;   // 센서 오류 = 주황
        private static readonly Color ColorOffline = Color.Gray;       // 연결 안 됨/통신 오류 = 회색

        // ---- 통신 ---------------------------------------------------------------------
        // Comm/ModbusTcpClient.cs 객체. TCP 연결을 폼이 살아 있는 동안 계속 유지해야 하므로 필드에 하나 만들어 둠
        // 쓰는 곳: btnConnect_Click(ConnectAsync), PollLoopAsync(IsConnected, 재연결), MainForm_Load(UnitId),
        //          StopPolling(Disconnect), MainForm_FormClosing(Dispose)
        private readonly ModbusTcpClient _modbus = new ModbusTcpClient();
        // Comm/Sig5560Client.cs 객체. "온도 읽어 줘 / 최저최고 초기화해 줘" 를 장비 언어로 부를 때 사용
        // 선언 줄에서 new 하지 않는 이유: _modbus 를 넘겨야 하는데, 필드 선언 줄에서는 다른 필드를 쓸 수 없음(CS0236)
        //   → 생성자에서 _sensor = new Sig5560Client(_modbus); 로 만듦 (readonly 도 생성자에서는 값을 넣을 수 있음)
        private readonly Sig5560Client _sensor;
        // 폴링 루프에 "그만!" 신호를 보내는 리모컨. null 이면 = 모니터링 중 아님
        //   만드는 곳 btnConnect_Click / 누르는 곳 StopPolling(Cancel) / 확인하는 곳 FormClosing, btnResetMinMax_Click
        private CancellationTokenSource _pollCts;
        private string _host;   // 연결 성공한 IP   → 끊겼을 때 PollLoopAsync 가 재연결할 주소
        private int _port;      // 연결 성공한 포트

        // ---- 데이터 접근 / 서비스 -----------------------------------------------------
        // 각 Repository 는 테이블 하나씩 담당. 필요한 SQL 을 대신 실행해 줌
        private readonly SensorRepository _sensorRepo = new SensorRepository();       // TB_SENSOR  : GetDefaultSensorAsync(Load), UpdateAddressAsync(연결 성공)
        private readonly TempLogRepository _tempRepo = new TempLogRepository();       // TB_TEMP_LOG: InsertAsync(저장), SearchAsync(기록 조회)
        private readonly AlertRepository _alertRepo = new AlertRepository();          // TB_ALERT_LOG: InsertAsync(경고), UpdateKakaoStatusAsync, SearchAsync(경고 조회)
        private readonly KakaoLogRepository _kakaoRepo = new KakaoLogRepository();    // TB_KAKAO_LOG: InsertAsync(발송 이력), SearchAsync(카톡 조회)
        private readonly ConfigRepository _configRepo = new ConfigRepository();       // TB_CONFIG  : LoadAppConfigAsync(Load), SaveAppConfigAsync(설정 저장)
        private readonly KakaoService _kakao = new KakaoService();                    // 카톡 발송   : SendToMeAsync, IsConfiguredAsync

        // ---- 상태 ---------------------------------------------------------------------
        // 현재 설정 (Models/AppConfig). 처음엔 기본값, Load 에서 DB 값으로 교체, 설정 저장 시 새 객체로 교체
        // readonly 가 아닌 이유: 통째로 새 객체로 바꿔 끼워야 하므로 (_config = newConfig;)
        // 읽는 곳: Evaluate(경고 기준), PollLoopAsync(읽기 주기), SaveIfNeededAsync(저장 주기), CheckAlertAsync(간격/상승폭/카톡 사용) 등
        private AppConfig _config = new AppConfig();
        private int _sensorId = 1;                // DB 의 SENSOR_ID. 모든 저장/조회의 @SENSOR_ID. Load 에서 DB 값으로
        private string _sensorName = "온도센서";  // 카톡 메시지 "[온도 경고] 온도센서" 에 들어가는 이름. Load 에서 DB 값으로

        private DateTime _lastSaveTime = DateTime.MinValue;   // 마지막 DB 저장 시각. MinValue(0001-01-01) → 첫 회차엔 무조건 "주기 지남" → 바로 저장
        private TempStatus? _lastSavedStatus;        // 상태가 바뀌면 저장 주기와 상관없이 즉시 저장 (? = 아직 저장한 적 없으면 null)
        private DateTime? _lastAlertTime;            // 마지막 경고 기록 시각 (경고 지속 알림 간격 계산용). null = 아직 없음
        private DateTime? _lastKakaoTime;            // 마지막 카톡 시도 시각 (카톡 폭탄 방지용)

        // ---- 경고 "구간" 상태 (정상 → 경고 시작 ~ 정상 복귀 까지를 하나의 경고 건으로 봄) ----
        // 경고는 매 초 새로 판단하는 게 아니라 "시작→지속→복귀" 로 이어지는 이야기라서, 그 사이의 기억을 필드에 둠
        // 쓰는 곳: CheckAlertAsync, EndAlert, BuildAlertMessage
        private bool _alertActive;              // 지금 경고 구간 안인가
        private long? _alertFirstId;           // 이 경고 구간의 첫 경고 ALERT_ID (복귀 알림 카톡 이력을 여기에 연결)
        private DateTime _alertStartTime;       // 구간 시작 시각
        private decimal _alertPeakTemp;         // 구간 중 최고 온도
        private DateTime _alertPeakTime;        // 최고 온도였던 시각
        private decimal _lastAlertTemp;         // 마지막으로 경고를 기록/알린 온도 (상승 알림의 기준)
        private bool _alertKakaoSent;           // 이 구간에서 카톡을 1건이라도 보냈는지 (보낸 적 없으면 복귀 알림도 생략)

        // ---- 조회 종료 시각 (켜 둔 채로 시간이 지나도 종료가 "켠 시각"에 멈추지 않게) ----
        // true = 조회할 때마다 종료 시각을 "지금" 으로 갱신 / false = 사용자가 고른 과거 시각으로 고정
        private bool _histEndFollowsNow = true;    // 기록 조회 탭
        private bool _alertEndFollowsNow = true;   // 경고 조회 탭

        // { get; private set; } : 밖(Program.cs)에서는 읽기만, 값은 이 클래스 안(btnLogout_Click)에서만 바꿈
        /// <summary>로그아웃 버튼으로 닫혔는지 (Program.cs 에서 로그인 창을 다시 띄울지 판단)</summary>
        public bool LogoutRequested { get; private set; }

        // 화면 속성(글자, 색, 그리드 컬럼, 차트 모양, 가운데 표시)과 이벤트 연결(버튼 Click, Load,
        // FormClosing, 시계 Timer 등)은 모두 디자이너(MainForm.Designer.cs)에서 설정합니다.
        // 이 파일에는 "실행 중에 바뀌는 것"과 "동작"만 둡니다.
        // ---- 생성자 : Program.Main 의 new MainForm() 때 실행 ----
        public MainForm()
        {
            InitializeComponent();                  // MainForm.Designer.cs: 컨트롤 생성 + 속성 + 이벤트 연결 (this.Load += MainForm_Load 등)
            _sensor = new Sig5560Client(_modbus);   // 같은 _modbus 를 넘김 → _modbus 로 연결하면 _sensor 도 그 연결로 읽음
        }

        // ---- 시계 ---------------------------------------------------------------------
        // 디자이너의 tmrClock(Timer, Interval 1000ms) 이 1초마다 이 메서드를 호출 (Tick 이벤트)
        private void tmrClock_Tick(object sender, EventArgs e)
        {
            tslClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");   // 상태 표시줄 시계. ToString(형식) = 날짜를 원하는 모양의 글자로
        }

        // 조회 종료 시각 "지금 따라가기": 종료를 현재 분 이후로 두면 조회/탭 이동 때마다 현재 시각으로 갱신,
        // 사용자가 과거 시각을 직접 고르면 그 값으로 고정
        // 디자이너에서 dateTimePicker2.ValueChanged 에 연결 (기록 조회 탭의 종료 시각)
        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            // 고른 값이 "지금 분" 이상이면 true(따라가기), 과거면 false(고정). 비교 결과(bool)를 그대로 대입
            _histEndFollowsNow = dateTimePicker2.Value >= TruncateToMinute(DateTime.Now);
        }

        // 디자이너에서 dtpAlertEnd.ValueChanged 에 연결 (경고 조회 탭의 종료 시각). 위와 같은 원리
        private void dtpAlertEnd_ValueChanged(object sender, EventArgs e)
        {
            _alertEndFollowsNow = dtpAlertEnd.Value >= TruncateToMinute(DateTime.Now);
        }

        // 디자이너에서 tabMain.SelectedIndexChanged 에 연결 → 탭을 옮길 때마다 종료 시각 갱신
        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshEndTimes();   // 아래 "조회 탭" 영역의 메서드
        }

        // =============================================================================
        //  초기화
        // =============================================================================

        // 폼이 처음 화면에 뜰 때 한 번 실행 (디자이너에서 this.Load 에 연결)
        // async void : 이벤트 핸들러라 void, 안에서 DB 를 await 하려고 async
        private async void MainForm_Load(object sender, EventArgs e)
        {
            // LoginForm 이 Session.CurrentUser 에 넣어 둔 사용자 꺼내기 (Session.cs)
            UserInfo user = Session.CurrentUser;
            // $"..." : 문자열 보간. { } 안에 변수를 바로 씀. user.RoleName = UserInfo 의 계산 속성("관리자"/"작업자")
            lblUser.Text = user == null ? "" : $"사용자 : {user.UserId}(권한 : {user.RoleName})";

            // 아래 같은 파일의 private 메서드들. Load 가 길어지지 않게 준비 작업을 이름 붙여 나눔
            InitCurrentPanel();   // 현재 온도 패널 (기준 온도 표시, 동그란 램프)
            InitGrids();          // 그리드 동작 붙이기
            InitSearchDates();    // 조회 날짜 기본값
            ApplyPermission();    // 관리자가 아니면 설정 잠그기

            tslClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");   // 첫 1초 동안 시계가 비지 않게 한 번 직접 표시
            tmrClock.Start();                                               // 디자이너의 Timer 시작 → 1초마다 tmrClock_Tick

            // DB 에서 설정과 센서 정보 읽기 (실패해도 화면은 뜨도록 기본값으로 진행)
            try
            {
                bool dbOk = await DbHelper.TestConnectionAsync();      // Data/DbHelper.cs: 연결만 시도 → true/false
                SetDbStatus(dbOk, dbOk ? "연결됨" : "연결 실패");       // 상태 표시줄 "DB: ..." (아래 메서드)
                if (!dbOk) return;                                     // DB 가 안 되면 여기서 끝 (기본 설정 _config 로 동작)

                // Data/ConfigRepository.cs: TB_CONFIG 를 읽어 AppConfig 객체로 → _config 교체
                // 이제부터 경고 판단·주기 등이 DB 설정값으로 동작
                _config = await _configRepo.LoadAppConfigAsync();
                ApplyConfigToUi();                                     // 설정 탭 입력칸에 값 채우기 (아래 메서드)

                // Data/SensorRepository.cs: 사용 중인 첫 센서 정보 (없으면 null)
                SensorInfo sensor = await _sensorRepo.GetDefaultSensorAsync();
                if (sensor != null)
                {
                    // 받은 값을 필요한 곳들에 나눠 보관
                    _sensorId = sensor.SensorId;             // 저장/조회용 센서 번호
                    _sensorName = sensor.SensorName;         // 카톡 메시지용 이름
                    _modbus.UnitId = sensor.UnitId;          // ModbusTcpClient 의 속성 → 요청 프레임의 장비 번호
                    txtIp.Text = sensor.IpAddress;           // 지난번 연결 성공한 IP 를 입력칸에 미리 채움
                    txtPort.Text = sensor.Port.ToString();   // int → string (TextBox.Text 는 문자열)
                }

                await RefreshKakaoStatusAsync();   // 카톡 토큰 설정 여부 표시 (아래 "경고 / 카카오톡" 영역)
            }
            catch (Exception ex)   // 위 어느 단계에서든 예외가 나면
            {
                SetDbStatus(false, "오류");
                MessageBox.Show(this, "초기 데이터를 불러오지 못했습니다.\n\n" + ex.Message, "DB 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);   // 메시지만 띄우고 화면은 계속 사용 가능
            }
        }

        // 현재 온도 패널 준비 (MainForm_Load 에서 호출)
        private void InitCurrentPanel()
        {
            // 경고 기준 온도는 DB 설정에 따라 바뀌므로 코드에서 표시
            UpdateThresholdLabel();   // "경고 기준 : 30.0 ℃ 이상" (아래 메서드)

            // 사각형 패널을 동그란 램프 모양으로 자르기 (디자이너 속성으로는 할 수 없는 것)
            var path = new GraphicsPath();                             // 도형 경로 객체
            path.AddEllipse(0, 0, pnlLamp.Width, pnlLamp.Height);      // 패널 크기에 꽉 차는 타원(원) 추가
            pnlLamp.Region = new Region(path);                         // Region = 컨트롤이 보이는 영역 → 원 밖은 안 그려짐
        }

        // 그리드 준비 (MainForm_Load 에서 호출)
        private void InitGrids()
        {
            // AutoGenerateColumns 는 속성 창에 나오지 않는 속성이라 코드에서만 설정할 수 있습니다.
            // (DataTable 을 바인딩할 때 디자이너에서 만든 컬럼만 쓰고, 자동 컬럼 생성은 끄기)
            // → true 면 DataTable 의 STATUS_CD 같은 컬럼이 그리드에 자동으로 더 생겨 버림
            // dgvRealtime 은 DataSource 를 안 쓰고 Rows.Insert 로 직접 넣으므로 필요 없음
            dgvHistory.AutoGenerateColumns = false;
            dgvAlert.AutoGenerateColumns = false;
            dgvKakao.AutoGenerateColumns = false;

            // 보기 전용, 정렬 가능한 컬럼, 컬럼 연결(DataPropertyName), 표시 형식은 디자이너에서 설정했고,
            // 여기서는 속성 창으로 할 수 없는 "동작"만 붙입니다.
            // 같은 동작을 4개 그리드에 재사용하려고 그리드를 매개변수로 받는 메서드(AttachViewOnlyBehavior)를 만듦
            // 두 번째 인수: 조회 직후 ▼ 를 표시할 기본 정렬 컬럼 (디자이너의 컬럼 필드 이름)
            AttachViewOnlyBehavior(dgvRealtime, null);            // 실시간은 항상 최신이 맨 위 → 정렬 금지
            AttachViewOnlyBehavior(dgvHistory, colHistTime);      // 시간 / 온도 / 상태 정렬 가능
            AttachViewOnlyBehavior(dgvAlert, colAlertTime);       // 발생 시각순만
            AttachViewOnlyBehavior(dgvKakao, colKakaoTime);       // 발송 시각순만
        }

        /// <summary>
        /// 보기 전용 그리드에 "동작"을 붙입니다. (속성은 디자이너에 있음)
        ///  - 행 선택(파란 하이라이트)을 바로 해제: 경고/오류 행의 색이 파란색에 가려지지 않게
        ///  - 조회 직후 기본 정렬 컬럼(시각)에 ▼ 표시
        ///  - 정렬 가능한 헤더 위에서는 손 모양 커서
        /// </summary>
        // static : 폼의 필드를 하나도 안 쓰고 매개변수만으로 일하므로
        // dgv               : 동작을 붙일 그리드
        // defaultSortColumn : ▼ 표시할 컬럼 (null 이면 표시 안 함)
        private static void AttachViewOnlyBehavior(DataGridView dgv, DataGridViewColumn defaultSortColumn)
        {
            // 선택되는 즉시 해제 → 하이라이트가 생기지 않음 (데이터 바인딩 직후 첫 행 자동 선택도 같이 해제됨)
            // +=        : 이벤트에 처리기를 "추가"
            // (s, e) => : 람다(이름 없는 짧은 메서드). s = sender, e = 이벤트 정보
            dgv.SelectionChanged += (s, e) => dgv.ClearSelection();

            // 조회 쿼리는 모두 "시각 DESC" 로 가져오므로, 조회 직후 시각 컬럼에 ▼ 를 표시해
            // 지금 화면이 어떤 순서인지 알려 줍니다. (헤더를 눌러 정렬하면 그 컬럼으로 ▲/▼ 가 옮겨감)
            if (defaultSortColumn != null)
            {
                // DataBindingComplete : DataSource 에 표를 넣고 화면 연결이 끝났을 때 발생
                dgv.DataBindingComplete += (s, e) =>
                {
                    // SortedColumn == null : 사용자가 헤더를 눌러 정렬한 적 없음 / DataSource != null : 데이터가 있음
                    if (dgv.SortedColumn == null && dgv.DataSource != null)
                        defaultSortColumn.HeaderCell.SortGlyphDirection = SortOrder.Descending;   // 헤더에 ▼ 그리기
                };
            }

            // 정렬 가능한 헤더 위에서는 손 모양 커서 → "누를 수 있다"는 신호
            // CellMouseEnter : 마우스가 칸 위로 들어왔을 때
            dgv.CellMouseEnter += (s, e) =>
            {
                // e.RowIndex == -1 : 헤더 줄 / e.ColumnIndex >= 0 : 맨 왼쪽 빈칸이 아님 / SortMode 가 정렬 가능
                bool sortableHeader = e.RowIndex == -1 && e.ColumnIndex >= 0
                    && dgv.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable;
                dgv.Cursor = sortableHeader ? Cursors.Hand : Cursors.Default;   // 손 / 기본 화살표
            };
            dgv.MouseLeave += (s, e) => dgv.Cursor = Cursors.Default;   // 그리드 밖으로 나가면 기본 커서
        }

        // 조회 탭 날짜 기본값 (MainForm_Load 에서 호출). "오늘" 기준이라 실행할 때마다 바뀌므로 코드에서
        private void InitSearchDates()
        {
            DateTime today = DateTime.Today;               // 오늘 0시 0분 0초
            dateTimePicker1.Value = today;                 // 기록 조회 시작 = 오늘 0시
            dateTimePicker2.Value = DateTime.Now;          // 기록 조회 종료 = 지금
            dtpAlertStart.Value = today.AddDays(-7);       // 경고 조회 시작 = 7일 전
            dtpAlertEnd.Value = DateTime.Now;              // 경고 조회 종료 = 지금
            dtpKakaoStart.Value = today.AddDays(-7);       // 카톡 조회 시작 = 7일 전
            dtpKakaoEnd.Value = today;                     // 카톡 조회 종료 = 오늘 (날짜만 고르는 화면)
        }

        // (MainForm_Load 에서 호출) 로그인한 사람마다 다르므로 코드에서
        /// <summary>권한에 따라 화면 기능 제한 (설정 변경은 관리자만)</summary>
        private void ApplyPermission()
        {
            bool admin = Session.IsAdmin;      // Session.cs: 관리자면 true
            grpAlertSetting.Enabled = admin;   // 경고 설정 그룹박스 (false 면 안의 컨트롤 전부 비활성)
            grpCycleSetting.Enabled = admin;   // 주기 설정 그룹박스
            btnSaveSetting.Enabled = admin;    // 저장 버튼
        }

        // _config 값을 설정 탭 입력칸에 채우기 (MainForm_Load 에서 호출)
        private void ApplyConfigToUi()
        {
            SetNumeric(nudAlertTemp, _config.AlertTemp);         // 아래 SetNumeric: 범위 안으로 맞춰 넣기
            SetNumeric(nudAlertInterval, _config.AlertIntervalMin);   // int → decimal 매개변수로 자동 변환
            SetNumeric(nudRiseStep, _config.AlertRiseStep);
            SetNumeric(nudReadCycle, _config.ReadCycleSec);
            SetNumeric(nudSaveCycle, _config.SaveCycleSec);
            chkKakaoAlert.Checked = _config.KakaoEnabled;        // 체크박스
            UpdateThresholdLabel();                              // 기준 온도 라벨도 새 설정으로
        }

        /// <summary>NumericUpDown 에 범위를 벗어난 값을 넣으면 예외가 나므로 범위 안으로 맞춰 넣습니다.</summary>
        private static void SetNumeric(NumericUpDown nud, decimal value)
        {
            // Math.Min(최대, 값) : 최대보다 크면 최대로 / Math.Max(최소, ...) : 최소보다 작으면 최소로
            nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, value));
        }

        // 기준 온도 라벨 갱신. 부르는 곳: InitCurrentPanel, ApplyConfigToUi, btnSaveSetting_Click (설정이 바뀔 때마다)
        private void UpdateThresholdLabel()
        {
            lblThresholdInfo.Text = $"경고 기준 : {_config.AlertTemp:0.0} ℃ 이상";   // {값:0.0} = 소수 1자리 형식
        }

        // =============================================================================
        //  센서 연결 / 해제
        // =============================================================================

        // [연결] 버튼 (디자이너에서 btnConnect.Click 에 연결)
        // 흐름: 입력 검사 → ModbusTcpClient.ConnectAsync → 주소 DB 저장 → PollLoopAsync 시작(여기서 계속 돎)
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string host = txtIp.Text.Trim();   // IP 입력칸
            int port;                          // 아래 TryParse 의 out 으로 채워질 변수

            if (host.Length == 0)   // IP 를 안 넣었으면
            {
                MessageBox.Show(this, "센서 IP 주소를 입력하세요.", "센서 연결", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIp.Focus();
                return;
            }
            // int.TryParse(문자, out port) : 숫자로 바뀌면 true + port 에 값. || : 하나라도 true 면 → 숫자가 아니거나 범위 밖
            if (!int.TryParse(txtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show(this, "포트는 1~65535 사이 숫자여야 합니다. (SIG-5560 기본값 502)", "센서 연결",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPort.Focus();
                return;
            }

            SetConnectionUi(connecting: true);   // connecting: true = "이름 붙인 인수" (뜻이 잘 보이게). 버튼 잠그고 "연결 중..."
            try
            {
                await _modbus.ConnectAsync(host, port);   // Comm/ModbusTcpClient.cs: 3초 안에 못 붙으면 예외
            }
            catch (Exception ex)   // 시간 초과, 연결 거부 등
            {
                SetConnectionUi(connecting: false);   // 다시 입력할 수 있게 풀기
                SetSensorStatus("연결 실패");
                MessageBox.Show(this, $"센서({host}:{port})에 연결하지 못했습니다.\n\n{ex.Message}", "센서 연결",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 연결 성공
            _host = host;   // 지역 변수 → 필드로 옮겨 보관 (이 메서드가 끝나도 PollLoopAsync 가 재연결에 씀)
            _port = port;
            SetConnectedUi();          // [연결 해제], [초기화] 켜고 입력칸 잠그기
            SetSensorStatus("연결됨");

            // 다음 실행 때 자동으로 채워지도록 접속 정보를 DB 에 저장 (실패해도 모니터링은 계속)
            try { await _sensorRepo.UpdateAddressAsync(_sensorId, host, port); }   // Data/SensorRepository.cs
            catch { /* DB 저장 실패는 무시 */ }

            _lastSavedStatus = null;   // 새 연결 → 첫 측정값은 바로 저장되게
            _alertActive = false;   // 새로 연결하면 경고 구간도 새로 시작
            _pollCts = new CancellationTokenSource();   // 새 "중지 리모컨" 만들기
            // 폴링 루프 시작. .Token = 리모컨의 "수신기" 를 루프에 넘김
            // await : 루프가 끝날 때(연결 해제/창 닫기)까지 이 클릭 핸들러는 끝나지 않음. 그래도 화면은 안 멈춤
            await PollLoopAsync(_pollCts.Token);
        }

        // [연결 해제] 버튼 (디자이너에서 btnDisconnect.Click 에 연결)
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            StopPolling();                        // 루프 중지 + 소켓 닫기 (아래 메서드)
            SetConnectionUi(connecting: false);   // 입력칸/연결 버튼 다시 켜기
            SetSensorStatus("연결 안 됨");
            lblStatus.Text = "연결 안 됨";
            lblStatus.ForeColor = ColorOffline;       // 회색
            lblCurrentTemp.ForeColor = ColorOffline;
            pnlLamp.BackColor = ColorOffline;
        }

        // 폴링 중지. 부르는 곳: btnDisconnect_Click, MainForm_FormClosing
        private void StopPolling()
        {
            if (_pollCts != null)
            {
                _pollCts.Cancel();   // 리모컨 누르기 → PollLoopAsync 의 Task.Delay(..., ct) 가 즉시 취소 예외 → 루프 break
                _pollCts = null;     // null = "모니터링 중 아님" (FormClosing, btnResetMinMax 가 이걸로 판단)
            }
            _modbus.Disconnect();    // ModbusTcpClient: 소켓 닫기
        }

        // 연결 전/연결 중 화면 상태. connecting = true(연결 시도 중), false(연결 안 됨)
        private void SetConnectionUi(bool connecting)
        {
            btnConnect.Enabled = !connecting;   // 연결 중엔 [연결] 잠금
            txtIp.Enabled = !connecting;
            txtPort.Enabled = !connecting;
            btnDisconnect.Enabled = false;      // 아직 연결 안 됐으니 둘 다 끔
            btnResetMinMax.Enabled = false;
            if (connecting) SetSensorStatus("연결 중...");
        }

        // 연결 성공 후 화면 상태
        private void SetConnectedUi()
        {
            btnConnect.Enabled = false;
            txtIp.Enabled = false;
            txtPort.Enabled = false;
            btnDisconnect.Enabled = true;
            btnResetMinMax.Enabled = true;
        }

        // =============================================================================
        //  폴링 루프 : 읽기 → 화면 갱신 → 경고 판단 → DB 저장 → 대기 → 반복
        // =============================================================================

        /// <summary>
        /// UI 스레드에서 시작한 async 메서드는 await 이후에도 UI 스레드로 돌아옵니다.
        /// 그래서 Invoke 없이 바로 컨트롤을 갱신할 수 있고, 통신을 기다리는 동안 화면은 멈추지 않습니다.
        /// Timer 와 달리 "이전 읽기가 끝나야 다음 읽기"라서 응답이 늦어도 요청이 겹치지 않습니다.
        /// </summary>
        // 호출: btnConnect_Click 마지막 줄
        // async Task (void 아님) : btnConnect_Click 이 await 로 기다릴 수 있게
        // ct : 중지 신호 수신기. StopPolling 이 Cancel() 하면 ct.IsCancellationRequested 가 true
        private async Task PollLoopAsync(CancellationToken ct)
        {
            int failCount = 0;   // 연속 통신 실패 횟수 (이 루프 안에서만 의미 있으므로 지역 변수)

            while (!ct.IsCancellationRequested)   // 중지 신호가 올 때까지 반복
            {
                try
                {
                    // 통신이 끊겼으면 자동 재연결 (공장에서는 케이블/스위치 문제로 순간 끊김이 흔함)
                    // (통신 오류가 나면 ModbusTcpClient.SendAsync 가 스스로 Disconnect 해 두므로 여기서 false 가 됨)
                    if (!_modbus.IsConnected)
                    {
                        SetSensorStatus("재연결 중...");
                        await _modbus.ConnectAsync(_host, _port);   // btnConnect 때 저장해 둔 주소로

                        // 재연결하는 사이 사용자가 '연결 해제'를 눌렀다면 방금 연 연결을 닫고 종료
                        if (ct.IsCancellationRequested)
                        {
                            if (_pollCts == null) _modbus.Disconnect();
                            break;   // while 탈출
                        }
                    }

                    // ★ Comm/Sig5560Client.cs → ModbusTcpClient 로 센서 값 읽기 → TempReading 한 개
                    TempReading reading = await _sensor.ReadAsync();
                    if (ct.IsCancellationRequested) break;   // 읽는 사이 [연결 해제] 를 눌렀으면 결과 버리고 종료

                    failCount = 0;               // 성공했으니 실패 횟수 초기화
                    SetSensorStatus("연결됨");
                    await HandleReadingAsync(reading);   // 한 회차 처리 (화면/차트/경고/저장) - 아래 메서드
                }
                catch (Exception ex)   // 아래 층(ModbusTcpClient, Sig5560Client)에서 던진 예외가 여기서 최종적으로 잡힘
                {
                    if (ct.IsCancellationRequested) break;   // 사용자가 연결 해제한 경우

                    failCount++;   // 실패 횟수 +1
                    // 상태 표시줄에는 짧게, 자세한 원인은 마우스를 올리면 보이는 툴팁으로
                    SetSensorStatus($"통신 오류 {failCount}회 (재연결 시도 중)", ex.Message);
                    lblStatus.Text = "통신 오류";
                    lblStatus.ForeColor = ColorOffline;
                    lblCurrentTemp.ForeColor = ColorOffline;   // 마지막 값은 남기되 회색 = "지금 값이 아님"
                    pnlLamp.BackColor = ColorOffline;
                    // catch 로 잡고 루프는 계속 → 다음 회차에 재연결 시도 (안 잡으면 루프가 죽어 모니터링이 영영 멈춤)
                }

                // 연속 실패 중에는 최소 3초 간격으로 재시도 (장비/네트워크에 부담 주지 않기)
                // _config.ReadCycleSec 를 매 회차 읽으므로, 설정을 저장하면 다음 회차부터 바로 반영됨
                int delaySec = failCount > 0 ? Math.Max(3, _config.ReadCycleSec) : _config.ReadCycleSec;
                try
                {
                    // delaySec 초 기다리기. ct 가 취소되면 기다리다 말고 즉시 OperationCanceledException
                    await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
                }
                catch (OperationCanceledException)
                {
                    break;   // 중지 신호 → 루프 종료
                }
            }
        }

        // 한 회차 처리. 호출: PollLoopAsync
        // reading/status 두 값을 모든 메서드에 똑같이 넘김 → 상태 판단은 Evaluate 한 곳에서만, 나머지는 결과만 사용
        private async Task HandleReadingAsync(TempReading reading)
        {
            TempStatus status = Evaluate(reading);   // 정상/경고/오류 판단

            UpdateCurrentPanel(reading, status);     // 현재 온도 패널 (즉시 끝나는 일이라 void)
            AddRealtimeRow(reading, status);         // 실시간 그리드 맨 위에 한 줄
            AddChartPoint(reading);                  // 차트에 점 하나

            await CheckAlertAsync(reading, status);  // 경고 판단 + 경고 저장 + 카톡 (DB 가 필요해서 await)
            await SaveIfNeededAsync(reading, status);// 온도 기록 저장 (주기/상태 변화 시에만)
        }

        // 상태 판단. 호출: HandleReadingAsync
        private TempStatus Evaluate(TempReading reading)
        {
            if (reading.HasError) return TempStatus.Error;                           // 오류 먼저 (오류면 온도 값을 믿을 수 없으니)
            if (reading.Temperature >= _config.AlertTemp) return TempStatus.Warning; // 기준 이상이면 경고
            return TempStatus.Normal;                                                // 나머지는 정상
        }

        // =============================================================================
        //  화면 갱신
        // =============================================================================

        // 현재 온도 패널 갱신. 호출: HandleReadingAsync (r = reading 을 짧게)
        private void UpdateCurrentPanel(TempReading r, TempStatus status)
        {
            lblLastUpdate.Text = "최종 수신 : " + r.ReadTime.ToString("HH:mm:ss");

            if (status == TempStatus.Error)   // 센서 오류
            {
                lblCurrentTemp.Text = "--.-";             // 온도 대신 --.-
                lblCurrentTemp.ForeColor = ColorError;
                // 패널은 좁으므로 짧은 이름 (전체 이름은 아래 실시간 기록 그리드에 표시)
                lblStatus.Text = "센서 오류 : " + r.ErrorShortText;   // TempReading 의 짧은 오류 이름 속성
                lblStatus.ForeColor = ColorError;
                pnlLamp.BackColor = ColorError;
                return;   // 아래 정상/경고 처리는 건너뜀
            }

            bool warn = status == TempStatus.Warning;                          // 경고인가 (비교 결과를 변수에)
            lblCurrentTemp.Text = r.Temperature.ToString("0.0");              // 27.2
            lblCurrentTemp.ForeColor = warn ? ColorWarning : SystemColors.ControlText;   // 경고면 빨강, 아니면 기본 글자색
            lblStatus.Text = status.ToDisplayName();                          // 확장 메서드(Models/TempReading.cs) → "정상"/"경고"
            lblStatus.ForeColor = warn ? ColorWarning : ColorNormal;
            pnlLamp.BackColor = warn ? ColorWarning : ColorNormal;            // 램프 빨강/초록
            lblMinMax.Text = $"최저 {r.MinTemperature:0.0} / 최고 {r.MaxTemperature:0.0}";
        }

        // 실시간 그리드 맨 위에 한 줄 추가. 호출: HandleReadingAsync
        private void AddRealtimeRow(TempReading r, TempStatus status)
        {
            string temp = r.HasError ? "-" : r.Temperature.ToString("0.0");   // 오류면 온도 대신 "-"
            string statusText = status == TempStatus.Error
                ? status.ToDisplayName() + " (" + r.ErrorText + ")"   // 어떤 오류인지까지 표시 (그리드는 넓어서 긴 이름)
                : status.ToDisplayName();
            // Rows.Insert(0, 값들) : 0번째(맨 위)에 행 추가. 값 순서 = 디자이너 컬럼 순서(colTime, colTemp, colStatus)
            dgvRealtime.Rows.Insert(0, r.ReadTime.ToString("HH:mm:ss"), temp, statusText);

            if (status != TempStatus.Normal)   // 정상이 아니면 그 행 글자색을 경고(빨강)/오류(주황)로
                dgvRealtime.Rows[0].DefaultCellStyle.ForeColor = status == TempStatus.Warning ? ColorWarning : ColorError;

            while (dgvRealtime.Rows.Count > MaxRealtimeRows)          // 200행을 넘으면
                dgvRealtime.Rows.RemoveAt(dgvRealtime.Rows.Count - 1);   // 맨 아래(가장 오래된) 행 삭제
        }

        // 차트에 점 추가. 호출: HandleReadingAsync
        private void AddChartPoint(TempReading r)
        {
            if (r.HasError) return;   // 오류 값은 차트에 그리지 않음

            // Series[0], [1] = 디자이너에서 chartTemp.Series.Add 한 순서 ("온도" 선, "기준" 점선)
            Series temp = chartTemp.Series[0];
            Series threshold = chartTemp.Series[1];

            // AddXY(X값, Y값) : X = 시각, Y = 온도. 차트는 decimal 을 못 받아서 (double) 로 변환
            temp.Points.AddXY(r.ReadTime, (double)r.Temperature);
            threshold.Points.AddXY(r.ReadTime, (double)_config.AlertTemp);   // 기준선도 매 점마다 → 설정이 바뀌면 그때부터 높이가 바뀜

            // 120개 넘으면 가장 오래된 점(0번) 삭제 → 최근 2분만 보이게
            while (temp.Points.Count > MaxChartPoints) temp.Points.RemoveAt(0);
            while (threshold.Points.Count > MaxChartPoints) threshold.Points.RemoveAt(0);

            chartTemp.ResetAutoValues();   // 점을 지운 뒤 축 범위를 다시 계산
        }

        /// <summary>
        /// 상태 표시줄은 공간이 좁으므로 text 는 짧게, 긴 예외 메시지는 detail(툴팁)로 넘깁니다.
        /// </summary>
        // string detail = null : "선택적 매개변수". 안 넘기면 null
        // 부르는 곳: 연결/루프/재연결 여러 곳 → 표시 형식을 한 곳에서 통일
        private void SetSensorStatus(string text, string detail = null)
        {
            tslSensor.Text = "| 센서: " + text;               // 상태 표시줄의 센서 칸
            tslSensor.ToolTipText = detail ?? tslSensor.Text;  // ?? : detail 이 null 이면 본문과 같은 글자를 툴팁으로
        }

        // DB 상태 표시줄. ok = false 면 빨간 글자. 부르는 곳: Load, 저장, 경고, 카톡, 조회 등
        private void SetDbStatus(bool ok, string text, string detail = null)
        {
            tslDb.Text = "DB: " + text;
            tslDb.ToolTipText = detail ?? tslDb.Text;
            tslDb.ForeColor = ok ? SystemColors.ControlText : ColorWarning;
        }

        // =============================================================================
        //  DB 저장
        // =============================================================================

        /// <summary>
        /// 읽기 주기(1초)마다 저장하면 하루 86,400건 → 저장은 별도 주기(기본 5초)로 줄입니다.
        /// 단, 정상→경고 같은 "상태 변화"는 순간을 놓치면 안 되므로 주기와 상관없이 바로 저장합니다.
        /// </summary>
        // 호출: HandleReadingAsync
        private async Task SaveIfNeededAsync(TempReading reading, TempStatus status)
        {
            // DateTime - DateTime = TimeSpan(시간 차). .TotalSeconds = 그 차이를 초로
            bool cycleDue = (reading.ReadTime - _lastSaveTime).TotalSeconds >= _config.SaveCycleSec;   // 저장 주기가 지났나
            bool statusChanged = _lastSavedStatus != status;   // 상태가 바뀌었나 (처음엔 null 이라 무조건 "다름")
            if (!cycleDue && !statusChanged) return;           // 둘 다 아니면 저장 안 함

            try
            {
                await _tempRepo.InsertAsync(_sensorId, reading, status);   // Data/TempLogRepository.cs → TB_TEMP_LOG INSERT
                // 저장 "성공 후에만" 기억 갱신 → 실패하면 다음 회차에 또 시도
                _lastSaveTime = reading.ReadTime;
                _lastSavedStatus = status;
                SetDbStatus(true, "저장 " + reading.ReadTime.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                // DB 가 잠깐 죽어도 화면 모니터링은 계속되어야 합니다.
                SetDbStatus(false, "저장 실패", ex.Message);
            }
        }

        // =============================================================================
        //  경고 / 카카오톡
        // =============================================================================

        // 이 클래스 안에서만 쓰는 알림 종류 (클래스 안에 선언한 private enum)
        private enum AlertKind { Onset, Rise, Repeat }   // 시작 / 상승 / 지속

        /// <summary>
        /// 경고 "구간" 단위로 알림을 관리합니다. (정상 → 경고 시작 ~ 정상 복귀 = 경고 1건)
        ///
        ///  ① 시작  : 정상 → 경고가 되는 순간 기록 + 카톡             [온도 경고]
        ///  ② 상승  : 직전 알림 온도보다 '재알림 상승 폭' 이상 오르면     [온도 상승]
        ///           최소 간격과 상관없이 즉시 기록 + 카톡 (악화는 놓치지 않기)
        ///  ③ 지속  : 경고가 계속되면 '알림 최소 간격'마다 기록 + 카톡   [경고 지속]
        ///  ④ 복귀  : 정상으로 돌아오면 구간 요약(지속 시간, 최고 온도)    [정상 복귀]
        ///
        /// 카톡 폭탄 방지
        ///  - ①③은 마지막 카톡 후 최소 간격이 지나야 발송 (기준 온도 근처에서 오르락내리락할 때 대비)
        ///  - ④는 그 구간에서 카톡을 실제로 보낸 경우에만 발송 (시작을 안 알렸으면 복귀도 안 알림)
        ///  - 센서 오류(ERROR)는 구간을 끝내지 않음 (일시적 오류로 복귀 알림이 나가지 않게)
        /// </summary>
        // 호출: HandleReadingAsync
        private async Task CheckAlertAsync(TempReading r, TempStatus status)
        {
            DateTime now = r.ReadTime;   // 이번 측정 시각

            if (status == TempStatus.Normal)   // 정상이면
            {
                if (_alertActive)              // 방금까지 경고 구간이었으면
                    EndAlert(r);               // ④ 복귀 처리 (아래 메서드)
                return;
            }
            if (status != TempStatus.Warning) return;   // ERROR: 구간 유지, 아무것도 안 함

            // 여기부터는 Warning(경고)
            decimal temp = r.Temperature;
            TimeSpan interval = TimeSpan.FromMinutes(_config.AlertIntervalMin);   // 설정의 "알림 최소 간격"(분) → TimeSpan
            AlertKind kind;   // 이번 알림 종류 (아래에서 정함)

            if (!_alertActive)
            {
                // ① 새 경고 구간 시작 → 구간 기억 필드들 초기화
                _alertActive = true;
                _alertFirstId = null;        // 첫 경고 ID 는 아래 Insert 후에 채움
                _alertStartTime = now;
                _alertPeakTemp = temp;
                _alertPeakTime = now;
                _alertKakaoSent = false;
                kind = AlertKind.Onset;
            }
            else
            {
                // 이미 경고 구간 안
                if (temp > _alertPeakTemp) { _alertPeakTemp = temp; _alertPeakTime = now; }   // 최고 온도 갱신

                // 상승 알림 조건: 상승폭 기능이 켜져 있고(>0) && 직전 알림 온도보다 상승폭 이상 올랐나
                bool rise = _config.AlertRiseStep > 0 && temp - _lastAlertTemp >= _config.AlertRiseStep;
                // 지속 알림 조건: 기록한 적이 없거나 || 마지막 기록 후 간격이 지났나
                // (|| 단락 평가: null 이면 오른쪽 .Value 를 안 봐서 예외가 안 남)
                bool repeatDue = _lastAlertTime == null || now - _lastAlertTime.Value >= interval;

                if (rise) kind = AlertKind.Rise;            // ② 상승이 지속보다 우선
                else if (repeatDue) kind = AlertKind.Repeat; // ③
                else return;                                 // 알릴 때가 아님 → 끝
            }

            // 카톡 상태 결정 → TB_ALERT_LOG.KAKAO_STATUS_CD 에 저장되는 코드
            string kakaoStatus;
            if (!_config.KakaoEnabled)
                kakaoStatus = "DISABLED";   // 설정에서 카톡을 꺼 둠
            else if (kind != AlertKind.Rise && _lastKakaoTime != null && now - _lastKakaoTime.Value < interval)
                kakaoStatus = "SKIPPED";    // 상승이 아니고, 마지막 카톡 후 간격이 안 지남 → 기록만 하고 카톡 생략
            else
                kakaoStatus = "PENDING";    // 보낼 예정 (보낸 뒤 SENT/FAILED 로 바뀜)

            decimal prevAlertTemp = _lastAlertTemp;   // 메시지의 "직전 알림 대비 +n℃" 계산용으로 덮어쓰기 전에 보관
            _lastAlertTime = now;     // 이번 기록 시각 기억
            _lastAlertTemp = temp;    // 이번 기록 온도 기억 (다음 상승 판단의 기준)

            try
            {
                // Data/AlertRepository.cs → TB_ALERT_LOG INSERT + 새 ALERT_ID 받기
                long alertId = await _alertRepo.InsertAsync(_sensorId, now, temp, _config.AlertTemp, kakaoStatus);
                if (_alertFirstId == null) _alertFirstId = alertId;   // 구간의 첫 경고 ID 만 기억 (복귀 카톡 이력을 여기에 연결)

                if (kakaoStatus == "PENDING")
                {
                    _lastKakaoTime = now;   // 실패해도 시도 시각을 기록 → API 장애 시 매초 재시도하지 않음
                    _alertKakaoSent = true; // 이 구간에서 카톡 보냄 → 복귀 알림도 보내게
                    string message = BuildAlertMessage(kind, temp, prevAlertTemp, now);   // 아래 메서드: 종류별 문구
                    // 발송은 기다리지 않음: 카카오 응답이 늦어도(최대 10초) 센서 읽기가 멈추면 안 되므로
                    // _ = : "버림(discard)". Task 를 받긴 하지만 await 하지 않겠다는 표시 (보내 놓고 루프는 바로 다음으로)
                    // 대신 SendAlertKakaoAsync 안에서 예외를 전부 처리해야 함 (아무도 await 안 하니까 밖에서 못 잡음)
                    _ = SendAlertKakaoAsync(alertId, message);
                }
            }
            catch (Exception ex)
            {
                SetDbStatus(false, "경고 저장 실패", ex.Message);
            }
        }

        // 카톡 메시지 문구 만들기. 호출: CheckAlertAsync
        private string BuildAlertMessage(AlertKind kind, decimal temp, decimal prevAlertTemp, DateTime now)
        {
            string body;
            switch (kind)   // 알림 종류별로 다른 문구
            {
                case AlertKind.Rise:   // ② 상승
                    // \n : 줄바꿈 / {값:0.0} 소수 1자리 / {시각:HH:mm:ss} 시각 형식
                    body = $"[온도 상승] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (직전 알림 {prevAlertTemp:0.0}℃ 대비 +{temp - prevAlertTemp:0.0}℃)\n" +
                           $"기준 {_config.AlertTemp:0.0}℃ / 경고 시작 {_alertStartTime:HH:mm:ss}";
                    break;
                case AlertKind.Repeat:   // ③ 지속
                    body = $"[경고 지속] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (기준 {_config.AlertTemp:0.0}℃ 이상)\n" +
                           $"지속 {FormatDuration(now - _alertStartTime)} / 최고 {_alertPeakTemp:0.0}℃";   // FormatDuration: 아래 "3분 20초"
                    break;
                default:   // ① 시작 (Onset)
                    body = $"[온도 경고] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (기준 {_config.AlertTemp:0.0}℃ 이상)";
                    break;
            }
            return body + $"\n{now:yyyy-MM-dd HH:mm:ss}";   // 마지막 줄에 발생 시각
        }

        /// <summary>
        /// 경고 카톡 발송 → 결과를 경고 이력(KAKAO_STATUS_CD)에 반영.
        /// 폴링 루프가 await 하지 않고 던져 두는 작업이라, 예외를 여기서 전부 처리해야 합니다.
        /// </summary>
        // 호출: CheckAlertAsync (_ = 로 기다리지 않고)
        private async Task SendAlertKakaoAsync(long alertId, string message)
        {
            try
            {
                bool ok = await SendKakaoAndLogAsync(alertId, message);                    // 보내고 이력 남기기 (아래)
                await _alertRepo.UpdateKakaoStatusAsync(alertId, ok ? "SENT" : "FAILED");  // 경고 행의 PENDING → SENT/FAILED
            }
            catch (Exception ex)
            {
                if (!IsDisposed) SetDbStatus(false, "경고 상태 갱신 실패", ex.Message);   // 그 사이 창이 닫혔을 수 있어서 확인
            }
        }

        // async 가 없는 void 메서드: 안에서 await 를 안 하고 발송을 _ = 로 던져 두기만 하므로
        // 호출: CheckAlertAsync (정상으로 돌아왔을 때)
        /// <summary>④ 정상 복귀: 구간 요약을 보내고 구간을 닫습니다.</summary>
        private void EndAlert(TempReading r)
        {
            _alertActive = false;                                   // 구간 닫기
            if (!_alertKakaoSent || !_config.KakaoEnabled) return;  // 이 구간에 카톡을 안 보냈거나 카톡이 꺼져 있으면 복귀 알림도 안 보냄

            DateTime now = r.ReadTime;
            string message = $"[정상 복귀] {_sensorName}\n" +
                             $"현재 {r.Temperature:0.0}℃ (기준 {_config.AlertTemp:0.0}℃ 미만)\n" +
                             $"경고 지속 {FormatDuration(now - _alertStartTime)} ({_alertStartTime:HH:mm:ss} ~ {now:HH:mm:ss})\n" +
                             $"최고 {_alertPeakTemp:0.0}℃ ({_alertPeakTime:HH:mm:ss})";

            _lastKakaoTime = now;   // 복귀 직후 다시 경고가 나도 최소 간격 안이면 시작 알림 생략 (오르락내리락 대비)
            // 복귀는 '경고'가 아니므로 TB_ALERT_LOG 에는 넣지 않고, 카톡 이력만 이 구간의 첫 경고에 연결
            // (경고 발송과 마찬가지로 기다리지 않음. SendKakaoAndLogAsync 는 내부에서 예외를 모두 처리)
            _ = SendKakaoAndLogAsync(_alertFirstId, message);
        }

        // TimeSpan → "1시간 5분" / "3분 20초" / "45초". static: 입력만으로 계산
        private static string FormatDuration(TimeSpan t)
        {
            // (int)t.TotalHours : 전체 시간을 정수로 (1.5시간 → 1) / t.Minutes : 시간을 뺀 나머지 분
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}시간 {t.Minutes}분";
            if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}분 {t.Seconds}초";
            return $"{Math.Max(0, t.Seconds)}초";   // 음수가 나오지 않게 0 이상으로
        }

        /// <summary>
        /// 카카오톡을 보내고 결과를 TB_KAKAO_LOG 에 남깁니다. 예외를 밖으로 던지지 않습니다.
        /// (KakaoService 는 실패를 결과값으로 돌려주고, 이력 저장 실패는 여기서 처리)
        /// </summary>
        // 부르는 곳 3군데: SendAlertKakaoAsync(경고), EndAlert(복귀), btnKakaoTest_Click(테스트)
        //   → "보내고 이력 남기기" 를 한 메서드로 모아서 세 기능이 똑같이 기록되게
        // long? alertId : 테스트 발송은 연결할 경고가 없어서 null 허용
        private async Task<bool> SendKakaoAndLogAsync(long? alertId, string message)
        {
            DateTime sendTime = DateTime.Now;   // 발송 시각
            // Services/KakaoService.cs: 실패해도 예외 대신 Success=false 결과를 돌려줌
            KakaoSendResult result = await _kakao.SendToMeAsync(message);

            try
            {
                // Data/KakaoLogRepository.cs → TB_KAKAO_LOG INSERT (성공이면 사유 null)
                await _kakaoRepo.InsertAsync(alertId, sendTime, _config.KakaoReceiverName, message,
                                             result.Success, result.Success ? null : result.Message);
            }
            catch (Exception ex)
            {
                // 발송 중에 창이 닫혔을 수 있으므로 확인 후 표시
                if (!IsDisposed) SetDbStatus(false, "카톡 이력 저장 실패", ex.Message);
            }
            return result.Success;   // 보낸 결과 (SendAlertKakaoAsync 가 SENT/FAILED 결정에 사용)
        }

        // 카톡 토큰 설정 여부 표시. 부르는 곳: MainForm_Load, btnKakaoTest_Click
        private async Task RefreshKakaoStatusAsync()
        {
            try
            {
                bool configured = await _kakao.IsConfiguredAsync();   // KakaoService → ConfigRepository → DB 의 토큰 확인
                lblKakaoStatus.Text = configured ? "카카오 연동: 토큰 설정됨" : "카카오 연동: 토큰 미설정";
                lblKakaoStatus.ForeColor = configured ? ColorNormal : Color.Gray;
            }
            catch   // DB 오류 등
            {
                lblKakaoStatus.Text = "카카오 연동: 확인 불가";
                lblKakaoStatus.ForeColor = Color.Gray;
            }
        }

        // [테스트 발송] 버튼 (디자이너에서 btnKakaoTest.Click 에 연결)
        private async void btnKakaoTest_Click(object sender, EventArgs e)
        {
            // 확인 창: 예/아니오. Yes 가 아니면 종료
            if (MessageBox.Show(this, "설정된 카카오톡 계정으로 테스트 메시지를 보낼까요?", "테스트 발송",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnKakaoTest.Enabled = false;   // 중복 클릭 방지
            try
            {
                string message = $"[테스트] 온도 모니터링 시스템 알림 테스트\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                bool ok = await SendKakaoAndLogAsync(null, message);   // 경고가 아니니까 alertId = null

                await RefreshKakaoStatusAsync();   // 토큰이 갱신됐을 수 있으니 상태 다시 표시
                await SearchKakaoAsync();          // 카톡 이력 탭 새로고침 (방금 보낸 이력이 보이게)
                MessageBox.Show(this, ok ? "발송되었습니다." : "발송에 실패했습니다. 결과 열에서 사유를 확인하세요.",
                    "테스트 발송", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "테스트 발송", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnKakaoTest.Enabled = true;   // 성공/실패 상관없이 버튼 다시 켜기
            }
        }

        // =============================================================================
        //  최저/최고 초기화
        // =============================================================================

        // [최저/최고 초기화] 버튼 (디자이너에서 btnResetMinMax.Click 에 연결)
        // 폴링 중에 눌러도 ModbusTcpClient 의 _ioLock 덕분에 읽기 요청과 섞이지 않음
        private async void btnResetMinMax_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "센서에 저장된 최저/최고 온도를 초기화할까요?", "초기화",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnResetMinMax.Enabled = false;
            try
            {
                await _sensor.ResetMinMaxAsync();   // Sig5560Client → ModbusTcpClient.WriteSingleCoilAsync(160, true)
                lblMinMax.Text = "최저/최고 초기화됨";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "초기화 실패\n\n" + ex.Message, "초기화", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnResetMinMax.Enabled = _pollCts != null;   // 그 사이 연결 해제했으면(null) 다시 켜지 않음
            }
        }

        // =============================================================================
        //  조회 탭
        // =============================================================================

        // 기록 조회 탭 [조회] 버튼 (디자이너에서 btnSearch.Click 에 연결)
        private async void btnSearch_Click(object sender, EventArgs e)
        {
            await SearchHistoryAsync();   // 실제 조회는 아래 메서드 ([오늘] 버튼도 같이 씀)
        }

        // 기록 조회 탭 [오늘] 버튼: 기간을 오늘 0시~지금으로 바꾸고 바로 조회
        private async void btnToday_Click(object sender, EventArgs e)
        {
            dateTimePicker1.Value = DateTime.Today;
            dateTimePicker2.Value = DateTime.Now;
            await SearchHistoryAsync();
        }

        // 부르는 곳: tabMain_SelectedIndexChanged, SearchHistoryAsync, btnAlertSearch_Click
        /// <summary>"지금 따라가기" 상태인 종료 시각을 현재 시각으로 갱신</summary>
        private void RefreshEndTimes()
        {
            if (_histEndFollowsNow) dateTimePicker2.Value = DateTime.Now;
            if (_alertEndFollowsNow) dtpAlertEnd.Value = DateTime.Now;
        }

        // 기록 조회. 부르는 곳: btnSearch_Click, btnToday_Click
        private async Task SearchHistoryAsync()
        {
            RefreshEndTimes();
            // DateTimePicker 는 초까지 값을 들고 있으므로 분 단위로 잘라서 사용
            DateTime from = TruncateToMinute(dateTimePicker1.Value);
            DateTime to = TruncateToMinute(dateTimePicker2.Value).AddMinutes(1);   // 종료 분 포함 (15:30 → 15:31 미만까지)
            if (!ValidateRange(from, to)) return;   // 시작이 종료보다 늦으면 경고 후 끝

            // RunSearchAsync(버튼, 조회내용) : 버튼 잠금/커서/오류 처리는 공통 틀이 하고, 가운데 "조회 내용" 만 람다로 넘김
            // async () => { ... } : 매개변수 없는 비동기 람다. 바깥 변수 from, to 를 그대로 사용(캡처)
            await RunSearchAsync(btnSearch, async () =>
            {
                // Data/TempLogRepository.cs → 기간 조회 결과 표
                DataTable dt = await _tempRepo.SearchAsync(_sensorId, from, to, MaxHistoryRows);
                dgvHistory.DataSource = dt;   // 그리드에 표 연결 → 디자이너 컬럼의 DataPropertyName 과 SQL 컬럼명이 짝지어져 표시
                lblTotalCount.Text = dt.Rows.Count >= MaxHistoryRows
                    ? $"총 {dt.Rows.Count:N0}건 (최대)"   // N0 : 천 단위 쉼표 (50,000)
                    : $"총 {dt.Rows.Count:N0}건";
            });
        }

        // 경고 조회 탭 [조회] 버튼 (디자이너에서 btnAlertSearch.Click 에 연결)
        private async void btnAlertSearch_Click(object sender, EventArgs e)
        {
            RefreshEndTimes();
            DateTime from = TruncateToMinute(dtpAlertStart.Value);
            DateTime to = TruncateToMinute(dtpAlertEnd.Value).AddMinutes(1);
            if (!ValidateRange(from, to)) return;

            await RunSearchAsync(btnAlertSearch, async () =>
            {
                DataTable dt = await _alertRepo.SearchAsync(from, to);   // Data/AlertRepository.cs
                dgvAlert.DataSource = dt;
                lblAlertCount.Text = $"경고 {dt.Rows.Count:N0}건";
            });
        }

        // 카톡 이력 탭 [조회] 버튼 (디자이너에서 btnKakaoSearch.Click 에 연결)
        private async void btnKakaoSearch_Click(object sender, EventArgs e)
        {
            await SearchKakaoAsync();   // btnKakaoTest_Click 도 같은 메서드를 씀
        }

        // 카톡 이력 조회. 부르는 곳: btnKakaoSearch_Click, btnKakaoTest_Click
        private async Task SearchKakaoAsync()
        {
            // 날짜만 고르는 화면: 종료일 "당일 전체"를 포함하려면 다음 날 0시 미만으로 조회
            DateTime from = dtpKakaoStart.Value.Date;           // .Date : 시각을 0시로 (날짜만)
            DateTime to = dtpKakaoEnd.Value.Date.AddDays(1);    // 종료일 다음 날 0시
            if (!ValidateRange(from, to)) return;

            await RunSearchAsync(btnKakaoSearch, async () =>
            {
                DataTable dt = await _kakaoRepo.SearchAsync(from, to);   // Data/KakaoLogRepository.cs
                dgvKakao.DataSource = dt;
                lblKakaoCount.Text = $"발송 {dt.Rows.Count:N0}건";
            });
        }

        /// <summary>조회 버튼 공통 처리: 중복 클릭 방지 + 대기 커서 + 오류 메시지</summary>
        // button : 잠글 조회 버튼 (어느 탭 버튼이든)
        // Func<Task> search : "매개변수 없고 Task 를 돌려주는 메서드" 를 담는 타입 → 코드를 변수처럼 넘겨받음
        // 조회 3개 모두 "잠그기 → 커서 → 조회 → 오류 처리 → 풀기" 가 같고 가운데 조회 내용만 달라서 틀을 하나로 모음
        private async Task RunSearchAsync(Button button, Func<Task> search)
        {
            button.Enabled = false;          // 중복 클릭 방지
            Cursor = Cursors.WaitCursor;     // 폼 전체 모래시계
            try
            {
                await search();              // ← 넘겨받은 람다(실제 조회 내용) 실행
                SetDbStatus(true, "연결됨");
            }
            catch (Exception ex)             // 조회 중 DB 오류 등
            {
                SetDbStatus(false, "조회 실패");
                MessageBox.Show(this, "조회 중 오류가 발생했습니다.\n\n" + ex.Message, "조회",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;    // 커서 원래대로
                button.Enabled = true;       // 버튼 다시 켜기
            }
        }

        // 기간 검사. 부르는 곳: 조회 3곳 공통. 올바르면 true
        private bool ValidateRange(DateTime from, DateTime to)
        {
            if (from >= to)
            {
                MessageBox.Show(this, "시작 시각이 종료 시각보다 늦습니다.", "조회", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // 초를 0 으로 잘라 분 단위로 (15:30:45 → 15:30:00). static: 입력만으로 계산
        private static DateTime TruncateToMinute(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);   // 년,월,일,시,분은 그대로, 초만 0
        }

        // 디자이너에서 dgvHistory.CellFormatting 에 연결: 그리드가 칸을 "그릴 때마다" 호출됨
        /// <summary>기록 조회에서 경고/오류 행을 색으로 구분</summary>
        private void dgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // DataBoundItem : 이 행에 연결된 원본 데이터. DataTable 을 바인딩하면 각 행은 DataRowView
            // as DataRowView : 타입이 안 맞으면 예외 대신 null
            var rowView = dgvHistory.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null) return;

            // STATUS_CD 는 그리드 컬럼에는 없지만 SQL 결과(DataTable)에는 있음
            // → 화면엔 한글 이름(STATUS_NM)을 보여 주고, 판단은 코드로 (한글 이름이 바뀌어도 색 판단이 안 깨지게)
            string code = rowView["STATUS_CD"] as string;   // DBNull 이면 as 가 null 로
            if (code == "WARNING") e.CellStyle.ForeColor = ColorWarning;   // 이 칸 글자색 빨강
            else if (code == "ERROR") e.CellStyle.ForeColor = ColorError;  // 주황
        }

        // =============================================================================
        //  설정 탭
        // =============================================================================

        // [설정 저장] 버튼 (디자이너에서 btnSaveSetting.Click 에 연결)
        // 흐름: 권한 확인 → 화면 값으로 새 AppConfig → 검사 → ConfigRepository.SaveAppConfigAsync(트랜잭션) → _config 교체
        private async void btnSaveSetting_Click(object sender, EventArgs e)
        {
            // 버튼이 비활성화되어 있어도, 권한 체크는 실제 처리하는 곳에서 한 번 더 합니다.
            if (!Session.IsAdmin)
            {
                MessageBox.Show(this, "설정은 관리자만 변경할 수 있습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 바로 _config 를 고치지 않고 "새 객체" 를 만드는 이유: DB 저장이 성공한 뒤에만 교체하려고 (실패하면 기존 설정 유지)
            // NumericUpDown.Value 는 decimal → int 속성에는 (int) 형변환
            var newConfig = new AppConfig
            {
                AlertTemp = nudAlertTemp.Value,
                AlertIntervalMin = (int)nudAlertInterval.Value,
                AlertRiseStep = nudRiseStep.Value,
                KakaoEnabled = chkKakaoAlert.Checked,
                ReadCycleSec = (int)nudReadCycle.Value,
                SaveCycleSec = (int)nudSaveCycle.Value,
                KakaoReceiverName = _config.KakaoReceiverName   // 화면에 없는 값은 기존 값 유지
            };

            if (newConfig.SaveCycleSec < newConfig.ReadCycleSec)   // 저장 주기가 읽기 주기보다 짧으면 의미가 없음
            {
                MessageBox.Show(this, "DB 저장 주기는 센서 읽기 주기보다 짧을 수 없습니다.\n(읽지 않은 값을 저장할 수는 없으니까요)",
                    "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSaveSetting.Enabled = false;
            try
            {
                // Data/ConfigRepository.cs: AppConfig → 사전 → 트랜잭션 UPSERT. Session.CurrentUser.UserId = 저장한 사람
                await _configRepo.SaveAppConfigAsync(newConfig, Session.CurrentUser.UserId);
                _config = newConfig;          // 폴링 루프는 다음 회차부터 새 설정을 사용
                UpdateThresholdLabel();       // 기준 온도 라벨 갱신
                MessageBox.Show(this, "설정이 저장되었습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)   // SaveAsync 가 Rollback 후 다시 던진 예외
            {
                MessageBox.Show(this, "설정 저장 실패\n\n" + ex.Message, "설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveSetting.Enabled = Session.IsAdmin;   // 관리자면 다시 켜기
            }
        }

        // =============================================================================
        //  로그아웃 / 종료
        // =============================================================================

        // [로그아웃] 버튼 (디자이너에서 btnLogout.Click 에 연결)
        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "로그아웃 하시겠습니까?", "로그아웃",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            LogoutRequested = true;   // Program.Main 이 창이 닫힌 뒤 이 값을 읽고 로그인 창을 다시 띄움
            Close();                  // 창 닫기 → 아래 MainForm_FormClosing 이 먼저 실행됨
        }

        // 창이 닫히기 직전에 실행 (디자이너에서 this.FormClosing 에 연결). X 버튼, Close() 모두
        // FormClosingEventArgs e : e.CloseReason(왜 닫히나), e.Cancel(true 로 하면 닫기 취소)
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 모니터링 중 실수로 창을 닫으면 데이터 수집이 멈추므로 한 번 확인
            // 조건: 모니터링 중(_pollCts != null) && 로그아웃 버튼이 아님(이미 확인했으니) && 사용자가 직접 닫음(X 버튼)
            if (_pollCts != null && !LogoutRequested && e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show(this, "센서 모니터링 중입니다. 종료하면 데이터 수집이 중단됩니다.\n종료하시겠습니까?",
                        "종료", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    e.Cancel = true;   // 닫기 취소 → 창 그대로
                    return;
                }
            }

            tmrClock.Stop();   // Timer 는 디자이너의 components 에 들어 있어서 폼이 닫힐 때 같이 정리됨
            StopPolling();     // 폴링 루프 중지 + 소켓 닫기
            _modbus.Dispose(); // ModbusTcpClient 정리 (IDisposable)
        }
    }
}
