# Video không nằm trong git

Trailer, teaser, demo và khung hình thô để ở máy quay:

`D:/VTC_Academy/NihongoLife/_work/marketing/`

Gợi ý thư mục con: `frames_trailer/`, `frames_demo/`, `xuat/`. Mp4 đã xem hết có thể copy sang `Bao_Cao/marketing/` khi anh Long đồng ý, nếu file không quá nặng với git. Bản gốc vẫn ở ổ D.

## Chưa quay trong đợt làm bộ kit

Máy dựng bộ chữ và ảnh này không chạy Unity, không có ổ D. Không có `NihongoLife_Trailer_v4`, teaser 30 giây, teaser dọc 15 giây, hay demo gameplay v3 trong commit này.

Khi quay trên máy có Unity:

1. Đóng Unity Editor trước batch.
2. Sửa shot trong `Assets/NihongoLife/TrailerReel/TrailerReel.cs` nếu cần. Không sửa gameplay.
3. `Trailer_RecordFrames` với `NL_FRAME_DIR` trỏ vào ổ D.
4. `Demo_RecordFrames` với `NL_DEMO_DIR` và file lời `NL_DEMO_VO`.
5. Nhạc chỉ lấy bài CC0 trong `NihongoLife/Docs/AUDIO_CREDITS.md`. Ghi nguồn vào `Bao_Cao/marketing/CREDITS.md`.
6. Xem contact sheet trước khi đưa link cho Huy đăng.

Danh sách cảnh từng bài: file `CAN_QUAY.md` trong thư mục bài. Bài đảo Midori chưa có ảnh pixel trong bản checkout này (file ảnh đảo là con trỏ Git LFS), nên không được thế bằng cảnh phố.
