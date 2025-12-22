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
   - 預設整合 Azure AI Translator（需 `AZURE_TRANSLATOR_KEY`, `AZURE_TRANSLATOR_REGION`, `AZURE_TRANSLATOR_ENDPOINT`）。
   - 若未設定金鑰，需 fallback 到 mock 翻譯（回傳原文並提示）。
   - 支援 chunk 分批呼叫，單次 request 不超過 4,500 字元。
4. 輸出
   - 產出 UTF-8 markdown (`.md`) 或純文字 (`.txt`)，以 page header 分隔。
   - 檔案不存在時自動建立；存在時需 `--force` 才覆蓋。
5. 錯誤處理
   - 無法讀檔、API 回傳非 2xx、網路問題都需具體訊息與非 0 return code。

## 非功能需求
- .NET 8 LTS。
- 單檔處理時間 < 2 分鐘（以 20 頁 PDF 為估算，含雲端翻譯）。
- Logs 須隱藏 API Key。
- 程式碼可單元測試，關鍵邏輯抽離至 service class。

## 作業流程
1. 使用者在工作站放入英文 PDF。
2. 於 CLI 下達 `dotnet run -- --input sample.pdf --output sample.zh.md --from en-US --to zh-TW`。
3. App 讀取 PDF → 逐頁切割 → 對每頁進行 chunk 翻譯 → 合併 → 輸出成 markdown。
4. 審閱人員於輸出檔進行校稿後，交付正式發佈。

## 系統架構構想
- Console 層：參數解析、排程流程。
- Service 層：`PdfTextExtractor`, `BatchTranslator`, `TranslationResultWriter`。
- Infra 層：`AzureTranslatorClient`, `FallbackTranslationProvider`, `EnvironmentConfig`。
- 透過 DI/Factory 選擇翻譯 provider，可擴充至 OpenAI/DeepL。

## 未決議題
- 翻譯 API quota 與成本控管方式。
- 是否需要保留原文與譯文的對照表格格式。
- 日後是否要支援圖片 OCR 或表格重建。
