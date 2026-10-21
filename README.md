# DINO CITY

Game Unity 2D dùng hình ảnh từ `Assets.zip` và bộ nền/vật cản pixel art mới: khủng long chạy vô tận, di chuyển trái/phải, nhảy qua các cụm bẫy, gom sao và mua tim cho từng lượt. Có menu tiếng Việt, cửa hàng, lịch sử thành tích và màn hình kết thúc lượt.

## Mở và chơi

1. Cài đúng **Unity 6000.4.0f1** qua Unity Hub, theo `ProjectSettings/ProjectVersion.txt` gốc của bạn (revision `8cf496087c8f`).
2. Trong Unity Hub chọn **Add → Add project from disk**, chọn thư mục `Khung-long` chứa `Assets`, `Packages`, `ProjectSettings`.
3. Chờ Unity Package Manager tải các package theo `Packages/manifest.json` và `Packages/packages-lock.json` gốc: URP **17.4.0**, Input System **1.19.0**, UGUI **2.0.0**, Test Framework **1.6.0** cùng các gói 2D của dự án cũ.
4. Mở `Assets/CityDino/Scenes/CityRunner.unity`, hoặc menu **City Dino → Open City Runner Scene**. Nhấn **Play**.
5. Đặt Game View **16:9**, ví dụ **1280 × 720**. Giao diện tự co theo cửa sổ; trên điện thoại dùng màn hình ngang.

Scene mới đã được thêm vào Build Settings. Nếu mở `Assets/Scenes/SampleScene.unity` cũ và nhấn Play, bootstrap cũng chuyển sang game mới; không phải gán prefab hoặc UI thủ công.

Project giữ cấu hình gốc từ `ProjectSettings.zip` và `Packages.zip`: tên/định danh ứng dụng, layer Ground/Obstacle, Input System actions, đồ họa URP theo Quality, âm thanh và vật lý. Thay đổi trong Build Settings là thêm `CityRunner.unity` ở đầu danh sách, giữ scene cũ và cấu hình Input System. Cấu hình package có 52 phụ thuộc trực tiếp và file khóa 65 package, được giữ nguyên từ bản bạn gửi.

### Cập nhật project đang chạy được

Đóng Unity, tải bản ZIP mới từ nhánh `codex/dino-city-runner`, rồi chép thư mục **`Assets/CityDino`** trong bản mới vào đúng vị trí đó trong project đang dùng; chọn thay thế các tệp trùng tên. Mở lại project và chờ Unity import ảnh/script mới. Menu hiện **BẢN 04** khi đã cập nhật. Các thay đổi gameplay lần này nằm trong thư mục đó; dùng tiếp `Packages` và `ProjectSettings` đang chạy được trên máy bạn. Khoá lưu `CityDino.Save.v1` và định danh ứng dụng được giữ nguyên để tiếp tục ví sao và kỷ lục trên cùng thiết bị. Chi phí nâng cấp vĩnh viễn cũ được hoàn lại vào ví một lần khi chuyển sang cơ chế mua tim dùng cho từng lượt.

| Thao tác | Bàn phím | Chuột / cảm ứng |
|---|---|---|
| Bắt đầu | Space | Nút Bắt đầu chạy |
| Di chuyển trái / phải | Giữ A / D hoặc ← / → | Giữ nút ← / → ở góc dưới trái |
| Nhảy | Space / ↑ / W | Chạm khu vực chơi hoặc nút NHẢY |
| Nhảy kép | Nhấn nhảy lần nữa khi ở trên không | Chạm lần nữa |
| Cúi tránh máy bay | Giữ ↓ / S | Giữ nút CÚI |
| Tạm dừng / tiếp tục | Esc | Nút II / Tiếp tục |
| Chơi lại sau khi chết | R | Nút Chạy lại |
| Bật / tắt hiệu ứng âm thanh | M | Nút nhạc |

Mất tiêu điểm cửa sổ sẽ tự tạm dừng. Các nút cửa hàng và thành tích chỉ nhận tương tác trong cửa sổ đang mở, không bấm xuyên xuống menu.

Cảm ứng nhận nhiều ngón: có thể giữ hướng bằng một ngón và nhảy bằng ngón khác. Nhấn nút di chuyển chỉ đổi vị trí, không kích hoạt nhảy. Nhân vật di chuyển trong vùng X100–720 trên màn hình ảo để còn khoảng nhìn thấy bẫy phía trước; màn chơi vẫn tự cuộn.

## Luật chơi đã thực hiện

- **Chạy vô tận:** 13 mẫu cụm bẫy gồm nhà, vòi cứu hoả, gai đơn/đôi/ba, rào chắn, rào bậc, cưa dưới đất, cưa bay, máy bay thấp, hố trên mây, cưa tuần tra ngang và rào chắn di chuyển. Các mẫu được xáo trộn, tránh lặp ngay; bẫy nâng cao mở sau 15/30 giây và cao thêm sau 60 giây. Cụm được thiết kế cho cùng một nhịp nhảy/cúi, khoảng cách phục hồi tính cả tốc độ cuộn và khả năng di chuyển sang phải. Cưa quay có va chạm hình tròn, gai có va chạm tam giác; cưa bay dao động lên/xuống rõ hơn, đi qua bằng cách cúi hoặc nhảy.
- **Sao:** mỗi sao cộng ngay 1 vào ví và bộ đếm lượt. Sao vẫn giữ khi hết mạng. Có hàng sao hướng dẫn và cung sao trên vật cản/hố.
- **Mua tim theo lượt:** mỗi tim giá **25 sao**, mua tối đa **3 tim** cho lượt kế tiếp. Mặc định 1 mạng; bắt đầu chạy sẽ dùng hết tim đã mua để có tối đa 4 mạng khởi đầu. Chơi lại cần mua lại; tim đã mua nhưng chưa dùng được lưu khi đóng game. Mua chỉ được ở menu/kết thúc lượt. Hồ sơ cũ được hoàn lại tổng chi phí nâng cấp **40/140/320 sao** theo cấp cũ, đúng một lần; ví và thành tích vẫn giữ.
- **Tim hiếm:** lần đầu sinh sau khoảng 20–30 giây; các lần tiếp theo cách nhau ngẫu nhiên **18–32 giây**, không tạo thành hàng liên tục. Nhặt hồi 1 mạng, tối đa 5; khi đầy thì đổi thành 1 sao.
- **Va chạm:** trừ 1 mạng. Nếu còn mạng, có 2 giây miễn sát thương và nhấp nháy. Hết mạng thì mô phỏng dừng, hiện kết quả và nút chơi lại/menu.
- **Va chạm cưa:** cưa kiểm tra theo đường bao khủng long của cả 5 khung hình, tính cả hướng quay mặt, cúi và độ nhún. Không dùng hình chữ nhật chứa khoảng trống sau cổ nữa; đầu và đuôi cũng được tính theo ảnh. Vòng cưa tính gần tới đầu răng, chừa viền nhỏ để dễ chơi. Tư thế lúc chết được giữ lại để hình không đổi đột ngột sau va chạm. Trong 2 giây bất tử sau khi mất mạng, chạm cưa vẫn không mất thêm mạng như luật cũ.
- **Sửa lỗi vẽ cưa bản 04:** ảnh cưa và collider dùng cùng tâm xoay trong toạ độ màn chơi. Phép xoay được ghép trước scale/letterbox, thay cho `GUIUtility.RotateAroundPivot` làm ảnh cưa quay lệch khỏi collider ở cửa sổ Game phóng/thu. Hàm vẽ đường/mưa cũng dùng phép biến đổi này. Bỏ nhãn CÚI/NHẢY nổi trên vật cản.
- **Bẫy di chuyển:** từ 30 giây có cưa tuần tra ngang ±32 đơn vị và rào chắn ngang ±24 đơn vị; cưa bay dao động dọc ±16, đáy từ 56–88 nên vẫn có làn cúi. Vị trí mô phỏng thật được dùng chung cho vẽ/va chạm, dừng khi pause. Khoảng sinh tính cả toàn bộ vùng chuyển động để giữ khoảng phục hồi giữa các cụm.
- **Rơi xuống hố:** trừ 1 mạng và hồi về mặt đường nếu còn mạng, xoá hố sát nhân vật để không rơi lặp. Nếu hết mạng thì dừng hẳn. Không có chuyện rơi vô hạn sau khi chết.
- **Điểm và thành tích:** 20 đơn vị thế giới = 1 mét = 1 điểm. Lưu kỷ lục, điểm lượt trước và 10 lượt gần nhất; giao diện thành tích hiển thị 5 lượt mới nhất.
- **Tăng tốc:** bắt đầu 360 đơn vị/giây (**nhanh hơn 44%** so với bản đầu), tăng 1,6 mỗi giây, giới hạn 610 (~1,69× tốc độ đầu). Tốc độ vẫn tăng từ từ và có giới hạn.
- **Nền mới:** hai ảnh skyline trong suốt được tạo riêng, ghép với lớp thành phố xa, trời/thời tiết và vệt sáng mặt đường. Các lớp cuộn ở tốc độ khác nhau để tạo chiều sâu; hình nền không có mặt đường vẽ sẵn che lên hố. Viền nối dùng tile phản chiếu, giữ tỉ lệ hình và căn đáy nhà vào chân trời.
- **Chọn map đầu:** mặc định **Ngẫu nhiên mỗi lượt**, chọn trong cả 6 map. Có thể bấm một map cụ thể trên menu để lượt nào cũng bắt đầu tại đó. Lựa chọn được lưu trên thiết bị, áp dụng cả nút Chạy lại/R; chọn Ngẫu nhiên thì mỗi lượt bốc lại, vẫn có thể trùng kết quả lượt trước.
- **Thay cảnh mỗi 35 giây:** từ lần chuyển đầu tiên, luôn chọn ngẫu nhiên một trong 5 map khác map đang chạy. Không còn vòng đầu cố định. Màu trời, nền, mưa, mặt trăng và ánh sáng chuyển theo cảnh; mỗi map ưu tiên một số cụm bẫy riêng, vẫn tuân theo thời gian mở khoá 15/30/60 giây và khoảng cách hồi phục chung.
- **Tối hoàn toàn:** Phố đêm và Giông bão có các đợt đen kín màn chơi trong **2–3 giây**, thời lượng và khoảng nghỉ đều ngẫu nhiên. Lần đầu sau 8–13 giây ở Phố đêm hoặc 7–11 giây ở Giông bão; các khoảng nghỉ sau cũng trong khoảng đó. Có báo trước 1,5 giây và fade 0,18 giây ở mỗi đầu, ngoài thời gian đen hoàn toàn. Trong lúc tối chỉ HUD/nút điều khiển còn hiện. Pause dừng đồng hồ; chuyển map/chơi lại đặt lại lịch tối.
- **Giao diện gọn hơn:** tiêu đề DINO CITY; bỏ thông báo giữa màn khi bắt đầu/đổi map/nhặt tim/mất mạng và chữ +SAO. Giữ HUD, kết quả lượt và một cảnh báo trước đợt tối; thời gian bảo vệ sau va chạm hiện nhỏ cạnh quãng đường.
- **Âm thanh:** hiệu ứng nhảy, nhặt sao, va chạm và mua tim được tạo tại runtime; có nút tắt/bật.

| Map | Thử thách riêng |
|---|---|
| Bình minh | Ưu tiên nhà và vòi cứu hoả, phù hợp tập nhịp nhảy. |
| Phố mưa | Ưu tiên rào chắn và máy bay thấp khi đã mở khoá; chú ý đổi nhảy/cúi. Có vũng nước phản chiếu. |
| Nắng chiều | Ưu tiên gai đôi/ba và rào bậc; cần canh nhịp vượt cụm. |
| Phố đêm | Ưu tiên cưa đất/cưa bay, thêm các đợt mất sáng ngắn. |
| Thành phố trên mây | Có hố thật và vạch cảnh báo; nếu chọn làm map đầu thì 10 giây đầu chưa sinh hố. Có vệt gió trên nền. |
| Giông bão | Ưu tiên cưa, gai đôi và máy bay; mưa dày và mất sáng ngắn thường hơn. |

Vũng nước và gió là hiệu ứng nền; không kéo nhân vật hoặc làm trượt nút điều khiển. Mỗi map vẫn có thể sinh các mẫu bẫy chung để lượt chạy đa dạng.

Dữ liệu dùng Unity `PlayerPrefs` với khoá `CityDino.Save.v1`. Kỷ lục `HighScore` của bản cũ được chuyển sang hồ sơ mới. Ví, tim đã mua và lịch sử lưu trên **thiết bị hiện tại**, không phải tài khoản trực tuyến. Tắt tiếng dùng `CityDino.Muted`; lựa chọn map đầu dùng `CityDino.StartDistrict`.

## Cấu trúc và lỗi bản cũ

- `Assets/CityDino/Core/RunnerSession.cs`: luật chơi C# thuần, trạng thái menu/chạy/tạm dừng/kết thúc, spawn, nhảy/va chạm và dữ liệu lưu.
- `Assets/CityDino/Core/RunnerGeometry.cs`: kích thước/khung hình nhân vật dùng chung cho vẽ và va chạm cưa; đường bao alpha từ ảnh khủng long gốc.
- `Assets/CityDino/Runtime/CityRunnerController.cs`: đọc phím/cảm ứng, menu/HUD/cửa hàng/thành tích, PlayerPrefs và âm thanh.
- `Assets/CityDino/Runtime/RunnerWorldView.cs`: vẽ thế giới, ảnh nhân vật, parallax, đường, thời tiết và hố.
- `Assets/CityDino/Resources/CityDino`: các ảnh gốc, 5 frame khủng long cùng khung, font tiếng Việt, cùng `SkylineFar.png`, `SkylineNear.png` và `HazardAtlas.png` mới. Atlas có gai, cưa, rào chắn và khối điện; `RunnerArt.cs` lưu UV theo vùng alpha thật để hình khớp va chạm.
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

Bộ kiểm tra chạy trực tiếp mã `RunnerSession.cs`, không cần Unity hay package NuGet. Nó kiểm tra nhảy/nhảy kép/cúi, va chạm và bất tử, rơi/hồi vị trí/chết dừng, sao/ví/mua tim/hoàn tiền hồ sơ cũ, lưu thành tích, tạm dừng, giới hạn tốc độ, thời tiết, khoảng cách sinh và độ hiếm tim. Mã trong `Tests` nằm ngoài `Assets`, nên Unity không đưa bộ kiểm tra console vào bản game.

Kết quả kiểm tra và giới hạn của bộ kiểm thử được ghi tại `Tests/README.md`. Có thêm kiểm tra di chuyển, va chạm theo vị trí thật, hình dạng gai/cưa, các cụm bẫy, khoảng phục hồi và vùng nút cảm ứng. `./scripts/test-core.sh --compile-unity` biên dịch lớp runtime mới và 7 script cũ với stub API; `./scripts/test-core.sh --input-mapping` chạy kiểm tra ánh xạ toạ độ/đa chạm bằng thiết bị giả lập. `./scripts/test-core.sh --render-contracts` ghi nhận lệnh vẽ production với phép toán ma trận thật để kiểm tra cưa/đường đúng vị trí ở nhiều scale, bỏ nhãn và lớp phủ đen hoàn toàn. Các kiểm tra này không chạy Unity Editor. 172 tệp gốc trong Assets.zip được giữ nguyên.

### Cần xác nhận bằng Unity Editor

Môi trường đám mây hiện không có Unity Editor. Vì vậy chưa chạy Play Mode, kiểm tra hình ảnh, touch trên thiết bị thật hoặc xuất bản Windows/Android/WebGL. Kiểm tra C# với API giả lập nếu có chỉ bắt lỗi tích hợp cú pháp, không thay thế bước này.

Checklist khi mở Unity:

1. Package Manager hoàn tất, Console không có lỗi biên dịch hoặc missing script; mở scene mới và nhấn Play.
2. Menu hiển thị tiếng Việt và hình khủng long; Play vào game, giữ A/D hoặc ←/→ để đổi vị trí cả khi nhảy. Va chạm/nhặt sao theo đúng vị trí nhân vật. Nền có chiều sâu và vật cản nổi rõ.
3. Va chạm khi còn 1 mạng: hiện kết quả và điểm không tăng thêm. Chơi lại: điểm/sao lượt về 0, ví/kỷ lục giữ nguyên.
4. Đủ 25 sao: mua 1 tim, đóng/mở lại để kiểm tra tim chưa dùng vẫn còn. Bắt đầu với 2 mạng; lượt sau trở về 1 mạng nếu không mua lại. Hồ sơ cấp 1/2/3 cũ được hoàn 40/140/320 sao đúng một lần.
5. Va chạm khi còn mạng: chỉ mất 1 mạng, miễn sát thương 2 giây. Tạm dừng: mọi vật thể và điểm đứng yên; tiếp tục không nhảy thời gian.
6. Thử chọn từng map đầu và Ngẫu nhiên; Chạy lại dùng đúng lựa chọn, các chặng tiếp theo đổi ngẫu nhiên sau 35 giây. Trên mây có hố thật sau thời gian đầu làm quen; rơi khi còn mạng hồi an toàn. Máy bay: giữ CÚI đi qua. Map đêm/bão có báo trước rồi đen hoàn toàn 2–3 giây; HUD/nút vẫn hiện; tạm dừng không làm hiệu ứng chạy tiếp.
7. Thử chuột/cảm ứng ở tỷ lệ màn hình khác nhau: nút hướng không gây nhảy, giữ hướng bằng một ngón và nhảy bằng ngón khác, các nút không chồng và không bấm xuyên cửa sổ cửa hàng. Thử gai đôi/ba, cưa xoay/bay, rào chắn/khối điện và cụm nối tiếp ở tốc độ cao. Với cưa, thử sát sau cổ và đầu ở cả hai hướng, lúc nhảy/cúi; thay đổi scale cửa sổ Game và bật Maximize On Play để kiểm tra ảnh vẫn đúng vị trí va chạm. Thử cưa tuần tra/rào di chuyển; phân biệt lần chạm khi đang được bảo vệ.

Các hằng số cân bằng chính (`InitialSpeed`, `SpeedAcceleration`, `MaximumSpeed`, `JumpVelocity`, `DistrictDuration`) ở đầu `RunnerSession.cs`. Game này là bản chạy vô tận hoàn chỉnh ở mức cơ chế; chưa có máy chủ, đồng bộ tài khoản hay bảng xếp hạng trực tuyến.
