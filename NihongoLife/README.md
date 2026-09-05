# 🌸 Nihongo Life: N5 Japanese Daily Life Simulator

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[English](#english) | [日本語 (Japanese)](#日本語) | [Tiếng Việt (Vietnamese)](#tiếng-việt)

---

<a name="english"></a>
## 🇬🇧 English

Nihongo Life is a production-ready, data-driven 3D simulator designed for the N5 Japanese E-learning context. It places learners in realistic Japanese environments (convenience stores, restaurants, transit hubs) where they must apply vocabulary and grammar to solve everyday tasks.

Designed as a modular, extensible engine, Nihongo Life separates gameplay logic from lesson content. It is ready for WebGL builds and direct future integration with Next.js web LMS and Supabase backend databases.

### Key Features
- **Data-Driven Scenario Engine**: Entire lessons are authored via ScriptableObjects (`ScenarioDefinition`), handling objectives, trigger actions, and dialog structures with zero custom coding.
- **Dynamic Dialogue & Hint Controls**: Branching conversations support Vietnamese/English localizations, romaji, and reading/furigana guides, selectively shown or hidden depending on target learning modes.
- **Service Locator Architecture**: Persistent systems (audio, saves, scenes) are accessed through interfaces, allowing local implementations to be seamlessly swapped with web APIs later.
- **Robust Third-Person Controller**: Clean player physics, walk/run variables, look-at orbit camera, and collision raycasting.
- **Metric-Based E-Learning Assessment**: Evaluates learner accuracy across Vocabulary, Grammar, Listening, Response Accuracy, and Task Completion.

### Getting Started
1. Open this project folder inside Unity Editor version **6000.3.12f1**.
2. Run from the scene `Assets/NihongoLife/Scenes/00_Bootstrap` to start initialization.
3. Open **Window -> General -> Test Runner** to execute NUnit tests verifying the scenario runtime engine.

---

<a name="日本語"></a>
## 🇯🇵 日本語

**Nihongo Life** は、N5レベルの日本語学習向けに設計された、本番環境対応のデータ駆動型3Dシミュレーターです。コンビニエンスストア、レストラン、交通機関など、リアルな日本の環境に学習者を配置し、日常の課題を解決するために語彙と文法を応用させます。

モジュール式で拡張可能なエンジンとして設計されており、ゲームプレイのロジックとレッスンのコンテンツを分離しています。WebGLビルドや、将来的なNext.js Web LMSおよびSupabaseバックエンドデータベースとの直接統合にも対応可能です。

### 主な機能
- **データ駆動型シナリオエンジン**: レッスン全体はScriptableObject（`ScenarioDefinition`）を介して作成され、カスタムコーディングなしで目標、トリガーアクション、対話構造を処理します。
- **動的な対話とヒントの制御**: 分岐する会話は、ベトナム語/英語のローカリゼーション、ローマ字、ふりがなガイドをサポートし、学習モードに応じて表示/非表示を選択できます。
- **サービスロケーターアーキテクチャ**: 永続的なシステム（オーディオ、セーブ、シーン）にはインターフェースを介してアクセスするため、後でローカルの実装をWeb APIとシームレスに交換できます。
- **Eラーニング評価**: 語彙、文法、リスニング、応答の正確さ、タスク完了率にわたって学習者の正確さを評価します。

### はじめに
1. Unityエディター バージョン **6000.3.12f1** でこのプロジェクトフォルダーを開きます。
2. 初期化を開始するには、シーン `Assets/NihongoLife/Scenes/00_Bootstrap` から実行します。
3. **Window -> General -> Test Runner** を開いて、NUnitテストを実行します。

---

<a name="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

**Nihongo Life** là một trình mô phỏng 3D theo hướng dữ liệu (data-driven) được thiết kế sẵn sàng cho môi trường học tiếng Nhật N5. Ứng dụng đưa người học vào các bối cảnh thực tế ở Nhật Bản (cửa hàng tiện lợi, nhà hàng, trạm trung chuyển), nơi họ phải áp dụng từ vựng và ngữ pháp để giải quyết các tình huống hàng ngày.

Được thiết kế với kiến trúc engine dạng mô-đun, dễ mở rộng, Nihongo Life tách biệt logic game khỏi nội dung bài học. Dự án sẵn sàng để build WebGL và tích hợp trực tiếp với hệ thống LMS web Next.js cùng cơ sở dữ liệu Supabase trong tương lai.

### Các Tính Năng Chính
- **Engine Kịch Bản Theo Dữ Liệu**: Toàn bộ bài học được tạo thông qua ScriptableObjects (`ScenarioDefinition`), quản lý mục tiêu, hành động và cấu trúc hội thoại mà không cần code thêm.
- **Hội Thoại Động & Gợi Ý**: Các nhánh hội thoại hỗ trợ bản địa hóa Tiếng Việt/Tiếng Anh, romaji, và furigana, có thể bật/tắt tùy theo chế độ học (Hướng dẫn, Thực hành, Kiểm tra).
- **Kiến Trúc Service Locator**: Các hệ thống cốt lõi (âm thanh, lưu trữ, scene) được truy cập qua interface, cho phép dễ dàng chuyển đổi sang Web API sau này.
- **Điều Khiển Nhân Vật Chân Thực**: Vật lý nhân vật mượt mà, camera xoay, và hệ thống raycasting va chạm.
- **Đánh Giá E-Learning**: Đánh giá độ chính xác của người học qua Từ vựng, Ngữ pháp, Nghe hiểu, Phản xạ và Mức độ Hoàn thành nhiệm vụ.

### Hướng Dẫn Cài Đặt
1. Mở thư mục dự án này bằng Unity Editor phiên bản **6000.3.12f1**.
2. Chạy từ scene `Assets/NihongoLife/Scenes/00_Bootstrap` để bắt đầu.
3. Mở **Window -> General -> Test Runner** để chạy các bài test NUnit.

---
*For full documentation, please check the `Docs/` directory.*
*詳細なドキュメントについては、`Docs/`ディレクトリをご確認ください。*
*Để xem tài liệu chi tiết, vui lòng tham khảo thư mục `Docs/`.*
