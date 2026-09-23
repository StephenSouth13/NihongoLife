# NihongoLife Handoff

## Muc tieu

Hoan thien game NihongoLife theo huong E-learning JLPT + IELTS va chuan bi WebGL website truoc 2026-10-15.

## Blocker hien tai

Unity Editor batch khong khoi tao duoc Licensing Client, du Unity Hub dang hien license Personal Active:

```text
Licensing::IpcConnector: Connection to channel LicenseClient-Admin refused
Licensing initialization failed after 74.82s
```

Vi vay Unity chua chay duoc builder/test va scene `50_LearningCenter.unity` chua duoc ghi ra. Day khong phai loi ban quyen asset hay loi source builder.

## Project

- Root: `D:/VTC_Academy/NihongoLife/NihongoLife/NihongoLife`
- Unity: `6000.3.12f1`
- Main test scene: `Assets/NihongoLife/Scenes/90_TestSandbox.unity`
- Existing scenes: `00_Bootstrap`, `01_MainMenu`, `20_StationDistrict`, `30_SushiRestaurant`, `90_TestSandbox`, `99_ControlRoom`, `99_Trailer`
- School asset pack: `Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX`

## Source da cap nhat

- `Assets/NihongoLife/Scripts/Editor/GameplayZoneSceneBuilder.cs`: co `BuildLearningCenter()` va `BuildShoppingDistrict()`; dung asset Styloo cho JLPT, IELTS va Mock Exam.
- `Assets/NihongoLife/Scripts/Core/WorldLocationCatalog.cs`: them shopping/learning locations va spawn IDs.
- `Assets/NihongoLife/Scripts/Core/StandaloneZoneBootstrap.cs`: them spawn cho shopping va learning center.
- `Assets/NihongoLife/Scripts/Core/SceneFlowController.cs`: them display name cho hai scene.
- `Assets/NihongoLife/Scripts/UI/WorldMapUI.cs`: them map Shopping District va Learning Center; map click ca hai tab deu hoat dong.
- `Assets/NihongoLife/Scripts/Player/PlayerController.cs`: click-to-move dung NavMesh path.
- `Assets/NihongoLife/Scripts/World/RuntimeCollisionRepair.cs`: khong tao them shell store runtime va khong an NPC hang loat.
- `Assets/NihongoLife/Scripts/Exam/ExamManager.cs` va `UI/ExamCenterPopup.cs`: them `using NihongoLife.Save;` de fix `IProgressRepository` CS0246.
- `Assets/NihongoLife/Scripts/Learning/AssessmentModels.cs` va `AssessmentEngine.cs`: core question bank JLPT/IELTS, blueprint, random seed, grading, weak target IDs.

## Viec can lam ngay

1. Sua Unity Licensing Client tren Windows. Dong Unity, Hub va `Unity.Licensing.Client`; mo Hub, refresh Personal license; mo project bang Editor GUI mot lan; cho compile/import xong; dong Editor.
2. Chay builder:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\VTC_Academy\NihongoLife\NihongoLife\NihongoLife" -executeMethod NihongoLife.EditorTools.GameplayZoneSceneBuilder.BuildLearningCenter -quit -logFile "learning-builder.log"
```

3. Xac nhan ton tai `Assets/NihongoLife/Scenes/50_LearningCenter.unity` va `40_ShoppingDistrict.unity` neu build shopping.
4. Mo scene Learning Center, test visual, spawn, camera, colliders, exit portal.
5. Chay EditMode va PlayMode tests. Khong claim done neu chua Play Mode visual/collision test.

## E-learning scope

1. Lesson Hub trong Learning Center.
2. Muc tieu hoc truoc khi vao bai.
3. Flashcard tu vung.
4. Listening co audio, replay, ket qua.
5. Speaking co microphone, transcription/score va fallback khi API khong co.
6. Reading va Writing UI.
7. Quiz sau scenario.
8. Luu cau sai de Review.
9. Dashboard mastery theo Vocabulary, Grammar, Listening, Speaking, Reading, Writing.
10. Curriculum JLPT N5/N4 khoa/mo bai.
11. IELTS 4 skills theo blueprint va band score.
12. Bao cao hoc tap.
13. WebGL/mobile responsive test.

## Nguyen tac

- Khong tao scene moi ngoai `50_LearningCenter` va `40_ShoppingDistrict` da duoc phe duyet.
- Khong chong nha/store runtime len authored environment.
- Dung prefab/asset da co, giu phong cach low-poly/URP.
- Dung curated question bank + deterministic random; khong sinh cau hoi tu do trong luc thi.
- Khong xoa thay doi nguoi dung.
- Kiem tra bang `git diff --check`.

## Warnings khong chan compile

- `MainMenuUI.authorName/projectRole` chua dung.
- `StationTravelController.ticketPrice` chua dung.
- Unity packages immutable bi altered: kiem tra rieng, khong sua tiep package neu khong can.
