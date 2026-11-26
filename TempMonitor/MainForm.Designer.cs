namespace TempMonitor
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabMonitor = new System.Windows.Forms.TabPage();
            this.statusMain = new System.Windows.Forms.StatusStrip();
            this.tslDb = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslSensor = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslClock = new System.Windows.Forms.ToolStripStatusLabel();
            this.grpRealtime = new System.Windows.Forms.GroupBox();
            this.dgvRealtime = new System.Windows.Forms.DataGridView();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpChart = new System.Windows.Forms.GroupBox();
            this.chartTemp = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.grpCurrent = new System.Windows.Forms.GroupBox();
            this.btnResetMinMax = new System.Windows.Forms.Button();
            this.lblMinMax = new System.Windows.Forms.Label();
            this.lblThresholdInfo = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlLamp = new System.Windows.Forms.Panel();
            this.lblUnit = new System.Windows.Forms.Label();
            this.lblCurrentTemp = new System.Windows.Forms.Label();
            this.grpSensor = new System.Windows.Forms.GroupBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblPortNo = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblIp = new System.Windows.Forms.Label();
            this.tabHistory = new System.Windows.Forms.TabPage();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colHistTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlHistorySearch = new System.Windows.Forms.Panel();
            this.lblTotalCount = new System.Windows.Forms.Label();
            this.btnToday = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.lblEnd = new System.Windows.Forms.Label();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.lblStart = new System.Windows.Forms.Label();
            this.tabAlert = new System.Windows.Forms.TabPage();
            this.dgvAlert = new System.Windows.Forms.DataGridView();
            this.colAlertTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertKakao = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlAlertSearch = new System.Windows.Forms.Panel();
            this.lblAlertCount = new System.Windows.Forms.Label();
            this.btnAlertSearch = new System.Windows.Forms.Button();
            this.lblAlertEnd = new System.Windows.Forms.Label();
            this.dtpAlertEnd = new System.Windows.Forms.DateTimePicker();
            this.dtpAlertStart = new System.Windows.Forms.DateTimePicker();
            this.lblAlertStart = new System.Windows.Forms.Label();
            this.tabKakao = new System.Windows.Forms.TabPage();
            this.dgvKakao = new System.Windows.Forms.DataGridView();
            this.colKakaoTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKakaoReceiver = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKakaoMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKakaoResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlKakaoSearch = new System.Windows.Forms.Panel();
            this.lblKakaoStatus = new System.Windows.Forms.Label();
            this.btnKakaoTest = new System.Windows.Forms.Button();
            this.lblKakaoCount = new System.Windows.Forms.Label();
            this.btnKakaoSearch = new System.Windows.Forms.Button();
            this.lblKakaoEnd = new System.Windows.Forms.Label();
            this.dtpKakaoEnd = new System.Windows.Forms.DateTimePicker();
            this.dtpKakaoStart = new System.Windows.Forms.DateTimePicker();
            this.lblKakaoStart = new System.Windows.Forms.Label();
            this.tabSetting = new System.Windows.Forms.TabPage();
            this.lblAdminNotice = new System.Windows.Forms.Label();
            this.btnSaveSetting = new System.Windows.Forms.Button();
            this.grpCycleSetting = new System.Windows.Forms.GroupBox();
            this.nudSaveCycle = new System.Windows.Forms.NumericUpDown();
            this.nudReadCycle = new System.Windows.Forms.NumericUpDown();
            this.lblSaveCycle = new System.Windows.Forms.Label();
            this.lblReadCycle = new System.Windows.Forms.Label();
            this.grpAlertSetting = new System.Windows.Forms.GroupBox();
            this.chkKakaoAlert = new System.Windows.Forms.CheckBox();
            this.nudAlertInterval = new System.Windows.Forms.NumericUpDown();
            this.nudAlertTemp = new System.Windows.Forms.NumericUpDown();
            this.lblIntervalDesc = new System.Windows.Forms.Label();
            this.lblRiseStep = new System.Windows.Forms.Label();
            this.nudRiseStep = new System.Windows.Forms.NumericUpDown();
            this.lblRiseStepDesc = new System.Windows.Forms.Label();
            this.lblAlertInterval = new System.Windows.Forms.Label();
            this.lblAlertTemp = new System.Windows.Forms.Label();
            this.tmrClock = new System.Windows.Forms.Timer(this.components);
            this.pnlTop.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabMonitor.SuspendLayout();
            this.statusMain.SuspendLayout();
            this.grpRealtime.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRealtime)).BeginInit();
            this.grpChart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTemp)).BeginInit();
            this.grpCurrent.SuspendLayout();
            this.grpSensor.SuspendLayout();
            this.tabHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.pnlHistorySearch.SuspendLayout();
            this.tabAlert.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlert)).BeginInit();
            this.pnlAlertSearch.SuspendLayout();
            this.tabKakao.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKakao)).BeginInit();
            this.pnlKakaoSearch.SuspendLayout();
            this.tabSetting.SuspendLayout();
            this.grpCycleSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveCycle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReadCycle)).BeginInit();
            this.grpAlertSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAlertInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAlertTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRiseStep)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            this.pnlTop.Controls.Add(this.btnLogout);
            this.pnlTop.Controls.Add(this.lblUser);
            this.pnlTop.Controls.Add(this.lblAppTitle);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(842, 84);
            this.pnlTop.TabIndex = 0;
            // 
            // btnLogout
            // 
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(730, 28);
            this.btnLogout.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(96, 35);
            this.btnLogout.TabIndex = 2;
            this.btnLogout.Text = "로그아웃";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // lblUser
            // 
            this.lblUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUser.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblUser.ForeColor = System.Drawing.Color.White;
            this.lblUser.Location = new System.Drawing.Point(487, 35);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(236, 23);
            this.lblUser.TabIndex = 1;
            this.lblUser.Text = "사용자 : admin(권한 : 관리자)";
            this.lblUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(17, 30);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Size = new System.Drawing.Size(246, 32);
            this.lblAppTitle.TabIndex = 0;
            this.lblAppTitle.Text = "온도 모니터링 시스템";
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabMonitor);
            this.tabMain.Controls.Add(this.tabHistory);
            this.tabMain.Controls.Add(this.tabAlert);
            this.tabMain.Controls.Add(this.tabKakao);
            this.tabMain.Controls.Add(this.tabSetting);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 84);
            this.tabMain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(842, 624);
            this.tabMain.TabIndex = 1;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // tabMonitor
            // 
            this.tabMonitor.Controls.Add(this.statusMain);
            this.tabMonitor.Controls.Add(this.grpRealtime);
            this.tabMonitor.Controls.Add(this.grpChart);
            this.tabMonitor.Controls.Add(this.grpCurrent);
            this.tabMonitor.Controls.Add(this.grpSensor);
            this.tabMonitor.Location = new System.Drawing.Point(4, 25);
            this.tabMonitor.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabMonitor.Name = "tabMonitor";
            this.tabMonitor.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabMonitor.Size = new System.Drawing.Size(834, 595);
            this.tabMonitor.TabIndex = 0;
            this.tabMonitor.Text = "실시간 온도";
            this.tabMonitor.UseVisualStyleBackColor = true;
            // 
            // statusMain
            // 
            this.statusMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslDb,
            this.tslSensor,
            this.tslClock});
            this.statusMain.Location = new System.Drawing.Point(3, 565);
            this.statusMain.Name = "statusMain";
            this.statusMain.ShowItemToolTips = true;
            this.statusMain.Size = new System.Drawing.Size(828, 26);
            this.statusMain.TabIndex = 4;
            this.statusMain.Text = "statusStrip1";
            // 
            // tslDb
            // 
            this.tslDb.Name = "tslDb";
            this.tslDb.Size = new System.Drawing.Size(107, 20);
            this.tslDb.Text = "DB: 연결 안 됨";
            // 
            // tslSensor
            // 
            this.tslSensor.Name = "tslSensor";
            this.tslSensor.Size = new System.Drawing.Size(562, 20);
            this.tslSensor.Spring = true;
            this.tslSensor.Text = "| 센서: 연결 안 됨";
            this.tslSensor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tslClock
            // 
            this.tslClock.Name = "tslClock";
            this.tslClock.Size = new System.Drawing.Size(144, 20);
            this.tslClock.Text = "2026-11-07 14:32:00";
            // 
            // grpRealtime
            // 
            this.grpRealtime.Controls.Add(this.dgvRealtime);
            this.grpRealtime.Cursor = System.Windows.Forms.Cursors.Default;
            this.grpRealtime.Location = new System.Drawing.Point(11, 382);
            this.grpRealtime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpRealtime.Name = "grpRealtime";
            this.grpRealtime.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpRealtime.Size = new System.Drawing.Size(804, 162);
            this.grpRealtime.TabIndex = 3;
            this.grpRealtime.TabStop = false;
            this.grpRealtime.Text = "실시간 수신 기록";
            // 
            // dgvRealtime
            // 
            this.dgvRealtime.AllowUserToAddRows = false;
            this.dgvRealtime.AllowUserToDeleteRows = false;
            this.dgvRealtime.AllowUserToResizeRows = false;
            this.dgvRealtime.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvRealtime.BackgroundColor = System.Drawing.Color.White;
            this.dgvRealtime.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvRealtime.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle5.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRealtime.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvRealtime.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRealtime.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTime,
            this.colTemp,
            this.colStatus});
            this.dgvRealtime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRealtime.EnableHeadersVisualStyles = false;
            this.dgvRealtime.Location = new System.Drawing.Point(3, 22);
            this.dgvRealtime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvRealtime.MultiSelect = false;
            this.dgvRealtime.Name = "dgvRealtime";
            this.dgvRealtime.ReadOnly = true;
            this.dgvRealtime.RowHeadersVisible = false;
            this.dgvRealtime.RowHeadersWidth = 51;
            this.dgvRealtime.RowTemplate.Height = 23;
            this.dgvRealtime.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRealtime.Size = new System.Drawing.Size(798, 136);
            this.dgvRealtime.TabIndex = 0;
            // 
            // colTime
            // 
            this.colTime.HeaderText = "시간";
            this.colTime.MinimumWidth = 6;
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colTemp
            //
            this.colTemp.HeaderText = "온도(℃)";
            this.colTemp.MinimumWidth = 6;
            this.colTemp.Name = "colTemp";
            this.colTemp.ReadOnly = true;
            this.colTemp.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colStatus
            //
            this.colStatus.HeaderText = "상태";
            this.colStatus.MinimumWidth = 6;
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // grpChart
            // 
            this.grpChart.Controls.Add(this.chartTemp);
            this.grpChart.Location = new System.Drawing.Point(289, 14);
            this.grpChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpChart.Name = "grpChart";
            this.grpChart.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpChart.Size = new System.Drawing.Size(529, 361);
            this.grpChart.TabIndex = 2;
            this.grpChart.TabStop = false;
            this.grpChart.Text = "온도 추이";
            // 
            // chartTemp
            // 
            chartArea2.AxisX.IntervalAutoMode = System.Windows.Forms.DataVisualization.Charting.IntervalAutoMode.VariableCount;
            chartArea2.AxisX.LabelStyle.Format = "HH:mm:ss";
            chartArea2.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea2.AxisY.IsStartedFromZero = false;
            chartArea2.AxisY.LabelStyle.Format = "0.0";
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea2.AxisY.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Horizontal;
            chartArea2.AxisY.Title = "℃";
            chartArea2.Name = "ChartArea1";
            this.chartTemp.ChartAreas.Add(chartArea2);
            this.chartTemp.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend2.Name = "Legend1";
            this.chartTemp.Legends.Add(legend2);
            this.chartTemp.Location = new System.Drawing.Point(3, 22);
            this.chartTemp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chartTemp.Name = "chartTemp";
            series2.BorderWidth = 2;
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Color = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            series2.Legend = "Legend1";
            series2.Name = "온도";
            series2.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            series3.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series3.Color = System.Drawing.Color.Crimson;
            series3.Legend = "Legend1";
            series3.Name = "기준";
            series3.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            this.chartTemp.Series.Add(series2);
            this.chartTemp.Series.Add(series3);
            this.chartTemp.Size = new System.Drawing.Size(523, 335);
            this.chartTemp.TabIndex = 0;
            this.chartTemp.Text = "chart1";
            // 
            // grpCurrent
            // 
            this.grpCurrent.Controls.Add(this.btnResetMinMax);
            this.grpCurrent.Controls.Add(this.lblMinMax);
            this.grpCurrent.Controls.Add(this.lblThresholdInfo);
            this.grpCurrent.Controls.Add(this.lblLastUpdate);
            this.grpCurrent.Controls.Add(this.lblStatus);
            this.grpCurrent.Controls.Add(this.pnlLamp);
            this.grpCurrent.Controls.Add(this.lblUnit);
            this.grpCurrent.Controls.Add(this.lblCurrentTemp);
            this.grpCurrent.Location = new System.Drawing.Point(11, 180);
            this.grpCurrent.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpCurrent.Name = "grpCurrent";
            this.grpCurrent.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpCurrent.Size = new System.Drawing.Size(245, 195);
            this.grpCurrent.TabIndex = 1;
            this.grpCurrent.TabStop = false;
            this.grpCurrent.Text = "현재 온도";
            // 
            // btnResetMinMax
            // 
            this.btnResetMinMax.Enabled = false;
            this.btnResetMinMax.Location = new System.Drawing.Point(158, 157);
            this.btnResetMinMax.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnResetMinMax.Name = "btnResetMinMax";
            this.btnResetMinMax.Size = new System.Drawing.Size(79, 31);
            this.btnResetMinMax.TabIndex = 8;
            this.btnResetMinMax.Text = "초기화";
            this.btnResetMinMax.UseVisualStyleBackColor = true;
            this.btnResetMinMax.Click += new System.EventHandler(this.btnResetMinMax_Click);
            // 
            // lblMinMax
            // 
            this.lblMinMax.AutoEllipsis = true;
            this.lblMinMax.Location = new System.Drawing.Point(14, 162);
            this.lblMinMax.Name = "lblMinMax";
            this.lblMinMax.Size = new System.Drawing.Size(140, 22);
            this.lblMinMax.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblMinMax.TabIndex = 7;
            this.lblMinMax.Text = "최저 - / 최고 -";
            // 
            // lblThresholdInfo
            // 
            this.lblThresholdInfo.AutoSize = true;
            this.lblThresholdInfo.Location = new System.Drawing.Point(14, 139);
            this.lblThresholdInfo.Name = "lblThresholdInfo";
            this.lblThresholdInfo.Size = new System.Drawing.Size(45, 15);
            this.lblThresholdInfo.TabIndex = 6;
            this.lblThresholdInfo.Text = "경고 기준 : -";
            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Location = new System.Drawing.Point(14, 111);
            this.lblLastUpdate.Name = "lblLastUpdate";
            this.lblLastUpdate.Size = new System.Drawing.Size(45, 15);
            this.lblLastUpdate.TabIndex = 5;
            this.lblLastUpdate.Text = "최종 수신 : -";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Location = new System.Drawing.Point(45, 70);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(192, 28);
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "연결 안 됨";
            // 
            // pnlLamp
            //
            this.pnlLamp.BackColor = System.Drawing.Color.Gray;
            this.pnlLamp.Location = new System.Drawing.Point(14, 70);
            this.pnlLamp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlLamp.Name = "pnlLamp";
            this.pnlLamp.Size = new System.Drawing.Size(25, 28);
            this.pnlLamp.TabIndex = 2;
            // 
            // lblUnit
            // 
            this.lblUnit.Font = new System.Drawing.Font("맑은 고딕", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblUnit.Location = new System.Drawing.Point(198, 22);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(40, 34);
            this.lblUnit.TabIndex = 1;
            this.lblUnit.Text = "℃";
            this.lblUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCurrentTemp
            // 
            this.lblCurrentTemp.Font = new System.Drawing.Font("맑은 고딕", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblCurrentTemp.Location = new System.Drawing.Point(28, 10);
            this.lblCurrentTemp.Name = "lblCurrentTemp";
            this.lblCurrentTemp.Size = new System.Drawing.Size(172, 58);
            this.lblCurrentTemp.TabIndex = 0;
            this.lblCurrentTemp.Text = "--.-";
            this.lblCurrentTemp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpSensor
            // 
            this.grpSensor.Controls.Add(this.btnDisconnect);
            this.grpSensor.Controls.Add(this.btnConnect);
            this.grpSensor.Controls.Add(this.lblPortNo);
            this.grpSensor.Controls.Add(this.txtPort);
            this.grpSensor.Controls.Add(this.txtIp);
            this.grpSensor.Controls.Add(this.lblIp);
            this.grpSensor.Location = new System.Drawing.Point(11, 14);
            this.grpSensor.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpSensor.Name = "grpSensor";
            this.grpSensor.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpSensor.Size = new System.Drawing.Size(245, 155);
            this.grpSensor.TabIndex = 0;
            this.grpSensor.TabStop = false;
            this.grpSensor.Text = "센서 연결 (SIG-5560)";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(126, 110);
            this.btnDisconnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(102, 30);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "연결 해제";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(16, 110);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(102, 30);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "연결";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lblPortNo
            // 
            this.lblPortNo.AutoSize = true;
            this.lblPortNo.Location = new System.Drawing.Point(7, 72);
            this.lblPortNo.Name = "lblPortNo";
            this.lblPortNo.Size = new System.Drawing.Size(37, 15);
            this.lblPortNo.TabIndex = 3;
            this.lblPortNo.Text = "포트";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(74, 69);
            this.txtPort.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtPort.MaxLength = 20;
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(93, 25);
            this.txtPort.TabIndex = 2;
            // 
            // txtIp
            // 
            this.txtIp.Location = new System.Drawing.Point(74, 26);
            this.txtIp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtIp.MaxLength = 20;
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(151, 25);
            this.txtIp.TabIndex = 1;
            // 
            // lblIp
            // 
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(7, 31);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(55, 15);
            this.lblIp.TabIndex = 0;
            this.lblIp.Text = "IP 주소";
            // 
            // tabHistory
            // 
            this.tabHistory.Controls.Add(this.dgvHistory);
            this.tabHistory.Controls.Add(this.pnlHistorySearch);
            this.tabHistory.Location = new System.Drawing.Point(4, 25);
            this.tabHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabHistory.Name = "tabHistory";
            this.tabHistory.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabHistory.Size = new System.Drawing.Size(834, 595);
            this.tabHistory.TabIndex = 1;
            this.tabHistory.Text = "온도 기록";
            this.tabHistory.UseVisualStyleBackColor = true;
            // 
            // dgvHistory
            // 
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AllowUserToResizeRows = false;
            this.dgvHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvHistory.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvHistory.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHistTime,
            this.colHistTemp,
            this.colHistStatus});
            this.dgvHistory.EnableHeadersVisualStyles = false;
            this.dgvHistory.Location = new System.Drawing.Point(18, 79);
            this.dgvHistory.MultiSelect = false;
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowHeadersWidth = 51;
            this.dgvHistory.RowTemplate.Height = 27;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistory.Size = new System.Drawing.Size(800, 507);
            this.dgvHistory.TabIndex = 1;
            this.dgvHistory.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvHistory_CellFormatting);
            //
            // colHistTime
            //
            this.colHistTime.DataPropertyName = "MEASURE_DT";
            dataGridViewCellStyle9.Format = "yyyy-MM-dd HH:mm:ss";
            this.colHistTime.DefaultCellStyle = dataGridViewCellStyle9;
            this.colHistTime.HeaderText = "측정 시간";
            this.colHistTime.MinimumWidth = 6;
            this.colHistTime.Name = "colHistTime";
            this.colHistTime.ReadOnly = true;
            this.colHistTime.ToolTipText = "클릭하면 정렬됩니다";
            //
            // colHistTemp
            //
            this.colHistTemp.DataPropertyName = "TEMP_VALUE";
            dataGridViewCellStyle10.Format = "0.0";
            dataGridViewCellStyle10.NullValue = "-";
            this.colHistTemp.DefaultCellStyle = dataGridViewCellStyle10;
            this.colHistTemp.HeaderText = "온도(℃)";
            this.colHistTemp.MinimumWidth = 6;
            this.colHistTemp.Name = "colHistTemp";
            this.colHistTemp.ReadOnly = true;
            this.colHistTemp.ToolTipText = "클릭하면 정렬됩니다";
            //
            // colHistStatus
            //
            this.colHistStatus.DataPropertyName = "STATUS_NM";
            this.colHistStatus.HeaderText = "상태";
            this.colHistStatus.MinimumWidth = 6;
            this.colHistStatus.Name = "colHistStatus";
            this.colHistStatus.ReadOnly = true;
            this.colHistStatus.ToolTipText = "클릭하면 정렬됩니다";
            // 
            // pnlHistorySearch
            // 
            this.pnlHistorySearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlHistorySearch.Controls.Add(this.lblTotalCount);
            this.pnlHistorySearch.Controls.Add(this.btnToday);
            this.pnlHistorySearch.Controls.Add(this.btnSearch);
            this.pnlHistorySearch.Controls.Add(this.lblEnd);
            this.pnlHistorySearch.Controls.Add(this.dateTimePicker2);
            this.pnlHistorySearch.Controls.Add(this.dateTimePicker1);
            this.pnlHistorySearch.Controls.Add(this.lblStart);
            this.pnlHistorySearch.Location = new System.Drawing.Point(18, 10);
            this.pnlHistorySearch.Name = "pnlHistorySearch";
            this.pnlHistorySearch.Size = new System.Drawing.Size(800, 57);
            this.pnlHistorySearch.TabIndex = 0;
            // 
            // lblTotalCount
            // 
            this.lblTotalCount.AutoSize = true;
            this.lblTotalCount.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblTotalCount.Location = new System.Drawing.Point(672, 17);
            this.lblTotalCount.Name = "lblTotalCount";
            this.lblTotalCount.Size = new System.Drawing.Size(53, 20);
            this.lblTotalCount.TabIndex = 6;
            this.lblTotalCount.Text = "총 0건";
            // 
            // btnToday
            // 
            this.btnToday.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnToday.Location = new System.Drawing.Point(595, 7);
            this.btnToday.Name = "btnToday";
            this.btnToday.Size = new System.Drawing.Size(68, 39);
            this.btnToday.TabIndex = 5;
            this.btnToday.Text = "오늘";
            this.btnToday.UseVisualStyleBackColor = true;
            this.btnToday.Click += new System.EventHandler(this.btnToday_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSearch.Location = new System.Drawing.Point(521, 7);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(68, 39);
            this.btnSearch.TabIndex = 4;
            this.btnSearch.Text = "조회";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // lblEnd
            // 
            this.lblEnd.AutoSize = true;
            this.lblEnd.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblEnd.Location = new System.Drawing.Point(263, 14);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(44, 23);
            this.lblEnd.TabIndex = 3;
            this.lblEnd.Text = "종료";
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker2.Location = new System.Drawing.Point(307, 14);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(200, 25);
            this.dateTimePicker2.TabIndex = 2;
            this.dateTimePicker2.ValueChanged += new System.EventHandler(this.dateTimePicker2_ValueChanged);
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Location = new System.Drawing.Point(56, 14);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(200, 25);
            this.dateTimePicker1.TabIndex = 1;
            // 
            // lblStart
            // 
            this.lblStart.AutoSize = true;
            this.lblStart.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblStart.Location = new System.Drawing.Point(11, 13);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(44, 23);
            this.lblStart.TabIndex = 0;
            this.lblStart.Text = "시작";
            // 
            // tabAlert
            // 
            this.tabAlert.Controls.Add(this.dgvAlert);
            this.tabAlert.Controls.Add(this.pnlAlertSearch);
            this.tabAlert.Location = new System.Drawing.Point(4, 25);
            this.tabAlert.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabAlert.Name = "tabAlert";
            this.tabAlert.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabAlert.Size = new System.Drawing.Size(834, 595);
            this.tabAlert.TabIndex = 2;
            this.tabAlert.Text = "경고 내역";
            this.tabAlert.UseVisualStyleBackColor = true;
            // 
            // dgvAlert
            // 
            this.dgvAlert.AllowUserToAddRows = false;
            this.dgvAlert.AllowUserToDeleteRows = false;
            this.dgvAlert.AllowUserToResizeRows = false;
            this.dgvAlert.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAlert.BackgroundColor = System.Drawing.Color.White;
            this.dgvAlert.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvAlert.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvAlert.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvAlert.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAlert.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAlertTime,
            this.colAlertTemp,
            this.colAlertThreshold,
            this.colAlertKakao});
            this.dgvAlert.EnableHeadersVisualStyles = false;
            this.dgvAlert.Location = new System.Drawing.Point(18, 80);
            this.dgvAlert.MultiSelect = false;
            this.dgvAlert.Name = "dgvAlert";
            this.dgvAlert.ReadOnly = true;
            this.dgvAlert.RowHeadersVisible = false;
            this.dgvAlert.RowHeadersWidth = 51;
            this.dgvAlert.RowTemplate.Height = 27;
            this.dgvAlert.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAlert.Size = new System.Drawing.Size(800, 497);
            this.dgvAlert.TabIndex = 2;
            // 
            // colAlertTime
            // 
            this.colAlertTime.DataPropertyName = "ALERT_DT";
            dataGridViewCellStyle11.Format = "yyyy-MM-dd HH:mm:ss";
            this.colAlertTime.DefaultCellStyle = dataGridViewCellStyle11;
            this.colAlertTime.HeaderText = "발생 시각";
            this.colAlertTime.MinimumWidth = 6;
            this.colAlertTime.Name = "colAlertTime";
            this.colAlertTime.ReadOnly = true;
            this.colAlertTime.ToolTipText = "클릭하면 정렬됩니다";
            //
            // colAlertTemp
            //
            this.colAlertTemp.DataPropertyName = "TEMP_VALUE";
            dataGridViewCellStyle12.Format = "0.0";
            this.colAlertTemp.DefaultCellStyle = dataGridViewCellStyle12;
            this.colAlertTemp.HeaderText = "온도(℃)";
            this.colAlertTemp.MinimumWidth = 6;
            this.colAlertTemp.Name = "colAlertTemp";
            this.colAlertTemp.ReadOnly = true;
            this.colAlertTemp.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAlertThreshold
            //
            this.colAlertThreshold.DataPropertyName = "THRESHOLD_VALUE";
            dataGridViewCellStyle13.Format = "0.0";
            this.colAlertThreshold.DefaultCellStyle = dataGridViewCellStyle13;
            this.colAlertThreshold.HeaderText = "기준 온도(℃)";
            this.colAlertThreshold.MinimumWidth = 6;
            this.colAlertThreshold.Name = "colAlertThreshold";
            this.colAlertThreshold.ReadOnly = true;
            this.colAlertThreshold.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAlertKakao
            //
            this.colAlertKakao.DataPropertyName = "KAKAO_STATUS_NM";
            this.colAlertKakao.HeaderText = "카톡 발송";
            this.colAlertKakao.MinimumWidth = 6;
            this.colAlertKakao.Name = "colAlertKakao";
            this.colAlertKakao.ReadOnly = true;
            this.colAlertKakao.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pnlAlertSearch
            // 
            this.pnlAlertSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlAlertSearch.Controls.Add(this.lblAlertCount);
            this.pnlAlertSearch.Controls.Add(this.btnAlertSearch);
            this.pnlAlertSearch.Controls.Add(this.lblAlertEnd);
            this.pnlAlertSearch.Controls.Add(this.dtpAlertEnd);
            this.pnlAlertSearch.Controls.Add(this.dtpAlertStart);
            this.pnlAlertSearch.Controls.Add(this.lblAlertStart);
            this.pnlAlertSearch.Location = new System.Drawing.Point(18, 10);
            this.pnlAlertSearch.Name = "pnlAlertSearch";
            this.pnlAlertSearch.Size = new System.Drawing.Size(800, 57);
            this.pnlAlertSearch.TabIndex = 1;
            // 
            // lblAlertCount
            // 
            this.lblAlertCount.AutoSize = true;
            this.lblAlertCount.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAlertCount.Location = new System.Drawing.Point(548, 16);
            this.lblAlertCount.Name = "lblAlertCount";
            this.lblAlertCount.Size = new System.Drawing.Size(68, 20);
            this.lblAlertCount.TabIndex = 6;
            this.lblAlertCount.Text = "경고 0건";
            // 
            // btnAlertSearch
            // 
            this.btnAlertSearch.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnAlertSearch.Location = new System.Drawing.Point(470, 7);
            this.btnAlertSearch.Name = "btnAlertSearch";
            this.btnAlertSearch.Size = new System.Drawing.Size(68, 39);
            this.btnAlertSearch.TabIndex = 4;
            this.btnAlertSearch.Text = "조회";
            this.btnAlertSearch.UseVisualStyleBackColor = true;
            this.btnAlertSearch.Click += new System.EventHandler(this.btnAlertSearch_Click);
            // 
            // lblAlertEnd
            // 
            this.lblAlertEnd.AutoSize = true;
            this.lblAlertEnd.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAlertEnd.Location = new System.Drawing.Point(239, 14);
            this.lblAlertEnd.Name = "lblAlertEnd";
            this.lblAlertEnd.Size = new System.Drawing.Size(44, 23);
            this.lblAlertEnd.TabIndex = 3;
            this.lblAlertEnd.Text = "종료";
            // 
            // dtpAlertEnd
            // 
            this.dtpAlertEnd.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpAlertEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpAlertEnd.Location = new System.Drawing.Point(283, 14);
            this.dtpAlertEnd.Name = "dtpAlertEnd";
            this.dtpAlertEnd.Size = new System.Drawing.Size(175, 25);
            this.dtpAlertEnd.TabIndex = 2;
            this.dtpAlertEnd.ValueChanged += new System.EventHandler(this.dtpAlertEnd_ValueChanged);
            // 
            // dtpAlertStart
            // 
            this.dtpAlertStart.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpAlertStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpAlertStart.Location = new System.Drawing.Point(56, 14);
            this.dtpAlertStart.Name = "dtpAlertStart";
            this.dtpAlertStart.Size = new System.Drawing.Size(175, 25);
            this.dtpAlertStart.TabIndex = 1;
            // 
            // lblAlertStart
            // 
            this.lblAlertStart.AutoSize = true;
            this.lblAlertStart.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAlertStart.Location = new System.Drawing.Point(11, 13);
            this.lblAlertStart.Name = "lblAlertStart";
            this.lblAlertStart.Size = new System.Drawing.Size(44, 23);
            this.lblAlertStart.TabIndex = 0;
            this.lblAlertStart.Text = "시작";
            // 
            // tabKakao
            // 
            this.tabKakao.Controls.Add(this.dgvKakao);
            this.tabKakao.Controls.Add(this.pnlKakaoSearch);
            this.tabKakao.Location = new System.Drawing.Point(4, 25);
            this.tabKakao.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabKakao.Name = "tabKakao";
            this.tabKakao.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabKakao.Size = new System.Drawing.Size(834, 595);
            this.tabKakao.TabIndex = 3;
            this.tabKakao.Text = "카톡 발송 내역";
            this.tabKakao.UseVisualStyleBackColor = true;
            // 
            // dgvKakao
            // 
            this.dgvKakao.AllowUserToAddRows = false;
            this.dgvKakao.AllowUserToDeleteRows = false;
            this.dgvKakao.AllowUserToResizeRows = false;
            this.dgvKakao.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvKakao.BackgroundColor = System.Drawing.Color.White;
            this.dgvKakao.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvKakao.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle8.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvKakao.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
            this.dgvKakao.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvKakao.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colKakaoTime,
            this.colKakaoReceiver,
            this.colKakaoMessage,
            this.colKakaoResult});
            this.dgvKakao.EnableHeadersVisualStyles = false;
            this.dgvKakao.Location = new System.Drawing.Point(18, 80);
            this.dgvKakao.MultiSelect = false;
            this.dgvKakao.Name = "dgvKakao";
            this.dgvKakao.ReadOnly = true;
            this.dgvKakao.RowHeadersVisible = false;
            this.dgvKakao.RowHeadersWidth = 51;
            this.dgvKakao.RowTemplate.Height = 27;
            this.dgvKakao.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvKakao.Size = new System.Drawing.Size(800, 497);
            this.dgvKakao.TabIndex = 3;
            // 
            // colKakaoTime
            // 
            this.colKakaoTime.DataPropertyName = "SEND_DT";
            dataGridViewCellStyle14.Format = "yyyy-MM-dd HH:mm:ss";
            this.colKakaoTime.DefaultCellStyle = dataGridViewCellStyle14;
            this.colKakaoTime.HeaderText = "발송 시각";
            this.colKakaoTime.MinimumWidth = 6;
            this.colKakaoTime.Name = "colKakaoTime";
            this.colKakaoTime.ReadOnly = true;
            this.colKakaoTime.ToolTipText = "클릭하면 정렬됩니다";
            //
            // colKakaoReceiver
            //
            this.colKakaoReceiver.DataPropertyName = "RECEIVER_NM";
            this.colKakaoReceiver.HeaderText = "수신자";
            this.colKakaoReceiver.MinimumWidth = 6;
            this.colKakaoReceiver.Name = "colKakaoReceiver";
            this.colKakaoReceiver.ReadOnly = true;
            this.colKakaoReceiver.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colKakaoMessage
            //
            this.colKakaoMessage.DataPropertyName = "MESSAGE";
            this.colKakaoMessage.FillWeight = 200F;
            this.colKakaoMessage.HeaderText = "내용";
            this.colKakaoMessage.MinimumWidth = 6;
            this.colKakaoMessage.Name = "colKakaoMessage";
            this.colKakaoMessage.ReadOnly = true;
            this.colKakaoMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colKakaoResult
            //
            this.colKakaoResult.DataPropertyName = "RESULT_NM";
            this.colKakaoResult.HeaderText = "결과";
            this.colKakaoResult.MinimumWidth = 6;
            this.colKakaoResult.Name = "colKakaoResult";
            this.colKakaoResult.ReadOnly = true;
            this.colKakaoResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pnlKakaoSearch
            // 
            this.pnlKakaoSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlKakaoSearch.Controls.Add(this.lblKakaoStatus);
            this.pnlKakaoSearch.Controls.Add(this.btnKakaoTest);
            this.pnlKakaoSearch.Controls.Add(this.lblKakaoCount);
            this.pnlKakaoSearch.Controls.Add(this.btnKakaoSearch);
            this.pnlKakaoSearch.Controls.Add(this.lblKakaoEnd);
            this.pnlKakaoSearch.Controls.Add(this.dtpKakaoEnd);
            this.pnlKakaoSearch.Controls.Add(this.dtpKakaoStart);
            this.pnlKakaoSearch.Controls.Add(this.lblKakaoStart);
            this.pnlKakaoSearch.Location = new System.Drawing.Point(18, 10);
            this.pnlKakaoSearch.Name = "pnlKakaoSearch";
            this.pnlKakaoSearch.Size = new System.Drawing.Size(800, 57);
            this.pnlKakaoSearch.TabIndex = 2;
            // 
            // lblKakaoStatus
            // 
            this.lblKakaoStatus.AutoSize = true;
            this.lblKakaoStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblKakaoStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblKakaoStatus.Location = new System.Drawing.Point(618, 16);
            this.lblKakaoStatus.Name = "lblKakaoStatus";
            this.lblKakaoStatus.Size = new System.Drawing.Size(168, 20);
            this.lblKakaoStatus.TabIndex = 8;
            this.lblKakaoStatus.Text = "카카오 연동: 연결 안 됨";
            // 
            // btnKakaoTest
            // 
            this.btnKakaoTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnKakaoTest.Location = new System.Drawing.Point(442, 7);
            this.btnKakaoTest.Name = "btnKakaoTest";
            this.btnKakaoTest.Size = new System.Drawing.Size(99, 39);
            this.btnKakaoTest.TabIndex = 7;
            this.btnKakaoTest.Text = "테스트 발송";
            this.btnKakaoTest.UseVisualStyleBackColor = true;
            this.btnKakaoTest.Click += new System.EventHandler(this.btnKakaoTest_Click);
            // 
            // lblKakaoCount
            // 
            this.lblKakaoCount.AutoSize = true;
            this.lblKakaoCount.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblKakaoCount.Location = new System.Drawing.Point(544, 16);
            this.lblKakaoCount.Name = "lblKakaoCount";
            this.lblKakaoCount.Size = new System.Drawing.Size(68, 20);
            this.lblKakaoCount.TabIndex = 6;
            this.lblKakaoCount.Text = "발송 0건";
            // 
            // btnKakaoSearch
            // 
            this.btnKakaoSearch.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnKakaoSearch.Location = new System.Drawing.Point(368, 7);
            this.btnKakaoSearch.Name = "btnKakaoSearch";
            this.btnKakaoSearch.Size = new System.Drawing.Size(68, 39);
            this.btnKakaoSearch.TabIndex = 4;
            this.btnKakaoSearch.Text = "조회";
            this.btnKakaoSearch.UseVisualStyleBackColor = true;
            this.btnKakaoSearch.Click += new System.EventHandler(this.btnKakaoSearch_Click);
            // 
            // lblKakaoEnd
            // 
            this.lblKakaoEnd.AutoSize = true;
            this.lblKakaoEnd.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblKakaoEnd.Location = new System.Drawing.Point(187, 14);
            this.lblKakaoEnd.Name = "lblKakaoEnd";
            this.lblKakaoEnd.Size = new System.Drawing.Size(44, 23);
            this.lblKakaoEnd.TabIndex = 3;
            this.lblKakaoEnd.Text = "종료";
            // 
            // dtpKakaoEnd
            // 
            this.dtpKakaoEnd.CustomFormat = "yyyy-MM-dd";
            this.dtpKakaoEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpKakaoEnd.Location = new System.Drawing.Point(231, 14);
            this.dtpKakaoEnd.Name = "dtpKakaoEnd";
            this.dtpKakaoEnd.Size = new System.Drawing.Size(126, 25);
            this.dtpKakaoEnd.TabIndex = 2;
            // 
            // dtpKakaoStart
            // 
            this.dtpKakaoStart.CustomFormat = "yyyy-MM-dd";
            this.dtpKakaoStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpKakaoStart.Location = new System.Drawing.Point(56, 14);
            this.dtpKakaoStart.Name = "dtpKakaoStart";
            this.dtpKakaoStart.Size = new System.Drawing.Size(126, 25);
            this.dtpKakaoStart.TabIndex = 1;
            // 
            // lblKakaoStart
            // 
            this.lblKakaoStart.AutoSize = true;
            this.lblKakaoStart.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblKakaoStart.Location = new System.Drawing.Point(11, 13);
            this.lblKakaoStart.Name = "lblKakaoStart";
            this.lblKakaoStart.Size = new System.Drawing.Size(44, 23);
            this.lblKakaoStart.TabIndex = 0;
            this.lblKakaoStart.Text = "시작";
            // 
            // tabSetting
            // 
            this.tabSetting.Controls.Add(this.lblAdminNotice);
            this.tabSetting.Controls.Add(this.btnSaveSetting);
            this.tabSetting.Controls.Add(this.grpCycleSetting);
            this.tabSetting.Controls.Add(this.grpAlertSetting);
            this.tabSetting.Location = new System.Drawing.Point(4, 25);
            this.tabSetting.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabSetting.Name = "tabSetting";
            this.tabSetting.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabSetting.Size = new System.Drawing.Size(834, 595);
            this.tabSetting.TabIndex = 4;
            this.tabSetting.Text = "설정";
            this.tabSetting.UseVisualStyleBackColor = true;
            // 
            // lblAdminNotice
            // 
            this.lblAdminNotice.AutoSize = true;
            this.lblAdminNotice.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAdminNotice.ForeColor = System.Drawing.Color.Gray;
            this.lblAdminNotice.Location = new System.Drawing.Point(210, 541);
            this.lblAdminNotice.Name = "lblAdminNotice";
            this.lblAdminNotice.Size = new System.Drawing.Size(309, 20);
            this.lblAdminNotice.TabIndex = 3;
            this.lblAdminNotice.Text = "※ 설정은 관리자 권한만 변경할 수 있습니다.";
            // 
            // btnSaveSetting
            // 
            this.btnSaveSetting.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSaveSetting.Location = new System.Drawing.Point(19, 528);
            this.btnSaveSetting.Name = "btnSaveSetting";
            this.btnSaveSetting.Size = new System.Drawing.Size(169, 47);
            this.btnSaveSetting.TabIndex = 2;
            this.btnSaveSetting.Text = "설정 저장";
            this.btnSaveSetting.UseVisualStyleBackColor = true;
            this.btnSaveSetting.Click += new System.EventHandler(this.btnSaveSetting_Click);
            // 
            // grpCycleSetting
            // 
            this.grpCycleSetting.Controls.Add(this.nudSaveCycle);
            this.grpCycleSetting.Controls.Add(this.nudReadCycle);
            this.grpCycleSetting.Controls.Add(this.lblSaveCycle);
            this.grpCycleSetting.Controls.Add(this.lblReadCycle);
            this.grpCycleSetting.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.grpCycleSetting.Location = new System.Drawing.Point(19, 330);
            this.grpCycleSetting.Name = "grpCycleSetting";
            this.grpCycleSetting.Size = new System.Drawing.Size(500, 176);
            this.grpCycleSetting.TabIndex = 1;
            this.grpCycleSetting.TabStop = false;
            this.grpCycleSetting.Text = "측정 및 저장 주기";
            // 
            // nudSaveCycle
            // 
            this.nudSaveCycle.Location = new System.Drawing.Point(223, 120);
            this.nudSaveCycle.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSaveCycle.Name = "nudSaveCycle";
            this.nudSaveCycle.Size = new System.Drawing.Size(139, 30);
            this.nudSaveCycle.TabIndex = 6;
            this.nudSaveCycle.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // nudReadCycle
            // 
            this.nudReadCycle.Location = new System.Drawing.Point(223, 50);
            this.nudReadCycle.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudReadCycle.Name = "nudReadCycle";
            this.nudReadCycle.Size = new System.Drawing.Size(139, 30);
            this.nudReadCycle.TabIndex = 5;
            this.nudReadCycle.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblSaveCycle
            // 
            this.lblSaveCycle.AutoSize = true;
            this.lblSaveCycle.Location = new System.Drawing.Point(11, 122);
            this.lblSaveCycle.Name = "lblSaveCycle";
            this.lblSaveCycle.Size = new System.Drawing.Size(139, 23);
            this.lblSaveCycle.TabIndex = 4;
            this.lblSaveCycle.Text = "DB 저장 주기(초)";
            // 
            // lblReadCycle
            // 
            this.lblReadCycle.AutoSize = true;
            this.lblReadCycle.Location = new System.Drawing.Point(11, 51);
            this.lblReadCycle.Name = "lblReadCycle";
            this.lblReadCycle.Size = new System.Drawing.Size(151, 23);
            this.lblReadCycle.TabIndex = 3;
            this.lblReadCycle.Text = "센서 읽기 주기(초)";
            // 
            // grpAlertSetting
            // 
            this.grpAlertSetting.Controls.Add(this.lblRiseStepDesc);
            this.grpAlertSetting.Controls.Add(this.nudRiseStep);
            this.grpAlertSetting.Controls.Add(this.lblRiseStep);
            this.grpAlertSetting.Controls.Add(this.chkKakaoAlert);
            this.grpAlertSetting.Controls.Add(this.nudAlertInterval);
            this.grpAlertSetting.Controls.Add(this.nudAlertTemp);
            this.grpAlertSetting.Controls.Add(this.lblIntervalDesc);
            this.grpAlertSetting.Controls.Add(this.lblAlertInterval);
            this.grpAlertSetting.Controls.Add(this.lblAlertTemp);
            this.grpAlertSetting.Font = new System.Drawing.Font("맑은 고딕", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.grpAlertSetting.Location = new System.Drawing.Point(19, 17);
            this.grpAlertSetting.Name = "grpAlertSetting";
            this.grpAlertSetting.Size = new System.Drawing.Size(500, 297);
            this.grpAlertSetting.TabIndex = 0;
            this.grpAlertSetting.TabStop = false;
            this.grpAlertSetting.Text = "경고 설정";
            // 
            // chkKakaoAlert
            // 
            this.chkKakaoAlert.AutoSize = true;
            this.chkKakaoAlert.Checked = true;
            this.chkKakaoAlert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkKakaoAlert.Location = new System.Drawing.Point(15, 252);
            this.chkKakaoAlert.Name = "chkKakaoAlert";
            this.chkKakaoAlert.Size = new System.Drawing.Size(180, 27);
            this.chkKakaoAlert.TabIndex = 5;
            this.chkKakaoAlert.Text = "카카오톡 알림 사용";
            this.chkKakaoAlert.UseVisualStyleBackColor = true;
            //
            // lblRiseStep
            //
            this.lblRiseStep.AutoSize = true;
            this.lblRiseStep.Location = new System.Drawing.Point(11, 178);
            this.lblRiseStep.Name = "lblRiseStep";
            this.lblRiseStep.Size = new System.Drawing.Size(155, 23);
            this.lblRiseStep.TabIndex = 6;
            this.lblRiseStep.Text = "재알림 상승 폭(℃)";
            //
            // nudRiseStep
            //
            this.nudRiseStep.DecimalPlaces = 1;
            this.nudRiseStep.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudRiseStep.Location = new System.Drawing.Point(223, 176);
            this.nudRiseStep.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudRiseStep.Name = "nudRiseStep";
            this.nudRiseStep.Size = new System.Drawing.Size(139, 30);
            this.nudRiseStep.TabIndex = 7;
            this.nudRiseStep.Value = new decimal(new int[] {
            50,
            0,
            0,
            65536});
            //
            // lblRiseStepDesc
            //
            this.lblRiseStepDesc.AutoSize = true;
            this.lblRiseStepDesc.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblRiseStepDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblRiseStepDesc.Location = new System.Drawing.Point(11, 216);
            this.lblRiseStepDesc.Name = "lblRiseStepDesc";
            this.lblRiseStepDesc.Size = new System.Drawing.Size(470, 20);
            this.lblRiseStepDesc.TabIndex = 8;
            this.lblRiseStepDesc.Text = "직전 알림보다 이만큼 오르면 간격과 상관없이 바로 알립니다. (0=끔)";
            //
            // nudAlertInterval
            // 
            this.nudAlertInterval.Location = new System.Drawing.Point(223, 101);
            this.nudAlertInterval.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.nudAlertInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudAlertInterval.Name = "nudAlertInterval";
            this.nudAlertInterval.Size = new System.Drawing.Size(139, 30);
            this.nudAlertInterval.TabIndex = 4;
            this.nudAlertInterval.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // nudAlertTemp
            // 
            this.nudAlertTemp.DecimalPlaces = 1;
            this.nudAlertTemp.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudAlertTemp.Location = new System.Drawing.Point(223, 44);
            this.nudAlertTemp.Maximum = new decimal(new int[] {
            150,
            0,
            0,
            0});
            this.nudAlertTemp.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            -2147483648});
            this.nudAlertTemp.Name = "nudAlertTemp";
            this.nudAlertTemp.Size = new System.Drawing.Size(139, 30);
            this.nudAlertTemp.TabIndex = 3;
            this.nudAlertTemp.Value = new decimal(new int[] {
            300,
            0,
            0,
            65536});
            // 
            // lblIntervalDesc
            // 
            this.lblIntervalDesc.AutoSize = true;
            this.lblIntervalDesc.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblIntervalDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblIntervalDesc.Location = new System.Drawing.Point(11, 141);
            this.lblIntervalDesc.Name = "lblIntervalDesc";
            this.lblIntervalDesc.Size = new System.Drawing.Size(432, 20);
            this.lblIntervalDesc.TabIndex = 2;
            this.lblIntervalDesc.Text = "같은 경고로 카톡이 계속 오는 것을 막기 위한 최소 간격입니다.";
            // 
            // lblAlertInterval
            // 
            this.lblAlertInterval.AutoSize = true;
            this.lblAlertInterval.Location = new System.Drawing.Point(11, 103);
            this.lblAlertInterval.Name = "lblAlertInterval";
            this.lblAlertInterval.Size = new System.Drawing.Size(151, 23);
            this.lblAlertInterval.TabIndex = 1;
            this.lblAlertInterval.Text = "알림 최소 간격(분)";
            // 
            // lblAlertTemp
            // 
            this.lblAlertTemp.AutoSize = true;
            this.lblAlertTemp.Location = new System.Drawing.Point(11, 47);
            this.lblAlertTemp.Name = "lblAlertTemp";
            this.lblAlertTemp.Size = new System.Drawing.Size(150, 23);
            this.lblAlertTemp.TabIndex = 0;
            this.lblAlertTemp.Text = "경고 기준 온도(℃)";
            //
            // tmrClock
            //
            this.tmrClock.Interval = 1000;
            this.tmrClock.Tick += new System.EventHandler(this.tmrClock_Tick);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(842, 708);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlTop);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "온도 모니터링 시스템";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.tabMain.ResumeLayout(false);
            this.tabMonitor.ResumeLayout(false);
            this.tabMonitor.PerformLayout();
            this.statusMain.ResumeLayout(false);
            this.statusMain.PerformLayout();
            this.grpRealtime.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRealtime)).EndInit();
            this.grpChart.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartTemp)).EndInit();
            this.grpCurrent.ResumeLayout(false);
            this.grpCurrent.PerformLayout();
            this.grpSensor.ResumeLayout(false);
            this.grpSensor.PerformLayout();
            this.tabHistory.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.pnlHistorySearch.ResumeLayout(false);
            this.pnlHistorySearch.PerformLayout();
            this.tabAlert.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlert)).EndInit();
            this.pnlAlertSearch.ResumeLayout(false);
            this.pnlAlertSearch.PerformLayout();
            this.tabKakao.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvKakao)).EndInit();
            this.pnlKakaoSearch.ResumeLayout(false);
            this.pnlKakaoSearch.PerformLayout();
            this.tabSetting.ResumeLayout(false);
            this.tabSetting.PerformLayout();
            this.grpCycleSetting.ResumeLayout(false);
            this.grpCycleSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveCycle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReadCycle)).EndInit();
            this.grpAlertSetting.ResumeLayout(false);
            this.grpAlertSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAlertInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAlertTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRiseStep)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabMonitor;
        private System.Windows.Forms.GroupBox grpSensor;
        private System.Windows.Forms.TabPage tabHistory;
        private System.Windows.Forms.TabPage tabAlert;
        private System.Windows.Forms.TabPage tabKakao;
        private System.Windows.Forms.TabPage tabSetting;
        private System.Windows.Forms.Label lblPortNo;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.GroupBox grpRealtime;
        private System.Windows.Forms.GroupBox grpChart;
        private System.Windows.Forms.GroupBox grpCurrent;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.Label lblCurrentTemp;
        private System.Windows.Forms.Panel pnlLamp;
        private System.Windows.Forms.Button btnResetMinMax;
        private System.Windows.Forms.Label lblMinMax;
        private System.Windows.Forms.Label lblThresholdInfo;
        private System.Windows.Forms.Label lblLastUpdate;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartTemp;
        private System.Windows.Forms.DataGridView dgvRealtime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.StatusStrip statusMain;
        private System.Windows.Forms.ToolStripStatusLabel tslDb;
        private System.Windows.Forms.ToolStripStatusLabel tslSensor;
        private System.Windows.Forms.ToolStripStatusLabel tslClock;
        private System.Windows.Forms.Panel pnlHistorySearch;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Button btnToday;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.Label lblTotalCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistStatus;
        private System.Windows.Forms.Panel pnlAlertSearch;
        private System.Windows.Forms.Label lblAlertCount;
        private System.Windows.Forms.Button btnAlertSearch;
        private System.Windows.Forms.Label lblAlertEnd;
        private System.Windows.Forms.DateTimePicker dtpAlertEnd;
        private System.Windows.Forms.DateTimePicker dtpAlertStart;
        private System.Windows.Forms.Label lblAlertStart;
        private System.Windows.Forms.DataGridView dgvAlert;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertKakao;
        private System.Windows.Forms.Panel pnlKakaoSearch;
        private System.Windows.Forms.Label lblKakaoCount;
        private System.Windows.Forms.Button btnKakaoSearch;
        private System.Windows.Forms.Label lblKakaoEnd;
        private System.Windows.Forms.DateTimePicker dtpKakaoEnd;
        private System.Windows.Forms.DateTimePicker dtpKakaoStart;
        private System.Windows.Forms.Label lblKakaoStart;
        private System.Windows.Forms.Label lblKakaoStatus;
        private System.Windows.Forms.Button btnKakaoTest;
        private System.Windows.Forms.DataGridView dgvKakao;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKakaoTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKakaoReceiver;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKakaoMessage;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKakaoResult;
        private System.Windows.Forms.GroupBox grpCycleSetting;
        private System.Windows.Forms.GroupBox grpAlertSetting;
        private System.Windows.Forms.NumericUpDown nudSaveCycle;
        private System.Windows.Forms.NumericUpDown nudReadCycle;
        private System.Windows.Forms.Label lblSaveCycle;
        private System.Windows.Forms.Label lblReadCycle;
        private System.Windows.Forms.NumericUpDown nudAlertInterval;
        private System.Windows.Forms.NumericUpDown nudAlertTemp;
        private System.Windows.Forms.Label lblIntervalDesc;
        private System.Windows.Forms.Label lblRiseStep;
        private System.Windows.Forms.NumericUpDown nudRiseStep;
        private System.Windows.Forms.Label lblRiseStepDesc;
        private System.Windows.Forms.Label lblAlertInterval;
        private System.Windows.Forms.Label lblAlertTemp;
        private System.Windows.Forms.CheckBox chkKakaoAlert;
        private System.Windows.Forms.Label lblAdminNotice;
        private System.Windows.Forms.Button btnSaveSetting;
        private System.Windows.Forms.Timer tmrClock;
    }
}