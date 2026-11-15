namespace TempMonitor.Models
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RoleCode { get; set; }

        public bool IsAdmin
        {
            get { return RoleCode == "ADMIN"; }
        }

        public string RoleName
        {
            get { return IsAdmin ? "관리자" : "작업자"; }
        }
    }
}
