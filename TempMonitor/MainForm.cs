using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TempMonitor.Comm;
using TempMonitor.Data;
using TempMonitor.Models;
using TempMonitor.Services;

namespace TempMonitor
{
    public partial class MainForm : Form
    {
        // ---- 화면 표시 한도 -------------------------------------------------------------
        private const int MaxChartPoints = 120;      // 차트에 유지할 점 개수 (1초 주기면 최근 2분)
        private const int MaxRealtimeRows = 200;     // 실시간 그리드에 유지할 행 수
        private const int MaxHistoryRows = 50000;    // 기록 조회 최대 행 수 (메모리/화면 보호)

        // ---- 색상 ---------------------------------------------------------------------
        private static readonly Color ColorNormal = Color.SeaGreen;
        private static readonly Color ColorWarning = Color.Crimson;
        private static readonly Color ColorError = Color.DarkOrange;
        private static readonly Color ColorOffline = Color.Gray;
        private static readonly Color ColorNavy = Color.FromArgb(31, 56, 100);

        // ---- 통신 ---------------------------------------------------------------------
        private readonly ModbusTcpClient _modbus = new ModbusTcpClient();
        private readonly Sig5560Client _sensor;
        private CancellationTokenSource _pollCts;
        private string _host;
        private int _port;

        // ---- 데이터 접근 / 서비스 -----------------------------------------------------
        private readonly SensorRepository _sensorRepo = new SensorRepository();
        private readonly TempLogRepository _tempRepo = new TempLogRepository();
        private readonly AlertRepository _alertRepo = new AlertRepository();
        private readonly KakaoLogRepository _kakaoRepo = new KakaoLogRepository();
        private readonly ConfigRepository _configRepo = new ConfigRepository();
        private readonly KakaoService _kakao = new KakaoService();

        // ---- 상태 ---------------------------------------------------------------------
        private AppConfig _config = new AppConfig();
        private int _sensorId = 1;
        private string _sensorName = "온도센서";

        private DateTime _lastSaveTime = DateTime.MinValue;
        private TempStatus? _lastSavedStatus;        // 상태가 바뀌면 저장 주기와 상관없이 즉시 저장
        private DateTime? _lastAlertTime;
        private DateTime? _lastKakaoTime;

        // ---- 경고 "구간" 상태 (정상 → 경고 시작 ~ 정상 복귀 까지를 하나의 경고 건으로 봄) ----
        private bool _alertActive;
        private long? _alertFirstId;           // 이 경고 구간의 첫 경고 ALERT_ID (복귀 알림 카톡 이력을 여기에 연결)
        private DateTime _alertStartTime;
        private decimal _alertPeakTemp;         // 구간 중 최고 온도
        private DateTime _alertPeakTime;
        private decimal _lastAlertTemp;         // 마지막으로 경고를 기록/알린 온도 (상승 알림의 기준)
        private bool _alertKakaoSent;           // 이 구간에서 카톡을 1건이라도 보냈는지 (보낸 적 없으면 복귀 알림도 생략)

        // ---- 조회 종료 시각 (켜 둔 채로 시간이 지나도 종료가 "켠 시각"에 멈추지 않게) ----
        private bool _histEndFollowsNow = true;
        private bool _alertEndFollowsNow = true;

        private readonly System.Windows.Forms.Timer _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };

        /// <summary>로그아웃 버튼으로 닫혔는지 (Program.cs 에서 로그인 창을 다시 띄울지 판단)</summary>
        public bool LogoutRequested { get; private set; }

        public MainForm()
        {
            InitializeComponent();
            _sensor = new Sig5560Client(_modbus);

            StartPosition = FormStartPosition.CenterScreen;

            // 이벤트 연결
            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
            btnLogout.Click += btnLogout_Click;
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
            btnResetMinMax.Click += btnResetMinMax_Click;
            btnSearch.Click += btnSearch_Click;
            btnToday.Click += btnToday_Click;
            btnAlertSearch.Click += btnAlertSearch_Click;
            btnKakaoSearch.Click += btnKakaoSearch_Click;
            btnKakaoTest.Click += btnKakaoTest_Click;
            btnSaveSetting.Click += btnSaveSetting_Click;
            dgvHistory.CellFormatting += dgvHistory_CellFormatting;
            _clockTimer.Tick += (s, e) => tslClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 조회 종료 시각 "지금 따라가기": 종료를 현재 분 이후로 두면 조회/탭 이동 때마다 현재 시각으로 갱신,
            // 사용자가 과거 시각을 직접 고르면 그 값으로 고정
            dateTimePicker2.ValueChanged += (s, e) => _histEndFollowsNow = dateTimePicker2.Value >= TruncateToMinute(DateTime.Now);
            dtpAlertEnd.ValueChanged += (s, e) => _alertEndFollowsNow = dtpAlertEnd.Value >= TruncateToMinute(DateTime.Now);
            tabMain.SelectedIndexChanged += (s, e) => RefreshEndTimes();
        }

        // =============================================================================
        //  초기화
        // =============================================================================

        private async void MainForm_Load(object sender, EventArgs e)
        {
            UserInfo user = Session.CurrentUser;
            lblUser.Text = user == null ? "" : $"사용자 : {user.UserId}(권한 : {user.RoleName})";

            InitCurrentPanel();
            InitChart();
            InitGrids();
            InitSearchDates();
            ApplyPermission();

            tslClock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();

            // DB 에서 설정과 센서 정보 읽기 (실패해도 화면은 뜨도록 기본값으로 진행)
            try
            {
                bool dbOk = await DbHelper.TestConnectionAsync();
                SetDbStatus(dbOk, dbOk ? "연결됨" : "연결 실패");
                if (!dbOk) return;

                _config = await _configRepo.LoadAppConfigAsync();
                ApplyConfigToUi();

                SensorInfo sensor = await _sensorRepo.GetDefaultSensorAsync();
                if (sensor != null)
                {
                    _sensorId = sensor.SensorId;
                    _sensorName = sensor.SensorName;
                    _modbus.UnitId = sensor.UnitId;
                    txtIp.Text = sensor.IpAddress;
                    txtPort.Text = sensor.Port.ToString();
                }

                await RefreshKakaoStatusAsync();
            }
            catch (Exception ex)
            {
                SetDbStatus(false, "오류");
                MessageBox.Show(this, "초기 데이터를 불러오지 못했습니다.\n\n" + ex.Message, "DB 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitCurrentPanel()
        {
            lblCurrentTemp.Text = "--.-";
            lblUnit.Text = "℃";
            lblStatus.Text = "연결 안 됨";
            lblLastUpdate.Text = "최종 수신 : -";
            lblMinMax.Text = "최저 - / 최고 -";
            UpdateThresholdLabel();

            // 사각형 패널을 동그란 램프 모양으로 자르기
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, pnlLamp.Width, pnlLamp.Height);
            pnlLamp.Region = new Region(path);
            pnlLamp.BackColor = ColorOffline;
        }

        private void InitChart()
        {
            ChartArea area = chartTemp.ChartAreas[0];
            area.AxisX.LabelStyle.Format = "HH:mm:ss";
            area.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.IsStartedFromZero = false;    // 0℃ 부터 그리면 변화가 납작하게 보임
            area.AxisY.LabelStyle.Format = "0.0";
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.Title = "℃";
            // Y축 제목은 기본이 90도 회전(세로 쓰기)이라 ℃ 가 눕혀 보임 → 가로로 똑바로
            area.AxisY.TextOrientation = TextOrientation.Horizontal;

            Series temp = chartTemp.Series[0];
            temp.Name = "온도";
            temp.ChartType = SeriesChartType.Line;
            temp.XValueType = ChartValueType.DateTime;
            temp.BorderWidth = 2;
            temp.Color = ColorNavy;

            // 경고 기준선: 별도 시리즈로 그려야 Y축 자동 범위에 기준 온도가 포함됩니다.
            var threshold = new Series("기준")
            {
                ChartArea = area.Name,
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.DateTime,
                BorderDashStyle = ChartDashStyle.Dash,
                BorderWidth = 1,
                Color = ColorWarning,
                IsVisibleInLegend = true
            };
            chartTemp.Series.Add(threshold);

            temp.IsVisibleInLegend = true;
            chartTemp.Legends[0].Docking = Docking.Top;
        }

        private void InitGrids()
        {
            // DataTable 을 바인딩할 때 디자이너에서 만든 컬럼을 그대로 쓰고, 자동 컬럼 생성은 끕니다.
            dgvHistory.AutoGenerateColumns = false;
            colHistTime.DataPropertyName = "MEASURE_DT";
            colHistTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            colHistTemp.DataPropertyName = "TEMP_VALUE";
            colHistTemp.DefaultCellStyle.Format = "0.0";
            colHistTemp.DefaultCellStyle.NullValue = "-";   // 센서 오류로 온도가 NULL 인 행은 빈칸 대신 "-"
            colHistStatus.DataPropertyName = "STATUS_NM";

            dgvAlert.AutoGenerateColumns = false;
            colAlertTime.DataPropertyName = "ALERT_DT";
            colAlertTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            colAlertTemp.DataPropertyName = "TEMP_VALUE";
            colAlertTemp.DefaultCellStyle.Format = "0.0";
            colAlertThreshold.DataPropertyName = "THRESHOLD_VALUE";
            colAlertThreshold.DefaultCellStyle.Format = "0.0";
            colAlertKakao.DataPropertyName = "KAKAO_STATUS_NM";

            dgvKakao.AutoGenerateColumns = false;
            colKakaoTime.DataPropertyName = "SEND_DT";
            colKakaoTime.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            colKakaoReceiver.DataPropertyName = "RECEIVER_NM";
            colKakaoMessage.DataPropertyName = "MESSAGE";
            colKakaoResult.DataPropertyName = "RESULT_NM";

            lblTotalCount.Text = "총 0건";
            lblAlertCount.Text = "경고 0건";
            lblKakaoCount.Text = "발송 0건";

            // 모든 그리드는 "보기 전용" (장비/DB 에서 받은 데이터를 보여주기만 함)
            // 정렬은 "의미 있는 컬럼"에만 허용. 나머지 헤더는 눌러도 아무 일 없음.
            MakeViewOnly(dgvRealtime);                              // 실시간은 항상 최신이 맨 위 → 정렬 금지
            MakeViewOnly(dgvHistory, colHistTime, colHistTemp, colHistStatus); // 시간순 + 온도순(최고/최저 시점) + 상태별(경고/오류 모아보기)
            MakeViewOnly(dgvAlert, colAlertTime);                  // 발생 시각순만
            MakeViewOnly(dgvKakao, colKakaoTime);                  // 발송 시각순만
        }

        /// <summary>
        /// 행 선택(파란 하이라이트)을 없애고 편집도 막습니다.
        /// 선택 상태가 남아 있으면 경고/오류 행의 색(주황/빨강)이 파란색에 가려지고,
        /// 사용자가 "선택한 행에 뭔가 할 수 있나?" 하고 오해하게 됩니다.
        /// sortableColumns 에 넘긴 컬럼만 헤더 클릭 정렬이 됩니다.
        /// </summary>
        private static void MakeViewOnly(DataGridView dgv, params DataGridViewColumn[] sortableColumns)
        {
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.MultiSelect = false;

            // 선택되는 즉시 해제 → 하이라이트가 생기지 않음 (데이터 바인딩 직후 첫 행 자동 선택도 같이 해제됨)
            dgv.SelectionChanged += (s, e) => dgv.ClearSelection();

            // 현재 셀이 있는 컬럼의 헤더가 파랗게 칠해지는 것도 막기 (헤더 선택색 = 평소색)
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                bool sortable = Array.IndexOf(sortableColumns, col) >= 0;
                col.SortMode = sortable ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable;
                if (sortable) col.ToolTipText = "클릭하면 정렬됩니다";
            }

            // 조회 쿼리는 모두 "시각 DESC" 로 가져오므로, 조회 직후 첫 번째 정렬 컬럼(시각)에 ▼ 를 표시해
            // 지금 화면이 어떤 순서인지 알려 줍니다. (헤더를 눌러 정렬하면 그 컬럼으로 ▲/▼ 가 옮겨감)
            if (sortableColumns.Length > 0)
            {
                DataGridViewColumn defaultColumn = sortableColumns[0];
                dgv.DataBindingComplete += (s, e) =>
                {
                    if (dgv.SortedColumn == null && dgv.DataSource != null)
                        defaultColumn.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                };
            }

            // 정렬 가능한 헤더 위에서는 손 모양 커서 → "누를 수 있다"는 신호
            dgv.CellMouseEnter += (s, e) =>
            {
                bool sortableHeader = e.RowIndex == -1 && e.ColumnIndex >= 0
                    && dgv.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable;
                dgv.Cursor = sortableHeader ? Cursors.Hand : Cursors.Default;
            };
            dgv.MouseLeave += (s, e) => dgv.Cursor = Cursors.Default;
        }

        private void InitSearchDates()
        {
            DateTime today = DateTime.Today;
            dateTimePicker1.Value = today;
            dateTimePicker2.Value = DateTime.Now;
            dtpAlertStart.Value = today.AddDays(-7);
            dtpAlertEnd.Value = DateTime.Now;
            dtpKakaoStart.Value = today.AddDays(-7);
            dtpKakaoEnd.Value = today;
        }

        /// <summary>권한에 따라 화면 기능 제한 (설정 변경은 관리자만)</summary>
        private void ApplyPermission()
        {
            bool admin = Session.IsAdmin;
            grpAlertSetting.Enabled = admin;
            grpCycleSetting.Enabled = admin;
            btnSaveSetting.Enabled = admin;
        }

        private void ApplyConfigToUi()
        {
            SetNumeric(nudAlertTemp, _config.AlertTemp);
            SetNumeric(nudAlertInterval, _config.AlertIntervalMin);
            SetNumeric(nudRiseStep, _config.AlertRiseStep);
            SetNumeric(nudReadCycle, _config.ReadCycleSec);
            SetNumeric(nudSaveCycle, _config.SaveCycleSec);
            chkKakaoAlert.Checked = _config.KakaoEnabled;
            UpdateThresholdLabel();
        }

        /// <summary>NumericUpDown 에 범위를 벗어난 값을 넣으면 예외가 나므로 범위 안으로 맞춰 넣습니다.</summary>
        private static void SetNumeric(NumericUpDown nud, decimal value)
        {
            nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, value));
        }

        private void UpdateThresholdLabel()
        {
            lblThresholdInfo.Text = $"경고 기준 : {_config.AlertTemp:0.0} ℃ 이상";
        }

        // =============================================================================
        //  센서 연결 / 해제
        // =============================================================================

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string host = txtIp.Text.Trim();
            int port;

            if (host.Length == 0)
            {
                MessageBox.Show(this, "센서 IP 주소를 입력하세요.", "센서 연결", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIp.Focus();
                return;
            }
            if (!int.TryParse(txtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show(this, "포트는 1~65535 사이 숫자여야 합니다. (SIG-5560 기본값 502)", "센서 연결",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPort.Focus();
                return;
            }

            SetConnectionUi(connecting: true);
            try
            {
                await _modbus.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                SetConnectionUi(connecting: false);
                SetSensorStatus("연결 실패");
                MessageBox.Show(this, $"센서({host}:{port})에 연결하지 못했습니다.\n\n{ex.Message}", "센서 연결",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _host = host;
            _port = port;
            SetConnectedUi();
            SetSensorStatus("연결됨");

            // 다음 실행 때 자동으로 채워지도록 접속 정보를 DB 에 저장 (실패해도 모니터링은 계속)
            try { await _sensorRepo.UpdateAddressAsync(_sensorId, host, port); }
            catch { /* DB 저장 실패는 무시 */ }

            _lastSavedStatus = null;
            _alertActive = false;   // 새로 연결하면 경고 구간도 새로 시작
            _pollCts = new CancellationTokenSource();
            await PollLoopAsync(_pollCts.Token);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            StopPolling();
            SetConnectionUi(connecting: false);
            SetSensorStatus("연결 안 됨");
            lblStatus.Text = "연결 안 됨";
            lblStatus.ForeColor = ColorOffline;
            lblCurrentTemp.ForeColor = ColorOffline;
            pnlLamp.BackColor = ColorOffline;
        }

        private void StopPolling()
        {
            if (_pollCts != null)
            {
                _pollCts.Cancel();
                _pollCts = null;
            }
            _modbus.Disconnect();
        }

        private void SetConnectionUi(bool connecting)
        {
            btnConnect.Enabled = !connecting;
            txtIp.Enabled = !connecting;
            txtPort.Enabled = !connecting;
            btnDisconnect.Enabled = false;
            btnResetMinMax.Enabled = false;
            if (connecting) SetSensorStatus("연결 중...");
        }

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
        private async Task PollLoopAsync(CancellationToken ct)
        {
            int failCount = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 통신이 끊겼으면 자동 재연결 (공장에서는 케이블/스위치 문제로 순간 끊김이 흔함)
                    if (!_modbus.IsConnected)
                    {
                        SetSensorStatus("재연결 중...");
                        await _modbus.ConnectAsync(_host, _port);

                        // 재연결하는 사이 사용자가 '연결 해제'를 눌렀다면 방금 연 연결을 닫고 종료
                        if (ct.IsCancellationRequested)
                        {
                            if (_pollCts == null) _modbus.Disconnect();
                            break;
                        }
                    }

                    TempReading reading = await _sensor.ReadAsync();
                    if (ct.IsCancellationRequested) break;

                    failCount = 0;
                    SetSensorStatus("연결됨");
                    await HandleReadingAsync(reading);
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested) break;   // 사용자가 연결 해제한 경우

                    failCount++;
                    // 상태 표시줄에는 짧게, 자세한 원인은 마우스를 올리면 보이는 툴팁으로
                    SetSensorStatus($"통신 오류 {failCount}회 (재연결 시도 중)", ex.Message);
                    lblStatus.Text = "통신 오류";
                    lblStatus.ForeColor = ColorOffline;
                    lblCurrentTemp.ForeColor = ColorOffline;   // 마지막 값은 남기되 회색 = "지금 값이 아님"
                    pnlLamp.BackColor = ColorOffline;
                }

                // 연속 실패 중에는 최소 3초 간격으로 재시도 (장비/네트워크에 부담 주지 않기)
                int delaySec = failCount > 0 ? Math.Max(3, _config.ReadCycleSec) : _config.ReadCycleSec;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task HandleReadingAsync(TempReading reading)
        {
            TempStatus status = Evaluate(reading);

            UpdateCurrentPanel(reading, status);
            AddRealtimeRow(reading, status);
            AddChartPoint(reading);

            await CheckAlertAsync(reading, status);
            await SaveIfNeededAsync(reading, status);
        }

        private TempStatus Evaluate(TempReading reading)
        {
            if (reading.HasError) return TempStatus.Error;
            if (reading.Temperature >= _config.AlertTemp) return TempStatus.Warning;
            return TempStatus.Normal;
        }

        // =============================================================================
        //  화면 갱신
        // =============================================================================

        private void UpdateCurrentPanel(TempReading r, TempStatus status)
        {
            lblLastUpdate.Text = "최종 수신 : " + r.ReadTime.ToString("HH:mm:ss");

            if (status == TempStatus.Error)
            {
                lblCurrentTemp.Text = "--.-";
                lblCurrentTemp.ForeColor = ColorError;
                // 패널은 좁으므로 짧은 이름 (전체 이름은 아래 실시간 기록 그리드에 표시)
                lblStatus.Text = "센서 오류 : " + r.ErrorShortText;
                lblStatus.ForeColor = ColorError;
                pnlLamp.BackColor = ColorError;
                return;
            }

            bool warn = status == TempStatus.Warning;
            lblCurrentTemp.Text = r.Temperature.ToString("0.0");
            lblCurrentTemp.ForeColor = warn ? ColorWarning : SystemColors.ControlText;
            lblStatus.Text = status.ToDisplayName();
            lblStatus.ForeColor = warn ? ColorWarning : ColorNormal;
            pnlLamp.BackColor = warn ? ColorWarning : ColorNormal;
            lblMinMax.Text = $"최저 {r.MinTemperature:0.0} / 최고 {r.MaxTemperature:0.0}";
        }

        private void AddRealtimeRow(TempReading r, TempStatus status)
        {
            string temp = r.HasError ? "-" : r.Temperature.ToString("0.0");
            string statusText = status == TempStatus.Error
                ? status.ToDisplayName() + " (" + r.ErrorText + ")"   // 어떤 오류인지까지 표시
                : status.ToDisplayName();
            dgvRealtime.Rows.Insert(0, r.ReadTime.ToString("HH:mm:ss"), temp, statusText);

            if (status != TempStatus.Normal)
                dgvRealtime.Rows[0].DefaultCellStyle.ForeColor = status == TempStatus.Warning ? ColorWarning : ColorError;

            while (dgvRealtime.Rows.Count > MaxRealtimeRows)
                dgvRealtime.Rows.RemoveAt(dgvRealtime.Rows.Count - 1);
        }

        private void AddChartPoint(TempReading r)
        {
            if (r.HasError) return;   // 오류 값은 차트에 그리지 않음

            Series temp = chartTemp.Series[0];
            Series threshold = chartTemp.Series[1];

            temp.Points.AddXY(r.ReadTime, (double)r.Temperature);
            threshold.Points.AddXY(r.ReadTime, (double)_config.AlertTemp);

            while (temp.Points.Count > MaxChartPoints) temp.Points.RemoveAt(0);
            while (threshold.Points.Count > MaxChartPoints) threshold.Points.RemoveAt(0);

            chartTemp.ResetAutoValues();   // 점을 지운 뒤 축 범위를 다시 계산
        }

        /// <summary>
        /// 상태 표시줄은 공간이 좁으므로 text 는 짧게, 긴 예외 메시지는 detail(툴팁)로 넘깁니다.
        /// </summary>
        private void SetSensorStatus(string text, string detail = null)
        {
            tslSensor.Text = "| 센서: " + text;
            tslSensor.ToolTipText = detail ?? tslSensor.Text;
        }

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
        private async Task SaveIfNeededAsync(TempReading reading, TempStatus status)
        {
            bool cycleDue = (reading.ReadTime - _lastSaveTime).TotalSeconds >= _config.SaveCycleSec;
            bool statusChanged = _lastSavedStatus != status;
            if (!cycleDue && !statusChanged) return;

            try
            {
                await _tempRepo.InsertAsync(_sensorId, reading, status);
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

        private enum AlertKind { Onset, Rise, Repeat }

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
        private async Task CheckAlertAsync(TempReading r, TempStatus status)
        {
            DateTime now = r.ReadTime;

            if (status == TempStatus.Normal)
            {
                if (_alertActive)
                    EndAlert(r);
                return;
            }
            if (status != TempStatus.Warning) return;   // ERROR: 구간 유지, 아무것도 안 함

            decimal temp = r.Temperature;
            TimeSpan interval = TimeSpan.FromMinutes(_config.AlertIntervalMin);
            AlertKind kind;

            if (!_alertActive)
            {
                // ① 새 경고 구간 시작
                _alertActive = true;
                _alertFirstId = null;
                _alertStartTime = now;
                _alertPeakTemp = temp;
                _alertPeakTime = now;
                _alertKakaoSent = false;
                kind = AlertKind.Onset;
            }
            else
            {
                if (temp > _alertPeakTemp) { _alertPeakTemp = temp; _alertPeakTime = now; }

                bool rise = _config.AlertRiseStep > 0 && temp - _lastAlertTemp >= _config.AlertRiseStep;
                bool repeatDue = _lastAlertTime == null || now - _lastAlertTime.Value >= interval;

                if (rise) kind = AlertKind.Rise;            // ② 상승이 지속보다 우선
                else if (repeatDue) kind = AlertKind.Repeat; // ③
                else return;
            }

            string kakaoStatus;
            if (!_config.KakaoEnabled)
                kakaoStatus = "DISABLED";
            else if (kind != AlertKind.Rise && _lastKakaoTime != null && now - _lastKakaoTime.Value < interval)
                kakaoStatus = "SKIPPED";
            else
                kakaoStatus = "PENDING";

            decimal prevAlertTemp = _lastAlertTemp;
            _lastAlertTime = now;
            _lastAlertTemp = temp;

            try
            {
                long alertId = await _alertRepo.InsertAsync(_sensorId, now, temp, _config.AlertTemp, kakaoStatus);
                if (_alertFirstId == null) _alertFirstId = alertId;

                if (kakaoStatus == "PENDING")
                {
                    _lastKakaoTime = now;   // 실패해도 시도 시각을 기록 → API 장애 시 매초 재시도하지 않음
                    _alertKakaoSent = true;
                    string message = BuildAlertMessage(kind, temp, prevAlertTemp, now);
                    // 발송은 기다리지 않음: 카카오 응답이 늦어도(최대 10초) 센서 읽기가 멈추면 안 되므로
                    _ = SendAlertKakaoAsync(alertId, message);
                }
            }
            catch (Exception ex)
            {
                SetDbStatus(false, "경고 저장 실패", ex.Message);
            }
        }

        private string BuildAlertMessage(AlertKind kind, decimal temp, decimal prevAlertTemp, DateTime now)
        {
            string body;
            switch (kind)
            {
                case AlertKind.Rise:
                    body = $"[온도 상승] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (직전 알림 {prevAlertTemp:0.0}℃ 대비 +{temp - prevAlertTemp:0.0}℃)\n" +
                           $"기준 {_config.AlertTemp:0.0}℃ / 경고 시작 {_alertStartTime:HH:mm:ss}";
                    break;
                case AlertKind.Repeat:
                    body = $"[경고 지속] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (기준 {_config.AlertTemp:0.0}℃ 이상)\n" +
                           $"지속 {FormatDuration(now - _alertStartTime)} / 최고 {_alertPeakTemp:0.0}℃";
                    break;
                default:
                    body = $"[온도 경고] {_sensorName}\n" +
                           $"현재 {temp:0.0}℃ (기준 {_config.AlertTemp:0.0}℃ 이상)";
                    break;
            }
            return body + $"\n{now:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// 경고 카톡 발송 → 결과를 경고 이력(KAKAO_STATUS_CD)에 반영.
        /// 폴링 루프가 await 하지 않고 던져 두는 작업이라, 예외를 여기서 전부 처리해야 합니다.
        /// </summary>
        private async Task SendAlertKakaoAsync(long alertId, string message)
        {
            try
            {
                bool ok = await SendKakaoAndLogAsync(alertId, message);
                await _alertRepo.UpdateKakaoStatusAsync(alertId, ok ? "SENT" : "FAILED");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) SetDbStatus(false, "경고 상태 갱신 실패", ex.Message);
            }
        }

        /// <summary>④ 정상 복귀: 구간 요약을 보내고 구간을 닫습니다.</summary>
        private void EndAlert(TempReading r)
        {
            _alertActive = false;
            if (!_alertKakaoSent || !_config.KakaoEnabled) return;

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

        private static string FormatDuration(TimeSpan t)
        {
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}시간 {t.Minutes}분";
            if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}분 {t.Seconds}초";
            return $"{Math.Max(0, t.Seconds)}초";
        }

        /// <summary>
        /// 카카오톡을 보내고 결과를 TB_KAKAO_LOG 에 남깁니다. 예외를 밖으로 던지지 않습니다.
        /// (KakaoService 는 실패를 결과값으로 돌려주고, 이력 저장 실패는 여기서 처리)
        /// </summary>
        private async Task<bool> SendKakaoAndLogAsync(long? alertId, string message)
        {
            DateTime sendTime = DateTime.Now;
            KakaoSendResult result = await _kakao.SendToMeAsync(message);

            try
            {
                await _kakaoRepo.InsertAsync(alertId, sendTime, _config.KakaoReceiverName, message,
                                             result.Success, result.Success ? null : result.Message);
            }
            catch (Exception ex)
            {
                // 발송 중에 창이 닫혔을 수 있으므로 확인 후 표시
                if (!IsDisposed) SetDbStatus(false, "카톡 이력 저장 실패", ex.Message);
            }
            return result.Success;
        }

        private async Task RefreshKakaoStatusAsync()
        {
            try
            {
                bool configured = await _kakao.IsConfiguredAsync();
                lblKakaoStatus.Text = configured ? "카카오 연동: 토큰 설정됨" : "카카오 연동: 토큰 미설정";
                lblKakaoStatus.ForeColor = configured ? ColorNormal : Color.Gray;
            }
            catch
            {
                lblKakaoStatus.Text = "카카오 연동: 확인 불가";
                lblKakaoStatus.ForeColor = Color.Gray;
            }
        }

        private async void btnKakaoTest_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "설정된 카카오톡 계정으로 테스트 메시지를 보낼까요?", "테스트 발송",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnKakaoTest.Enabled = false;
            try
            {
                string message = $"[테스트] 온도 모니터링 시스템 알림 테스트\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                bool ok = await SendKakaoAndLogAsync(null, message);

                await RefreshKakaoStatusAsync();
                await SearchKakaoAsync();
                MessageBox.Show(this, ok ? "발송되었습니다." : "발송에 실패했습니다. 결과 열에서 사유를 확인하세요.",
                    "테스트 발송", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "테스트 발송", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnKakaoTest.Enabled = true;
            }
        }

        // =============================================================================
        //  최저/최고 초기화
        // =============================================================================

        private async void btnResetMinMax_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "센서에 저장된 최저/최고 온도를 초기화할까요?", "초기화",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnResetMinMax.Enabled = false;
            try
            {
                await _sensor.ResetMinMaxAsync();
                lblMinMax.Text = "최저/최고 초기화됨";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "초기화 실패\n\n" + ex.Message, "초기화", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnResetMinMax.Enabled = _pollCts != null;
            }
        }

        // =============================================================================
        //  조회 탭
        // =============================================================================

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            await SearchHistoryAsync();
        }

        private async void btnToday_Click(object sender, EventArgs e)
        {
            dateTimePicker1.Value = DateTime.Today;
            dateTimePicker2.Value = DateTime.Now;
            await SearchHistoryAsync();
        }

        /// <summary>"지금 따라가기" 상태인 종료 시각을 현재 시각으로 갱신</summary>
        private void RefreshEndTimes()
        {
            if (_histEndFollowsNow) dateTimePicker2.Value = DateTime.Now;
            if (_alertEndFollowsNow) dtpAlertEnd.Value = DateTime.Now;
        }

        private async Task SearchHistoryAsync()
        {
            RefreshEndTimes();
            // DateTimePicker 는 초까지 값을 들고 있으므로 분 단위로 잘라서 사용
            DateTime from = TruncateToMinute(dateTimePicker1.Value);
            DateTime to = TruncateToMinute(dateTimePicker2.Value).AddMinutes(1);   // 종료 분 포함
            if (!ValidateRange(from, to)) return;

            await RunSearchAsync(btnSearch, async () =>
            {
                DataTable dt = await _tempRepo.SearchAsync(_sensorId, from, to, MaxHistoryRows);
                dgvHistory.DataSource = dt;
                lblTotalCount.Text = dt.Rows.Count >= MaxHistoryRows
                    ? $"총 {dt.Rows.Count:N0}건 (최대)"
                    : $"총 {dt.Rows.Count:N0}건";
            });
        }

        private async void btnAlertSearch_Click(object sender, EventArgs e)
        {
            RefreshEndTimes();
            DateTime from = TruncateToMinute(dtpAlertStart.Value);
            DateTime to = TruncateToMinute(dtpAlertEnd.Value).AddMinutes(1);
            if (!ValidateRange(from, to)) return;

            await RunSearchAsync(btnAlertSearch, async () =>
            {
                DataTable dt = await _alertRepo.SearchAsync(from, to);
                dgvAlert.DataSource = dt;
                lblAlertCount.Text = $"경고 {dt.Rows.Count:N0}건";
            });
        }

        private async void btnKakaoSearch_Click(object sender, EventArgs e)
        {
            await SearchKakaoAsync();
        }

        private async Task SearchKakaoAsync()
        {
            // 날짜만 고르는 화면: 종료일 "당일 전체"를 포함하려면 다음 날 0시 미만으로 조회
            DateTime from = dtpKakaoStart.Value.Date;
            DateTime to = dtpKakaoEnd.Value.Date.AddDays(1);
            if (!ValidateRange(from, to)) return;

            await RunSearchAsync(btnKakaoSearch, async () =>
            {
                DataTable dt = await _kakaoRepo.SearchAsync(from, to);
                dgvKakao.DataSource = dt;
                lblKakaoCount.Text = $"발송 {dt.Rows.Count:N0}건";
            });
        }

        /// <summary>조회 버튼 공통 처리: 중복 클릭 방지 + 대기 커서 + 오류 메시지</summary>
        private async Task RunSearchAsync(Button button, Func<Task> search)
        {
            button.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                await search();
                SetDbStatus(true, "연결됨");
            }
            catch (Exception ex)
            {
                SetDbStatus(false, "조회 실패");
                MessageBox.Show(this, "조회 중 오류가 발생했습니다.\n\n" + ex.Message, "조회",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                button.Enabled = true;
            }
        }

        private bool ValidateRange(DateTime from, DateTime to)
        {
            if (from >= to)
            {
                MessageBox.Show(this, "시작 시각이 종료 시각보다 늦습니다.", "조회", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static DateTime TruncateToMinute(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        }

        /// <summary>기록 조회에서 경고/오류 행을 색으로 구분</summary>
        private void dgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var rowView = dgvHistory.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null) return;

            string code = rowView["STATUS_CD"] as string;
            if (code == "WARNING") e.CellStyle.ForeColor = ColorWarning;
            else if (code == "ERROR") e.CellStyle.ForeColor = ColorError;
        }

        // =============================================================================
        //  설정 탭
        // =============================================================================

        private async void btnSaveSetting_Click(object sender, EventArgs e)
        {
            // 버튼이 비활성화되어 있어도, 권한 체크는 실제 처리하는 곳에서 한 번 더 합니다.
            if (!Session.IsAdmin)
            {
                MessageBox.Show(this, "설정은 관리자만 변경할 수 있습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newConfig = new AppConfig
            {
                AlertTemp = nudAlertTemp.Value,
                AlertIntervalMin = (int)nudAlertInterval.Value,
                AlertRiseStep = nudRiseStep.Value,
                KakaoEnabled = chkKakaoAlert.Checked,
                ReadCycleSec = (int)nudReadCycle.Value,
                SaveCycleSec = (int)nudSaveCycle.Value,
                KakaoReceiverName = _config.KakaoReceiverName
            };

            if (newConfig.SaveCycleSec < newConfig.ReadCycleSec)
            {
                MessageBox.Show(this, "DB 저장 주기는 센서 읽기 주기보다 짧을 수 없습니다.\n(읽지 않은 값을 저장할 수는 없으니까요)",
                    "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSaveSetting.Enabled = false;
            try
            {
                await _configRepo.SaveAppConfigAsync(newConfig, Session.CurrentUser.UserId);
                _config = newConfig;          // 폴링 루프는 다음 회차부터 새 설정을 사용
                UpdateThresholdLabel();
                MessageBox.Show(this, "설정이 저장되었습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "설정 저장 실패\n\n" + ex.Message, "설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveSetting.Enabled = Session.IsAdmin;
            }
        }

        // =============================================================================
        //  로그아웃 / 종료
        // =============================================================================

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "로그아웃 하시겠습니까?", "로그아웃",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            LogoutRequested = true;
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 모니터링 중 실수로 창을 닫으면 데이터 수집이 멈추므로 한 번 확인
            if (_pollCts != null && !LogoutRequested && e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show(this, "센서 모니터링 중입니다. 종료하면 데이터 수집이 중단됩니다.\n종료하시겠습니까?",
                        "종료", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _clockTimer.Stop();
            _clockTimer.Dispose();
            StopPolling();
            _modbus.Dispose();
        }
    }
}
