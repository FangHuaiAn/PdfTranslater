## 商業流程（Business Process）
1. **PDF 蒐集**：業務/法遵單位提供英文 PDF，經由檔案門戶或 Git LFS 上傳至翻譯暫存區。
2. **翻譯啟動**：文件負責人在本機或 CI Pipe 觸發 console app，指定來源 PDF 與輸出目標（Markdown 或純文字）。CLI 也可以不傳 `--input`，此時會從 [config/test-resources.json](config/test-resources.json#L1-L3) 所列的 `Spec/test-pdfs` 目錄自動挑第一個 PDF 做測試。
3. **審閱校稿**：專案成員在輸出檔（逐頁保留）進行雙語校稿，必要時重跑個別頁面。
4. **發佈與歸檔**：校稿完成後，翻譯結果連同校稿記錄送交 ECM/Knowledge Base，並於 Jira 任務結案。
5. **監控與優化**：依月收集 API 使用量、翻譯品質回饋，調整配額與 Prompt Template。

## 系統架構概覽
- **Console 層**：`PdfTranslater.Console` 專案提供 CLI、參數驗證與整體流程管理。
- **Service 層**：`PdfTextExtractor`（PDF 拆頁）、`BatchTranslator`（chunk 管理）、`TranslationResultWriter`（產出檔案與 metadata）。
- **Integration 層**：`OpenAITranslationProvider` 優先呼叫 OpenAI Responses API（需 `OPENAI_API_KEY`，可選 `OPENAI_API_BASE`、`OPENAI_MODEL`）。當 OpenAI 金鑰缺失但設定了 Azure 環境變數時會退回 `AzureTranslatorClient`，否則由 `MockTranslationProvider` 提供原文回應。
- **設定與機密**：使用環境變數或 `dotnet user-secrets` 提供 `OPENAI_API_KEY`（可選 `OPENAI_API_BASE`、`OPENAI_MODEL`）或 `AZURE_TRANSLATOR_KEY`, `AZURE_TRANSLATOR_REGION`, `AZURE_TRANSLATOR_ENDPOINT`；log 內自動遮罩。
- **Test assets**：Sample PDF 測試檔放在 Spec/test-pdfs 目錄，由 [config/test-resources.json](config/test-resources.json#L1-L2) 管理其位置，這樣 test 資料能保持與程式碼分離且可以輕鬆調整。
- **輸出格式**：每次翻譯會在 `OutputDicuments/<source-file-name>/` 建立子資料夾，裡面保留 `translated.md` 或指定輸出檔案，並同時備份原始 PDF；所有頁面都會顯示句對句的英中區塊（OpenAI 以 JSON array 輸出句子對），方便校對。

## 部署與營運
- **Runtime**：鎖定 .NET 10，於 macOS/Linux/Windows CLI 上皆可執行。
- **套件管理**：採 `dotnet restore` 拉取 NuGet 相依（PdfPig、System.CommandLine 等）。
- **CI/CD**：推薦於 Azure DevOps 或 GitHub Actions 建立 job，自動針對新 PDF 進行翻譯並上傳結果 artifact。
- **監控**：CLI 以結構化 log（JSON 行）輸出，利於集中式 log 收集；錯誤碼非 0 時觸發告警。

更多互動式討論與需求追蹤請參見 [Spec/pdf-translater-console.md](Spec/pdf-translater-console.md)。
