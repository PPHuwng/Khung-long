# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp các file trong `Assets/CityDino/Core/` bằng C# 9,
không dùng bản sao của logic game và không cần thư viện kiểm thử bên ngoài.
Bộ kiểm tra có **60 kiểm tra hành vi**, bao gồm:

- Nhảy kép, coyote time, vượt vật cản mở đầu, cúi tránh máy bay.
- Di chuyển trái/phải khi đứng hoặc trên không; giới hạn màn hình; axis không hợp lệ.
- Va chạm, nhặt sao/tim, kiểm tra mặt đường và hồi sau khi rơi đều dùng vị trí X hiện tại.
- Tạm dừng/kết thúc/menu/chơi lại dừng input ngang và giữ hoặc đặt lại vị trí đúng lúc.
- Tốc độ tăng đều không giới hạn, kiểm tra qua 20.000 điểm và các mốc 1/10 giờ; đồng hồ/quãng đường tích lũy bằng double để không đứng lại ở tốc độ cao.
- Vật cản mỏng 8px và sao nhỏ vẫn được kiểm tra ở tốc độ 8.280/79.560 nhờ bước va chạm thích ứng.
- Sinh ngẫu nhiên có seed; toàn bộ loại vật cản và cụm gai/bậc chắn.
- Va chạm gai theo tam giác và cưa theo hình tròn, không tính góc trống là vật cản.
- Cưa quay/dao động nhưng giữ làn cúi an toàn.
- Cưa/rào tuần tra ngang và máy bay lên xuống, dùng vị trí thật cho va chạm, không trôi sai sau nhiều chu kỳ; pause/reset và frame partition nhất quán.
- Biên độ/tần số bẫy tăng theo thời gian sinh; vùng chuyển động đầy đủ được tính vào khoảng cách cụm, làn cúi vẫn còn ở giai đoạn muộn.
- Cưa chạm vào phần đầu hiện rõ, bỏ qua vùng cổ trong suốt, theo đúng hướng nhìn,
  khung hình nhảy, cúi và độ nhấp nhô; rìa răng cưa và góc trống được kiểm tra riêng.
- Sáu khung cúi/nhảy mới theo alpha thật ở cả hai hướng; chọn đúng khung đạp đất/thu chân/rơi, nhảy kép và ba nhịp cúi.
- Va chạm cưa tôn trọng thời gian miễn thương; pose chạy/cúi/nhảy được giữ nguyên lúc chết nhờ vận tốc ảnh riêng, dù vận tốc vật lý đã dừng.
- Nhặt sao một lần, ví, mua tim dùng một lượt, hoàn tiền nâng cấp cũ đúng một lần, hồi tim và nhịp sinh tim hiếm.
- Chọn đủ sáu map khởi đầu, mặc định ngẫu nhiên và đổi map ngẫu nhiên không lặp liền nhau.
- Đêm/giông bão có báo trước, đen hoàn toàn 2–3 giây, thời lượng/lịch ngẫu nhiên, chuyển độ tối mượt; dừng đồng hồ lúc pause,
  đặt lại hiệu ứng khi đổi map/chơi lại; bốn map còn lại không bị tối.
- Mưa/bão có một xung sấm chớp mỗi chu kỳ, danh tính tia chớp dùng chung với âm thanh; bốn map khác không chớp.
- Map trên mây không có hố trong 10 giây đầu; sau đó sinh khe đường thật,
  hồi phục sau rơi và dừng khi hết mạng.
- Lưu kỷ lục/lịch sử đúng một lần, chơi lại và chuẩn hóa dữ liệu lưu.

Các bài kiểm tra va chạm tạo một tình huống cụ thể trong danh sách thực thể của
core; sau đó chạy chính bước mô phỏng sản xuất để kiểm tra kết quả.

Kiểm tra sinh map ghi lại các cụm trên **12 seed × 215 giây mô phỏng**, với hai seed khởi đầu trên mỗi map. Nó chỉ
loại vật cản sau khi ghi nhận cụm đầy đủ để quan sát sinh map suốt lượt; bài
kiểm tra riêng phát lại **81 hình học cụm thực tế** ở bốn tốc độ
**360/900/1.800/3.600** và hai biên X (**648 tình huống**), tìm cách nhảy/nhảy kép/cúi qua bằng va chạm thật.
Bài chạy liên tục dùng **6 seed × 2 vị trí X × 215 giây**, mỗi seed mở đầu ở một map khác nhau, không xoá vật cản hoặc
bật bất tử, để kiểm tra chuyển tiếp giữa các cụm. Bộ điều khiển mô phỏng tìm
nhịp nhảy có dung sai ±25 ms và bốn pha chuyển động ngang; đây là kiểm tra đường vượt tồn tại, không thay
thế đánh giá độ khó bằng người chơi. Kiểm tra đổi map ngẫu nhiên chạy thêm
**12 seed × 245 giây**, không yêu cầu một lượt ngẫu nhiên phải đi qua đủ mọi map.

Mẫu giai đoạn muộn dùng **6 map × 3 mốc 600/1.200/3.600 giây**, mỗi mẫu sinh
thực trong 20 giây. **18 hình học bẫy chuyển động** được phát lại ở tốc độ thực
của thời điểm sinh, hai biên X và bốn pha (**144 tình huống**). Khi gom mẫu,
chữ ký làm tròn biên độ/tần số thành nhóm để tránh lặp lại hàng trăm mẫu gần
nhau; mẫu có vùng quét rộng nhất trong nhóm được giữ lại. Đây là kiểm tra
đường vượt bằng mô phỏng; không khẳng định người chơi có đủ thời gian phản
ứng ở mọi tốc độ lớn tùy ý.

Có thể chỉ chạy một nhóm khi đang chẩn đoán lỗi:

```bash
./scripts/test-core.sh --only 'seeded full'
```

Console ghi rõ những bài không được chọn chưa chạy; chạy không có tham số sẽ
thực hiện toàn bộ 60 kiểm tra.

## Ánh xạ input và đa điểm chạm

```bash
./scripts/test-core.sh --input-mapping
```

Bộ này có **14 kiểm tra** gọi trực tiếp `ApplyPointer`/`ReadPointers` trong
controller production bằng fixture tọa độ và thiết bị giả. Nó kiểm tra nút
trái/phải không kích hoạt nhảy, trái+phải triệt tiêu, di chuyển cùng nhảy bằng
hai ngón, crouch, vùng HUD, letterbox và scale màn hình.

Đây là kiểm tra logic ánh xạ với phép toán/API tối thiểu. Nó **không kiểm tra
sự kiện Unity, cảm ứng thật hoặc việc hệ điều hành cung cấp input**.

## Hợp đồng toạ độ vẽ

```bash
./scripts/test-core.sh --render-contracts
```

**7 nhóm kiểm tra** gọi trực tiếp `RunnerWorldView.Draw` và ghi lại lệnh vẽ:

- Tâm cưa giữ đúng tâm va chạm qua 5 góc xoay × 5 scale/offset màn hình.
- Vệt mưa/đường giữ đúng hai đầu khi scale và letterbox.
- Mỗi vật cản có lệnh vẽ ảnh, không còn nhãn CÚI/NHẢY nổi trên map.
- Lệnh cuối của thế giới khi tối là phủ đen RGB=0, alpha=1; không vẽ bóng vật thể đè lên.
- Tia chớp có lệnh vẽ đúng pha dùng chung với mô hình sấm.
- Hiệu ứng thời tiết giữ nguyên khi tạm dừng, không chạy theo đồng hồ ambient.
- Sáu khung cúi/nhảy dùng đúng UV atlas và hướng lật ở cả hai phía; ảnh và hình va chạm cùng một tư thế.

Stub dùng phép toán affine thực bằng `System.Numerics`, texture giả lấy kích thước từ PNG gốc và ghi lệnh draw; **không raster hoá ảnh hoặc chạy Unity**. Phép xoay cũ của Unity 6000.4 đã được mô hình hoá và tái hiện lỗi ở bản sao tạm: kiểm tra tâm cưa/đầu đường thất bại; cùng bộ kiểm tra đạt với phép xoay mới. Tham chiếu: [UnityCsReference GUIUtility.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.4/Modules/IMGUI/GUIUtility.cs#L404).

## Kiểm tra biên dịch tích hợp

```bash
./scripts/test-core.sh --compile-unity
```

Lệnh này chỉ biên dịch toàn bộ script runtime mới và script cũ bằng bộ chữ ký
API Unity tối thiểu tại `UnityCompile/UnityStubs.cs`. Nó giúp phát hiện lỗi cú
pháp hoặc liên kết C#, **không chạy Unity**, không chứng minh import/serialize,
input thực tế, đồ họa, âm thanh hoặc build Unity hoạt động. Vẫn cần kiểm tra
Play Mode trong Unity Editor theo README chính. Bộ stub và console test nằm
ngoài `Assets`, nên Unity không nhập chúng vào trò chơi.
