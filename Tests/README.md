# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp các file trong `Assets/CityDino/Core/` bằng C# 9,
không dùng bản sao của logic game và không cần thư viện kiểm thử bên ngoài.
Bộ kiểm tra có **53 kiểm tra hành vi**, bao gồm:

- Nhảy kép, coyote time, vượt vật cản mở đầu, cúi tránh máy bay.
- Di chuyển trái/phải khi đứng hoặc trên không; giới hạn màn hình; axis không hợp lệ.
- Va chạm, nhặt sao/tim, kiểm tra mặt đường và hồi sau khi rơi đều dùng vị trí X hiện tại.
- Tạm dừng/kết thúc/menu/chơi lại dừng input ngang và giữ hoặc đặt lại vị trí đúng lúc.
- Tăng tốc khởi đầu, gia tốc có giới hạn và tính điểm theo quãng đường.
- Sinh ngẫu nhiên có seed; toàn bộ loại vật cản và cụm gai/bậc chắn.
- Va chạm gai theo tam giác và cưa theo hình tròn, không tính góc trống là vật cản.
- Cưa quay/dao động nhưng giữ làn cúi an toàn.
- Cưa/rào tuần tra ngang, dùng vị trí thật cho va chạm, không trôi sai sau nhiều chu kỳ; pause/reset và frame partition nhất quán.
- Cưa chạm vào phần đầu hiện rõ, bỏ qua vùng cổ trong suốt, theo đúng hướng nhìn,
  khung hình nhảy, cúi và độ nhấp nhô; rìa răng cưa và góc trống được kiểm tra riêng.
- Va chạm cưa tôn trọng thời gian miễn thương; pose/khung hình được giữ nguyên lúc chết.
- Nhặt sao một lần, ví, mua tim dùng một lượt, hoàn tiền nâng cấp cũ đúng một lần, hồi tim và nhịp sinh tim hiếm.
- Chọn đủ sáu map khởi đầu, mặc định ngẫu nhiên và đổi map ngẫu nhiên không lặp liền nhau.
- Đêm/giông bão có báo trước, đen hoàn toàn 2–3 giây, thời lượng/lịch ngẫu nhiên, chuyển độ tối mượt; dừng đồng hồ lúc pause,
  đặt lại hiệu ứng khi đổi map/chơi lại; bốn map còn lại không bị tối.
- Map trên mây không có hố trong 10 giây đầu; sau đó sinh khe đường thật,
  hồi phục sau rơi và dừng khi hết mạng.
- Lưu kỷ lục/lịch sử đúng một lần, chơi lại và chuẩn hóa dữ liệu lưu.

Các bài kiểm tra va chạm tạo một tình huống cụ thể trong danh sách thực thể của
core; sau đó chạy chính bước mô phỏng sản xuất để kiểm tra kết quả.

Kiểm tra sinh map ghi lại các cụm trên **12 seed × 215 giây mô phỏng**, với hai seed khởi đầu trên mỗi map. Nó chỉ
loại vật cản sau khi ghi nhận cụm đầy đủ để quan sát sinh map suốt lượt; bài
kiểm tra riêng phát lại **61 hình học cụm thực tế** ở cả tốc độ đầu/tối đa và
hai biên X (**244 tình huống**), tìm cách nhảy/nhảy kép/cúi qua bằng va chạm thật.
Bài chạy liên tục dùng **6 seed × 2 vị trí X × 215 giây**, mỗi seed mở đầu ở một map khác nhau, không xoá vật cản hoặc
bật bất tử, để kiểm tra chuyển tiếp giữa các cụm. Bộ điều khiển mô phỏng tìm
nhịp nhảy có dung sai ±25 ms và bốn pha chuyển động ngang; đây là kiểm tra đường vượt tồn tại, không thay
thế đánh giá độ khó bằng người chơi. Kiểm tra đổi map ngẫu nhiên chạy thêm
**12 seed × 245 giây**, không yêu cầu một lượt ngẫu nhiên phải đi qua đủ mọi map.

Có thể chỉ chạy một nhóm khi đang chẩn đoán lỗi:

```bash
./scripts/test-core.sh --only 'seeded full'
```

Console ghi rõ những bài không được chọn chưa chạy; chạy không có tham số sẽ
thực hiện toàn bộ 53 kiểm tra.

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

**4 nhóm kiểm tra** gọi trực tiếp `RunnerWorldView.Draw` và ghi lại lệnh vẽ:

- Tâm cưa giữ đúng tâm va chạm qua 5 góc xoay × 5 scale/offset màn hình.
- Vệt mưa/đường giữ đúng hai đầu khi scale và letterbox.
- Mỗi vật cản có lệnh vẽ ảnh, không còn nhãn CÚI/NHẢY nổi trên map.
- Lệnh cuối của thế giới khi tối là phủ đen RGB=0, alpha=1; không vẽ bóng vật thể đè lên.

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
