# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp `Assets/CityDino/Core/RunnerSession.cs` bằng C# 9,
không dùng bản sao của logic game và không cần thư viện kiểm thử bên ngoài.
Bộ kiểm tra có **38 kiểm tra hành vi**, bao gồm:

- Nhảy kép, coyote time, vượt vật cản mở đầu, cúi tránh máy bay.
- Di chuyển trái/phải khi đứng hoặc trên không; giới hạn màn hình; axis không hợp lệ.
- Va chạm, nhặt sao/tim, kiểm tra mặt đường và hồi sau khi rơi đều dùng vị trí X hiện tại.
- Tạm dừng/kết thúc/menu/chơi lại dừng input ngang và giữ hoặc đặt lại vị trí đúng lúc.
- Tăng tốc khởi đầu, gia tốc có giới hạn và tính điểm theo quãng đường.
- Sinh ngẫu nhiên có seed; toàn bộ loại vật cản và cụm gai/bậc chắn.
- Va chạm gai theo tam giác và cưa theo hình tròn, không tính góc trống là vật cản.
- Cưa quay/dao động nhưng giữ làn cúi an toàn.
- Nhặt sao một lần, ví, mua nâng cấp, hồi tim và nhịp sinh tim hiếm.
- Đổi khu vực, khoảng trống trên mây, hồi phục sau rơi và dừng khi hết mạng.
- Lưu kỷ lục/lịch sử đúng một lần, chơi lại và chuẩn hóa dữ liệu lưu.

Các bài kiểm tra va chạm tạo một tình huống cụ thể trong danh sách thực thể của
core; sau đó chạy chính bước mô phỏng sản xuất để kiểm tra kết quả.

Kiểm tra sinh map ghi lại các cụm trên **12 seed × 215 giây mô phỏng**. Nó chỉ
loại vật cản sau khi ghi nhận cụm đầy đủ để quan sát sinh map suốt lượt; bài
kiểm tra riêng phát lại **46 hình học cụm thực tế** ở cả tốc độ đầu/tối đa và
hai biên X (**184 tình huống**), tìm cách nhảy/nhảy kép/cúi qua bằng va chạm thật.
Bài chạy liên tục dùng **6 seed × 2 vị trí X × 215 giây**, không xoá vật cản hoặc
bật bất tử, để kiểm tra chuyển tiếp giữa các cụm. Bộ điều khiển mô phỏng tìm
nhịp nhảy có dung sai ±25 ms; đây là kiểm tra đường vượt tồn tại, không thay
thế đánh giá độ khó bằng người chơi.

Có thể chỉ chạy một nhóm khi đang chẩn đoán lỗi:

```bash
./scripts/test-core.sh --only 'seeded full'
```

Console ghi rõ những bài không được chọn chưa chạy; chạy không có tham số sẽ
thực hiện toàn bộ 38 kiểm tra.

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
