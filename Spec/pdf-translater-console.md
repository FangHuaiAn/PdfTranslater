# PDF 翻譯 Console App 討論紀錄

## 背景與目標
- 針對只能取得英文 PDF 的跨部門文件，提供自動化中文翻譯流程。
- 以 .NET C# console app 形式部署，方便於本機或 CI Job 呼叫。
- 需保留逐頁內容，方便人工核對與校稿。

## 功能需求
1. CLI 介面
   - 參數：`--input`, `--output`, `--from`, `--to`, `--force`。
   - 顯示進度與錯誤訊息，複製到 CI log 仍能閱讀。
2. PDF 文字擷取
   - 使用 `UglyToad.PdfPig` 逐頁抽取文字，保留基本段落順序。
   - 對空白頁或無文字頁須有明確提示。
3. 翻譯引擎
   - 以 OpenAI API 為主要翻譯引擎，需提供 `OPENAI_API_KEY`（可選 `OPENAI_API_BASE`, `OPENAI_MODEL` 來覆寫基底路徑或模型）。
   - 當 OpenAI 金鑰缺失但 Azure 設定齊全時，會退回 Azure AI Translator；兩者皆未配置時，使用 mock 翻譯（回傳原文並提示）。
   - 支援 chunk 分批呼叫，單次 request 不超過 4,500 字元。
4. 輸出
   - 產出 UTF-8 markdown (`.md`) 或純文字 (`.txt`)，以 page header 分隔。
   - 檔案不存在時自動建立；存在時需 `--force` 才覆蓋。
   - 每次翻譯結果會寫到 `OutputDocuments/<source-file-name>/`，翻譯檔以 `<source-base>-中文` 命名並保留副檔名；同一資料夾也會存放原始 PDF，且每頁仍展示 OpenAI 回傳的 JSON 句對句清單，方便比對。
5. 錯誤處理
   - 無法讀檔、API 回傳非 2xx、網路問題都需具體訊息與非 0 return code。

## 非功能需求
- .NET 10。
- 單檔處理時間 < 2 分鐘（以 20 頁 PDF 為估算，含雲端翻譯）。
- Logs 須隱藏 API Key。
- 程式碼可單元測試，關鍵邏輯抽離至 service class。

## 作業流程
1. 使用者在工作站放入英文 PDF。
2. 於 CLI 下達 `dotnet run -- --input sample.pdf --output sample.zh.md --from en-US --to zh-TW`。
    - 如不提供 `--input`，CLI 會自動使用 [config/test-resources.json](../config/test-resources.json#L1-L3) 所指向的 `Spec/test-pdfs` 目錄中的第一筆 PDF 進行測試。
3. App 讀取 PDF → 逐頁切割 → 對每頁進行 chunk 翻譯 → 合併 → 輸出成 markdown。
4. 審閱人員於輸出檔進行校稿後，交付正式發佈。

## 測試資源
   - Sample PDF 置於 Spec/test-pdfs，並透過 [config/test-resources.json](config/test-resources.json#L1-L2) 控制其實體位置，讓測試資料與執行檔分隔，方便對應不同環境或測試資料集。

## 系統架構構想
- Console 層：參數解析、排程流程。
- Service 層：`PdfTextExtractor`, `BatchTranslator`, `TranslationResultWriter`。
- Infra 層：`AzureTranslatorClient`, `FallbackTranslationProvider`, `EnvironmentConfig`。
- 透過 DI/Factory 選擇翻譯 provider，可擴充至 OpenAI/DeepL。

## 未決議題
- 翻譯 API quota 與成本控管方式。
- 是否需要保留原文與譯文的對照表格格式。
- 日後是否要支援圖片 OCR 或表格重建。
