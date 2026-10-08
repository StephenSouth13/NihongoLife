import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

def build_complete_timeline():
    wb = openpyxl.Workbook()

    # Dynamic styling setup
    font_family = "Segoe UI"
    
    # ----------------------------------------------------
    # SHEET 1: Master Timeline (Tiến độ & Deadline Chi Tiết Core Source & Game)
    # ----------------------------------------------------
    ws1 = wb.active
    ws1.title = "Master Timeline Source & Game"
    ws1.views.sheetView[0].showGridLines = True

    # Main Title Header
    ws1.merge_cells("A1:M2")
    t_cell = ws1["A1"]
    t_cell.value = "BẢNG TIẾN ĐỘ THI CÔNG SOURCE CODE & GAME 3D METAVERSE - NIHONGOLIFE"
    t_cell.font = Font(name=font_family, size=15, bold=True, color="FFFFFF")
    t_cell.fill = PatternFill(start_color="0F172A", end_color="0F172A", fill_type="solid") # Dark Navy
    t_cell.alignment = Alignment(horizontal="center", vertical="center")

    # Subtitle Header
    ws1.merge_cells("A3:M3")
    sub_cell = ws1["A3"]
    sub_cell.value = "Solo Developer: Quách Thành Long | Dự án: NihongoLife (Unity 6 URP + Supabase Backend + JLPT/IELTS Engine) | Timeline: 01/06/2026 - 15/10/2026"
    sub_cell.font = Font(name=font_family, size=10, italic=True, color="334155")
    sub_cell.fill = PatternFill(start_color="F1F5F9", end_color="F1F5F9", fill_type="solid")
    sub_cell.alignment = Alignment(horizontal="center", vertical="center")

    headers = [
        "STT", "Mã Module / Script", "Giai Đoạn Phát Triển", "Đầu Công Việc Kỹ Thuật (Task Name)",
        "Tệp Source / Class / Scene Chính", "Độ Ưu Tiên", "Ngày Bắt Đầu", "Deadline (Hạn Chót)",
        "Số Ngày", "Trạng Thái", "Hoàn Thành (%)", "Đánh Giá", "Mô Tả Kỹ Thuật Chi Tiết / Chi Tiết Thực Hiện"
    ]

    header_fill = PatternFill(start_color="1E293B", end_color="1E293B", fill_type="solid")
    header_font = Font(name=font_family, size=10, bold=True, color="FFFFFF")
    thin_border = Border(
        left=Side(style='thin', color='CBD5E1'),
        right=Side(style='thin', color='CBD5E1'),
        top=Side(style='thin', color='CBD5E1'),
        bottom=Side(style='thin', color='CBD5E1')
    )

    ws1.row_dimensions[5].height = 28

    for c_idx, h in enumerate(headers, 1):
        cell = ws1.cell(row=5, column=c_idx, value=h)
        cell.font = header_font
        cell.fill = header_fill
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
        cell.border = thin_border

    # 40 Full Source & Game Engineering Tasks
    all_tasks = [
        # === PHASE 1: CORE ARCHITECTURE & ENGINE SETUP (01/06 - 15/06) ===
        (1, "CORE-001", "Phase 1: Architecture & Boot", "Khởi tạo Unity Project 6000.3.12f1 URP Pipeline", "ProjectSettings, Packages, Asset Folder", "Rất cao", "2026-06-01", "2026-06-03", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Cấu hình URP Asset, Graphic Settings, Assembly Definitions (NihongoLife.asmdef)"),
        (2, "CORE-002", "Phase 1: Architecture & Boot", "Xây dựng Singleton GameManager & Service Locator", "AppRoot.cs, GameServices.cs", "Rất cao", "2026-06-04", "2026-06-07", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Tạo kiến trúc dependency injection, quản lý vòng đời ứng dụng và khởi tạo dịch vụ hệ thống"),
        (3, "CORE-003", "Phase 1: Architecture & Boot", "Phát triển SceneFlowController & Bootloader", "SceneFlowController.cs, 00_Bootstrap.unity", "Cao", "2026-06-08", "2026-06-11", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Xử lý chuyển cảnh async với loading bar, Fade transition effect và unload unused assets"),
        (4, "CORE-004", "Phase 1: Architecture & Boot", "Tối ưu hóa Bypass MainMenu Boot Direct Sandbox", "StandaloneZoneBootstrap.cs, 90_TestSandbox", "Cao", "2026-06-12", "2026-06-15", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Tải trực tiếp môi trường test sandbox giúp tăng tốc độ kiểm thử tính năng 500%"),

        # === PHASE 2: CLOUD PERSISTENCE & DATA PIPELINE (16/06 - 30/06) ===
        (5, "DATA-001", "Phase 2: Cloud & Data Engine", "Viết REST Client tích hợp Supabase Cloud Backend", "SupabaseClient.cs, SupabaseConfig.cs", "Rất cao", "2026-06-16", "2026-06-19", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Kết nối Supabase REST API, Auth JWT token, Leaderboard table sync và HTTPS SSL security"),
        (6, "DATA-002", "Phase 2: Cloud & Data Engine", "Xây dựng Local & Cloud Progress Persistence", "LocalProgressRepository.cs, SaveData.cs", "Rất cao", "2026-06-20", "2026-06-24", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Triển khai mẫu IProgressRepository, JSON Serialization local fallback khi offline và sync cloud"),
        (7, "DATA-003", "Phase 2: Cloud & Data Engine", "Thiết kế ScriptableObject Data-Driven Framework", "ScenarioDataSO.cs, QuestDataSO.cs", "Cao", "2026-06-25", "2026-06-27", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Đóng gói dữ liệu bài học, nhiệm vụ, phần thưởng thành các asset ScriptableObject dễ mở rộng"),
        (8, "DATA-004", "Phase 2: Cloud & Data Engine", "Phát triển User Profile & Player Stats Engine", "PlayerProfileManager.cs, UserStats.cs", "Trung bình", "2026-06-28", "2026-06-30", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Quản lý level, điểm JLPT/IELTS mastery, streak học tập và tiền tệ mua sắm"),

        # === PHASE 3: PLAYER CONTROLLER & INTERACTION SYSTEM (01/07 - 15/07) ===
        (9, "GAME-001", "Phase 3: Controller & Interaction", "Phát triển Player Movement & NavMesh Click-to-Move", "PlayerController.cs, NavMeshAgent", "Rất cao", "2026-07-01", "2026-07-04", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Điều khiển di chuyển 3D bằng Click mouse / Touch, xử lý pathfinding và Smooth rotation"),
        (10, "GAME-002", "Phase 3: Controller & Interaction", "Viết 3D Camera Follow & Smooth Zoom Engine", "CameraController.cs, Cinemachine Virtual Camera", "Cao", "2026-07-05", "2026-07-08", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Camera góc nhìn thứ 3 linh hoạt, tự động né vật cản địa hình và pinch-to-zoom"),
        (11, "GAME-003", "Phase 3: Controller & Interaction", "Phát triển Hệ Thống Tương Tác 3D (Interactive Engine)", "IInteractable.cs, InteractionDetector.cs", "Rất cao", "2026-07-09", "2026-07-12", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Raycast / Trigger zone phát hiện NPC, cửa hàng, sách học tập và hiển thị UI Proximity Prompt"),
        (12, "GAME-004", "Phase 4: Controller & Interaction", "Xây dựng NPC Controller & Dialogue System", "NPCController.cs, DialogueManager.cs", "Cao", "2026-07-13", "2026-07-15", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Hệ thống hội thoại Kaiwa tiếng Nhật, hiệu ứng gõ chữ Typewriter, voice clip phát âm"),

        # === PHASE 4: 3D ENVIRONMENT & SCENE BUILDING (16/07 - 05/08) ===
        (13, "WORLD-001", "Phase 4: 3D Environment & Scenes", "Thiết kế & Authoring Scene 20_StationDistrict (Ga Tàu)", "20_StationDistrict.unity, Environment Prefabs", "Cao", "2026-07-16", "2026-07-20", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Dựng khu vực nhà ga đô thị Nhật Bản, bake NavMesh, cấu hình Occlusion Culling và Lighting"),
        (14, "WORLD-002", "Phase 4: 3D Environment & Scenes", "Thiết kế & Authoring Scene 30_SushiRestaurant (Nhà Hàng)", "30_SushiRestaurant.unity, Styloo Assets", "Trung bình", "2026-07-21", "2026-07-25", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Dựng không gian nhà hàng Sushi, NPC phục vụ Kaiwa gọi món bằng tiếng Nhật"),
        (15, "WORLD-003", "Phase 4: 3D Environment & Scenes", "Lập trình Tự Động Hóa Scene Builder Script", "GameplayZoneSceneBuilder.cs", "Rất cao", "2026-07-26", "2026-07-30", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Tự động hóa build scene `50_LearningCenter` và `40_ShoppingDistrict` từ Editor script"),
        (16, "WORLD-004", "Phase 4: 3D Environment & Scenes", "Tích hợp Visual Environment & URP Shaders", "VisualEnvironmentBuilder.cs, Custom URP Shaders", "Cao", "2026-07-31", "2026-08-02", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Cấu hình Post-processing Bloom, Color Grading, Ambient Occlusion chuẩn phong cách Anime/Low-poly"),
        (17, "WORLD-005", "Phase 4: 3D Environment & Scenes", "Phát triển World Location Catalog & Teleport Portal", "WorldLocationCatalog.cs, ZonePortal.cs", "Cao", "2026-08-03", "2026-08-05", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Quản lý danh mục tọa độ địa điểm, chuyển cảnh giữa các zone trơn tru và lưu vị trí spawn"),

        # === PHASE 5: CORE E-LEARNING ENGINE (JLPT & IELTS) (06/08 - 25/08) ===
        (18, "LEARN-001", "Phase 5: E-Learning Core Engine", "Xây dựng Question Models & Assessment Blueprint", "AssessmentModels.cs, QuestionBank.cs", "Rất cao", "2026-08-06", "2026-08-09", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Cấu trúc đề thi N5-N1, IELTS Reading/Listening/Speaking/Writing, hệ thống random seed câu hỏi"),
        (19, "LEARN-002", "Phase 5: E-Learning Core Engine", "Phát triển Assessment Engine Auto Grading", "AssessmentEngine.cs, ScoringService.cs", "Rất cao", "2026-08-10", "2026-08-13", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Tự động chấm điểm JLPT (N5-N1) chính xác, tính điểm IELTS Band Score scale 0 - 9.0"),
        (20, "LEARN-003", "Phase 5: E-Learning Core Engine", "Xây dựng UI Exam Center Popup & Timer Clock", "ExamCenterPopup.cs, TimedExamController.cs", "Cao", "2026-08-14", "2026-08-17", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Giao diện làm bài thi trắc nghiệm hiện đại, đồng hồ đếm ngược, chuyển câu hỏi và nộp bài"),
        (21, "LEARN-004", "Phase 5: E-Learning Core Engine", "Xây dựng Hệ Thống Review Câu Sai (Mistake Target)", "WeakTargetReviewSystem.cs, ReviewUI.cs", "Cao", "2026-08-18", "2026-08-21", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Tự động trích xuất các câu làm sai lưu vào ngân hàng ôn tập riêng giúp người học sửa lỗi"),
        (22, "LEARN-005", "Phase 5: E-Learning Core Engine", "Phát triển Audio Service & Media Catalog", "AudioService.cs, RemoteMediaCatalog.cs", "Cao", "2026-08-22", "2026-08-25", 4, "Đã hoàn thành", 1.0, "☑ Đạt", "Stream âm thanh nghe thi JLPT/IELTS từ Cloud Server tránh làm nặng file Unity Build"),

        # === PHASE 6: ADVANCED METAVERSE & TECH BREAKTHROUGH (26/08 - 10/09) ===
        (23, "META-001", "Phase 6: Advanced Metaverse", "Tích hợp Agora Native 3D Video Call Engine", "EduMeetingManager.cs, AgoraSDK integration", "Rất cao", "2026-08-26", "2026-08-30", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Kết nối camera trực tiếp giảng viên lên màn hình 3D TV trong phòng học Metaverse"),
        (24, "META-002", "Phase 6: Advanced Metaverse", "Phát triển Scene 50_LearningCenter (Trường Học 3D)", "50_LearningCenter.unity, Classroom Prefabs", "Rất cao", "2026-08-31", "2026-09-04", 5, "Đã hoàn thành", 1.0, "☑ Đạt", "Dựng không gian trường học với phòng thi JLPT, phòng hội thảo IELTS và bảng tương tác"),
        (25, "META-003", "Phase 6: Advanced Metaverse", "Phát triển Scene 40_ShoppingDistrict (Khu Mua Sắm)", "40_ShoppingDistrict.unity, ShopUI.cs", "Trung bình", "2026-09-05", "2026-09-07", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Dựng trung tâm thương mại mua trang phục nhân vật, sách học tập và vật phẩm trang trí"),
        (26, "META-004", "Phase 6: Advanced Metaverse", "Viết Shop & Inventory Economy Controller", "ShopController.cs, InventoryManager.cs", "Cao", "2026-09-08", "2026-09-10", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Hệ thống mua bán items bằng coin tích lũy từ kết quả học tập và quản lý kho đồ 3D"),

        # === PHASE 7: UI/UX & SYSTEM GRAPHICS (11/09 - 20/09) ===
        (27, "UI-001", "Phase 7: UI/UX System", "Thiết kế Main Menu & Character Selection UI", "MainMenuUI.cs, CharacterSelectUI.cs", "Cao", "2026-09-11", "2026-09-13", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Giao diện trang chủ hiện đại, chọn nhân vật nam/nữ với hiệu ứng animation mượt mà"),
        (28, "UI-002", "Phase 7: UI/UX System", "Phát triển Interactive World Map UI Navigation", "WorldMapUI.cs, MapLocationTab.cs", "Cao", "2026-09-14", "2026-09-16", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Bản đồ thế giới thu nhỏ (Minimap & World Map) hỗ trợ click dịch chuyển tức thời đến các zone"),
        (29, "UI-003", "Phase 7: UI/UX System", "Thiết kế Dashboard Analytics & Skill Radar", "LearningDashboardUI.cs, RadarChart.cs", "Cao", "2026-09-17", "2026-09-18", 2, "Đã hoàn thành", 1.0, "☑ Đạt", "Biểu đồ trực quan hóa năng lực 6 kỹ năng (Từ vựng, Ngữ pháp, Đọc, Nghe, Nói, Viết)"),
        (30, "UI-004", "Phase 7: UI/UX System", "Xây dựng HUD Overlay & Pause Settings Menu", "HUDController.cs, SettingsUI.cs", "Trung bình", "2026-09-19", "2026-09-20", 2, "Đã hoàn thành", 1.0, "☑ Đạt", "Thanh trạng thái HUD (Cấp độ, Tiền, Quest hiện tại), Menu cài đặt âm thanh, đồ họa và phím tắt"),

        # === PHASE 8: QA, AUTOMATED TESTING & OPTIMIZATION (21/09 - 30/09) ===
        (31, "QA-001", "Phase 8: QA & Automated Testing", "Viết Suite Test ScenarioPlayModeTests.cs", "ScenarioPlayModeTests.cs", "Rất cao", "2026-09-21", "2026-09-23", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Chạy automated test kiểm tra di chuyển player, portal va chạm, nhận quest và làm đề thi"),
        (32, "QA-002", "Phase 8: QA & Automated Testing", "Sửa Lỗi Collision Repair & Mesh Clean up", "RuntimeCollisionRepair.cs", "Cao", "2026-09-24", "2026-09-25", 2, "Đã hoàn thành", 1.0, "☑ Đạt", "Khắc phục lỗi dính tường, lún sàn 3D, tối ưu Mesh Colliders và Rigidbody physics"),
        (33, "QA-003", "Phase 8: QA & Automated Testing", "Khắc phục Unity Licensing & Batch Build Script", "UnityBatchBuild.cs, Editor Scripts", "Rất cao", "2026-09-26", "2026-09-28", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Xử lý lỗi kết nối Unity License Client trên Windows, tự động hóa build WebGL/PC qua dòng lệnh"),
        (34, "QA-004", "Phase 8: QA & Automated Testing", "Tối ưu hóa Performance & Draw Calls Frame Rate", "Profiler Audit, Occlusion Culling", "Cao", "2026-09-29", "2026-09-30", 2, "Đã hoàn thành", 1.0, "☑ Đạt", "Giảm Draw Calls từ 1200 xuống 350, duy trì mượt mà 60 FPS trên máy tầm trung và WebGL"),

        # === PHASE 9: DOCUMENTATION, MEDIA & FINAL DEFENSE (01/10 - 15/10) ===
        (35, "DOC-001", "Phase 9: Media & Documentation", "Quay phim & Dựng Trailer Game NihongoLife", "NihongoLife_Trailer.mp4 (Bao_Cao/)", "Cao", "2026-10-01", "2026-10-03", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Trailer 4K độ phân giải cao giới thiệu tính năng 3D Metaverse và thi JLPT/IELTS"),
        (36, "DOC-002", "Phase 9: Media & Documentation", "Biên soạn Báo Cáo Đồ Án Capstone (.docx / .pdf)", "NihongoLife_Bao_Cao_Capstone.docx", "Rất cao", "2026-10-04", "2026-10-06", 3, "Đã hoàn thành", 1.0, "☑ Đạt", "Tài liệu báo cáo kỹ thuật >100 trang phân tích chi tiết kiến trúc, UML, Database & Kết quả"),
        (37, "DOC-003", "Phase 9: Media & Documentation", "Thiết kế Slide Thuyết Trình Bảo Vệ Đồ Án", "NihongoLife_Thuyet_Trinh_moi.pptx", "Cao", "2026-10-06", "2026-10-07", 2, "Đã hoàn thành", 1.0, "☑ Đạt", "Bộ Slide 35 trang thiết kế chuẩn visual sang trọng, xúc tích đầy đủ demo sản phẩm"),
        (38, "DOC-004", "Phase 9: Media & Documentation", "Tạo Bảng Excel Master Timeline & KPI Tiến Độ", "NihongoLife_Timeline_QuachThanhLong.xlsx", "Cao", "2026-10-08", "2026-10-08", 1, "Đang thực hiện", 0.98, "☐ Đang làm", "Bảng Excel quản lý toàn bộ 40 đầu việc lập trình source code & xây dựng game chi tiết"),
        (39, "DOC-005", "Phase 9: Media & Documentation", "Đóng Gói Product Release & WebGL Build", "Build/WebGL/NihongoLife", "Rất cao", "2026-10-09", "2026-10-12", 4, "Chưa bắt đầu", 0.0, "☐ Chưa đạt", "Xuất bản gói WebGL hoàn chỉnh sẵn sàng chạy trên trình duyệt web không cần cài đặt"),
        (40, "DOC-006", "Phase 9: Media & Documentation", "Tổng Duyệt Kỹ Thuật & Bảo Vệ Đồ Án Capstone", "Hội đồng đánh giá VTC Academy", "Rất cao", "2026-10-13", "2026-10-15", 3, "Chưa bắt đầu", 0.0, "☐ Chưa đạt", "Thuyết trình và demo trực tiếp sản phẩm NihongoLife trước Hội đồng giám khảo")
    ]

    start_row = 6
    for r_idx, task in enumerate(all_tasks, start=start_row):
        ws1.row_dimensions[r_idx].height = 24
        for c_idx, val in enumerate(task, start=1):
            cell = ws1.cell(row=r_idx, column=c_idx, value=val)
            cell.font = Font(name=font_family, size=9.5)
            cell.border = thin_border
            
            # Formats & Alignments
            if c_idx in [1, 2, 7, 8, 9, 10, 12]:
                cell.alignment = Alignment(horizontal="center", vertical="center")
            elif c_idx == 11:
                cell.alignment = Alignment(horizontal="right", vertical="center")
                cell.number_format = '0.0%'
            else:
                cell.alignment = Alignment(horizontal="left", vertical="center", wrap_text=True)

            # Priority Stylings
            if c_idx == 6:
                if val == "Rất cao":
                    cell.fill = PatternFill(start_color="FEE2E2", end_color="FEE2E2", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, bold=True, color="991B1B")
                elif val == "Cao":
                    cell.fill = PatternFill(start_color="FFEDD5", end_color="FFEDD5", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, bold=True, color="9A3412")
                elif val == "Trung bình":
                    cell.fill = PatternFill(start_color="FEF3C7", end_color="FEF3C7", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, color="92400E")

            # Status Stylings
            if c_idx == 10:
                if val == "Đã hoàn thành":
                    cell.fill = PatternFill(start_color="DCFCE7", end_color="DCFCE7", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, bold=True, color="166534")
                elif val == "Đang thực hiện":
                    cell.fill = PatternFill(start_color="E0F2FE", end_color="E0F2FE", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, bold=True, color="075985")
                elif val == "Chưa bắt đầu":
                    cell.fill = PatternFill(start_color="F3F4F6", end_color="F3F4F6", fill_type="solid")
                    cell.font = Font(name=font_family, size=9.5, italic=True, color="6B7280")

            # Checkmarks
            if c_idx == 12:
                if "☑" in str(val):
                    cell.font = Font(name=font_family, size=10, bold=True, color="16A34A")
                else:
                    cell.font = Font(name=font_family, size=10, color="EA580C")

    # Summary Footer Row
    sum_row = start_row + len(all_tasks) + 1
    ws1.merge_cells(f"A{sum_row}:C{sum_row}")
    ws1.cell(row=sum_row, column=1, value="TỔNG CỘNG HẠNG MỤC / CHỈ SỐ").font = Font(name=font_family, bold=True, color="0F172A")
    ws1.cell(row=sum_row, column=1).alignment = Alignment(horizontal="center", vertical="center")
    ws1.cell(row=sum_row, column=1).fill = PatternFill(start_color="CBD5E1", end_color="CBD5E1", fill_type="solid")

    ws1.cell(row=sum_row, column=4, value=f"=COUNTA(A6:A{sum_row-2}) & ' Tasks Source & Game'").font = Font(name=font_family, bold=True)
    ws1.cell(row=sum_row, column=4).fill = PatternFill(start_color="E2E8F0", end_color="E2E8F0", fill_type="solid")

    ws1.cell(row=sum_row, column=8, value="TỔNG NGÀY:").font = Font(name=font_family, bold=True)
    ws1.cell(row=sum_row, column=8).alignment = Alignment(horizontal="right", vertical="center")
    ws1.cell(row=sum_row, column=8).fill = PatternFill(start_color="E2E8F0", end_color="E2E8F0", fill_type="solid")

    total_days_cell = ws1.cell(row=sum_row, column=9, value=f"=SUM(I6:I{sum_row-2})")
    total_days_cell.font = Font(name=font_family, bold=True, color="1E40AF")
    total_days_cell.fill = PatternFill(start_color="E2E8F0", end_color="E2E8F0", fill_type="solid")
    total_days_cell.alignment = Alignment(horizontal="center", vertical="center")

    ws1.cell(row=sum_row, column=10, value="TIẾN ĐỘ CHUNG:").font = Font(name=font_family, bold=True)
    ws1.cell(row=sum_row, column=10).alignment = Alignment(horizontal="right", vertical="center")
    ws1.cell(row=sum_row, column=10).fill = PatternFill(start_color="E2E8F0", end_color="E2E8F0", fill_type="solid")

    avg_progress_cell = ws1.cell(row=sum_row, column=11, value=f"=AVERAGE(K6:K{sum_row-2})")
    avg_progress_cell.font = Font(name=font_family, bold=True, color="15803D")
    avg_progress_cell.number_format = '0.0%'
    avg_progress_cell.fill = PatternFill(start_color="E2E8F0", end_color="E2E8F0", fill_type="solid")
    avg_progress_cell.alignment = Alignment(horizontal="right", vertical="center")

    for c in range(1, 14):
        ws1.cell(row=sum_row, column=c).border = thin_border

    # Set Column Widths for Sheet 1
    ws1.column_dimensions['A'].width = 6
    ws1.column_dimensions['B'].width = 14
    ws1.column_dimensions['C'].width = 32
    ws1.column_dimensions['D'].width = 44
    ws1.column_dimensions['E'].width = 38
    ws1.column_dimensions['F'].width = 14
    ws1.column_dimensions['G'].width = 14
    ws1.column_dimensions['H'].width = 14
    ws1.column_dimensions['I'].width = 14
    ws1.column_dimensions['J'].width = 18
    ws1.column_dimensions['K'].width = 16
    ws1.column_dimensions['L'].width = 14
    ws1.column_dimensions['M'].width = 65

    # ----------------------------------------------------
    # SHEET 2: Technical Summary & Dashboard KPI
    # ----------------------------------------------------
    ws2 = wb.create_sheet(title="Dashboard & Báo Cáo Kỹ Thuật")
    ws2.views.sheetView[0].showGridLines = True

    ws2.merge_cells("A1:G2")
    d_title = ws2["A1"]
    d_title.value = "BÁO CÁO KỸ THUẬT SOURCE CODE & CHỈ SỐ KPI SOLO DEVELOPER"
    d_title.font = Font(name=font_family, size=15, bold=True, color="FFFFFF")
    d_title.fill = PatternFill(start_color="0F172A", end_color="0F172A", fill_type="solid")
    d_title.alignment = Alignment(horizontal="center", vertical="center")

    # KPI Summary Cards
    kpi_cards = [
        ("TỔNG ĐẦU VIỆC CODE/GAME", len(all_tasks), "1E40AF", "EFF6FF"),
        ("ĐÃ HOÀN THÀNH & DEPLOY", len([t for t in all_tasks if t[9] == "Đã hoàn thành"]), "166534", "F0FDF4"),
        ("ĐANG FINALIZE / REVIEW", len([t for t in all_tasks if t[9] == "Đang thực hiện"]), "075985", "F0F9FF"),
        ("CHƯA THỰC HIỆN", len([t for t in all_tasks if t[9] == "Chưa bắt đầu"]), "374151", "F9FAFB"),
        ("TỔNG THỜI GIAN THI CÔNG", f"{sum([t[8] for t in all_tasks])} Ngày", "6B21A8", "F3E8FF"),
    ]

    for idx, (k_title, k_val, c_hex, bg_hex) in enumerate(kpi_cards, start=1):
        c_let1 = get_column_letter(idx * 2 - 1)
        c_let2 = get_column_letter(idx * 2)
        
        ws2.merge_cells(f"{c_let1}4:{c_let2}4")
        ws2.merge_cells(f"{c_let1}5:{c_let2}6")
        
        head = ws2[f"{c_let1}4"]
        head.value = k_title
        head.font = Font(name=font_family, size=8.5, bold=True, color=c_hex)
        head.fill = PatternFill(start_color=bg_hex, end_color=bg_hex, fill_type="solid")
        head.alignment = Alignment(horizontal="center", vertical="center")
        
        val_c = ws2[f"{c_let1}5"]
        val_c.value = k_val
        val_c.font = Font(name=font_family, size=16, bold=True, color=c_hex)
        val_c.fill = PatternFill(start_color=bg_hex, end_color=bg_hex, fill_type="solid")
        val_c.alignment = Alignment(horizontal="center", vertical="center")

    # Phase Breakdown Table
    ws2.cell(row=9, column=1, value="Phân Tích Tiến Độ Lập Trình Theo 9 Giai Đoạn (Phase Breakdown)").font = Font(name=font_family, size=12, bold=True, color="0F172A")

    p_headers = ["STT", "Giai Đoạn Phát Triển (Phase)", "Số Lượng Task", "Tổng Số Ngày", "Task Hoàn Thành", "Tỷ Lệ Hoàn Thành"]
    for col_i, ph in enumerate(p_headers, start=1):
        cell = ws2.cell(row=10, column=col_i, value=ph)
        cell.font = Font(name=font_family, size=10, bold=True, color="FFFFFF")
        cell.fill = PatternFill(start_color="334155", end_color="334155", fill_type="solid")
        cell.alignment = Alignment(horizontal="center", vertical="center")
        cell.border = thin_border

    p_names = []
    for t in all_tasks:
        if t[2] not in p_names:
            p_names.append(t[2])

    for p_idx, p_name in enumerate(p_names, start=11):
        p_t_list = [t for t in all_tasks if t[2] == p_name]
        p_cnt = len(p_t_list)
        p_d = sum([t[8] for t in p_t_list])
        p_dn = len([t for t in p_t_list if t[9] == "Đã hoàn thành"])
        p_rt = p_dn / p_cnt if p_cnt > 0 else 0
        
        row_vals = [p_idx-10, p_name, p_cnt, p_d, p_dn, p_rt]
        for c_i, v in enumerate(row_vals, start=1):
            c = ws2.cell(row=p_idx, column=c_i, value=v)
            c.font = Font(name=font_family, size=9.5)
            c.border = thin_border
            if c_i in [1, 3, 4, 5]:
                c.alignment = Alignment(horizontal="center", vertical="center")
            elif c_i == 6:
                c.alignment = Alignment(horizontal="right", vertical="center")
                c.number_format = '0.0%'
            else:
                c.alignment = Alignment(horizontal="left", vertical="center")

    # Set Column Widths for Sheet 2
    ws2.column_dimensions['A'].width = 8
    ws2.column_dimensions['B'].width = 42
    ws2.column_dimensions['C'].width = 16
    ws2.column_dimensions['D'].width = 16
    ws2.column_dimensions['E'].width = 20
    ws2.column_dimensions['F'].width = 20

    output_file = r"d:\VTC_Academy\NihongoLife\NihongoLife\Bao_Cao\NihongoLife_Timeline_QuachThanhLong.xlsx"
    wb.save(output_file)
    print(f"SUCCESS: Generated complete Excel workbook at {output_file}")

if __name__ == "__main__":
    build_complete_timeline()
