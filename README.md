# DINO CITY

Game Unity 2D chạy vô tận: khủng long di chuyển trái/phải, nhảy kép, cúi tránh bẫy, gom sao và mua vật phẩm. Có menu tiếng Việt, sáu map, cửa hàng, kỷ lục và lịch sử lượt chơi. Hình ảnh gốc từ `Assets.zip` được giữ lại cùng nền, bẫy, hoạt ảnh và vật phẩm pixel art mới.

## Mở và cập nhật

1. Dùng **Unity 6000.4.0f1**, revision `8cf496087c8f`, theo `ProjectSettings/ProjectVersion.txt` gốc.
2. Unity Hub → **Add → Add project from disk**, chọn thư mục chứa `Assets`, `Packages`, `ProjectSettings`.
3. Chờ Package Manager hoàn tất. Project giữ package gốc từ bản bạn gửi, gồm URP **17.4.0**, Input System **1.19.0**, UGUI **2.0.0**, Test Framework **1.6.0** và các gói 2D.
4. Mở `Assets/CityDino/Scenes/CityRunner.unity`, hoặc menu **City Dino → Open City Runner Scene**, rồi nhấn **Play**.
5. Chọn tab **Game**, tỷ lệ **16:9**, ví dụ 1280 × 720. Giao diện tự co theo cửa sổ; điện thoại dùng màn hình ngang.

**Nếu project trên máy đã chạy được:** đóng Unity, giải nén ZIP mới rồi chép và thay thế riêng thư mục **`Assets/CityDino`** vào project đang dùng. Giữ `Packages` và `ProjectSettings` đang chạy được. Mở lại, chờ import; menu phải hiện **BẢN 07**.

Scene mới đứng đầu Build Settings. Chạy `Assets/Scenes/SampleScene.unity` cũ cũng được bootstrap sang game mới, không cần gán prefab hoặc UI. Cấu hình gốc, định danh ứng dụng, layer và Input System actions được giữ lại. **Active Input Handling = Both** để các menu IMGUI hoạt động cùng Input System của gameplay.

| Thao tác | Bàn phím | Chuột / cảm ứng |
|---|---|---|
| Bắt đầu | Space | Bắt đầu chạy |
| Trái / phải | Giữ A / D hoặc ← / → | Giữ nút ← / → |
| Nhảy / nhảy kép | Space / ↑ / W, nhấn lần nữa trên không | Chạm khu vực chơi hoặc nút NHẢY |
| Cúi | Giữ ↓ / S | Giữ nút CÚI |
| Tạm dừng / tiếp tục | Esc | II / Tiếp tục |
| Chơi lại | R | Chạy lại |
| Bật / tắt âm thanh | M | Nút nhạc |

Mất tiêu điểm tự tạm dừng. Cảm ứng hỗ trợ nhiều ngón để giữ hướng và nhảy cùng lúc; nút hướng không kích hoạt nhảy. Cửa hàng/thành tích chặn thao tác bấm xuyên. Nhân vật di chuyển trong vùng X100–720 trên màn hình ảo, màn chơi vẫn tự cuộn.

## Sáu map và thử thách BẢN 07

Chọn **Ngẫu nhiên** hoặc một map khởi đầu ở menu. Mỗi chặng dài **35 giây**. Sáu map được xáo thành một lượt: đi hết sáu map trước khi xáo tiếp, không lặp ngay ở ranh giới hai lượt. Map được chọn chỉ cố định chặng đầu; lựa chọn được lưu và áp dụng cả khi Chạy lại.

| Map | Thử thách |
|---|---|
| Thành phố ban ngày | Nhà, vòi cứu hoả, gai và cưa tuần tra; lá bay, gió và nền nhiều lớp. |
| Phố tuyết | Thay cho Phố mưa cũ. Tuyết rơi, đường đóng băng; tăng tốc, phanh và đổi hướng có quán tính, trượt xa hơn trên không. Có băng nhọn nứt rồi rơi, cần tránh điểm rơi hoặc vượt mảnh băng đã đáp. Nhặt cốc nóng để giữ ấm. |
| Nắng gắt | Hơi nóng, bụi sáng, ưu tiên gai đôi/ba và rào bậc; khe phun nóng báo đỏ rồi phun theo chu kỳ, tắt có thể chạy qua. Nhặt chai nước để chống mất nước. |
| Đêm neon | Ưu tiên cưa; đen hoàn toàn **2–3 giây**, thời lượng và khoảng nghỉ ngẫu nhiên. Báo trước 1,5 giây và fade 0,18 giây mỗi đầu; HUD/nút vẫn hiện. |
| Đường trên mây | Nhiều hố đơn, hố đôi có đảo nhỏ và cầu nứt rồi sập thành hố thật. Gió từng đợt đẩy sang trái, mạnh hơn khi nhảy; có vệt gió để nhận biết. Chọn làm map đầu thì 10 giây đầu chưa có hố. |
| Giông bão | Mưa dày luôn nhìn thấy; nhịp tối → tia sét/chớp sáng → bình thường, kèm sấm mỗi **1,8 giây** từ giây thứ 1. Không dùng đợt đen kín kéo dài của Đêm neon. Có biển quảng cáo bị gió cuốn theo quỹ đạo ngang/lên xuống; ưu tiên cưa, gai và bẫy di chuyển. |

Tuyết/nắng có thanh **Giữ ấm / Nước** từ 100. Hai giây đầu khi vào map chưa hao; sau đó giảm lần lượt 7 / 6 mỗi giây. Đồ tiếp tế sinh cách nhau khoảng 3,7–4,6 giây, tránh vùng quét của bẫy dưới đất; cốc nóng hồi 45, nước hồi 40, tối đa 100. Cạn thanh mất một mạng; nếu còn sống, thanh hồi 45 để có thời gian tìm đồ. Vào map mới đặt lại thanh. Đồ dự trữ mua trước lượt tự dùng khi cạn, hồi lên 85.

Hố, cầu sập, hình vẽ và kiểm tra mặt đường dùng chung dữ liệu. Cầu có vết nứt tăng dần khi tiến đến gần, sập trước vùng điều khiển của nhân vật. Rơi mất một mạng và hồi an toàn nếu còn mạng; hết mạng dừng hẳn.

## Bẫy mới BẢN 07

- **Băng rơi:** xuất hiện ở Phố tuyết, treo phía trên với vết nứt, vụn băng và vạch đánh dấu điểm rơi. Sau cảnh báo, mảnh băng rơi theo vị trí thực rồi nằm trên mặt đường để nhảy qua. Dấu báo không gây sát thương; phần băng thật kiểm tra theo tam giác chúc xuống.
- **Khe phun nóng:** xuất hiện ở Nắng gắt. Miệng khe báo đỏ trước khi phun, có khoảng tắt an toàn giữa các đợt. Chỉ cột nóng đang hiện mới gây sát thương; vùng va chạm thu hẹp về ngọn và dùng đúng chiều cao đang vẽ, không tính cả hình chữ nhật trống.
- **Biển quảng cáo bị cuốn:** xuất hiện ở Giông bão, báo bằng dây/vệt gió và màu nhạt trước khi thành vật cản. Biển dao động ngang và lên/xuống, giữ hình nằm ngang cùng vùng va chạm để không tái hiện lỗi ảnh xoay lệch collider. Biên độ/tần số tăng dần theo thời gian như bẫy chuyển động khác.
- **Khoảng hồi phục:** tuyết giãn khoảng giữa các cụm theo ít nhất 1 giây tiếp cận để có thời gian phanh; bão giãn theo 0,9 giây. Các map khác giữ nhịp cũ; khoảng cách còn tính chiều rộng, vùng quét và gió. Không ghép ba bẫy mới với một hố hoặc bẫy bay trong cùng cụm.
- Nước/cốc nóng và powerup tránh cả vùng hoạt động tương lai của bẫy mới. Cảnh báo, hình và sát thương dùng chung trạng thái mô phỏng; pause đóng băng chúng, khiên và miễn thương vẫn áp dụng cho va chạm thật.

Ba loại mở sau **15 giây tổng thời gian chạy**. Khi vào map tương ứng, sau ít nhất 4 giây làm quen sẽ có bẫy đặc trưng trong một lượt sinh cụm bình thường; các lần sau tiếp tục xáo ngẫu nhiên. Để thử nhanh, chọn Tuyết/Nắng/Bão làm map đầu và chạy qua mốc 15 giây. Thời gian kích hoạt khe nóng/biển cuốn tăng nhịp theo tốc độ cuộn để chúng không đi qua nhân vật khi vẫn còn ở trạng thái báo trước trong lượt dài. Khe nóng có bốn nhịp chu kỳ để không luôn tắt ở cùng vị trí X. Va chạm ba bẫy mới kiểm tra theo đường bao thật của khung khủng long, tính cả đầu, đuôi, cổ trống, hướng nhìn và tư thế nhảy/cúi.

## Vật phẩm và cửa hàng

Mỗi sao cộng **1 sao vào ví** ngay, giữ cả khi chết. Vật phẩm hiếm trong lượt gồm **×2 điểm**, **nam châm hút sao** và **khiên một lần**. ×2 chỉ nhân điểm tích lũy khi hiệu lực, không nhân ví sao hoặc số mét. Nam châm hút sao trong bán kính 260. Thời lượng gốc ×2/nam châm là 8 giây; nhặt tiếp gia hạn tối đa gấp đôi thời lượng hiện tại, không thành ×4 điểm.

| Mua ở menu / sau khi chết | Giá sao | Cách dùng |
|---|---|---|
| Tim thứ 1 / 2 / 3 | **100 / 300 / 600** | Thêm mạng cho lượt kế tiếp; mặc định 1 mạng, mua tối đa 3 tim. |
| Khiên khởi đầu | **200** | Chặn một va chạm vật cản, không chống rơi hoặc cạn thanh sinh tồn. |
| Nước dự trữ | **150** | Tự dùng một lần khi cạn nước trong lượt kế tiếp. |
| Bình giữ ấm | **150** | Tự dùng một lần khi cạn thanh giữ ấm trong lượt kế tiếp. |
| Tăng thời lượng ×2 điểm | **200 / 400 / 700 / 1.100** | Nâng vĩnh viễn từ 8 lên **10 / 12 / 14 / 16 giây**. |
| Tăng thời lượng nam châm | **200 / 400 / 700 / 1.100** | Nâng vĩnh viễn từ 8 lên **10 / 12 / 14 / 16 giây**. |

Tim/khiên/đồ dự trữ chưa dùng được lưu khi đóng game. Bắt đầu lượt dùng hàng đã mua cho lượt đó; lượt sau cần mua lại. Mỗi loại khiên/dự trữ chỉ trữ một món. Nâng thời lượng được giữ lâu dài, vẫn cần nhặt vật phẩm trong lượt để kích hoạt.

Tim hồi máu vẫn sinh hiếm: lần đầu sau 20–30 giây, sau đó cách 18–32 giây; nhặt hồi một mạng, tối đa 5, đầy mạng đổi thành một sao. Va chạm mất một mạng và cho 2 giây miễn sát thương nếu còn sống. Khiên chặn va chạm cho 1 giây bảo vệ.

## Chạy lâu và tính điểm

- Tốc độ bắt đầu **360 đơn vị/giây**, tăng đều **2,2 đơn vị/giây mỗi giây**, không giới hạn và không nhảy bậc theo chặng. Sau 1/5/10 phút khoảng 1,37×/2,83×/4,67×. Khoảng 20.000 điểm ở mức 3,8× nếu chưa có ×2 điểm.
- Ngoài nhà/vòi/gai/rào/cưa/máy bay có cưa tuần tra, rào di chuyển ngang, máy bay lên xuống, hố đôi và cầu sập. Bẫy nâng cao mở sau 15/30 giây, cao thêm sau 60 giây; cụm được xáo và tránh lặp ngay.
- Bẫy sinh càng muộn càng di chuyển xa/nhanh: với `t` giây, biên độ nhân `1 + 0,35 × √(t/120)`, tần số nhân `1 + 0,4 × √(t/120)`. Khoảng cách cụm tính cả vùng quét và tốc độ tiếp cận. Bẫy bay giữ làn cúi.
- Cưa dùng va chạm tròn với đường bao alpha của đúng khung khủng long; gai dùng tam giác. Hình cưa quay quanh đúng tâm va chạm ở các tỷ lệ Game View. Bỏ chữ CÚI/NHẢY nổi trên bẫy; nút điều khiển vẫn ở dưới.
- **Ảnh chạy mới BẢN 07:** năm khung đạp đất/co chân/đẩy chân/lướt/tiếp đất được vẽ lại theo phong cách pixel art của ảnh nhảy/cúi. Các khung dùng chung kích thước và mốc đầu/chân để không co giãn hoặc giật; hình lật trái/phải và vùng va chạm lấy từ đúng khung đang vẽ. Giữ ba khung nhảy, ba khung cúi và tư thế khi chết. Năm ảnh chạy gốc vẫn được giữ để đối chiếu.
- Mỗi 20 đơn vị thế giới = 1 mét = 1 điểm cơ bản. Lưu kỷ lục, điểm lượt trước và 10 lượt gần nhất; thành tích hiện 5 lượt mới nhất. ×2 tăng điểm, số mét vẫn là quãng đường thật.
- Mô phỏng chia nhỏ bước theo tốc độ để giảm xuyên vật cản; đồng hồ/quãng đường dùng double. Không khẳng định người chơi có thể phản ứng kịp ở mọi tốc độ lớn tùy ý.

Menu mang tên **DINO CITY**, ít thông báo giữa màn; giữ HUD, thanh sinh tồn/vật phẩm, cảnh báo trước tối và kết quả lượt. Âm thanh nhảy, sao, va chạm, mua hàng và sấm tạo tại runtime, có thể tắt.

## Dữ liệu và cấu trúc

Dùng Unity `PlayerPrefs`, khoá **`CityDino.Save.v1`** giữ nguyên, schema tăng lên 3. Giữ ví, kỷ lục, lịch sử và tim chưa dùng; kỷ lục `HighScore` gốc được nhập khi chưa có hồ sơ mới. Nâng tim vĩnh viễn bản cũ được hoàn **40/140/320 sao** theo cấp đúng một lần. Map đầu lưu ở `CityDino.StartDistrict`, âm thanh ở `CityDino.Muted`. Dữ liệu trên thiết bị hiện tại, chưa đồng bộ tài khoản hoặc có bảng xếp hạng trực tuyến.

- `Assets/CityDino/Core/RunnerSession.cs`: mô phỏng, sinh bẫy/map, sinh tồn, quán tính/gió và dữ liệu lưu.
- `Assets/CityDino/Core/RunnerItems.cs`: cửa hàng, kho hàng, ×2 điểm, nam châm và khiên.
- `Assets/CityDino/Core/RunnerGeometry.cs`: tư thế và đường bao va chạm dùng chung.
- `Assets/CityDino/Core/RunnerWeather.cs`: pha chớp và độ tối của bão.
- `Assets/CityDino/Core/RunnerBiomeHazards.cs`: trạng thái báo trước/kích hoạt, hình va chạm băng, khe nóng và biển cuốn.
- `Assets/CityDino/Runtime/CityRunnerController.cs`: input, menu/HUD, cửa hàng, lưu và âm thanh.
- `Assets/CityDino/Runtime/RunnerWorldView.cs`, `RunnerArt.cs`: vẽ và ánh xạ atlas, cùng hệ toạ độ với core.
- `Assets/CityDino/Resources/CityDino`: ảnh gốc, font tiếng Việt, hai skyline, atlas bẫy, hành động, **`DinoRunAtlas.png`** cho năm khung chạy mới, **`BiomeHazardAtlas.png`** cho ba bẫy theo map và **`PickupAtlas.png`** gồm nước/cốc nóng/khiên/nam châm/×2/bình giữ ấm.
- `Assets/CityDino/Scenes/CityRunner.unity`: scene khởi động.

172 tệp của `Assets.zip` gốc cùng script/scene cũ được giữ nguyên để đối chiếu. Bản mới sửa lỗi đường cuộn không đồng nhất, nhiều script cùng kéo nền, rơi vô hạn và xoay cưa lệch collider.

## Kiểm tra

Cần .NET SDK 8; bộ console ngoài `Assets` chạy trực tiếp mã production, không cần Unity/NuGet:

```bash
./scripts/test-core.sh
./scripts/test-core.sh --input-mapping
./scripts/test-core.sh --render-contracts
./scripts/test-core.sh --compile-unity
```

Windows có thể dùng `dotnet run --project Tests/RunnerCore.Tests.csproj --configuration Release`. Phạm vi, số kiểm tra và các lượt mô phỏng ở [Tests/README.md](Tests/README.md).

Môi trường đám mây **không có Unity Editor**. Stub kiểm tra biên dịch, phép toán input và lệnh vẽ; không chạy Play Mode, raster hoá ảnh, âm thanh, cảm ứng thật hoặc build Unity. Cần thử trong Unity:

1. Import hoàn tất, Console không lỗi biên dịch; menu hiện BẢN 07 và tiếng Việt đầy đủ.
2. Trái/phải, nhảy kép, cúi, đa chạm và pause; cửa hàng không bấm xuyên.
3. Tuyết trượt/cốc nóng/băng rơi, nắng/nước/khe phun, gió/hố đôi/cầu sập, đêm đen 2–3 giây, bão nháy có mưa/sấm/biển cuốn. Thử lúc cảnh báo, đang kích hoạt và khoảng tắt để phân biệt vùng thật gây sát thương.
4. Giá tim theo số mua, kho hàng lưu khi đóng game và dùng ở lượt sau; nâng thời lượng, ×2, nam châm và khiên.
5. Hoạt ảnh chạy mới: nhịp chân rõ, đầu/chân không giật, menu và trong lượt đều dùng ảnh mới; thử cưa lúc chạy/nhảy/cúi, hai hướng và nhiều kích thước Game View. Hết mạng dừng và lưu đúng một lần; chơi lại giữ ví/kỷ lục.
