# PDF 翻譯 Console App 工作流程

1. **啟動與參數解析**
   - 使用者在 `Code/PdfTranslater.Console` 執行 `dotnet run -- --input <英文 PDF> --output <中文檔案> --from en-US --to zh-TW [--force]`。
   - `CliOptionsParser` 檢查所需參數、計算絕對路徑並處理 `--force` 標記。
2. **環境與翻譯提供者決定**
   - `EnvironmentConfig` 從環境變數讀取 Azure 翻譯金鑰等設定。
   - 若金鑰齊全，實例化 `AzureTranslatorClient`；否則使用 `MockTranslationProvider`，讓 CLI 仍能回傳原文。
3. **PDF 文字擷取**
   - `PdfTextExtractor` 用 `PdfPig` 逐頁讀取，記錄頁碼與純文字，空白頁會發出警示。
4. **分批翻譯**
   - `BatchTranslator` 以段落打散文字，每筆 chunk 不超過 4500 個字元，分批呼叫翻譯 provider。
   - Azure 回應的 JSON 被解析，收集所有翻譯段落，將結果依頁碼順序累積。
5. **結果產出**
   - `PdfTranslationApp` 依照輸出副檔名（Markdown/純文字）組裝每頁區塊，並交給 `TranslationResultWriter` 寫檔。
   - `TranslationResultWriter` 會在必要時建立目錄，`--force` 控制是否覆寫既有檔案。
6. **結束與回報**
   - 程式在完成後輸出成功資訊，並以不同 exit code 表示 CLI 錯誤、取消或未預期例外。
