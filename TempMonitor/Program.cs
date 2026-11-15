using System;
using System.Net;
using System.Windows.Forms;

namespace TempMonitor
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        ///
        /// 흐름: 로그인 창(모달) → 성공하면 메인 창 → 로그아웃하면 다시 로그인 창 → ...
        ///       로그인 창에서 종료하거나 메인 창을 X 로 닫으면 프로그램 종료.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 카카오 API 는 TLS 1.2 이상만 허용합니다.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                }

                bool logout;
                using (var main = new MainForm())
                {
                    Application.Run(main);
                    logout = main.LogoutRequested;
                }

                Session.Clear();
                if (!logout)
                    return;
            }
        }
    }
}
