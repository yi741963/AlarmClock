# 智慧鬧鐘系統 🔔

使用 .NET 9 和 WPF 建立的 Windows 11 智慧鬧鐘小工具，具備完整的 UI 編輯功能和 JSON 設定持久化。

## 功能特色

### 核心功能
- ⏰ **動態鬧鐘管理**
  - ➕ 新增無限數量的鬧鐘
  - ✏️ 編輯現有鬧鐘
  - 🗑️ 刪除鬧鐘
  - 🔔/🔕 快速啟用/停用切換

- 🎯 **智慧響鈴邏輯**
  - 檢測使用者是否正在使用電腦
  - **有人使用時**：響鈴自訂秒數（1-60秒）後自動關閉
  - **無人使用時**：可設定持續響鈴時間（1-30分鐘）或永不停止（設為0）
  - 每個鬧鐘可獨立設定響鈴時間
  - ♾️ 支援無限響鈴模式，直到手動關閉

- 👤 **使用者活動檢測**
  - 即時監測鍵盤和滑鼠活動
  - 可自訂閒置閾值時間（10-300秒，透過全域設定調整）
  - UI 顯示當前使用者狀態（活躍中/閒置中）

- 🎵 **自訂音樂功能**
  - 支援上傳自訂鬧鐘音樂（MP3, WAV, WMA, M4A）
  - 檔案大小限制：5 MB
  - 自動管理音樂資料夾（`%AppData%\SmartAlarmClock\Music`）
  - 可選擇複製檔案或使用原始路徑
  - 未設定時使用系統預設音效

- ⚙️ **全域設定**
  - 可自訂使用者活躍判斷時間（10-300秒）
  - 可指定音樂資料夾路徑
  - 設定即時生效並自動儲存

- 💾 **JSON 設定持久化**
  - 自動儲存所有鬧鐘設定到 JSON 檔案
  - 應用程式重啟後自動載入設定
  - 設定檔位置：`%AppData%\SmartAlarmClock\alarm_config.json`

### UI 特色
- 🎨 現代化深色主題介面
- ⏱️ 大型時鐘顯示當前時間
- 📊 即時狀態更新
- 🔴 鬧鐘響起時顯示紅色警示
- 📱 直覺的編輯對話框
- 🎚️ 滑桿調整響鈴時間

## 專案結構

```
AlarmClock/
├── AlarmService.cs           # 鬧鐘核心邏輯
├── AlarmConfig.cs            # JSON 設定管理
├── MusicManager.cs           # 音樂檔案管理
├── AlarmEditDialog.xaml      # 編輯對話框 UI
├── AlarmEditDialog.xaml.cs   # 編輯對話框邏輯
├── SettingsDialog.xaml       # 全域設定對話框 UI
├── SettingsDialog.xaml.cs    # 全域設定對話框邏輯
├── MainWindow.xaml           # 主視窗 UI
├── MainWindow.xaml.cs        # 主視窗邏輯
├── App.xaml                  # 應用程式資源
└── AlarmClock.csproj         # 專案設定檔
```

## 技術架構

### AlarmConfig.cs
負責 JSON 設定檔的讀寫：
- 儲存路徑：`%AppData%\SmartAlarmClock\alarm_config.json`
- 自動建立預設設定
- 支援中文編碼

### AlarmService.cs
主要包含三個類別：

1. **AlarmService**
   - 管理所有鬧鐘（新增、編輯、刪除、切換）
   - 每秒檢查是否該觸發鬧鐘
   - 處理響鈴邏輯和自動關閉
   - 支援永不停止模式（MaxRingingDurationMinutes = 0）
   - 自動儲存設定到 JSON

2. **AlarmItem**
   - 鬧鐘資料模型
   - 儲存 ID、時間、名稱、狀態、響鈴時間、音樂路徑等

3. **UserActivityDetector**
   - 使用 Windows API (GetLastInputInfo)
   - 檢測鍵盤/滑鼠最後活動時間
   - 可自訂閒置閾值（透過全域設定）

### MusicManager.cs
音樂檔案管理類別：
- 驗證音樂檔案格式和大小
- 複製音樂檔案到應用程式資料夾
- 管理音樂資料夾路徑
- 支援格式：MP3, WAV, WMA, M4A
- 檔案大小限制：5 MB

### JSON 設定檔格式

```json
{
  "Alarms": [
    {
      "Id": "uuid-here",
      "Hour": 23,
      "Minute": 0,
      "Name": "晚上 11 點提醒",
      "IsEnabled": true,
      "customRingingDurationSeconds": 5,
      "maxRingingDurationMinutes": 10,
      "musicFilePath": ""
    },
    {
      "Id": "uuid-here-2",
      "Hour": 7,
      "Minute": 30,
      "Name": "早晨起床",
      "IsEnabled": true,
      "customRingingDurationSeconds": 10,
      "maxRingingDurationMinutes": 0,
      "musicFilePath": "C:\\Users\\...\\Music\\morning.mp3"
    }
  ],
  "defaultRingingDurationSeconds": 5,
  "maxRingingDurationMinutes": 10,
  "idleThresholdSeconds": 30,
  "musicFolderPath": ""
}
```

**說明**：
- `maxRingingDurationMinutes`: 0 = 永不停止，需手動關閉
- `musicFilePath`: 空字串 = 使用系統預設音效
- `idleThresholdSeconds`: 可透過全域設定調整（10-300秒）

### 核心邏輯流程

```
開始
  ↓
載入 JSON 設定
  ↓
每秒檢查時間
  ↓
時間匹配? ─No→ 繼續檢查
  ↓ Yes
觸發鬧鐘
  ↓
播放音效
  ↓
檢測使用者活動
  ↓
有活動? ─Yes→ 等待自訂秒數 → 自動關閉
  ↓ No
持續響鈴 (最多設定的分鐘數)
  ↓
儲存變更到 JSON
```

## 如何執行

### 方法 1：使用 Visual Studio
1. 開啟 `AlarmClock.sln` 或 `AlarmClock.csproj`
2. 按 F5 執行

### 方法 2：使用命令列
```bash
cd AlarmClock
dotnet run
```

### 方法 3：執行建置後的檔案
```bash
cd AlarmClock
dotnet build
./bin/Debug/net9.0-windows/AlarmClock.exe
```

## 使用說明

### 新增鬧鐘
1. 點擊「➕ 新增鬧鐘」按鈕
2. 輸入鬧鐘名稱
3. 選擇時間（小時和分鐘）
4. 拖動「有人使用時響鈴秒數」滑桿（1-60秒）
5. 拖動「無人使用時持續響鈴時間」滑桿（0-30分鐘）
   - 設為 0 = ♾️ 永不停止，直到手動關閉
   - 設為其他值 = 響鈴該分鐘數後自動停止
6. （選填）點擊「瀏覽...」上傳自訂音樂檔案
   - 支援格式：MP3, WAV, WMA, M4A
   - 檔案大小限制：5 MB
   - 可選擇複製到程式資料夾或使用原始路徑
7. 勾選「啟用此鬧鐘」
8. 點擊「儲存」

### 編輯鬧鐘
1. 點擊鬧鐘卡片右側的 ✏️ 按鈕
2. 修改設定
3. 點擊「儲存」

### 全域設定
1. 點擊「⚙️ 設定」按鈕
2. 調整「使用者活躍判斷時間」（10-300秒）
   - 此設定決定多久沒有鍵盤/滑鼠活動算作閒置
3. （選填）設定「音樂資料夾路徑」
   - 可指定自訂的音樂檔案存放位置
4. 點擊「儲存」

### 快速操作
- 🔔/🔕 按鈕：快速啟用/停用鬧鐘
- 🗑️ 按鈕：刪除鬧鐘（會有確認提示）
- 響鈴時點擊「關閉」：立即停止響鈴

## 設定檔位置

設定檔自動儲存在：
```
C:\Users\{你的使用者名稱}\AppData\Roaming\SmartAlarmClock\alarm_config.json
```

你可以直接編輯此 JSON 檔案來批次修改設定。

## 技術細節

### 使用者活動檢測邏輯
```csharp
// 透過 Windows API 取得最後輸入時間
GetLastInputInfo(ref lastInput);
var idleTime = Environment.TickCount - lastInput.dwTime;

// 閾值時間內有活動 = 使用中（可自訂）
return idleTime < (thresholdSeconds * 1000);
```

### 智慧關閉邏輯
```csharp
bool isUserActive = UserActivityDetector.IsUserActive(config.IdleThresholdSeconds);
var ringingDuration = DateTime.Now - alarm.RingingStartTime;

// 有人用：自訂秒數後自動關
if (isUserActive && ringingDuration.TotalSeconds >= alarm.CustomRingingDurationSeconds)
    StopAlarm();

// 沒人用，且設定了時限（不為0）：持續響到時限
else if (!isUserActive && alarm.MaxRingingDurationMinutes > 0
        && ringingDuration.TotalMinutes >= alarm.MaxRingingDurationMinutes)
    StopAlarm();

// 沒人用，且 MaxRingingDurationMinutes = 0：永不自動停止
```

### 音樂播放邏輯
```csharp
private void PlayAlarmSound(AlarmItem alarm)
{
    // 如果有自訂音樂檔案且存在，使用自訂音樂
    if (!string.IsNullOrEmpty(alarm.MusicFilePath) && File.Exists(alarm.MusicFilePath))
    {
        _alarmSound.SoundLocation = alarm.MusicFilePath;
        _alarmSound.PlayLooping();
    }
    else
    {
        // 使用系統預設音效
        SetSystemBeep();
        _alarmSound.PlayLooping();
    }
}
```

### JSON 自動儲存
每次新增、編輯、刪除、切換鬧鐘時，都會自動儲存到 JSON：
```csharp
private void SaveToConfig()
{
    _config.Alarms.Clear();
    foreach (var alarm in _alarms)
    {
        _config.Alarms.Add(new AlarmConfigItem { ... });
    }
    _config.Save();  // 自動序列化並寫入檔案
}
```

## 系統需求

- Windows 10/11
- .NET 9.0 SDK
- 支援 WPF 的系統

## 已實現功能 ✅

- ✅ 支援新增/編輯/刪除鬧鐘的 UI
- ✅ 鬧鐘啟用/停用切換
- ✅ JSON 設定持久化
- ✅ 自訂響鈴持續時間（有人使用時 1-60秒）
- ✅ 自訂最大響鈴時間（無人使用時 0-30分鐘）
- ✅ 永不停止模式（設為 0 分鐘）
- ✅ 自訂音樂檔案上傳功能
- ✅ 音樂檔案管理（複製/驗證/格式限制）
- ✅ 全域設定對話框
- ✅ 可自訂使用者活躍判斷時間（10-300秒）
- ✅ 可自訂音樂資料夾路徑
- ✅ 浮動的編輯介面
- ✅ 即時狀態更新
- ✅ 大型清晰的時間選擇器（32px 粗體）

## 未來改進方向

- [ ] 週期性鬧鐘（每週特定日子）
- [ ] 貪睡功能（Snooze）
- [ ] 系統托盤最小化
- [ ] 鬧鐘歷史記錄
- [ ] 匯出/匯入設定
- [ ] 更多音效選項（淡入效果、音量控制）
- [ ] 深色/淺色主題切換

## Vibe Coding 學習重點

這個專案展示了：
1. **WPF 基礎**：XAML 設計與 C# 邏輯分離
2. **對話框視窗**：子視窗的建立和資料傳遞（AlarmEditDialog、SettingsDialog）
3. **計時器應用**：DispatcherTimer 的使用
4. **Windows API 整合**：P/Invoke 呼叫原生 API（GetLastInputInfo）
5. **事件驅動架構**：Event/EventHandler 模式
6. **UI 動態更新**：即時反映狀態變化
7. **JSON 序列化**：System.Text.Json 的使用
8. **檔案系統操作**：AppData 資料夾的使用、檔案驗證、複製
9. **現代化設計**：深色主題與友善介面、大型易讀的 UI 元素
10. **CRUD 操作**：完整的增刪改查實作
11. **檔案管理**：音樂檔案的驗證、大小限制、格式檢查
12. **使用者體驗設計**：滑桿、視覺回饋（永不停止的無限符號）、即時預覽
13. **全域設定管理**：獨立的設定對話框與設定持久化

## 授權

MIT License
