// ============================================================================================
//  LoginForm.cs : 로그인 화면의 "동작" 코드
// --------------------------------------------------------------------------------------------
//  이 클래스는 partial class 라서 두 파일에 나뉘어 있습니다.
//    LoginForm.cs          (이 파일) : 버튼을 눌렀을 때 하는 일
//    LoginForm.Designer.cs (자동 생성) : txtUserId, txtPassword, btnLogin, lblMessage 같은 컨트롤 선언,
//                                       크기/위치/글자, 그리고 "btnLogin.Click += btnLogin_Click" 같은 이벤트 연결
//  그래서 이 파일에 txtUserId 를 선언한 적이 없어도 쓸 수 있는 것.
//
//  누가 이 창을 띄우나? → Program.Main 에서  new LoginForm() → ShowDialog()
//  결과는 어떻게 전달하나? → DialogResult 속성 (OK = 로그인 성공) + Session.CurrentUser
//
//  로그인 버튼 호출 흐름
//    btnLogin_Click → _auth.LoginAsync (Services/AuthService.cs)
//                       → UserRepository → DbHelper → DB
//                   ← LoginResult
//    성공: Session.CurrentUser = 사용자 / DialogResult = OK → 창 닫힘 → Program 이 메인 창을 띄움
//    실패: lblMessage 에 이유 표시
// ============================================================================================

using System;                  // EventArgs, Exception
using System.Data.SqlClient;   // SqlException (DB 오류를 따로 잡으려고)
using System.Windows.Forms;    // Form, MessageBox, DialogResult 등
using TempMonitor.Services;    // AuthService, LoginResult (Services/AuthService.cs)

namespace TempMonitor
{
    // partial : 이 클래스의 나머지 절반이 LoginForm.Designer.cs 에 있음
    // : Form  : WinForms 의 Form(창) 을 상속 → Show, Close, DialogResult, IsDisposed 같은 기능을 물려받음
    public partial class LoginForm : Form
    {
        // Services/AuthService.cs 의 로그인 규칙 객체.
        // 왜 만들었나: 로그인 판단(아이디 확인, 잠김, 비번 비교, 이력 기록)은 AuthService 에 있고,
        //             화면은 _auth.LoginAsync(...) 를 "부르기만" 하려고 하나 만들어 보관.
        // 어디서 쓰나: btnLogin_Click 한 곳
        // 실행 순서: 이 필드 초기화(new AuthService())가 생성자 본문(InitializeComponent)보다 "먼저" 실행됨
        private readonly AuthService _auth = new AuthService();

        // 생성자: Program.Main 의 new LoginForm() 때 실행
        public LoginForm()
        {
            // 화면 속성(AcceptButton=엔터 로그인, CancelButton=ESC 종료, 가운데 표시, 크기 고정)과
            // 버튼 이벤트 연결은 모두 디자이너(LoginForm.Designer.cs)에서 설정합니다.
            InitializeComponent();   // LoginForm.Designer.cs 에 있는 메서드. 컨트롤 생성 + 속성 설정 + 이벤트 연결
        }

        // ---- [종료] 버튼 ---------------------------------------------------------------------------
        // 디자이너에서 btnExit.Click 에 연결됨. ESC 키도 CancelButton 설정으로 이 버튼이 눌린 것과 같음
        // (object sender, EventArgs e) : 모든 이벤트 처리기의 정해진 모양. sender = 이벤트를 일으킨 컨트롤(btnExit)
        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;   // Form 이 물려준 속성. Program.Main 의 ShowDialog() 가 이 값을 받음 → 프로그램 종료
            Close();                              // 창 닫기
        }

        // ---- [로그인] 버튼 -------------------------------------------------------------------------
        // 디자이너에서 btnLogin.Click 에 연결됨. 엔터 키도 AcceptButton 설정으로 이 버튼이 눌린 것과 같음
        // async void 는 "이벤트 핸들러"에서만 씁니다. (예외는 반드시 내부에서 잡기)
        //   - Click 이벤트는 반환형이 void 로 정해져 있어서 Task 를 못 씀
        //   - async 를 붙여야 안에서 await 를 쓸 수 있음 (DB 를 기다리는 동안 화면이 멈추지 않게)
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string userId = txtUserId.Text.Trim();   // 아이디 입력칸 글자. Trim() = 앞뒤 공백 제거
            string password = txtPassword.Text;      // 비밀번호는 공백도 비밀번호의 일부일 수 있어서 Trim 안 함

            // 빈칸 검사: DB 까지 갈 필요도 없으니 화면에서 바로 끝냄
            // { 문장1; 문장2; return; } : 한 줄에 여러 문장. ShowMessage(아래 메서드) → Focus(커서를 그 칸으로) → 메서드 종료
            if (userId.Length == 0) { ShowMessage("아이디를 입력하세요."); txtUserId.Focus(); return; }
            if (password.Length == 0) { ShowMessage("비밀번호를 입력하세요."); txtPassword.Focus(); return; }

            SetBusy(true);   // 아래 메서드: 버튼/입력칸 잠그고 모래시계 커서 → 처리 중 두 번 클릭 방지
            try
            {
                // ★ Services/AuthService.cs 의 LoginAsync 호출 → 규칙 판단 결과(LoginResult)를 받음
                LoginResult result = await _auth.LoginAsync(userId, password);
                if (result.Success)   // 로그인 성공
                {
                    // Session.cs 의 static 속성에 사용자 정보 보관 → MainForm, KakaoService 가 꺼내 씀
                    Session.CurrentUser = result.User;
                    DialogResult = DialogResult.OK;   // ShowDialog() 가 OK 를 돌려주며 창이 닫힘
                    return;                           // (finally 는 return 해도 실행됨)
                }

                // 로그인 실패: AuthService 가 만든 메시지를 그대로 표시
                ShowMessage(result.Message);
                txtPassword.SelectAll();   // 비밀번호 칸 글자 전체 선택 → 바로 다시 입력하면 덮어써짐
                txtPassword.Focus();
            }
            // AuthService → UserRepository → DbHelper 어디서든 DB 오류가 나면 아래 층들은 catch 를 안 했으므로 여기까지 올라옴
            // 좁은 예외(SqlException)를 먼저, 넓은 예외(Exception)를 나중에. 순서를 바꾸면 Exception 이 다 먹어 버림
            catch (SqlException ex)   // DB 연결/쿼리 오류
            {
                ShowMessage("DB 에 연결할 수 없습니다.");
                // MessageBox.Show(소유 창, 내용, 제목, 버튼 종류, 아이콘)
                MessageBox.Show(this, "DB 연결 오류\n\n" + ex.Message, "로그인", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)      // 그 밖의 모든 오류
            {
                ShowMessage("로그인 중 오류가 발생했습니다.");
                MessageBox.Show(this, ex.Message, "로그인", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally   // 성공/실패/return 상관없이 항상 실행
            {
                // IsDisposed : 창이 이미 정리됐나? 로그인 성공해서 창이 닫혔으면 컨트롤을 만지면 안 되므로 확인
                if (!IsDisposed) SetBusy(false);   // 잠근 버튼/입력칸 다시 풀기
            }
        }

        // ---- 처리 중 표시 -------------------------------------------------------------------------
        // busy = true : 잠그기 / false : 풀기  → 매개변수 하나로 두 동작을 처리하려고 !busy 사용
        private void SetBusy(bool busy)
        {
            btnLogin.Enabled = !busy;           // 처리 중 중복 클릭 방지
            txtUserId.Enabled = !busy;
            txtPassword.Enabled = !busy;
            UseWaitCursor = busy;               // Form 의 속성: true 면 모래시계 커서
            if (busy) lblMessage.Text = "로그인 중...";
        }

        // ---- 안내 문구 표시 -----------------------------------------------------------------------
        // 메시지를 보여 주는 곳을 한 메서드로 모아 둠 → 나중에 표시 방식을 바꿀 때 여기만 고치면 됨
        private void ShowMessage(string message)
        {
            lblMessage.Text = message;   // 디자이너의 lblMessage 라벨
        }
    }
}
