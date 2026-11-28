// ============================================================================================
//  Program.cs : 프로그램의 "시작점" 파일
// --------------------------------------------------------------------------------------------
//  exe 를 더블클릭하면 Windows 가 제일 먼저 이 파일의 Main() 메서드를 실행합니다.
//  이 파일이 하는 일은 딱 하나: "어떤 창을 어떤 순서로 띄울지" 결정하는 것.
//
//    로그인 창(LoginForm.cs) ──성공──▶ 메인 창(MainForm.cs) ──로그아웃──▶ 다시 로그인 창 ...
//         │                                 │
//         └─ 종료/ESC ─▶ 프로그램 끝         └─ X 로 닫기 ─▶ 프로그램 끝
//
//  두 창은 서로를 직접 열지 않습니다. 둘 다 "결과"만 남기고 닫히면, Program 이 그 결과를 보고
//  다음 창을 정합니다.
//    - LoginForm  → DialogResult 속성 (OK = 로그인 성공)
//    - MainForm   → LogoutRequested 속성 (true = 로그아웃 버튼으로 닫힘)
// ============================================================================================

using System;                  // 기본 타입들 (여기서는 [STAThread] 특성이 System 안에 있음)
using System.Net;              // ServicePointManager, SecurityProtocolType (HTTPS 보안 설정) 을 쓰기 위해
using System.Windows.Forms;    // Application, DialogResult 등 WinForms 기능을 쓰기 위해

namespace TempMonitor          // 이 프로젝트의 클래스들이 모여 있는 "이름 공간" (폴더 같은 개념)
{
    // internal : 이 프로젝트(exe) 안에서만 보이는 클래스
    // static   : 객체를 만들지 않고(new Program() 불가) 바로 쓰는 클래스. Main 은 객체 없이 실행돼야 하므로 static.
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        ///
        /// 흐름: 로그인 창(모달) → 성공하면 메인 창 → 로그아웃하면 다시 로그인 창 → ...
        ///       로그인 창에서 종료하거나 메인 창을 X 로 닫으면 프로그램 종료.
        /// </summary>
        [STAThread]            // WinForms 는 "단일 스레드 아파트(STA)" 방식이어야 함. 클립보드·파일 대화상자 등이 이걸 요구 (외워 두기)
        static void Main()     // C# 프로그램의 시작 메서드. 이름이 반드시 Main 이어야 Windows 가 찾아서 실행함
        {
            Application.EnableVisualStyles();                      // 버튼·체크박스 등을 Windows 최신 테마 모양으로 그리기
            Application.SetCompatibleTextRenderingDefault(false);  // 글자를 새 방식(GDI)으로 그리기. 템플릿 기본 코드 (Form 만들기 전에 호출해야 함)

            // 카카오 API 는 TLS 1.2 이상만 허용합니다.
            // ServicePointManager.SecurityProtocol : 이 프로그램의 모든 HTTPS 통신이 쓸 보안 방식 목록
            // |=  : "기존 값은 그대로 두고 Tls12 를 추가로 켜기" (비트 OR 연산 후 대입)
            // 왜 여기서? → Services/KakaoService.cs 가 카카오 서버에 접속하기 "전에" 딱 한 번만 하면 되는 설정이라 시작점에 둠
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            while (true)       // 무한 반복: 로그아웃하면 다시 로그인 창으로 돌아오기 위해 (빠져나가는 건 아래 return 들)
            {
                // ---- ① 로그인 창 ---------------------------------------------------------------
                // new LoginForm()  → LoginForm.cs 의 생성자 실행 → InitializeComponent() 로 화면 구성
                // using (...) { } → 블록이 끝나면 login.Dispose() 가 자동 호출되어 창 자원(메모리, 핸들) 정리
                using (var login = new LoginForm())   // var : 오른쪽을 보고 타입(LoginForm)을 컴파일러가 알아서 정함
                {
                    // ShowDialog() : "모달" 로 띄움 = 이 창이 닫힐 때까지 다음 줄로 넘어가지 않고 여기서 기다림
                    // 창이 닫히면 그 창의 DialogResult 값을 돌려줌
                    //   - LoginForm.btnLogin_Click 에서 로그인 성공 시 DialogResult = OK 로 설정 → OK 반환
                    //   - LoginForm.btnExit_Click / ESC / X 버튼 → Cancel 반환
                    if (login.ShowDialog() != DialogResult.OK)   // OK 가 아니면 (= 로그인 안 하고 닫음)
                        return;                                  // Main 종료 = 프로그램 종료
                }   // ← 여기서 login 창 객체 정리됨

                // ---- ② 메인 창 -----------------------------------------------------------------
                bool logout;   // 메인 창이 "로그아웃 버튼" 으로 닫혔는지 담을 변수.
                               // using 블록 밖에 선언하는 이유: 블록이 끝나면 main 객체는 정리되므로,
                               // 필요한 값만 미리 이 변수로 옮겨 두고 블록 밖에서 쓰려고.

                using (var main = new MainForm())   // MainForm.cs 생성자 실행 → 화면 구성 + 센서 객체 준비
                {
                    // Application.Run(main) : 메인 창을 띄우고, 창이 닫힐 때까지 여기서 대기.
                    //  (ShowDialog 와 비슷하지만, 프로그램의 "주 창" 으로 메시지 루프를 돌리는 방식)
                    Application.Run(main);

                    // 창이 닫힌 뒤 MainForm 의 공개 속성 LogoutRequested 를 읽음.
                    // 이 값은 MainForm.btnLogout_Click 에서만 true 로 바뀜 (X 로 닫으면 false 그대로)
                    logout = main.LogoutRequested;
                }   // ← 여기서 main 창 객체 정리됨

                // Session.cs 의 static 메서드 → Session.CurrentUser = null (로그인 정보 지우기)
                // 다음 사람이 로그인하기 전에 이전 사용자 정보가 남지 않게.
                Session.Clear();

                if (!logout)   // 로그아웃이 아니면 (= X 로 닫음)
                    return;    // 프로그램 종료
                // 로그아웃이면 while 처음으로 돌아가서 로그인 창을 다시 띄움
            }
        }
    }
}
