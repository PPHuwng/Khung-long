# Nộp DINO CITY: Unity, Windows và Android

Ba tệp cần nộp là:

| Tệp | Nội dung và cách dùng |
|---|---|
| `DINO-CITY-Unity.zip` | Project để mở trong Unity Hub, có `Assets`, `Packages`, `ProjectSettings` và các tệp `.meta`. |
| `DINO-CITY-Windows.zip` | Bản Windows 64-bit. Giải nén toàn bộ rồi chạy `DinoCity.exe`; giữ DLL, thư mục Data và các tệp đi kèm. |
| `DINO-CITY-Android.apk` | Bản Android ARM64, cài trực tiếp trên điện thoại để thử/nộp bài. |

Mã nguồn và bộ xuất nằm trên nhánh [codex/dino-city-runner](https://github.com/PPHuwng/Khung-long/tree/codex/dino-city-runner). Thư mục `Builds` không đưa vào Git vì bản dựng lớn; ba tệp hoàn chỉnh được tải lên **GitHub Releases** bằng lệnh bên dưới.

Môi trường đám mây hiện không có Unity Editor/giấy phép và module build, nên chưa tạo được `.exe`/`.apk` thực tế. Bộ xuất gọi Unity thật trên máy Windows của bạn. Kiểm tra đóng gói ở máy đám mây không thay cho việc build/chạy thử game.

## Chuẩn bị một lần

1. Lấy bản source mới từ nhánh trên. Nếu đang có project chạy tốt, chép đè `Assets/CityDino`, chép thêm thư mục `scripts` và `BUILDING.md` vào project đó; giữ `Packages` và `ProjectSettings` đang dùng.
2. Trong **Unity Hub → Installs**, dùng bản Editor ghi ở `ProjectSettings/ProjectVersion.txt` (hiện là **6000.4.0f1**).
3. Bấm dấu bánh răng/ba chấm cạnh Editor → **Add modules**, cài **Android Build Support**, gồm cả **Android SDK & NDK Tools** và **OpenJDK**. Windows 64-bit dùng backend Mono của Editor Windows. Nếu Hub báo thiếu Windows Build Support, cài module đó nữa.
4. Mở scene `Assets/CityDino/Scenes/CityRunner.unity` và thử Play. Chờ import/compile xong, lưu project rồi **đóng Unity Editor** trước khi chạy script.

## Xuất cả ba gói bằng một lệnh

Mở thư mục project trong File Explorer, gõ `powershell` vào thanh địa chỉ rồi Enter. Chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-submission.ps1
```

Script tìm Editor theo đúng `ProjectVersion.txt`, build Windows trước rồi Android trong hai tiến trình riêng, cuối cùng đóng gói source. Không tự đóng hoặc tắt tiến trình Unity đang mở. Build Android/IL2CPP lần đầu có thể mất nhiều phút.

Nếu Unity được cài ở ổ khác, chỉ rõ đường dẫn thật tới `Unity.exe`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-submission.ps1 -UnityPath "D:\Unity\6000.4.0f1\Editor\Unity.exe"
```

Khi **cả hai build thành công và đủ tệp**, kết quả ở:

```text
Builds/Submission/
  DINO-CITY-Unity.zip
  DINO-CITY-Windows.zip
  DINO-CITY-Android.apk
  SHA256SUMS.txt
  SUBMISSION-README.txt
  windows-build.log
  android-build.log
```

`SHA256SUMS.txt` chỉ được tạo cho bộ ba hoàn chỉnh. Lỗi build trả mã lỗi, có log và không báo đã tạo bản Windows/Android khi mới chỉ có source.

Nếu chỉ cần ZIP project, không cần Unity/license:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-submission.ps1 -SourceOnly
```

Lệnh này chỉ tạo source và `SHA256SUMS-Unity.txt`, không tạo hoặc giả lập EXE/APK.

## Đưa đủ ba gói lên GitHub

Cài [GitHub CLI](https://cli.github.com/) và đăng nhập tài khoản có quyền với repository:

```powershell
gh auth login
```

Build và tải lên một release mới:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-submission.ps1 -Publish
```

Nếu đã build đủ ba gói, tải lên mà không build lại:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-submission.ps1 -SkipBuild -Publish
```

Script kiểm tra đủ tệp và đối chiếu SHA256 trước khi tải. Nó dùng phiên đăng nhập GitHub CLI trên máy bạn, không chứa token/mật khẩu. Tên release có thời gian để không ghi đè bản cũ. Liên kết tải được in ra sau khi upload; xem tại [GitHub Releases](https://github.com/PPHuwng/Khung-long/releases).

## Nếu muốn bấm trực tiếp trong Unity

Menu **City Dino → Build Submission** có:

1. **Export Unity project ZIP**.
2. **Build Windows 64-bit ZIP** — trước đó vào **File → Build Profiles**, chọn Windows, Intel64-bit và kích hoạt profile.
3. **Build Android APK** — thêm/chọn profile Android rồi kích hoạt. Cần đủ SDK/NDK/OpenJDK từ Unity Hub.
4. **Open submission folder**.

Các menu tạo từng gói; script PowerShell là đường tự động kiểm tra cả ba và xuất checksum/upload. Dừng Play Mode trước khi bấm. Nếu chưa kích hoạt đúng nền tảng hoặc thiếu module, menu sẽ chỉ rõ lỗi.

## Cấu hình bản dựng

- Chỉ scene `CityRunner` được đưa vào player; source vẫn giữ các scene cũ.
- Windows dùng Mono, Intel64-bit, bản Player thông thường và không bật Development Build.
- Android dùng IL2CPP, ARM64, xuất APK trực tiếp, tắt AAB/Gradle-project export và OBB để không phải nộp thêm tệp dữ liệu. Dùng chữ ký debug/local của Unity để cài thử/nộp bài; không phải bản ký phát hành lên Play Store.
- Tên game lúc build là `DINO CITY`, Android ID `com.pphuwng.dinocity`. Bộ xuất khôi phục các setting đã thay của project sau mỗi build, kể cả khi lỗi.
- Giữ cấu hình Input Handling của project đang chạy. Project dùng Both cho menu IMGUI; nếu Android hiện cảnh báo Input Handling, cần kiểm tra menu và cảm ứng trên máy thật trước khi nộp, không tự chuyển tùy tiện làm hỏng menu.

## Thử trước khi nộp

Windows: giải nén ZIP sang một thư mục mới, chạy EXE ở đó để kiểm tra không thiếu DLL/Data. Android: chép APK sang điện thoại ARM64, cài từ trình quản lý tệp và thử ở màn hình ngang; cho phép trình quản lý tệp cài ứng dụng nếu Android hỏi. Thử menu, bắt đầu/chơi lại, nhiều ngón cùng di chuyển/nhảy/bắn, âm thanh, lưu ví/kỷ lục, pause và trận boss. Không cần cài Unity trên máy chỉ dùng để chơi Windows hoặc Android.

Nếu script lỗi, xem cuối `windows-build.log`/`android-build.log`. Lỗi thiếu module, SDK/NDK/JDK hoặc Unity license cần xử lý trong Unity Hub. Mở đúng project trong Editor và sửa hết lỗi Console trước khi build lại; không sửa scene/package chỉ để che lỗi.

## Kiểm tra công cụ xuất ngoài Unity

```bash
./scripts/test-core.sh --submission
```

Bộ kiểm tra dùng API stubs và output giả chỉ trong thư mục test tạm để kiểm tra ZIP, dữ liệu đi kèm, mã lỗi và khôi phục setting. Không tạo player Unity thật, không kiểm tra FPS/Android ký/chạy thiết bị.
