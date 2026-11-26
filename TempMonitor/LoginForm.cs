using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using TempMonitor.Services;

namespace TempMonitor
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _auth = new AuthService();

        public LoginForm()
        {
            // 화면 속성(AcceptButton=엔터 로그인, CancelButton=ESC 종료, 가운데 표시, 크기 고정)과
            // 버튼 이벤트 연결은 모두 디자이너(LoginForm.Designer.cs)에서 설정합니다.
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // async void 는 "이벤트 핸들러"에서만 씁니다. (예외는 반드시 내부에서 잡기)
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string userId = txtUserId.Text.Trim();
            string password = txtPassword.Text;

            if (userId.Length == 0) { ShowMessage("아이디를 입력하세요."); txtUserId.Focus(); return; }
            if (password.Length == 0) { ShowMessage("비밀번호를 입력하세요."); txtPassword.Focus(); return; }

            SetBusy(true);
            try
            {
                LoginResult result = await _auth.LoginAsync(userId, password);
                if (result.Success)
                {
                    Session.CurrentUser = result.User;
                    DialogResult = DialogResult.OK;   // ShowDialog() 가 OK 를 돌려주며 창이 닫힘
                    return;
                }

                ShowMessage(result.Message);
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
            catch (SqlException ex)
            {
                ShowMessage("DB 에 연결할 수 없습니다.");
                MessageBox.Show(this, "DB 연결 오류\n\n" + ex.Message, "로그인", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ShowMessage("로그인 중 오류가 발생했습니다.");
                MessageBox.Show(this, ex.Message, "로그인", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            btnLogin.Enabled = !busy;           // 처리 중 중복 클릭 방지
            txtUserId.Enabled = !busy;
            txtPassword.Enabled = !busy;
            UseWaitCursor = busy;
            if (busy) lblMessage.Text = "로그인 중...";
        }

        private void ShowMessage(string message)
        {
            lblMessage.Text = message;
        }
    }
}
