# 🚀 Lccnet AI Competition Platform (AI 競賽與算力資源管理平台)

## 📖 專案簡介 (Introduction)

**Lccnet AI Competition Platform** 是一個整合「AI 賽事管理」與「運算資源調度」的全端 Web 系統。旨在解決傳統競賽中「評測流程繁瑣」與「開發環境配置困難」的痛點。

本系統提供從**資料集加密下載**、**即時自動評測 (Auto-Evaluation)**、**動態排行榜**到**GPU 開發環境預約**的一站式解決方案。系統後端採用 ASP.NET Core 構建，並針對高併發的提交場景進行了 I/O 與演算法層面的深度優化。

---

## 📸 系統展示 (Demo & Screenshots)

### 1. 競賽儀表板與排行榜 (Dashboard & Leaderboard)

使用者可檢視當前排名，系統利用 ViewModel 優化前端渲染，並透過 LINQ 在記憶體中快速聚合計算排名。

<img width="1919" height="834" alt="image" src="https://github.com/user-attachments/assets/9c66d157-e4b7-4b41-8e8a-c244f03b336f" />

### 2. 檔案提交與即時評測 (Submission Flow)

支援 CSV 與 ZIP 影像檔上傳，後端即時計算 F1-Score / IoU 並回傳結果。

### 3. 開發環境預約系統 (Resource Reservation)

管理員控管的資源分配介面，解決多使用者間的算力衝突問題。

---

## 🔥 核心技術亮點 (Technical Highlights)

這是本專案與一般 CRUD 系統最大的不同之處，展現了對**演算法實作**、**效能優化**與**資料一致性**的掌握。

### 1. C# 原生 AI 評測演算法 (Native AI Metrics Implementation)

為了減少呼叫外部 Python Script 的 Process 開銷 (Overhead)，本系統直接在 .NET Core 層實作了完整的評估邏輯：

* **Classification Task:** 實作 `ConfusionMatrix` 統計 (TP/TN/FP/FN)，並據此計算 Precision, Recall 與 **Macro F1-Score**。
* **Semantic Segmentation Task:** 使用 **SixLabors.ImageSharp** 進行像素級 (Pixel-level) 操作。直接讀取 `L8` 格式的灰階遮罩，進行二值化處理並計算 **IoU (Intersection over Union)**，實現高效的影像比對。

### 2. 高效能記憶體流處理 (In-Memory Stream Processing)

針對競賽中頻繁的大檔上傳 (ZIP/CSV)，系統採用 **Zero-Disk-I/O** 策略：

* 利用 `IFormFile.OpenReadStream()` 與 `ZipArchive` 直接在記憶體中解壓並遍歷檔案。
* **優勢：** 大幅降低伺服器硬碟 I/O 負載，並避免惡意檔案落地 (File Landing) 的資安風險。

### 3. 資料一致性與交易機制 (ACID Transactions)

使用 EF Core 的 `BeginTransaction()` 機制包裹「評分邏輯」與「資料庫寫入」：

* 確保 **Score Calculation** 與 **Record Insertion** 具備原子性 (Atomicity)。
* 若評分過程中發生異常 (如檔案格式錯誤)，系統會自動 **Rollback**，防止資料庫產生無效的垃圾資料 (Dirty Data)。

---

## 🏗️ 系統架構 (System Architecture)

本專案遵循標準的 **MVC (Model-View-Controller)** 設計模式，並透過 **ViewModel** 實現前後端資料隔離。

```text
lccnet-competition/
├── Controllers/              # 核心業務邏輯 (Auth, Competition, Reservation)
├── Data/                     # EF Core DbContext 與資料庫遷移配置
├── Models/                   # 資料庫實體 (Entities)
├── ViewModels/               # DTOs (如 LeaderboardViewModel) 用於優化前端傳輸
├── Views/                    # Razor Views 與 Partial Views (模組化 UI)
└── Services/                 # (可選) 封裝共用邏輯

```

### 技術棧 (Tech Stack)

* **Backend:** ASP.NET Core 8.0 MVC
* **Database:** SQL Server (Production) / EF Core (ORM)
* **Image Processing:** SixLabors.ImageSharp
* **Security:** Cookie Authentication, HSTS, Anti-Forgery Token
* **Frontend:** HTML5, CSS3, Bootstrap 5, jQuery

---

## 🚀 如何執行 (Getting Started)

### 前置需求

* .NET SDK 8.0+
* SQL Server (LocalDB or Docker container)

### 安裝步驟

1. **Clone 專案**
```bash
git clone https://github.com/your-username/lccnet-competition.git
cd lccnet-competition

```


2. **設定資料庫連線**
打開 `appsettings.json` 並修改 `ConnectionStrings`:
```json
"ConnectionStrings": {
  "DeploymentConnection": "Server=(localdb)\\mssqllocaldb;Database=LccnetCompetition;Trusted_Connection=True;"
}

```


3. **執行資料庫遷移 (Migrations)**
```bash
dotnet ef database update

```


4. **啟動專案**
```bash
dotnet run

```


瀏覽器打開 `https://localhost:<port>` 即可看到畫面。

---

## 👤 Author

* **Jian-An Wang** - 
* Open for Full-Stack / Backend Developer opportunities.
