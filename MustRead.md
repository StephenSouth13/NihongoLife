# MustRead — NihongoLife

Cho Long, Huy, Dũng. Một máy mới, `git pull` nhánh `main`, rồi mở Unity, phải biên dịch và build được.

Project Unity là thư mục `NihongoLife/` bên trong repo, không phải thư mục gốc.

## 1. Unity

- **Đúng bản:** Unity **6000.3.12f1**, revision **fca03ac9b0d5** (`NihongoLife/ProjectSettings/ProjectVersion.txt`).
- Cài bằng Unity Hub, editor Windows (README yêu cầu Windows 10/11).
- Module trong Hub, cùng lúc cài editor:
  - **Windows Build Support** (Mono hoặc IL2CPP) để chọn target Standalone và Play trong editor. Agora native chạy trên target này.
  - **WebGL Build Support** để compile và build bản web.
  - **Android Build Support** khi build file `.aab`, kèm OpenJDK, Android SDK và NDK mà Hub cài cùng module đó. Lệnh là `NihongoLife.Editor.ReleaseBuild.BuildAndroid`.

Pipeline là URP. Asset đang gán trong `ProjectSettings/GraphicsSettings.asset` là `Assets/Settings/PC_RPAsset.asset`.

## 2. Package (Packages/manifest.json)

Unity tự tải các package này khi mở project (cần mạng tới registry Unity). Version dưới đây là version khai báo trong manifest.

| Package | Version |
|---|---|
| com.unity.ai.navigation | 2.0.11 |
| com.unity.collab-proxy | 2.11.4 |
| com.unity.ide.rider | 3.0.39 |
| com.unity.ide.visualstudio | 2.0.26 |
| com.unity.inputsystem | 1.19.0 |
| com.unity.multiplayer.center | 1.0.1 |
| com.unity.pipeline | 0.8.0-exp.1 |
| com.unity.render-pipelines.universal | 17.3.0 |
| com.unity.services.vivox | 16.12.1 |
| com.unity.test-framework | 1.6.0 |
| com.unity.timeline | 1.8.11 |
| com.unity.ugui | 2.0.0 |
| com.unity.visualscripting | 1.9.11 |

Mọi dòng `com.unity.modules.*` trong cùng file đều là **1.0.0** (module có sẵn của editor: accessibility, adaptiveperformance, ai, androidjni, animation, assetbundle, audio, cloth, director, imageconversion, imgui, jsonserialize, particlesystem, physics, physics2d, screencapture, terrain, terrainphysics, tilemap, ui, uielements, umbra, unityanalytics, unitywebrequest, unitywebrequestassetbundle, unitywebrequestaudio, unitywebrequesttexture, unitywebrequestwww, vectorgraphics, vehicles, video, vr, wind, xr).

`Packages/packages-lock.json` khoá thêm các package phụ. Unity tải chúng cùng lúc, không cài tay: burst 1.8.28, collections 2.6.5, mathematics 1.3.3, shadergraph 17.3.0, render-pipelines.core 17.3.0, render-pipelines.universal-config 17.0.3, services.core 1.16.0, nuget.mono-cecil 1.11.6, nuget.newtonsoft-json 3.2.2, settings-manager 2.1.1, searcher 4.9.4, test-framework.performance 3.2.0, ext.nunit 2.0.5.

## 3. Asset bên thứ ba

Đã đối chiếu `git ls-files` với đĩa trên `main`. Các cây sau **có đủ trong git**. Sau `git pull` và `git lfs pull` thì không import lại, và không giải nén đè lên thư mục đang có.

| Thư mục | Trong git | Ghi chú |
|---|---|---|
| `NihongoLife/Assets/Agora-RTC-Plugin` | đủ (1378 file) | Native SDK **4.6.2** (chuỗi `CFBundleShortVersionString` trong `AgoraRtcWrapperUnity.bundle/Contents/Info.plist`). `AgoraRtcSDK.asmdef` loại WebGL. WebGL dùng `Assets/NihongoLife/Plugins/WebGL/NihongoAgora.jslib`. |
| `NihongoLife/Assets/Devion Games` | đủ (2625 file) | Inventory System, Stat System, Third Person Controller, Triggers, UI Widgets, Graphs, Utilities, Flat GUI, Module Manager, Standard Assets. |
| `NihongoLife/Assets/ThirdParty` | đủ | KayKit City Builder Bits, Kenney (kể cả impact/interface sounds), Minigame, Mixamo (animation Remy và farming pack đã commit), Styloo Classroom, Sushi Restaurant Kit, Train Pack, Ultimate Animated Animals, Ultimate Food Pack, Ultimate House Interior Pack, New Chac. |
| `NihongoLife/Assets/ZRNAssets` | đủ | PQAssets và các pack kèm theo. |
| `NihongoLife/Assets/みどり島 – Midori Island (Đảo Xanh)` | đủ | Farm Building, Survival Kit, Ultimate Crops Pack. Scene chơi là `60_MidoriIsland`. |
| `NihongoLife/Assets/TextMesh Pro` | đủ | Essentials đã có: Resources, Shaders, Fonts, Sprites, Examples & Extras. |

Nếu thư mục Agora bị xoá mất trên một máy, lấy **Agora Video SDK for Unity 4.6.2** từ tài liệu Unity của Agora và giải nén đúng vào `Assets/Agora-RTC-Plugin`. Đừng cài một bản SDK khác lên trên bản 4.6.2.

Devion chỉ lấy từ Asset Store khi thư mục `Assets/Devion Games` thực sự không có sau khi pull. Bản trong repo là bản project đang biên dịch.

### Mixamo Lilly / Eminem — không có trong git

`.gitignore` ở gốc repo bỏ qua:

- `NihongoLife/Assets/ThirdParty/Mixamo/Characters/lilly/source/*.fbx`
- `NihongoLife/Assets/ThirdParty/Mixamo/Characters/eminem/source/*.fbx` và `*.Fbx`

`CharacterBuilder` trỏ tới `Lilly.fbx` và `Eminem FBX.Fbx` trong hai đường dẫn đó. Thư mục `Characters/` không có trong git. Prefab đã lưu sẵn: `Assets/NihongoLife/Prefabs/Characters/NL_Player.prefab`, `NL_Guide.prefab`, `NL_Cashier.prefab`, `NL_Neighbor.prefab`. Compile và Play dùng prefab này.

Chỉ tải FBX gốc từ Mixamo (tài khoản nội bộ) khi có người chạy `NihongoLife.Editor.CharacterBuilder.BuildCharacterSystem`. File đó không được commit.

`CharacterBuilder` còn trỏ `Remy.fbx` và FBX Elizabeth trong `Mixamo/Characters/.../source/`. Hai file nguồn đó cũng không có trong git. Animation Remy (`Remy@Idle.fbx` và các clip cùng thư mục) thì có.

## 4. Git LFS

`.gitattributes` ở gốc repo đưa png, jpg và nhiều định dạng nhị phân (fbx, wav, dll, …) qua LFS. Trên `main` hiện có **477** object LFS, toàn bộ là png và jpg. Nhiều FBX cũ vẫn là blob git thường.

Trên máy mới:

```bash
git lfs install
git clone https://github.com/StephenSouth13/NihongoLife.git
cd NihongoLife
git lfs pull
```

`git lfs install` chỉ cần một lần cho mỗi máy. Mỗi lần pull có file ảnh mới thì `git lfs pull` lại.

## 5. Khoá local

`NihongoLife/LocalSecrets/secrets.example.json` là file mẫu, được commit.

Copy thành `NihongoLife/LocalSecrets/secrets.json`. File này nằm trong `.gitignore`. Khoá chia sẻ riêng trong nhóm, không ghi vào git, không dán vào chat công khai.

Ô trong file:

- `NIHONGOLIFE_SUPABASE_URL` — URL project Supabase
- `NIHONGOLIFE_SUPABASE_ANON_KEY` — anon key, không phải service_role
- `AGORA_APP_ID` — App ID, không phải primary certificate
- `NIHONGOLIFE_GEMINI_API_KEY` — khoá Gemini

Giá trị còn dạng `<...>` bị bỏ qua. Không có file hoặc không có mạng thì game vẫn Play offline (lưu cục bộ, NPC dùng câu soạn sẵn). Biến môi trường cùng tên ghi đè file.

## 6. Lần mở đầu

1. `git pull` trên `main`, rồi `git lfs pull`.
2. Unity Hub → Add → chọn thư mục `NihongoLife` bằng editor 6000.3.12f1.
3. Đợi import xong. Lần đầu lâu vì asset và package. Console phải hết lỗi CS trước khi Play.
4. Đổi platform ở **File → Build Profiles** (bản cũ: Build Settings). Target WebGL để kiểm tra bản build web. Target Windows Standalone để Play bằng Agora native.
5. Mở `Assets/NihongoLife/Scenes/00_Bootstrap.unity` và bấm Play. Scene này nạp menu. Muốn xem menu trực tiếp thì mở `01_MainMenu.unity`.
6. Không chạy baker menu, không có bước setup sau khi pull. Trạng thái scene đã lưu trong scene và prefab.

Scene production đang bật trong `EditorBuildSettings.asset`: `01_MainMenu`, `00_Bootstrap`, `90_TestSandbox` (phố Hibari-chō), `20_StationDistrict`, `30_SushiRestaurant`, `40_ HIBARICLASS`, `45_HomeBedroom`, `50_GameCenter`, `60_MidoriIsland`. `99_ControlRoom` và `99_Trailer` tắt.

## 7. Build WebGL và đưa lên Vercel

Không có menu item. Đóng editor nếu nó đang mở project, rồi từ thư mục repo:

```text
"C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath NihongoLife -executeMethod NihongoLife.Editor.ReleaseBuild.BuildWebGL -logFile webgl-build.log
```

`ReleaseBuild.BuildWebGL` đặt:

- nén **Brotli**
- **decompression fallback = bật** (loader của Unity tự giải nén `.unityweb`)
- tên file hashed, data caching, exception chỉ cho exception được ném tường minh
- heap đầu 256 MB, tối đa 2048 MB

`ProjectSettings` hiện cũng đang `webGLDecompressionFallback: 1` và `webGLCompressionFormat: 0` (Brotli).

Output mặc định: **`Build/Release/WebGL`** ở thư mục gốc repo (cạnh thư mục `NihongoLife`, vì `Application.dataPath/../..`). Đặt `NIHONGO_BUILD_ROOT` nếu muốn chỗ khác; file player nằm ở `%NIHONGO_BUILD_ROOT%/WebGL`.

Player WebGL chỉ chứa `01_MainMenu` và `00_Bootstrap`. Các khu còn lại thành bundle trong `zones/` (`nlzone-city`, `nlzone-station`, `nlzone-sushi`, `nlzone-school`, `nlzone-home`, `nlzone-arcade`, `nlzone-island`) cộng `zones/index.json`. Build cũng ghi `index.html`, `TemplateData/`, `Build/`, `_headers`, `.htaccess`, `vercel.json`.

Kiểm tra folder:

```bash
bash tools/publish-webgl.sh Build/Release/WebGL --check
```

Đưa lên **một project Vercel riêng** cho game, subdomain **nihongolife.quachthanhlong.com**. Tên miền này không nằm trong git; gắn domain trong dashboard của project Vercel đó. Project marketing / website đội là project khác.

```bash
cd Build/Release/WebGL
vercel --prod
```

Root deploy là chính folder output, để `vercel.json` nằm ở gốc site. Gửi cả `zones/`.

`vercel.json` do build ghi ra chỉ đặt `Content-Type` và `Cache-Control`. **Không thêm `Content-Encoding: br`.** Fallback đang bật: loader Unity tự inflate. Host mà cũng inflate thì file hỏng. `Build/*` và `zones/*` (trừ `zones/index.json`) cache một năm. `index.html` và `zones/index.json` là `no-cache`.

Chi tiết host khác: `docs/WEBGL_DEPLOY.md`.

## 8. Lỗi hay gặp

**CS0246 `Agora`, `VideoSurface`, `IRtcEngine`, `IRtcEngineEventHandler`** trong `EduMeetingManager.cs` và `VoiceChatService.cs` khi target là WebGL. `AgoraRtcSDK.asmdef` loại WebGL. Bản `main` hiện biên dịch stand-in `AgoraWebShim.cs`, `VideoSurface.cs`, `AgoraWebEvents.cs` mỗi khi `UNITY_WEBGL` bật (kể cả editor). Player gọi `NihongoAgora.jslib`. Editor dùng stub rỗng. Windows và Android vẫn dùng SDK native. Test micro native trong `IeltsProductivePlayModeTests` chỉ biên dịch khi không phải WebGL. Sau khi pull: để Unity recompile. Còn lỗi cũ thì đóng project, xoá `NihongoLife/Library`, mở lại.

**Thiếu Devion.** Script `DevionGames.*` báo missing. Thư mục phải có sau pull. Khôi phục bằng git, không cài một bản Asset Store khác đè lên.

**Material hồng.** Project dùng URP (`PC_RPAsset`). Ảnh png/jpg là LFS: file bắt đầu bằng `version https://git-lfs.github.com/spec/v1` nghĩa là chưa `git lfs pull`. Pull LFS, mở lại Unity, đợi import shader URP.

**File LFS dạng pointer.** Cài Git LFS, `git lfs pull`, xác nhận một file png mở được bằng trình xem ảnh.

**Reset Library.** Đóng Unity. Xoá `NihongoLife/Library` và `NihongoLife/Temp`. Mở lại. Hai thư mục này không commit.

## 9. Việc với git

- `git pull` `main` trước khi sửa.
- Commit nhỏ, một việc một commit.
- Làm trên nhánh, mở pull request vào `main`.
- Không commit `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, file `.csproj` / `.sln`, file zip.
- Không commit `secrets.json`, `.env`, service_role, certificate Agora.
- Tài liệu marketing để nội bộ. Không đưa vào repo.
- FBX nguồn Lilly/Eminem và bài thi có bản quyền (`NihongoLife/Docs/Exam/`, `NihongoLife/LocalContent/`) không commit.
