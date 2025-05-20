# TeraLinkaMSDocEditorApi

## 設定檔說明 (appsettings.json)

### 文檔相關設定

- `DocStoragePath`: "D:\\WorkSpace\\Storage\\Doc"

  - 用於存儲文檔的本地路徑
  - 系統會將所有上傳的文檔存儲在此路徑下

- `DocStorageUrl`: "http://192.168.50.131:860/doc"

  - 文檔的 HTTP 訪問 URL
  - 用於外部系統訪問存儲的文檔

- `SelfHostedUrl`: "http://localhost:5292/api"
  - API 的自託管 URL
  - 用於配置 API 的訪問地址

### 系統設定

- `Language`: "zh-TW"
  - 系統使用的語言設定
  - 目前設定為繁體中文

### 安全設定

- `AllowedOrigins`: 允許跨域請求的來源列表

  ```json
  [
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:5174",
    "https://localhost:5174",
    "http://localhost:8080",
    "https://localhost:8080",
    "http://localhost:3001",
    "http://localhost:3000"
  ]
  ```

  - 用於 CORS（跨源資源共享）安全設定
  - 只有列表中的網址可以訪問 API

- `JWTSecret`
  - JWT（JSON Web Token）認證使用的密鑰
  - 用於生成和驗證 JWT 令牌的安全密鑰
  - 建議在生產環境中使用強密鑰並妥善保管
