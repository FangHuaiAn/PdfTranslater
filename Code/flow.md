# PDF 翻譯 Console App 工作流程

1. **啟動與參數解析**
   - 使用者在 `Code/PdfTranslater.Console` 執行 `dotnet run -- --input <英文 PDF> --output <中文檔案> --from en-US --to zh-TW [--force]`。
   - `CliOptionsParser` 檢查所需參數、計算絕對路徑並處理 `--force` 標記。
   - 若不提供 `--input`，CLI 會依據 [config/test-resources.json](../config/test-resources.json#L1-L3) 中的預設測試資料夾自動選擇 `Spec/test-pdfs` 內排序最前的 PDF 做翻譯，方便開發與自動化測試。
2. **環境與翻譯提供者決定**
   - `EnvironmentConfig` 從環境變數讀取 `OPENAI_API_KEY`（可選 `OPENAI_MODEL`, `OPENAI_API_BASE`）以及 Azure 翻譯金鑰等設定。
   - 只要 OpenAI 金鑰存在，就會使用 `OpenAITranslationProvider`；若缺少 OpenAI 設定但 Azure 金鑰齊全，則改以 `AzureTranslatorClient`；兩者皆缺則回退到 `MockTranslationProvider`，讓 CLI 仍能回傳原文。
   - 測試 PDF 檔案打算放在 Spec/test-pdfs 目錄，這個資料夾由 [config/test-resources.json](config/test-resources.json#L1-L2) 控制，確保測試資源與程式碼分離。
3. **PDF 文字擷取**
   - `PdfTextExtractor` 用 `PdfPig` 逐頁讀取，記錄頁碼與純文字，空白頁會發出警示。
4. **分批翻譯**
   - `BatchTranslator` 以段落打散文字，每筆 chunk 不超過 4500 個字元，分批呼叫翻譯 provider。
   - 依據實際翻譯 provider 的 JSON 回應（OpenAI 或 Azure），收集所有翻譯段落，將結果依頁碼順序累積。
5. **結果產出**
   - `PdfTranslationApp` 依照輸出副檔名（Markdown/純文字）組裝每頁區塊，並交給 `TranslationResultWriter` 寫檔。
   - `TranslationResultWriter` 會在必要時建立目錄，`--force` 控制是否覆寫既有檔案。
   - 翻譯結果與原始 PDF 會被存到 `OutputDicuments/<source-file-name>/`，並在每頁都輸出 JSON 句對句中英文區塊，方便快速校對。
6. **結束與回報**
   - 程式在完成後輸出成功資訊，並以不同 exit code 表示 CLI 錯誤、取消或未預期例外。
