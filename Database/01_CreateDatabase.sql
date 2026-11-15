/* =====================================================================
   TempMonitorDB 생성 스크립트 (SQL Server)
   - 여러 번 실행해도 안전하도록(idempotent) 작성했습니다.
     이미 있는 DB/테이블/데이터는 건너뜁니다.
   - 실행 방법
       1) SSMS에서 이 파일을 열고 F5
       2) 또는 명령줄:  sqlcmd -S localhost -E -C -f 65001 -i 01_CreateDatabase.sql
   - 카카오 토큰은 이 스크립트 실행 후 02_KakaoToken.ps1 로 등록합니다. (비밀값이라 파일에 넣지 않음)
   ===================================================================== */
USE master;
GO

IF DB_ID(N'TempMonitorDB') IS NULL
    CREATE DATABASE TempMonitorDB;
GO

USE TempMonitorDB;
GO

/* ---------------------------------------------------------------------
   1. TB_COMMON_CODE : 공통코드
   - 상태값/권한 같은 "코드"와 화면에 보여줄 "이름"을 분리해서 관리합니다.
   - 데이터 테이블에는 'WARNING' 같은 코드만 저장하고, 화면 표시용 이름(경고)은
     조회 시 JOIN 해서 가져옵니다. 이름이 바뀌어도 데이터는 손댈 필요가 없습니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_COMMON_CODE', N'U') IS NULL
CREATE TABLE dbo.TB_COMMON_CODE
(
    GROUP_CD    VARCHAR(20)   NOT NULL,                 -- 코드 그룹 (예: TEMP_STATUS)
    CODE        VARCHAR(20)   NOT NULL,                 -- 코드 값   (예: WARNING)
    CODE_NM     NVARCHAR(50)  NOT NULL,                 -- 표시 이름 (예: 경고)
    SORT_NO     INT           NOT NULL CONSTRAINT DF_TB_COMMON_CODE_SORT_NO DEFAULT (0),
    USE_YN      CHAR(1)       NOT NULL CONSTRAINT DF_TB_COMMON_CODE_USE_YN  DEFAULT ('Y'),
    CONSTRAINT PK_TB_COMMON_CODE PRIMARY KEY (GROUP_CD, CODE),
    CONSTRAINT CK_TB_COMMON_CODE_USE_YN CHECK (USE_YN IN ('Y', 'N'))
);
GO

/* ---------------------------------------------------------------------
   2. TB_USER : 사용자
   - 비밀번호는 절대 평문으로 저장하지 않습니다. SALT + 비밀번호를 SHA-256 해시한 값만 저장.
   - FAIL_CNT 로 연속 로그인 실패 횟수를 세어 5회 이상이면 잠급니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_USER', N'U') IS NULL
CREATE TABLE dbo.TB_USER
(
    USER_ID        VARCHAR(30)    NOT NULL,
    USER_NM        NVARCHAR(50)   NOT NULL,
    PASSWORD_HASH  VARBINARY(32)  NOT NULL,             -- SHA-256 결과는 항상 32바이트
    PASSWORD_SALT  NVARCHAR(32)   NOT NULL,
    ROLE_CD        VARCHAR(20)    NOT NULL,             -- 공통코드 ROLE (ADMIN / OPERATOR)
    USE_YN         CHAR(1)        NOT NULL CONSTRAINT DF_TB_USER_USE_YN   DEFAULT ('Y'),
    FAIL_CNT       INT            NOT NULL CONSTRAINT DF_TB_USER_FAIL_CNT DEFAULT (0),
    LAST_LOGIN_DT  DATETIME2(0)   NULL,
    REG_DT         DATETIME2(0)   NOT NULL CONSTRAINT DF_TB_USER_REG_DT   DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_TB_USER PRIMARY KEY (USER_ID),
    CONSTRAINT CK_TB_USER_ROLE_CD CHECK (ROLE_CD IN ('ADMIN', 'OPERATOR')),
    CONSTRAINT CK_TB_USER_USE_YN  CHECK (USE_YN IN ('Y', 'N'))
);
GO

/* ---------------------------------------------------------------------
   3. TB_LOGIN_HIST : 로그인 이력 (감사 로그)
   - 존재하지 않는 아이디로 시도한 것도 남기기 위해 TB_USER 와 FK 를 걸지 않습니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_LOGIN_HIST', N'U') IS NULL
CREATE TABLE dbo.TB_LOGIN_HIST
(
    HIST_ID      BIGINT         IDENTITY(1,1) NOT NULL,
    USER_ID      VARCHAR(30)    NOT NULL,
    LOGIN_DT     DATETIME2(0)   NOT NULL CONSTRAINT DF_TB_LOGIN_HIST_LOGIN_DT DEFAULT (SYSDATETIME()),
    SUCCESS_YN   CHAR(1)        NOT NULL,
    FAIL_REASON  NVARCHAR(100)  NULL,
    CLIENT_PC    NVARCHAR(100)  NULL,
    CONSTRAINT PK_TB_LOGIN_HIST PRIMARY KEY (HIST_ID)
);
GO

/* ---------------------------------------------------------------------
   4. TB_SENSOR : 센서(설비) 마스터
   - 지금은 센서가 1대지만, MES 에서는 설비가 여러 대인 것이 기본입니다.
     측정 데이터에 SENSOR_ID 를 붙여 두면 나중에 센서가 늘어나도 테이블 구조를 바꿀 필요가 없습니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_SENSOR', N'U') IS NULL
CREATE TABLE dbo.TB_SENSOR
(
    SENSOR_ID   INT            IDENTITY(1,1) NOT NULL,
    SENSOR_NM   NVARCHAR(50)   NOT NULL,
    MODEL_NM    VARCHAR(30)    NOT NULL,
    IP_ADDR     VARCHAR(50)    NOT NULL,
    PORT_NO     INT            NOT NULL CONSTRAINT DF_TB_SENSOR_PORT_NO DEFAULT (502),
    UNIT_ID     TINYINT        NOT NULL CONSTRAINT DF_TB_SENSOR_UNIT_ID DEFAULT (1),
    LOCATION    NVARCHAR(100)  NULL,
    USE_YN      CHAR(1)        NOT NULL CONSTRAINT DF_TB_SENSOR_USE_YN  DEFAULT ('Y'),
    REG_DT      DATETIME2(0)   NOT NULL CONSTRAINT DF_TB_SENSOR_REG_DT  DEFAULT (SYSDATETIME()),
    UPD_DT      DATETIME2(0)   NULL,
    CONSTRAINT PK_TB_SENSOR PRIMARY KEY (SENSOR_ID),
    CONSTRAINT CK_TB_SENSOR_PORT_NO CHECK (PORT_NO BETWEEN 1 AND 65535)
);
GO

/* ---------------------------------------------------------------------
   5. TB_TEMP_LOG : 온도 측정 이력 (가장 많이 쌓이는 테이블)
   - TEMP_VALUE : DECIMAL(5,1)
       센서가 0.1℃ 단위(정수 x10)로 값을 주므로 소수 1자리 고정소수점이 정확히 맞습니다.
       FLOAT 은 27.2 가 27.199999... 처럼 저장될 수 있어 비교/집계 시 오차가 생깁니다.
       -200.0 ~ 850.0 범위는 5자리(정수 4 + 소수 1)면 충분합니다.
   - TEMP_VALUE 가 NULL 인 경우 = 센서 오류로 값을 믿을 수 없을 때 (0 으로 넣으면 진짜 0℃ 와 구분 불가)
   - 클러스터드 인덱스는 계속 증가하는 LOG_ID 에 두어 INSERT 가 항상 맨 끝에 붙도록(페이지 분할 최소화) 하고,
     조회 패턴(센서 + 기간)에 맞춰 비클러스터드 인덱스를 따로 둡니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_TEMP_LOG', N'U') IS NULL
CREATE TABLE dbo.TB_TEMP_LOG
(
    LOG_ID      BIGINT         IDENTITY(1,1) NOT NULL,
    SENSOR_ID   INT            NOT NULL,
    MEASURE_DT  DATETIME2(0)   NOT NULL,                -- 센서에서 값을 읽은 시각
    TEMP_VALUE  DECIMAL(5,1)   NULL,
    STATUS_CD   VARCHAR(10)    NOT NULL,                -- 공통코드 TEMP_STATUS
    ERROR_CD    TINYINT        NOT NULL CONSTRAINT DF_TB_TEMP_LOG_ERROR_CD DEFAULT (0),
    REG_DT      DATETIME2(3)   NOT NULL CONSTRAINT DF_TB_TEMP_LOG_REG_DT   DEFAULT (SYSDATETIME()), -- DB에 들어온 시각
    CONSTRAINT PK_TB_TEMP_LOG PRIMARY KEY CLUSTERED (LOG_ID),
    CONSTRAINT FK_TB_TEMP_LOG_SENSOR FOREIGN KEY (SENSOR_ID) REFERENCES dbo.TB_SENSOR (SENSOR_ID),
    CONSTRAINT CK_TB_TEMP_LOG_STATUS_CD CHECK (STATUS_CD IN ('NORMAL', 'WARNING', 'ERROR'))
);
GO

-- 예전 버전(INCLUDE 에 ERROR_CD 없음)으로 만들어진 인덱스는 지우고 다시 만듦 (온도 기록 조회에서 오류 종류도 함께 읽기 때문)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TB_TEMP_LOG_SENSOR_DT' AND object_id = OBJECT_ID(N'dbo.TB_TEMP_LOG'))
   AND NOT EXISTS (SELECT 1
                     FROM sys.indexes i
                     JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                     JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE i.name = N'IX_TB_TEMP_LOG_SENSOR_DT'
                      AND i.object_id = OBJECT_ID(N'dbo.TB_TEMP_LOG')
                      AND ic.is_included_column = 1
                      AND c.name = N'ERROR_CD')
    DROP INDEX IX_TB_TEMP_LOG_SENSOR_DT ON dbo.TB_TEMP_LOG;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TB_TEMP_LOG_SENSOR_DT' AND object_id = OBJECT_ID(N'dbo.TB_TEMP_LOG'))
    CREATE NONCLUSTERED INDEX IX_TB_TEMP_LOG_SENSOR_DT
        ON dbo.TB_TEMP_LOG (SENSOR_ID, MEASURE_DT)
        INCLUDE (TEMP_VALUE, STATUS_CD, ERROR_CD);   -- 조회에 필요한 컬럼을 포함시켜 테이블(클러스터드)까지 안 가도 되게 함(커버링 인덱스)
GO

/* ---------------------------------------------------------------------
   6. TB_ALERT_LOG : 경고 발생 이력
   - THRESHOLD_VALUE 를 같이 저장하는 이유:
     기준 온도는 설정에서 언제든 바뀝니다. "그때 기준이 몇 도였는지"를 남겨야
     나중에 이력을 봐도 왜 경고였는지 설명할 수 있습니다. (스냅샷 저장)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_ALERT_LOG', N'U') IS NULL
CREATE TABLE dbo.TB_ALERT_LOG
(
    ALERT_ID         BIGINT        IDENTITY(1,1) NOT NULL,
    SENSOR_ID        INT           NOT NULL,
    ALERT_DT         DATETIME2(0)  NOT NULL,
    TEMP_VALUE       DECIMAL(5,1)  NOT NULL,
    THRESHOLD_VALUE  DECIMAL(5,1)  NOT NULL,
    KAKAO_STATUS_CD  VARCHAR(10)   NOT NULL,             -- 공통코드 KAKAO_STATUS
    CONSTRAINT PK_TB_ALERT_LOG PRIMARY KEY CLUSTERED (ALERT_ID),
    CONSTRAINT FK_TB_ALERT_LOG_SENSOR FOREIGN KEY (SENSOR_ID) REFERENCES dbo.TB_SENSOR (SENSOR_ID)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TB_ALERT_LOG_ALERT_DT' AND object_id = OBJECT_ID(N'dbo.TB_ALERT_LOG'))
    CREATE NONCLUSTERED INDEX IX_TB_ALERT_LOG_ALERT_DT ON dbo.TB_ALERT_LOG (ALERT_DT);
GO

/* ---------------------------------------------------------------------
   7. TB_KAKAO_LOG : 카카오톡 발송 이력
   - ALERT_ID 는 NULL 허용: "테스트 발송"처럼 경고와 무관한 발송도 있기 때문.
   - 경고 1건 → 발송 0~N건 (1:N 관계)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_KAKAO_LOG', N'U') IS NULL
CREATE TABLE dbo.TB_KAKAO_LOG
(
    SEND_ID      BIGINT         IDENTITY(1,1) NOT NULL,
    ALERT_ID     BIGINT         NULL,
    SEND_DT      DATETIME2(0)   NOT NULL,
    RECEIVER_NM  NVARCHAR(50)   NOT NULL,
    MESSAGE      NVARCHAR(500)  NOT NULL,
    RESULT_CD    VARCHAR(10)    NOT NULL,                -- 공통코드 SEND_RESULT (SUCCESS / FAIL)
    RESULT_MSG   NVARCHAR(500)  NULL,                    -- 실패 사유
    CONSTRAINT PK_TB_KAKAO_LOG PRIMARY KEY CLUSTERED (SEND_ID),
    CONSTRAINT FK_TB_KAKAO_LOG_ALERT FOREIGN KEY (ALERT_ID) REFERENCES dbo.TB_ALERT_LOG (ALERT_ID)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TB_KAKAO_LOG_SEND_DT' AND object_id = OBJECT_ID(N'dbo.TB_KAKAO_LOG'))
    CREATE NONCLUSTERED INDEX IX_TB_KAKAO_LOG_SEND_DT ON dbo.TB_KAKAO_LOG (SEND_DT);
GO

/* ---------------------------------------------------------------------
   8. TB_CONFIG : 환경 설정 (Key-Value)
   - 설정 항목이 늘어나도 ALTER TABLE 없이 행만 추가하면 됩니다.
   - 대신 값이 문자열이므로 프로그램에서 형 변환/검증을 해야 합니다.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TB_CONFIG', N'U') IS NULL
CREATE TABLE dbo.TB_CONFIG
(
    CONFIG_KEY    VARCHAR(50)     NOT NULL,
    CONFIG_VALUE  NVARCHAR(1000)  NOT NULL CONSTRAINT DF_TB_CONFIG_VALUE  DEFAULT (N''),
    CONFIG_DESC   NVARCHAR(200)   NULL,
    UPD_USER_ID   VARCHAR(30)     NULL,                  -- 누가 바꿨는지
    UPD_DT        DATETIME2(0)    NOT NULL CONSTRAINT DF_TB_CONFIG_UPD_DT DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_TB_CONFIG PRIMARY KEY (CONFIG_KEY)
);
GO

/* =====================================================================
   기초 데이터 (없을 때만 넣음)
   ===================================================================== */

-- 공통코드
INSERT INTO dbo.TB_COMMON_CODE (GROUP_CD, CODE, CODE_NM, SORT_NO)
SELECT v.GROUP_CD, v.CODE, v.CODE_NM, v.SORT_NO
FROM (VALUES
    ('TEMP_STATUS',  'NORMAL',   N'정상',            1),
    ('TEMP_STATUS',  'WARNING',  N'경고',            2),
    ('TEMP_STATUS',  'ERROR',    N'센서 오류',       3),
    ('ROLE',         'ADMIN',    N'관리자',          1),
    ('ROLE',         'OPERATOR', N'작업자',          2),
    ('KAKAO_STATUS', 'SENT',     N'발송',            1),
    ('KAKAO_STATUS', 'FAILED',   N'발송 실패',       2),
    ('KAKAO_STATUS', 'SKIPPED',  N'생략(최소 간격)', 3),
    ('KAKAO_STATUS', 'DISABLED', N'미사용',          4),
    ('KAKAO_STATUS', 'PENDING',  N'발송 중',         5),
    ('SEND_RESULT',  'SUCCESS',  N'성공',            1),
    ('SEND_RESULT',  'FAIL',     N'실패',            2),
    ('SENSOR_ERROR', '0',        N'에러 없음',       1),
    ('SENSOR_ERROR', '1',        N'하드웨어 초기화 에러', 2),
    ('SENSOR_ERROR', '2',        N'하드웨어 런타임 에러', 3),
    ('SENSOR_ERROR', '3',        N'온도센서 연결 끊김',   4),
    ('SENSOR_ERROR', '4',        N'온도센서 측정 실패',   5)
) AS v (GROUP_CD, CODE, CODE_NM, SORT_NO)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TB_COMMON_CODE c WHERE c.GROUP_CD = v.GROUP_CD AND c.CODE = v.CODE);
GO

-- 사용자 (개발/테스트용 계정입니다. 운영 시 반드시 비밀번호를 바꾸세요)
--   admin    / admin1234  (관리자)
--   operator / oper1234   (작업자)
-- 해시 = SHA2_256( SALT + 비밀번호 ), NVARCHAR(UTF-16) 기준 → C# 의 Encoding.Unicode 와 동일
DECLARE @salt1 NVARCHAR(32) = N'b7Q2xK9mZ4pL1sVe';
DECLARE @salt2 NVARCHAR(32) = N'R3nT8wY6cH0jD5aF';

IF NOT EXISTS (SELECT 1 FROM dbo.TB_USER WHERE USER_ID = 'admin')
    INSERT INTO dbo.TB_USER (USER_ID, USER_NM, PASSWORD_HASH, PASSWORD_SALT, ROLE_CD)
    VALUES ('admin', N'관리자', HASHBYTES('SHA2_256', @salt1 + N'admin1234'), @salt1, 'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.TB_USER WHERE USER_ID = 'operator')
    INSERT INTO dbo.TB_USER (USER_ID, USER_NM, PASSWORD_HASH, PASSWORD_SALT, ROLE_CD)
    VALUES ('operator', N'작업자', HASHBYTES('SHA2_256', @salt2 + N'oper1234'), @salt2, 'OPERATOR');
GO

-- 센서 (IP 는 spFinder 로 확인한 실제 센서 IP 로 바꾸세요. 프로그램에서 연결하면 자동으로 갱신됩니다)
IF NOT EXISTS (SELECT 1 FROM dbo.TB_SENSOR)
    INSERT INTO dbo.TB_SENSOR (SENSOR_NM, MODEL_NM, IP_ADDR, PORT_NO, UNIT_ID, LOCATION)
    VALUES (N'온도센서 #1', 'SIG-5560', '10.6.0.110', 502, 1, N'1공장');
GO

-- 환경 설정
INSERT INTO dbo.TB_CONFIG (CONFIG_KEY, CONFIG_VALUE, CONFIG_DESC)
SELECT v.CONFIG_KEY, v.CONFIG_VALUE, v.CONFIG_DESC
FROM (VALUES
    ('ALERT_TEMP',          N'30.0',     N'경고 기준 온도(℃)'),
    ('ALERT_INTERVAL_MIN',  N'10',       N'알림 최소 간격(분)'),
    ('ALERT_RISE_STEP',     N'5.0',      N'재알림 상승 폭(℃) - 직전 알림보다 이만큼 오르면 즉시 재알림, 0=사용 안 함'),
    ('KAKAO_ENABLED',       N'Y',        N'카카오톡 알림 사용 여부(Y/N)'),
    ('READ_CYCLE_SEC',      N'1',        N'센서 읽기 주기(초)'),
    ('SAVE_CYCLE_SEC',      N'5',        N'DB 저장 주기(초)'),
    ('KAKAO_RECEIVER_NM',   N'담당자',   N'카카오톡 수신자 표시 이름'),
    ('KAKAO_REST_API_KEY',  N'',         N'카카오 REST API 키 (토큰 자동 갱신용)'),
    ('KAKAO_CLIENT_SECRET', N'',         N'카카오 클라이언트 시크릿 (앱에서 ON 이면 필수)'),
    ('KAKAO_ACCESS_TOKEN',  N'',         N'카카오 액세스 토큰 (나에게 보내기)'),
    ('KAKAO_REFRESH_TOKEN', N'',         N'카카오 리프레시 토큰')
) AS v (CONFIG_KEY, CONFIG_VALUE, CONFIG_DESC)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TB_CONFIG c WHERE c.CONFIG_KEY = v.CONFIG_KEY);
GO

/* =====================================================================
   데이터 보관 정책용 프로시저  (CREATE OR ALTER : SQL Server 2016 SP1 이상)
   - TB_TEMP_LOG 는 5초마다 1건이면 하루 17,280건, 1년이면 약 630만 건입니다.
   - 오래된 데이터를 한 번에 DELETE 하면 로그 파일이 커지고 테이블이 오래 잠기므로
     5,000건씩 나눠서 지웁니다. SQL Server Agent 작업으로 매일 새벽 실행하면 됩니다.
       EXEC dbo.USP_PURGE_TEMP_LOG @KEEP_DAYS = 365;
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.USP_PURGE_TEMP_LOG
    @KEEP_DAYS INT = 365
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cutoff DATETIME2(0) = DATEADD(DAY, -@KEEP_DAYS, SYSDATETIME());
    DECLARE @deleted INT = 1;

    WHILE @deleted > 0
    BEGIN
        DELETE TOP (5000) FROM dbo.TB_TEMP_LOG WHERE MEASURE_DT < @cutoff;
        SET @deleted = @@ROWCOUNT;
    END
END
GO

PRINT N'TempMonitorDB 준비 완료';
GO
