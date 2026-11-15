<#
  카카오 "나에게 보내기" 토큰 최초 발급 → TB_CONFIG 저장

  하는 일
    1) 브라우저로 카카오 로그인/동의 화면을 엽니다. (talk_message 권한 요청)
    2) 로그인 후 이동한 주소에서 인가 코드(code)를 받아
    3) 토큰(access_token, refresh_token)으로 교환하고
    4) TB_CONFIG 에 REST API 키 / 클라이언트 시크릿 / 토큰을 저장합니다.

  이후에는 TempMonitor 가 액세스 토큰이 만료(401)될 때마다 리프레시 토큰으로 알아서 갱신합니다.
  리프레시 토큰까지 만료되면(약 2개월 동안 한 번도 발송이 없을 때) 이 스크립트를 다시 실행하세요.

  실행: PowerShell 에서
    powershell -ExecutionPolicy Bypass -File .\02_KakaoToken.ps1
#>
param(
    [string]$Server   = "localhost",
    [string]$Database = "TempMonitorDB"
)

$ErrorActionPreference = "Stop"
# 카카오 API 는 TLS 1.2 이상만 허용 (Windows PowerShell 5.1 기본값은 TLS 1.0 일 수 있음)
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

Write-Host ""
Write-Host "=== 카카오 토큰 발급 ===" -ForegroundColor Cyan
Write-Host "카카오 디벨로퍼스 > 내 애플리케이션 > 앱 > 플랫폼 키 > REST API 키 에서 확인한 값을 입력하세요."
Write-Host ""

$restKey = (Read-Host "REST API 키").Trim()
if (-not $restKey) { Write-Host "REST API 키가 필요합니다." -ForegroundColor Red; exit 1 }

$secret = (Read-Host "클라이언트 시크릿 (사용 안 함으로 껐다면 그냥 엔터)").Trim()

$redirect = (Read-Host "리다이렉트 URI (엔터 = https://localhost)").Trim()
if (-not $redirect) { $redirect = "https://localhost" }

# 1) 인가 코드 요청 (scope=talk_message : 카카오톡 메시지 전송 권한 동의 요청)
$authUrl = "https://kauth.kakao.com/oauth/authorize?response_type=code" +
           "&client_id=$restKey" +
           "&redirect_uri=$([uri]::EscapeDataString($redirect))" +
           "&scope=talk_message"

Write-Host ""
Write-Host "브라우저가 열립니다. 카카오 로그인 → 동의하기 를 누르세요." -ForegroundColor Yellow
Write-Host "그러면 '사이트에 연결할 수 없음' 페이지가 뜨는데 정상입니다. (https://localhost 에는 서버가 없음)"
Write-Host "그 페이지의 주소창 전체를 복사해서 아래에 붙여 넣으세요.  예) https://localhost/?code=AbC123..."
Start-Process $authUrl

$pasted = (Read-Host "주소창 내용 (또는 code 값만)").Trim()
if ($pasted -match "[?&]error=([^&]+)") {
    Write-Host "카카오 인증 오류: $($Matches[1])  ($pasted)" -ForegroundColor Red
    exit 1
}
if ($pasted -match "[?&]code=([^&]+)") { $code = $Matches[1] } else { $code = $pasted }
if (-not $code) { Write-Host "code 를 찾지 못했습니다." -ForegroundColor Red; exit 1 }

# 2) 인가 코드 → 토큰 교환 (코드는 1회용, 몇 분 안에 써야 함)
$body = @{
    grant_type   = "authorization_code"
    client_id    = $restKey
    redirect_uri = $redirect
    code         = $code
}
if ($secret) { $body.client_secret = $secret }

try {
    $tok = Invoke-RestMethod -Method Post -Uri "https://kauth.kakao.com/oauth/token" `
                             -ContentType "application/x-www-form-urlencoded;charset=utf-8" -Body $body
}
catch {
    Write-Host "토큰 발급 실패: $($_.ErrorDetails.Message)" -ForegroundColor Red
    Write-Host "  KOE010 / invalid_client  → 클라이언트 시크릿 확인"
    Write-Host "  KOE006 / KOE303          → 리다이렉트 URI 가 콘솔에 등록된 값과 정확히 같은지 확인"
    Write-Host "  KOE320 / invalid_grant   → 코드가 만료/재사용됨. 스크립트를 처음부터 다시 실행"
    exit 1
}

if ($tok.scope -notmatch "talk_message") {
    Write-Host "경고: 토큰에 talk_message 권한이 없습니다. (scope = $($tok.scope))" -ForegroundColor Yellow
    Write-Host "      카카오 로그인 > 동의항목 에서 '카카오톡 메시지 전송' 을 설정했는지 확인하세요."
}

# 3) TB_CONFIG 저장 (있으면 UPDATE, 없으면 INSERT) — 하나라도 실패하면 전부 취소(트랜잭션)
$values = [ordered]@{
    "KAKAO_REST_API_KEY"  = $restKey
    "KAKAO_CLIENT_SECRET" = $secret
    "KAKAO_ACCESS_TOKEN"  = $tok.access_token
    "KAKAO_REFRESH_TOKEN" = $tok.refresh_token
}

$sql = @"
UPDATE dbo.TB_CONFIG
   SET CONFIG_VALUE = @VALUE, UPD_USER_ID = 'SCRIPT', UPD_DT = SYSDATETIME()
 WHERE CONFIG_KEY = @KEY;
IF @@ROWCOUNT = 0
    INSERT INTO dbo.TB_CONFIG (CONFIG_KEY, CONFIG_VALUE, UPD_USER_ID) VALUES (@KEY, @VALUE, 'SCRIPT');
"@

$conn = New-Object System.Data.SqlClient.SqlConnection(
    "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True")
$conn.Open()
$tx = $conn.BeginTransaction()
try {
    foreach ($k in $values.Keys) {
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = $sql
        [void]$cmd.Parameters.Add("@KEY", [Data.SqlDbType]::VarChar, 50)
        [void]$cmd.Parameters.Add("@VALUE", [Data.SqlDbType]::NVarChar, 1000)
        $cmd.Parameters["@KEY"].Value = $k
        $cmd.Parameters["@VALUE"].Value = [string]$values[$k]
        [void]$cmd.ExecuteNonQuery()
    }
    $tx.Commit()
}
catch {
    $tx.Rollback()
    throw
}
finally {
    $conn.Close()
}

$hours = [math]::Round($tok.expires_in / 3600, 1)
$days  = [math]::Round($tok.refresh_token_expires_in / 86400)
Write-Host ""
Write-Host "완료! TB_CONFIG 에 저장했습니다." -ForegroundColor Green
Write-Host "  액세스 토큰 유효: 약 $hours 시간 (만료되면 프로그램이 자동 갱신)"
Write-Host "  리프레시 토큰 유효: 약 $days 일"
Write-Host "이제 TempMonitor > 카톡 발송 내역 탭 > [테스트 발송] 으로 확인하세요."
