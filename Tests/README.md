# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp `Assets/CityDino/Core/RunnerSession.cs` bằng C# 9,
không dùng bản sao của logic game và không cần thư viện kiểm thử bên ngoài.
23 kiểm tra hành vi bao gồm:

- Nhảy kép, coyote time, vượt vật cản mở đầu, cúi tránh máy bay.
- Tạm dừng, tăng tốc có giới hạn và tính điểm theo quãng đường.
- Sinh ngẫu nhiên có seed, khoảng cách phục hồi giữa vật cản trên 12 seed.
- Nhặt sao một lần, ví, mua nâng cấp, hồi tim và nhịp sinh tim hiếm.
- Đổi khu vực, khoảng trống trên mây, hồi phục sau rơi và dừng khi hết mạng.
- Lưu kỷ lục/lịch sử đúng một lần, chơi lại và chuẩn hóa dữ liệu lưu.

Các bài kiểm tra va chạm tạo một tình huống cụ thể trong danh sách thực thể của
core; sau đó chạy chính bước mô phỏng sản xuất để kiểm tra kết quả. Các bài
kiểm tra sinh map ghi lại vị trí tuyệt đối của vật cản qua nhiều phút chơi.

Kiểm tra biên dịch tích hợp tùy chọn:

```bash
./scripts/test-core.sh --compile-unity
```

Lệnh thứ hai chỉ biên dịch toàn bộ script runtime mới và script cũ bằng bộ chữ
ký API Unity tối thiểu tại `UnityCompile/UnityStubs.cs`. Nó giúp phát hiện lỗi
cú pháp hoặc liên kết C#, **không chạy Unity**, không chứng minh import/serialize,
input thực tế, đồ họa, âm thanh hoặc build Unity hoạt động. Vẫn cần kiểm tra Play
Mode trong Unity Editor theo README chính. Bộ stub nằm ngoài `Assets`, nên Unity
không nhập nó vào trò chơi.
