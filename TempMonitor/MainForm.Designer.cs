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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabMonitor = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.grpChart = new System.Windows.Forms.GroupBox();
            this.grpCurrent = new System.Windows.Forms.GroupBox();
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
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.lblThresholdInfo = new System.Windows.Forms.Label();
            this.lblMinMax = new System.Windows.Forms.Label();
            this.btnResetMinMax = new System.Windows.Forms.Button();
            this.chartTemp = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.pnlTop.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabMonitor.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.grpChart.SuspendLayout();
            this.grpCurrent.SuspendLayout();
            this.grpSensor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
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
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(608, 67);
            this.pnlTop.TabIndex = 0;
            // 
            // btnLogout
            // 
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(511, 22);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(84, 28);
            this.btnLogout.TabIndex = 2;
            this.btnLogout.Text = "로그아웃";
            this.btnLogout.UseVisualStyleBackColor = true;
            // 
            // lblUser
            // 
            this.lblUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUser.AutoSize = true;
            this.lblUser.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblUser.ForeColor = System.Drawing.Color.White;
            this.lblUser.Location = new System.Drawing.Point(322, 27);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(183, 17);
            this.lblUser.TabIndex = 1;
            this.lblUser.Text = "사용자 : admin(권한 : 관리자)";
            this.lblUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(15, 24);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Size = new System.Drawing.Size(197, 25);
            this.lblAppTitle.TabIndex = 0;
            this.lblAppTitle.Text = "온도 모니터링 시스템";
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabMonitor);
            this.tabMain.Controls.Add(this.tabPage2);
            this.tabMain.Controls.Add(this.tabPage3);
            this.tabMain.Controls.Add(this.tabPage4);
            this.tabMain.Controls.Add(this.tabPage5);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 67);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(608, 459);
            this.tabMain.TabIndex = 1;
            // 
            // tabMonitor
            // 
            this.tabMonitor.Controls.Add(this.groupBox3);
            this.tabMonitor.Controls.Add(this.grpChart);
            this.tabMonitor.Controls.Add(this.grpCurrent);
            this.tabMonitor.Controls.Add(this.grpSensor);
            this.tabMonitor.Location = new System.Drawing.Point(4, 22);
            this.tabMonitor.Name = "tabMonitor";
            this.tabMonitor.Padding = new System.Windows.Forms.Padding(3);
            this.tabMonitor.Size = new System.Drawing.Size(600, 433);
            this.tabMonitor.TabIndex = 0;
            this.tabMonitor.Text = "실시간 온도";
            this.tabMonitor.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.dataGridView1);
            this.groupBox3.Location = new System.Drawing.Point(10, 306);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(581, 108);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "실시간 수신 기록";
            // 
            // grpChart
            // 
            this.grpChart.Controls.Add(this.chartTemp);
            this.grpChart.Location = new System.Drawing.Point(253, 11);
            this.grpChart.Name = "grpChart";
            this.grpChart.Size = new System.Drawing.Size(338, 289);
            this.grpChart.TabIndex = 2;
            this.grpChart.TabStop = false;
            this.grpChart.Text = "온도 추이";
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
            this.grpCurrent.Location = new System.Drawing.Point(10, 144);
            this.grpCurrent.Name = "grpCurrent";
            this.grpCurrent.Size = new System.Drawing.Size(214, 156);
            this.grpCurrent.TabIndex = 1;
            this.grpCurrent.TabStop = false;
            this.grpCurrent.Text = "현재 온도";
            // 
            // pnlLamp
            // 
            this.pnlLamp.Location = new System.Drawing.Point(12, 56);
            this.pnlLamp.Name = "pnlLamp";
            this.pnlLamp.Size = new System.Drawing.Size(22, 22);
            this.pnlLamp.TabIndex = 2;
            // 
            // lblUnit
            // 
            this.lblUnit.Location = new System.Drawing.Point(162, 34);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(31, 23);
            this.lblUnit.TabIndex = 1;
            this.lblUnit.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblCurrentTemp
            // 
            this.lblCurrentTemp.Font = new System.Drawing.Font("맑은 고딕", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblCurrentTemp.Location = new System.Drawing.Point(87, 10);
            this.lblCurrentTemp.Name = "lblCurrentTemp";
            this.lblCurrentTemp.Size = new System.Drawing.Size(100, 39);
            this.lblCurrentTemp.TabIndex = 0;
            this.lblCurrentTemp.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // grpSensor
            // 
            this.grpSensor.Controls.Add(this.btnDisconnect);
            this.grpSensor.Controls.Add(this.btnConnect);
            this.grpSensor.Controls.Add(this.lblPortNo);
            this.grpSensor.Controls.Add(this.txtPort);
            this.grpSensor.Controls.Add(this.txtIp);
            this.grpSensor.Controls.Add(this.lblIp);
            this.grpSensor.Location = new System.Drawing.Point(10, 11);
            this.grpSensor.Name = "grpSensor";
            this.grpSensor.Size = new System.Drawing.Size(214, 124);
            this.grpSensor.TabIndex = 0;
            this.grpSensor.TabStop = false;
            this.grpSensor.Text = "센서 연결 (SIG-5560)";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(110, 88);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(89, 24);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "연결 해제";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(14, 88);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(89, 24);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "연결";
            this.btnConnect.UseVisualStyleBackColor = true;
            // 
            // lblPortNo
            // 
            this.lblPortNo.AutoSize = true;
            this.lblPortNo.Location = new System.Drawing.Point(6, 58);
            this.lblPortNo.Name = "lblPortNo";
            this.lblPortNo.Size = new System.Drawing.Size(29, 12);
            this.lblPortNo.TabIndex = 3;
            this.lblPortNo.Text = "포트";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(65, 55);
            this.txtPort.MaxLength = 20;
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(82, 21);
            this.txtPort.TabIndex = 2;
            // 
            // txtIp
            // 
            this.txtIp.Location = new System.Drawing.Point(65, 21);
            this.txtIp.MaxLength = 20;
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(133, 21);
            this.txtIp.TabIndex = 1;
            // 
            // lblIp
            // 
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(6, 25);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(44, 12);
            this.lblIp.TabIndex = 0;
            this.lblIp.Text = "IP 주소";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(600, 433);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(600, 433);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(600, 433);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(600, 433);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "tabPage5";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(46, 61);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(38, 12);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "label2";
            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Location = new System.Drawing.Point(12, 89);
            this.lblLastUpdate.Name = "lblLastUpdate";
            this.lblLastUpdate.Size = new System.Drawing.Size(38, 12);
            this.lblLastUpdate.TabIndex = 5;
            this.lblLastUpdate.Text = "label3";
            // 
            // lblThresholdInfo
            // 
            this.lblThresholdInfo.AutoSize = true;
            this.lblThresholdInfo.Location = new System.Drawing.Point(12, 111);
            this.lblThresholdInfo.Name = "lblThresholdInfo";
            this.lblThresholdInfo.Size = new System.Drawing.Size(38, 12);
            this.lblThresholdInfo.TabIndex = 6;
            this.lblThresholdInfo.Text = "label4";
            // 
            // lblMinMax
            // 
            this.lblMinMax.AutoSize = true;
            this.lblMinMax.Location = new System.Drawing.Point(12, 134);
            this.lblMinMax.Name = "lblMinMax";
            this.lblMinMax.Size = new System.Drawing.Size(38, 12);
            this.lblMinMax.TabIndex = 7;
            this.lblMinMax.Text = "label5";
            // 
            // btnResetMinMax
            // 
            this.btnResetMinMax.Enabled = false;
            this.btnResetMinMax.Location = new System.Drawing.Point(135, 123);
            this.btnResetMinMax.Name = "btnResetMinMax";
            this.btnResetMinMax.Size = new System.Drawing.Size(69, 25);
            this.btnResetMinMax.TabIndex = 8;
            this.btnResetMinMax.Text = "초기화";
            this.btnResetMinMax.UseVisualStyleBackColor = true;
            // 
            // chartTemp
            // 
            chartArea2.Name = "ChartArea1";
            this.chartTemp.ChartAreas.Add(chartArea2);
            this.chartTemp.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Name = "Legend1";
            this.chartTemp.Legends.Add(legend2);
            this.chartTemp.Location = new System.Drawing.Point(3, 17);
            this.chartTemp.Name = "chartTemp";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.IsVisibleInLegend = false;
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chartTemp.Series.Add(series2);
            this.chartTemp.Size = new System.Drawing.Size(332, 269);
            this.chartTemp.TabIndex = 0;
            this.chartTemp.Text = "chart1";
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(3, 17);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(575, 88);
            this.dataGridView1.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(608, 526);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlTop);
            this.Name = "MainForm";
            this.Text = "온도 모니터링 시스템";
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.tabMain.ResumeLayout(false);
            this.tabMonitor.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.grpChart.ResumeLayout(false);
            this.grpCurrent.ResumeLayout(false);
            this.grpCurrent.PerformLayout();
            this.grpSensor.ResumeLayout(false);
            this.grpSensor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
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
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.Label lblPortNo;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.GroupBox groupBox3;
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
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}