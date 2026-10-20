# Kiểm tra ánh xạ chuột/cảm ứng

Bộ console gọi trực tiếp hai hàm production `CityRunnerController.ApplyPointer`
và `ReadPointers` bằng reflection. Nó kiểm tra 14 tình huống: nút trái/phải/cúi
không kích hoạt nhảy, giữ chuột không nhảy lặp, hai ngón tay vừa di chuyển vừa
nhảy, bỏ qua chuột giả khi có touch, vùng HUD/viền ngoài và tỷ lệ màn hình.

```bash
./scripts/test-core.sh --input-mapping
```

Hoặc với .NET SDK 8:

```bash
dotnet run --project Tests/InputMapping/InputMapping.csproj --configuration Release
```

Phép toán Vector2/Rect và các giá trị thiết bị đầu vào được cấp bằng stub tại
`Tests/UnityCompile/UnityStubs.cs`. Đây là kiểm tra logic ánh xạ tọa độ và kết hợp
các pointer, **không chạy Unity**, không giả lập hệ điều hành, cảm ứng phần cứng
hay sự kiện IMGUI. Cần kiểm tra lại nút menu và cảm ứng trong Unity/thiết bị thật.
