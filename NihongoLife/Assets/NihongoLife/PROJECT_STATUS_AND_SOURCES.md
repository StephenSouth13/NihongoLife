# NihongoLife - Tổng quan, hiện trạng và nguồn tài nguyên

> Cập nhật: 2026-09-20  
> Unity: 6000.3.12f1  
> Render pipeline: Universal Render Pipeline 17.3.0  
> Input System: 1.19.0  
> Trạng thái compile gần nhất: thành công, `Tundra build success`, exit code 0

## 1. Tóm tắt dự án

NihongoLife là game nhập vai mô phỏng đời sống kết hợp E-Learning tiếng Nhật. Người chơi sống trong một khu phố Nhật Bản, giao tiếp với NPC, mua sắm, hoàn thành nhiệm vụ và học từ vựng, ngữ pháp, nghe, nói trong ngữ cảnh thực tế.

Mục tiêu dài hạn không chỉ là một game hội thoại. Nền tảng được định hướng để mở rộng sang:

- Sinh hoạt, mua sắm và khám phá khu phố.
- Hội thoại phân nhánh và cốt truyện dài nhiều chương.
- Học tiếng Nhật theo tình huống, chấm điểm và theo dõi mức thành thạo.
- Chỉ số sinh tồn: máu, năng lượng, đói và khát.
- Balo giới hạn ô và số lượng vật phẩm.
- Trồng cây, nuôi thú, thú cưng và farm tài nguyên.
- Chiến đấu, quái vật và các sinh vật/tiên linh trong tương lai.
- Hệ thống hành vi xã hội: tấn công NPC, trộm cắp hoặc xả rác dẫn đến hình phạt.
- Tài khoản email, cloud save, bạn bè, bảng xếp hạng và co-op.
- Phát hành Windows trước, sau đó WebGL và mobile.

Trọng tâm thiết kế vẫn là E-Learning: **điểm Kiến thức** là tiến trình chính, tương tự cấp độ trong RPG. Nội dung càng khó, yêu cầu Kiến thức càng cao và phần thưởng Kiến thức càng lớn.

## 2. Phạm vi scene

### Scene đang bật trong Build Settings

| Scene | Vai trò | Trạng thái |
|---|---|---|
| `00_Bootstrap.unity` | Khởi tạo service và chuyển scene | Đang dùng |
| `01_MainMenu.unity` | Menu, tài khoản, ngôn ngữ, chọn nhân vật, cài đặt | Đang dùng |
| `90_TestSandbox.unity` | Toàn bộ gameplay chính và các khu vực cốt truyện | Đang dùng |
| `20_StationDistrict.unity` | Khu nhà ga tải additive, có platform/train/spawn/portal | Đang dùng |
| `30_SushiRestaurant.unity` | Nội thất nhà hàng sushi tải additive, có spawn/portal | Đang dùng |

### Scene công cụ, không đưa vào build

| Scene | Vai trò | Trạng thái |
|---|---|---|
| `99_ControlRoom.unity` | Kiểm tra nội bộ | Tắt trong build |
| `99_Trailer.unity` | Dựng trailer | Tắt trong build |

Kiến trúc hiện tại: `90_TestSandbox` là City Hub/persistent gameplay. Không chia từng con phố hoặc căn nhà thành scene riêng. Các khu nặng, interior hoặc district biệt lập dùng additive zone; hiện có Station District và Sushi Restaurant. `SceneFlowController` chịu trách nhiệm fade, tên địa điểm, tiến trình tải, khóa input, spawn và unload zone.

## 3. Kiến trúc kỹ thuật hiện tại

> **Quy tắc dự án:** Không tạo Unity editor menu (`[MenuItem]`) cho thao tác build, setup, tích hợp hoặc sửa scene. Không tạo thêm scene nếu chưa có yêu cầu rõ ràng. Nội dung hoàn chỉnh phải được lưu trực tiếp trong scene/prefab/asset và chạy được ngay khi mở project.

- `AppRoot`: khởi tạo và đăng ký service dùng xuyên scene.
- `GameServices`: service locator cho audio, setting, input, auth, progress, scenario và online world.
- `GameInputService`: một lớp input chung cho keyboard, mouse, gamepad và mobile bridge.
- `PlayerSessionService`: phân biệt Guest và Account.
- `IProgressRepository`: giao diện lưu tiến độ.
- `LocalProgressRepository`: lưu local cho tài khoản/offline.
- `SupabaseProgressRepository`: cache local và đồng bộ Supabase khi cấu hình online hợp lệ.
- `LocalScenarioRepository`: nạp scenario asset và scenario dựng sẵn bằng code.
- `ScenarioManager`: chạy node hội thoại, mục tiêu, tương tác và kết quả.
- `ScenarioCampaignManager`: sắp xếp/chuyển scenario theo chapter.
- `ScoringManager`: điểm tổng và điểm theo nhóm kỹ năng.
- `LearningMasteryManager`: theo dõi mức thành thạo từ vựng/ngữ pháp.
- `DialogueManager`: hiển thị hội thoại và phát voice clip theo node.
- `GeminiConversationService`: nền cho hội thoại AI và xử lý phần luyện nói khi được cấu hình.

Project hiện có khoảng 98 script C# trong thư mục NihongoLife.

## 4. Tính năng đã triển khai

### 4.1 Menu và tài khoản

- Menu chính, chọn ngôn ngữ VI/EN/JA.
- Chọn nhân vật và kéo ngang để xoay preview.
- Nhân vật chưa mở hiển thị `COMING SOON`.
- Đăng ký email/mật khẩu và tên hiển thị qua Supabase khi backend được cấu hình.
- Đăng nhập, khôi phục session và refresh token.
- Tên đăng ký được dùng làm tên nhân vật và lưu trong progress.
- Guest mode không cần đăng nhập.
- Progress của Guest chỉ tồn tại trong RAM; thoát ứng dụng sẽ mất tiến độ.

### 4.2 Điều khiển

- Di chuyển, chạy và tiêu hao năng lượng.
- Nhảy vật lý bằng `Space`.
- Tương tác mặc định chuyển sang `F`.
- Balo `B`, nhân vật `Tab`, chat `Enter`, ghi âm `V`.
- Tấn công bằng chuột trái và vứt vật phẩm bằng `G`.
- Settings cho phép remap toàn bộ action và lưu binding.
- Có binding gamepad.
- Có `MobileJoystick` và `MobileActionButton` làm bridge cho UI cảm ứng tương lai.

Lưu ý: nhảy đã có logic vật lý nhưng vẫn thiếu animation takeoff/airborne/landing để đạt chất lượng phát hành.

### 4.3 Gameplay và nhân vật

- Third-person controller và camera follow/zoom/collision.
- Camera có smoothing và tránh xuyên vật cản.
- NPC tương tác, nhìn người chơi, hội thoại và patrol.
- Có phục hồi khi NPC bị kẹt hoặc animation/controller thiếu tham số.
- Animator được kiểm tra rig/controller và hỗ trợ culling phù hợp.
- Hệ thống tương tác cửa, vật phẩm, quầy hàng và vùng nhiệm vụ.
- Cửa hàng tiện lợi đã được tái bố trí ở mức prototype nâng cấp.

### 4.4 Balo và chỉ số

- Balo giới hạn 16 ô.
- Stack tối đa mặc định 20 vật phẩm.
- Không nhặt vật phẩm nếu balo đầy.
- Vật phẩm có thể được vứt lại ra thế giới nếu có world template hợp lệ.
- Tiền Yen, mua hàng và thanh toán.
- Máu, năng lượng, đói, khát và Kiến thức.
- Chạy tiêu hao năng lượng; chỉ số sinh tồn thay đổi theo thời gian.

### 4.5 Hành vi xã hội

- Tấn công NPC chỉ raycast khi nhấn nút, không quét NPC mỗi frame.
- NPC có phản ứng nền khi bị tấn công.
- Tấn công NPC và xả rác tạo offense.
- Offense có thể trừ Yen, hiện màn cảnh báo và tạo hạn chế gameplay tạm thời khi tái phạm.
- Client không tự khóa tài khoản Supabase thật. Khóa tài khoản thật phải được backend xác nhận để tránh hack hoặc khóa nhầm.

### 4.6 E-Learning

- Scenario dạng graph gồm dialogue, choice, collect, inspect, go-to-area, talk, complete và fail.
- Choice hỗ trợ modifier điểm và tag từ vựng/ngữ pháp.
- Điểm theo Vocabulary, Grammar, Listening, Response Accuracy và Task Completion.
- Có theo dõi mastery theo mục tiêu học.
- Điểm Kiến thức không còn thưởng cố định.
- Phần thưởng Kiến thức tính theo chapter, learning difficulty, số learning target và overall score.
- Hoàn thành lần đầu nhận đầy đủ; chơi lại nhận 25% để hạn chế farm điểm vô nghĩa.
- Có thể dùng Kiến thức làm điều kiện mở nhiệm vụ về sau.

### 4.7 Cốt truyện hiện có

Repository hiện có thể nạp 12 scenario nếu không có ID trùng:

- 9 scenario asset trong `Resources/Scenarios`.
- 3 scenario nối tiếp được dựng trong `BuiltInStoryScenarioCatalog`.

Các tình huống hiện có bao gồm:

- Đến khu phố và chào hỏi.
- Hội thoại đường phố.
- Chào hỏi tại nhà.
- Tìm mèo và báo manh mối.
- Phân loại rác tái chế.
- Mua cơm nắm tại cửa hàng tiện lợi.
- Hỗ trợ ca tối cửa hàng.
- Gọi ramen.
- Mua vé tàu.
- Lễ hội mùa hè.

Đây là khung scenario có thể chơi/kiểm tra, chưa tương đương cốt truyện hoàn chỉnh trên 3 giờ. Nhiều chapter vẫn cần environment, animation, voice, cinematic và QA để trở thành nội dung phát hành.

### 4.8 Hội thoại và audio

- Mỗi node có thể dùng clip gán trực tiếp hoặc nạp từ `Resources/Voice`.
- Cấu trúc voice hỗ trợ `ja`, `vi`, `en` và fallback theo node ID.
- 230 SFX Kenney đã import; catalog runtime hiện gán 10 cue hành động và 15 biến thể bước chân cho UI, cửa, nhặt/thả, portal, va chạm NPC và bề mặt sàn.
- Chỉ asset được tham chiếu trong `GameAudioCatalog` mới đi vào runtime/build, tránh nạp toàn bộ thư viện và tăng dung lượng WebGL.
- Hỗ trợ MP3/WAV nhập thủ công.
- Không sinh âm giả bằng tone procedural.
- Thiếu clip thì im lặng và báo qua validation/log.
- Có nền tảng ghi âm/luyện phát âm thật; chất lượng production còn phụ thuộc cấu hình API, quyền microphone, giới hạn WebGL/mobile và kiểm thử thiết bị.

## 5. Những phần mới ở mức nền tảng, chưa được coi là hoàn thiện

- Joystick mobile đã có input bridge nhưng chưa có layout/prefab cuối cùng và chưa test nhiều kích thước màn hình.
- Combat mới có input và offense khi đánh NPC; chưa có combo, hitbox chuẩn, damage, enemy AI hoặc animation combat hoàn chỉnh.
- Vứt vật phẩm cần world prefab/template cho từng item; item thiếu model sẽ hủy thao tác thay vì sinh khối giả.
- Trồng cây, nuôi thú, farm quái và tiên linh mới ở roadmap, chưa có gameplay loop hoàn chỉnh.
- Co-op, friend, leaderboard và online world đã có service/foundation nhưng chưa đủ QA để gọi là production multiplayer.
- Hạn chế tài khoản mới là trạng thái client; enforcement thật cần database policy/Edge Function/backend.
- WebGL/mobile mới có định hướng và quality tier; chưa có báo cáo profiler/build-size cuối.
- Store environment đã cải thiện so với bản đầu nhưng vẫn thiếu asset retail chuyên dụng và vòng QA trực quan cuối.

## 6. Lỗi/rủi ro cần ưu tiên

### Mức cao

- Chưa có Play Mode regression test tự động cho luồng menu -> gameplay -> scenario -> save/load.
- Chưa test build WebGL thật trên Chrome/Safari/Android WebView.
- Chưa test microphone đầy đủ trên WebGL và mobile.
- Một số nội dung text/asset cũ có dấu hiệu encoding mojibake; cần audit toàn bộ tiếng Việt/Nhật.
- Cửa hàng, item placement và collider cần kiểm tra trực quan lại sau khi nhập asset thật.
- Animation jump/combat/pickup/cashier còn thiếu.

### Mức trung bình

- `90_TestSandbox` sẽ nặng dần nếu mọi zone cùng active.
- Cần object pooling cho dropped item, projectile, monster và VFX trước khi mở rộng combat.
- Cần giới hạn Animator/NPC update theo khoảng cách.
- Cần material atlas và shared material cho hàng hóa trên kệ.
- Cần tách save schema version và migration trước khi phát hành public.
- Cần server-authoritative validation cho tiền, progress, leaderboard và punishment.

## 7. Đánh giá tiến độ

Các tỷ lệ dưới đây là đánh giá kỹ thuật tại thời điểm cập nhật, không phải cam kết phát hành.

| Hạng mục | Tiến độ ước tính | Đánh giá |
|---|---:|---|
| Kiến trúc core/service | 80% | Nền tốt, compile sạch; cần test và giảm phụ thuộc runtime-generated UI |
| Menu, auth và session | 70% | Có email/guest/name; cần backend production và UX lỗi mạng |
| Controller, camera và input | 75% | Chơi được, remap tốt; thiếu animation jump và QA gamepad/mobile |
| UI/UX gameplay | 55% | Có HUD/balo/character/settings; cần responsive polish và test overlap |
| Balo, chỉ số, tiền | 60% | Loop cơ bản có; thiếu catalog item, consume/equip/drop UX hoàn chỉnh |
| Scenario/cốt truyện | 45% | Có engine và 12 scenario; chưa đủ nội dung/asset/voice cho game 3 giờ |
| E-Learning/scoring | 65% | Có score, mastery, Knowledge scaling; cần curriculum và đánh giá sư phạm |
| Audio/voice/speech | 55% | SFX gameplay đã nối catalog; vẫn thiếu ambience, phần lớn voice và test thiết bị/API |
| Store và world art | 40% | Prototype dùng được; thiếu asset chuyên dụng và pass art/lighting cuối |
| NPC/animation | 45% | Có patrol/recovery; thiếu nhiều clip và reaction chất lượng cao |
| Combat/conduct | 20% | Có foundation; chưa phải hệ chiến đấu hoàn chỉnh |
| Farming/pets/monsters | 5% | Roadmap và nguồn asset, chưa có gameplay production |
| Online/co-op | 25% | Có foundation service; chưa đủ an toàn/ổn định để phát hành |
| WebGL/mobile readiness | 30% | Có URP tier/input bridge; chưa profile và chưa build QA đầy đủ |
| Testing/release readiness | 20% | Compile sạch; thiếu automated test, device matrix và acceptance test |

### Kết luận tiến độ

- **Nền tảng kỹ thuật tổng thể:** khoảng 60-65% cho một vertical slice.
- **Vertical slice cửa hàng + học hội thoại:** khoảng 50-55%.
- **Game cốt truyện 3 giờ đạt chất lượng phát hành:** khoảng 30-35%.
- **Sẵn sàng phát hành WebGL/mobile:** khoảng 25-30%.

Không nên dùng một con số “hoàn thành 100%” ở giai đoạn này. Project đã vượt qua prototype rời rạc và có architecture đáng kể, nhưng thiếu content production, asset cuối, voice, performance profiling và QA.

## 8. Nguồn asset khuyến nghị

### 8.1 Nguồn ưu tiên cao

#### Kenney Food Kit

- Link: https://kenney.nl/assets/food-kit
- Giấy phép: CC0.
- Nội dung: khoảng 200 model food/kitchen.
- Dùng cho: hàng hóa thật trên kệ, food pickup và thumbnail balo.
- Mức phù hợp: rất cao với low-poly hiện tại.

#### Kenney - thông tin giấy phép

- Link: https://kenney.nl/support
- Kenney xác nhận asset trên trang asset là public domain/CC0, dùng được cho commercial project và không bắt buộc attribution.

#### Quaternius Free Game Assets

- Link: https://quaternius.com/
- Dùng cho: Sushi Restaurant Kit, Ultimate Food, House Interior, Furniture, Modular Streets, animals, monsters và character.
- Phong cách: low-poly/stylized, phù hợp hơn các pack realistic.
- Cần kiểm tra file license kèm từng pack khi tải.

#### Quaternius Ultimate Crops Pack

- Link: https://quaternius.com/packs/ultimatecrops.html
- Giấy phép: CC0.
- Nội dung: hơn 100 model, nhiều loại cây với năm giai đoạn phát triển.
- Dùng cho: farming system tương lai.

#### Mixamo

- Link: https://www.mixamo.com/
- Dùng cho: run, jump, pickup, cashier scan, listening idle, hit reaction, farming và combat.
- Cấu hình tải: FBX, `Without Skin`, 30 FPS, in-place nếu clip hỗ trợ.
- Trong Unity: Rig = Humanoid, Avatar Definition = Copy From Other Avatar hoặc Create From This Model tùy rig.
- Không thay skeleton nhân vật hiện tại nếu chỉ cần animation.

#### Lowpoly Environment Extreme Pack

- Link: https://assetstore.unity.com/packages/3d/environments/lowpoly-environment-extreme-pack-238098
- Giá: miễn phí tại thời điểm kiểm tra.
- Tương thích: URP theo trang Asset Store.
- Dùng cho: forest/fantasy zone, khu tiên linh hoặc vùng farm quái.
- Không nhập toàn bộ demo scene.

#### Free Low Poly Car Pack

- Link: https://assetstore.unity.com/packages/3d/vehicles/land/free-low-poly-car-pack-274606
- Giá: miễn phí tại thời điểm kiểm tra.
- Tương thích: URP theo trang Asset Store.
- Dùng cho: xe nền đường phố; nên để static hoặc pooled traffic đơn giản.

### 8.2 Nguồn theo hệ thống tương lai

| Hệ thống | Pack nên xem | Nguồn |
|---|---|---|
| Cửa hàng/sushi | Sushi Restaurant Kit | https://quaternius.com/ |
| Nội thất | Ultimate House Interior / Ultimate Furniture | https://quaternius.com/ |
| Farming | Ultimate Crops / Farm Buildings | https://quaternius.com/ |
| Nuôi thú | Farm Animal / Ultimate Animated Animal | https://quaternius.com/ |
| Quái vật | Ultimate Monsters / Cute Animated Monsters | https://quaternius.com/ |
| Tiên linh | Cute Animated Monsters + material/VFX tùy biến | https://quaternius.com/ |
| Đường phố | Modular Streets | https://quaternius.com/ |
| Food/shop props | Kenney Food Kit | https://kenney.nl/assets/food-kit |
| Human animation | Mixamo | https://www.mixamo.com/ |

### 8.3 Asset không nên ưu tiên

- Pack thành phố 2-4 GB chỉ để lấy vài căn nhà.
- Interior realistic có texture 2K/4K và nhiều material.
- Character pack dùng skeleton riêng không tương thích Humanoid hiện tại.
- Asset không có license rõ ràng hoặc được re-upload từ nguồn khác.
- Demo project chứa script/editor extension không cần thiết.
- Shader chỉ hỗ trợ Built-in/HDRP nếu không có kế hoạch chuyển URP.

## 9. Quy chuẩn import asset

### Model

- Chỉ đưa file cần dùng vào `Assets`.
- Scale thống nhất theo mét; kiểm tra chiều cao nhân vật khoảng 1.7-1.8 m.
- Tắt Read/Write nếu không cần sửa mesh runtime.
- Mesh Compression = Medium cho prop môi trường.
- Không bật Generate Colliders cho toàn bộ pack.
- Collider đơn giản: box/capsule; tránh MeshCollider cho hàng hóa nhỏ.

### Texture/material

- Prop nhỏ: tối đa 512 px.
- Character và atlas quan trọng: tối đa 1024 px trước khi profiling.
- Dùng URP Lit/Simple Lit và shared material.
- Dùng texture atlas cho sản phẩm trên kệ.
- Không tạo một material riêng cho từng chai/hộp nếu chỉ khác màu/nhãn.

### Animation

- Humanoid, 30 FPS.
- Bật Loop Time cho idle/walk/run.
- In-place cho locomotion vì PlayerController điều khiển dịch chuyển.
- Tắt Import Animation trên FBX chỉ dùng mesh.
- Dùng Animator culling cho NPC ngoài camera.

### Audio

- Dialogue: mono, Vorbis, chất lượng khoảng 60-75 tùy giọng.
- Ambience dài: Streaming.
- UI/SFX ngắn: Decompress On Load.
- Không dùng MP3 giả hoặc tone procedural thay voice thật.
- Tên file voice phải trùng node ID theo hướng dẫn trong `Resources/Voice/README.md`.

## 10. Ngân sách WebGL/mobile đề xuất

- Mục tiêu ban đầu: 60 FPS desktop, 30 FPS mobile/WebGL thiết bị trung bình.
- Không để toàn bộ zone/NPC active trong cùng thời điểm.
- NPC xa: giảm tần suất logic hoặc disable Animator/NavMeshAgent.
- Object động lặp lại: object pool, không Instantiate/Destroy liên tục.
- Hạn chế transparent overdraw và realtime shadow.
- Baked lighting cho interior; chỉ giữ một directional light chính nếu có thể.
- Giảm số material và shader variant.
- Addressables/remote content nên được cân nhắc khi audio và map tăng lớn.
- WebGL audio/microphone phải test sau tương tác người dùng vì trình duyệt chặn autoplay/quyền mic.
- Trước release cần ghi lại: build size, peak memory, draw calls, SetPass, triangles, GC alloc/frame và thời gian tải đầu.

## 11. Lộ trình đề xuất

### Milestone A - Vertical slice cửa hàng

- Nhập asset food/store đã chọn.
- Thay mọi placeholder không đúng nghĩa bằng model thật.
- Hoàn thiện collider, camera interior, cashier và item placement.
- Nhập animation run/jump/pickup/cashier/listening.
- Gắn voice MP3 cho một scenario hoàn chỉnh.
- Playtest từ menu đến thanh toán và kết quả.

### Milestone B - E-Learning production

- Chuẩn hóa curriculum N5 -> N4.
- Mỗi scenario khai báo difficulty, learning target và reward.
- Thêm prerequisite Knowledge/mastery.
- Dashboard tiến bộ theo kỹ năng.
- Kiểm tra chống farm điểm và migration save.

### Milestone C - Nội dung 3 giờ

- Hoàn thiện các zone ramen, station và summer festival trong gameplay scene.
- Viết/QA đủ dialogue, objective và voice.
- Thêm checkpoint, fail/retry và chapter transition.
- Đo thời gian chơi thật thay vì ước lượng từ số node.

### Milestone D - WebGL/mobile

- Dựng prefab joystick/action button responsive.
- Build WebGL development và profile.
- Tối ưu texture/audio/build stripping.
- Test Chrome, Edge, Safari và Android.
- Bổ sung safe area, touch camera và accessibility.

### Milestone E - Combat/farming/fantasy

- Item catalog và equipment trước.
- Combat state machine, hitbox, damage và enemy pooling.
- Farming grid/growth theo timestamp, không Update từng cây.
- Pet/animal AI theo state và distance tick.
- Tiên linh/quái vật dùng cùng combat/AI framework, không viết hệ riêng.

## 12. Tiêu chí để gọi là hoàn thiện

Một tính năng chỉ được đánh dấu hoàn thiện khi đáp ứng đủ:

- Compile không lỗi.
- Play Mode không phát sinh exception/warning lặp.
- Có asset thật hoặc trạng thái thiếu asset rõ ràng; không dùng hình khối giả gây hiểu nhầm.
- UI không overlap ở 16:9, 16:10, ultrawide và mobile portrait/landscape liên quan.
- Save/load và Guest behavior đúng.
- Có kiểm thử đường đi thành công, thất bại và thao tác bất thường.
- Đạt ngân sách hiệu năng trên platform mục tiêu.
- Có checklist nghiệm thu hoặc automated test cho logic quan trọng.

## 13. Tài liệu liên quan

- Danh sách asset còn thiếu: `Assets/NihongoLife/MISSING_ASSETS.md`
- Quy ước voice: `Assets/NihongoLife/Resources/Voice/README.md`
- Gameplay scene chính: `Assets/NihongoLife/Scenes/90_TestSandbox.unity`
- Scenario asset: `Assets/NihongoLife/Resources/Scenarios`
- Scenario runtime bổ sung: `Assets/NihongoLife/Scripts/Scenario/BuiltInStoryScenarioCatalog.cs`

## 14. Nhận xét tổng thể

Điểm mạnh hiện tại là project đã có nhiều hệ thống nối với nhau: auth, save, scenario, score, mastery, status, inventory, input và online foundation. Đây không còn là bản demo UI đơn lẻ.

Điểm yếu lớn nhất là khoảng cách giữa **hệ thống đã code** và **trải nghiệm đã được sản xuất/kiểm thử**. Cách tiến nhanh nhất không phải thêm nhiều feature cùng lúc, mà hoàn thiện một vertical slice cửa hàng thật sạch: asset đúng, animation đủ, voice thật, UI ổn định, save đúng và profiler đạt mục tiêu. Sau khi slice này đạt chuẩn, những khu ramen, ga tàu, farming và fantasy có thể tái sử dụng cùng framework mà không làm project phình mất kiểm soát.
