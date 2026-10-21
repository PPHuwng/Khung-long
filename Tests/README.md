# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp các file trong `Assets/CityDino/Core/` bằng C# 9,
không dùng bản sao logic game, không cần thư viện kiểm thử bên ngoài.
Bộ kiểm tra có **76 nhóm hành vi**, gồm:

- Nhảy kép, coyote time, cúi, di chuyển trái/phải trên đất/trên không, giới hạn màn hình và input không hợp lệ.
- Va chạm, nhặt đồ, mặt đường và hồi sau rơi dùng vị trí X hiện tại; chết chỉ ghi kết quả một lần và đóng băng tư thế.
- Tốc độ tăng từ từ không giới hạn, vượt 20.000 điểm, đồng hồ 1/10 giờ và va chạm thích ứng ở tốc độ 8.280/79.560.
- Ngẫu nhiên có seed, khoảng nghỉ giữa các cụm tính cả vùng quét chuyển động; không ghép làn cúi và làn nhảy bất khả thi.
- Gai dùng tam giác; cưa dùng vòng tròn và bóng dáng theo alpha nhân vật, bỏ qua góc/cổ trong suốt; cả hai hướng, sáu khung nhảy/cúi và độ nhấp nhô.
- Cưa/rào tuần tra và máy bay dao động; biên độ/tần số tăng theo thời gian, vẫn giữ làn cúi; không trôi sai, pause/reset và bước thời gian nhất quán.
- Tim hiếm, nhặt sao một lần, giới hạn ví/mạng; tim mua dùng một lượt giá **100/300/600**; hoàn nâng cấp v1 đúng một lần và chuyển lưu v2 sang v3 giữ ví/kỷ lục/lịch sử/tim dự trữ.
- Chọn map khởi đầu hoặc ngẫu nhiên; túi sáu map đi đủ sáu map trước khi lặp, không lặp liền nhau ở ranh giới hai túi.
- Đêm có cảnh báo và tối đen hoàn toàn ngẫu nhiên 2–3 giây; chỉ đêm dùng blackout này, pause giữ nguyên và đổi map/chơi lại đặt lại.
- Bão mưa có chớp mỗi 1,8 giây sau mở đầu 1 giây, pha chớp/tia sét dùng chung danh tính sấm; mức tối thay đổi liên tục; tuyết và các map khác không sấm chớp.
- Tuyết có quán tính khi thả/phanh/đổi hướng; gió trên mây đẩy mạnh hơn trên không và có thể điều khiển ngược để giữ X.
- Map mây có 10 giây mở đầu an toàn, sinh nhiều hố, hố kép và nền nứt/sập; nền sập là hố thật và hồi sau rơi không lặp vô hạn.
- Nhiệt/nước có 2 giây nghỉ đầu map, hao đúng tốc độ, nhặt đồ nóng/nước để hồi; cạn mất một mạng, hồi ngắn khi còn sống và không hồi giả sau khi chết.
- Bình nước/gói ấm dùng một lần giá **150**, khiên dùng một lần giá **200**; mua lưu vào lượt sau, tiêu kho khi bắt đầu, không trở thành nâng cấp vĩnh viễn.
- Khiên chặn đúng một va chạm vật lý, có khoảng miễn thương ngắn; không cứu rơi hố.
- ×2 chỉ nhân quãng đường ghi điểm trong thời gian hiệu lực, gồm bước hết hạn lẻ; không nhân điểm cũ, quãng đường thật hoặc ví sao.
- Nam châm chỉ hút sao trong bán kính, không hút tim/nước; thời gian hết hạn lẻ, gia hạn có giới hạn, pause/chơi lại đúng trạng thái.
- Hai nâng cấp thời lượng **200/400/700/1.100**, từ 8 lên tối đa 16 giây; ba loại powerup sinh ngẫu nhiên trong làn trống.

Các bài va chạm tạo tình huống cụ thể trong danh sách thực thể rồi chạy bước
mô phỏng production. Fixture cơ bản chọn map ngày để tách điều kiện thời tiết.
Các bài đo sinh cụm loại vật cản sau khi ghi đủ cụm, loại powerup và giữ đầy
thanh sinh tồn để chỉ đo generator; chúng không được dùng làm bằng chứng sống sót.

Kiểm tra cụm ghi **12 seed × 215 giây**, mở đầu trên đủ sáu map. Phát lại
**108 hình học thực tế × 4 tốc độ 360/900/1.800/3.600 × 2 biên X = 864 tình huống**,
tìm nhịp nhảy/nhảy kép/cúi bằng va chạm thật. Mẫu muộn sinh trên **6 map × 3 mốc
600/1.200/3.600 giây × 20 giây**, phát lại 18 hình học chuyển động ở tốc độ
thực, hai biên X và bốn pha (**144 tình huống**).

Bài chạy liên tục dùng **6 seed × 2 vị trí X × 215 giây**, mỗi seed bắt đầu ở
một map, giữ vật cản và nguồn nước/đồ ấm production, không bất tử hoặc hồi thanh
miễn phí. Powerup khiên/nam châm/×2 được loại khỏi bài này để tránh che lỗi.
Bot bù gió theo X mục tiêu, tìm nhịp nhảy có dung sai ±25 ms và bốn pha chuyển
động ngang. Có **37.154 khung hình đồ tiếp tế hiện trong màn hình** được kiểm tra
không giao vật cản/hố thật trong suốt đường đi, kể cả sau thời điểm sinh. Đây là kiểm tra đường vượt tồn tại, không thay đánh giá độ khó bởi
người chơi và không khẳng định mọi tốc độ lớn tùy ý đều dễ phản ứng.

Bài sinh tồn riêng chạy **12 seed × 2 map tuyết/nắng × 34 giây**, giữ hao thanh,
nhịp sinh, quãng đường di chuyển và va chạm nhặt đồ thật; chỉ bỏ vật cản để tách
độ đủ của nguồn tiếp tế. Nó ghi nhận **206 lần sinh và 190 lần nhặt hồi thanh**,
không tự hồi tài nguyên. Bài tiếp cận muộn thử vị trí sinh production trước/sau
các cụm thật tại 600/1.200/3.600 giây, kiểm tra vùng quét và thời gian tiếp cận;
đây là kiểm tra bố trí, không phải lượt chơi muộn hoàn chỉnh. Có 222 vị trí
sinh được thử, thời gian tiếp cận bằng cuộn đường dài nhất là 0,38 giây.

Đo mây trên **12 seed × 2 mốc 0/90 giây × 34 giây** có **258/427 cụm thuộc nhóm
hố (60,4%)**, gồm 58 hố kép và 84 nền sập. Túi map được kiểm tra thêm ba vòng
sáu map cho cả sáu lựa chọn khởi đầu và chế độ ngẫu nhiên.

Có thể chỉ chạy một nhóm khi chẩn đoán lỗi:

```bash
./scripts/test-core.sh --only 'seeded full'
```

Console ghi rõ những bài không được chọn chưa chạy; không truyền tham số sẽ
chạy toàn bộ 76 nhóm.

## Ánh xạ input và đa điểm chạm

```bash
./scripts/test-core.sh --input-mapping
```

**14 kiểm tra** gọi trực tiếp `ApplyPointer`/`ReadPointers` controller production
bằng tọa độ và thiết bị giả: trái/phải, hai hướng triệt tiêu, hai ngón di chuyển
cùng nhảy, crouch, HUD, letterbox và scale màn hình. Đây là kiểm tra logic/API
tối thiểu; không kiểm tra sự kiện Unity, cảm ứng thật hoặc input từ hệ điều hành.

## Hợp đồng toạ độ vẽ

```bash
./scripts/test-core.sh --render-contracts
```

**11 nhóm kiểm tra** gọi `RunnerWorldView.Draw` production và ghi lệnh vẽ:

- Tâm cưa đúng tâm va chạm qua 5 góc xoay × 5 scale/offset; đầu đường không lệch khi letterbox.
- Mỗi loại vật cản có ảnh, không nhãn CÚI/NHẢY nổi; nền sập/hố cắt mặt đường đúng vùng clip.
- Tuyết có mặt đường phủ tuyết và bông tuyết; mưa bão vẽ trên lớp tối và chớp theo đúng pha.
- Blackout đêm phủ RGB=0, alpha=1, không có vật thể thế giới vẽ đè sau.
- Năm pickup mới dùng vùng ảnh atlas riêng, đúng vị trí/UV; gió giữ nguyên khi pause.
- Sáu khung nhảy/cúi đúng UV và hướng lật; ảnh/va chạm dùng cùng tư thế.

Stub dùng phép toán affine `System.Numerics`, texture giả lấy kích thước từ
PNG thật và ghi lệnh draw, **không raster hoá ảnh hoặc chạy Unity**. Hợp đồng
xoay tham chiếu [UnityCsReference GUIUtility.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.4/Modules/IMGUI/GUIUtility.cs#L404).

## Kiểm tra biên dịch tích hợp

```bash
./scripts/test-core.sh --compile-unity
```

Lệnh chỉ biên dịch toàn bộ script runtime mới và script cũ bằng chữ ký API Unity
ở `UnityCompile/UnityStubs.cs`; không chạy Unity Editor, không chứng minh import,
serialize, input thật, đồ họa, âm thanh hoặc build Unity hoạt động. Vẫn cần kiểm
tra Play Mode theo README chính. Stub và console test ở ngoài `Assets` nên Unity
không nhập chúng vào trò chơi.
