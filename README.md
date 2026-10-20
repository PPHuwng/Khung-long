# Dino City — Khủng long chạy trong thành phố

Game Unity 2D dùng hình ảnh từ `Assets.zip`: khủng long chạy vô tận, nhảy qua nhà/vòi cứu hoả, gom sao và mở thêm mạng khởi đầu. Có menu tiếng Việt, cửa hàng, lịch sử thành tích và màn hình kết thúc lượt.

## Mở và chơi

1. Cài **Unity 6.0 LTS** qua Unity Hub. Project khai báo bản **6000.0.32f1**; nên mở bằng bản này trước để tránh thay đổi định dạng asset.
2. Trong Unity Hub chọn **Add → Add project from disk**, chọn thư mục `Khung-long` chứa `Assets`, `Packages`, `ProjectSettings`.
3. Chờ Unity Package Manager tải các package trong `Packages/manifest.json` (URP, Input System, UGUI và Test Framework).
4. Mở `Assets/CityDino/Scenes/CityRunner.unity`, hoặc menu **City Dino → Open City Runner Scene**. Nhấn **Play**.
5. Đặt Game View **16:9**, ví dụ **1280 × 720**. Giao diện tự co theo cửa sổ; trên điện thoại dùng màn hình ngang.

Scene mới đã được thêm vào Build Settings. Nếu mở `Assets/Scenes/SampleScene.unity` cũ và nhấn Play, bootstrap cũng chuyển sang game mới; không phải gán prefab hoặc UI thủ công.

| Thao tác | Bàn phím | Chuột / cảm ứng |
|---|---|---|
| Bắt đầu | Space | Nút Bắt đầu chạy |
| Nhảy | Space / ↑ / W | Chạm khu vực chơi hoặc nút NHẢY |
| Nhảy kép | Nhấn nhảy lần nữa khi ở trên không | Chạm lần nữa |
| Cúi tránh máy bay | Giữ ↓ / S | Giữ nút CÚI |
| Tạm dừng / tiếp tục | Esc | Nút II / Tiếp tục |
| Chơi lại sau khi chết | R | Nút Chạy lại |
| Bật / tắt hiệu ứng âm thanh | M | Nút nhạc |

Mất tiêu điểm cửa sổ sẽ tự tạm dừng. Các nút cửa hàng và thành tích chỉ nhận tương tác trong cửa sổ đang mở, không bấm xuyên xuống menu.

## Luật chơi đã thực hiện

- **Chạy vô tận:** vật cản được sinh liên tục bằng lựa chọn ngẫu nhiên, có khoảng cách tăng theo tốc độ để vẫn có thời gian tiếp đất và nhảy tiếp. Nhà có 3 hình; vòi cứu hoả thấp; từ chặng 3 có máy bay thấp cần cúi hoặc nhảy qua.
- **Sao:** mỗi sao cộng ngay 1 vào ví và bộ đếm lượt. Sao vẫn giữ khi hết mạng. Có hàng sao hướng dẫn và cung sao trên vật cản/hố.
- **Nâng cấp vĩnh viễn:** bắt đầu với 1 mạng; mua lần lượt **40 → 100 → 180 sao** để có 2, 3, rồi 4 mạng ở mỗi lượt sau. Không dùng được khi đang chạy.
- **Tim hiếm:** lần đầu sinh sau khoảng 20–30 giây; các lần tiếp theo cách nhau ngẫu nhiên **18–32 giây**, không tạo thành hàng liên tục. Nhặt hồi 1 mạng, tối đa 5; khi đầy thì đổi thành 1 sao.
- **Va chạm:** trừ 1 mạng. Nếu còn mạng, có 2 giây miễn sát thương và nhấp nháy. Hết mạng thì mô phỏng dừng, hiện kết quả và nút chơi lại/menu.
- **Rơi xuống hố:** trừ 1 mạng và hồi về mặt đường nếu còn mạng, xoá hố sát nhân vật để không rơi lặp. Nếu hết mạng thì dừng hẳn. Không có chuyện rơi vô hạn sau khi chết.
- **Điểm và thành tích:** 20 đơn vị thế giới = 1 mét = 1 điểm. Lưu kỷ lục, điểm lượt trước và 10 lượt gần nhất; giao diện thành tích hiển thị 5 lượt mới nhất.
- **Tăng tốc nhẹ:** bắt đầu 250 đơn vị/giây, tăng 1,1 mỗi giây, giới hạn 430 (~1,72× tốc độ đầu). Không tăng vô hạn.
- **Thay cảnh mỗi 35 giây:** bình minh → mưa → hoàng hôn → đêm → thành phố trên mây → giông bão; sau vòng đầu chọn cảnh ngẫu nhiên, không lặp cảnh ngay trước. Đường trên mây có hố ngẫu nhiên và vạch cảnh báo; các chặng sau có nhà cao hơn/máy bay. Màu trời, nền, mưa, mặt trăng và ánh sáng chuyển theo cảnh.
- **Âm thanh:** hiệu ứng nhảy, nhặt sao, va chạm và nâng cấp được tạo tại runtime; có nút tắt/bật.

Dữ liệu dùng Unity `PlayerPrefs` với khoá `CityDino.Save.v1`. Kỷ lục `HighScore` của bản cũ được chuyển sang hồ sơ mới. Ví, nâng cấp và lịch sử lưu trên **thiết bị hiện tại**, không phải tài khoản trực tuyến. Tắt tiếng dùng `CityDino.Muted`.

## Cấu trúc và lỗi bản cũ

- `Assets/CityDino/Core/RunnerSession.cs`: luật chơi C# thuần, trạng thái menu/chạy/tạm dừng/kết thúc, spawn, nhảy/va chạm và dữ liệu lưu.
- `Assets/CityDino/Runtime/CityRunnerController.cs`: đọc phím/cảm ứng, menu/HUD/cửa hàng/thành tích, PlayerPrefs và âm thanh.
- `Assets/CityDino/Runtime/RunnerWorldView.cs`: vẽ thế giới, ảnh nhân vật, parallax, đường, thời tiết và hố.
- `Assets/CityDino/Resources/CityDino`: bản sao các ảnh đã bỏ viền alpha trống, 5 frame khủng long cùng khung và font hỗ trợ tiếng Việt.
- `Assets/CityDino/Scenes/CityRunner.unity`: scene khởi động.
- `Assets/Script`, `Assets/Scenes`, sprite, prefab và tài liệu trong bản zip gốc được giữ lại để đối chiếu.

Trong bản cũ, `Player` chỉ gọi chết khi va chạm vật cản, chưa có giới hạn rơi; hai đoạn đường dùng `roadLength` khác nhau; hai script nền cùng cuộn một đối tượng. Bản mới dùng chung một hệ toạ độ và một mô phỏng cho mặt đường/vật cản. Hình đường chỉ cuộn để hiển thị, không kéo collider đứng dưới chân nhân vật. Hố được thể hiện và kiểm tra từ cùng dữ liệu; tử vong chỉ kết thúc và lưu lượt một lần.

## Kiểm thử

### Kiểm tra luật chơi trên máy có .NET SDK 8

```bash
./scripts/test-core.sh
```

Hoặc trên Windows / máy có .NET SDK:

```bash
dotnet run --project Tests/RunnerCore.Tests.csproj --configuration Release
```

Bộ kiểm tra chạy trực tiếp mã `RunnerSession.cs`, không cần Unity hay package NuGet. Nó kiểm tra nhảy/nhảy kép/cúi, va chạm và bất tử, rơi/hồi vị trí/chết dừng, sao/ví/nâng cấp, lưu thành tích, tạm dừng, giới hạn tốc độ, thời tiết, khoảng cách sinh và độ hiếm tim. Mã trong `Tests` nằm ngoài `Assets`, nên Unity không đưa bộ kiểm tra console vào bản game.

Kết quả trên máy đám mây: **23/23 kiểm tra hành vi đạt**. `./scripts/test-core.sh --compile-unity` biên dịch lớp runtime mới và 7 script cũ với stub API: **0 lỗi**, 4 cảnh báo từ các trường Inspector/không dùng trong script cũ. Đã kiểm tra tài nguyên, GUID, liên kết scene, font tiếng Việt và xác nhận 172 tệp gốc trong zip vẫn nguyên vẹn.

### Cần xác nhận bằng Unity Editor

Môi trường đám mây hiện không có Unity Editor. Vì vậy chưa chạy Play Mode, kiểm tra hình ảnh, touch trên thiết bị thật hoặc xuất bản Windows/Android/WebGL. Kiểm tra C# với API giả lập nếu có chỉ bắt lỗi tích hợp cú pháp, không thay thế bước này.

Checklist khi mở Unity:

1. Package Manager hoàn tất, Console không có lỗi biên dịch hoặc missing script; mở scene mới và nhấn Play.
2. Menu hiển thị tiếng Việt và hình khủng long; Play vào game, nhảy qua vòi cứu hoả/nhà, nhặt sao.
3. Va chạm khi còn 1 mạng: hiện kết quả và điểm không tăng thêm. Chơi lại: điểm/sao lượt về 0, ví/kỷ lục giữ nguyên.
4. Đủ 40 sao: mở cửa hàng, mua nâng cấp; lượt sau bắt đầu với 2 mạng. Đóng/mở game: ví/nâng cấp/kỷ lục giữ nguyên.
5. Va chạm khi còn mạng: chỉ mất 1 mạng, miễn sát thương 2 giây. Tạm dừng: mọi vật thể và điểm đứng yên; tiếp tục không nhảy thời gian.
6. Chạy đủ lâu để đổi cảnh; trên mây có hố thật. Bỏ lỡ hố khi còn nhiều mạng: hồi an toàn, không rơi lặp. Máy bay: giữ CÚI đi qua.
7. Thử chuột/cảm ứng ở tỷ lệ màn hình khác nhau, các nút không chồng và không bấm xuyên cửa sổ cửa hàng.

Các hằng số cân bằng chính (`InitialSpeed`, `SpeedAcceleration`, `MaximumSpeed`, `JumpVelocity`, `DistrictDuration`) ở đầu `RunnerSession.cs`. Game này là bản chạy vô tận hoàn chỉnh ở mức cơ chế; chưa có máy chủ, đồng bộ tài khoản hay bảng xếp hạng trực tuyến.
