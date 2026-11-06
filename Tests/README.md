# Kiểm tra DINO CITY — BẢN 15

Cần .NET SDK 8. Các bộ console ở ngoài `Assets`, chạy trực tiếp mã C# production; Unity không nhập stubs hoặc mã kiểm tra vào game.

```bash
./scripts/test-core.sh
./scripts/test-core.sh --render-contracts
./scripts/test-core.sh --input-mapping
./scripts/test-core.sh --audio-contracts
./scripts/test-core.sh --compile-unity
```

Có **147 nhóm gameplay, 64 nhóm lệnh vẽ, 48 nhóm input và 13 nhóm âm thanh**. Có thể chọn một nhóm gameplay bằng `./scripts/test-core.sh --only 'advanced boss'`; console ghi rõ các bài không được chọn chưa chạy.

## Gameplay và đường chạy

Bộ core bao gồm nhảy kép/coyote/cúi/A–D, va chạm theo alpha thật của ba nhân vật, gai tam giác/cưa tròn, vật phẩm/kho/lưu/chuyển schema, sáu map xáo không lặp liền nhau, băng/khe nóng/biển cuốn, quán tính tuyết/gió/hố/cầu sập, hao và hồi nước/giữ ấm từ từ, tăng tốc không giới hạn, pause/chết/chơi lại.

Tốc độ đầu **480**, gia tốc **2,2**; các bài thời gian dài kiểm tra cả một và mười giờ, tiếp xúc vật cản mỏng ở tốc độ 8.280/79.560. Đây là kiểm tra độ chính xác mô phỏng, không khẳng định người chơi có thể phản ứng ở mọi tốc độ tuỳ ý.

Bài hình học phát lại **135 cụm × 4 tốc độ × 2 biên X**, thử nhảy/nhảy kép/cúi bằng va chạm thật. Có thêm **168 trường hợp bẫy chuyển động muộn** và **36 trường hợp bẫy riêng theo map**. Các phép đo generator loại vật cản đã ghi nhận và giữ đầy thanh để tách cơ chế sinh; không dùng chúng làm bằng chứng sống sót.

Lượt chạy liên tục giữ toàn bộ bẫy, cột tên lửa, thời tiết và đồ uống production: **6 seed × 2 vị trí X × 215 giây**. Bot dùng lệnh thật, bù gió, tìm nhịp nhảy chịu dung sai ±25 ms/hai hướng nhìn; không bất tử, xoá vật cản hoặc tự hồi tài nguyên. Loại powerup khiên/nam châm/×2 để chúng không che lỗi. **28.564 khung đồ tiếp tế trong màn hình** được kiểm tra không giao bẫy/hố đang hoạt động.

Bài tiếp tế riêng ghi **208 lần sinh/192 lần nhặt thật** qua 24 lượt tuyết/nắng dài 34 giây; chỉ bỏ bẫy để đo độ đủ nguồn đồ uống. Bài bố trí muộn thử 216 vị trí tại 600/1.200/3.600 giây, thời gian tiếp cận dài nhất 0,99 giây. Đồ uống có khoảng tiếp cận/hạ chân theo tốc độ; vẫn cần nhặt vật thể và hồi qua hàng đợi, không cấp thanh miễn phí.

## Cột tên lửa dọc

Loạt sinh thật có 3/4 quả cùng vị trí X và cùng lúc; khe Y55/85/115, cao 180 được khóa trước khi phóng. Ghi nhận **60 cột thật** phủ cả hai số lượng và ba độ cao khe. Kiểm tra thời gian báo, vòng tròn sát thương, tốc độ cao, giáp/khiên/lướt, giới hạn vòng đời, pause/chết/reset/vào boss/khoảng nghỉ trở lại, không ghép hố/làn cúi và không để map đêm che cả loạt.

**324 đường vượt một mạng** = 3 nhân vật × 2 số quả × 3 khe × 3 mốc tốc độ × 2 vị trí X × 3 dung sai thời điểm. Chạy đến khi cả cột đi hết bằng nhảy thật, không xoá đạn, hồi máu hoặc bảo vệ. Đồ uống sau loạt tên lửa là pickup thật sau khi hạ chân.

## Boss, cấp độ và chiến đấu

Kiểm tra mốc điểm thật, kể cả ×2: Tank 10k → Gunship 20k → Submarine 30k → Sentinel 40k; sau đó xáo đủ bốn loại. Boss thường bắt đầu C3, tăng cấp sau mỗi bốn trận; đấu tập dùng đúng cấp đã chọn. Các trường hợp cấp 32/128 xác nhận máu còn tăng và số thực thể hữu hạn. Điểm/quãng đường/đồng hồ map/tài nguyên đứng yên trong đấu trường.

Các bài gia đình chiêu cũ dùng C1 để đo riêng nhịp/đường bay, tách khỏi phối hợp C3+. Audit đầu ra bảo vệ nhân vật để theo dõi đủ chiêu; **không phải bằng chứng né hoặc thắng**. Mẫu 75 giây C1/C2/C4/C8 ghi số nguy hiểm Tank 38/74/103/101, Gunship 39/74/79/147, Sentinel 22/38/50/60. Vì một mẫu hữu hạn có thể kết thúc giữa chuỗi phối hợp, không suy độ khó chỉ từ số đạn; bài C3/C8 cùng seed so sánh chính các formation và xác nhận thêm lượt phối hợp, nhiều họ chiêu và góc tấn công.

BẢN14 kiểm tra các chuỗi ngẫu nhiên có seed, trước/sau/chéo, nhiều họ chiêu, aim khóa khi dấu báo xuất hiện rồi không đuổi theo; đạn chéo có chuyển động và sát thương thật. Audit nguồn C8 ghi **132 đạn và 24 beam**, mỗi đợt có `SourceTelegraphId` và chờ đủ dấu báo riêng. Pause/thắng/menu xoá hoặc đóng băng đúng trạng thái, không tích tụ vô hạn. Bài tiếp xúc dựng vị trí cụ thể để tách hình học, không dùng nó làm bằng chứng thắng trận.

Laser/sonar nhanh có thêm **30 trường hợp độ cao/đà chuyển động**: thử các vị trí 20/60/140/220/260 và vận tốc −220/0/200, có 120 ms phản ứng rồi giữ hướng thật qua beam. Mỗi trường hợp có ít nhất một đường né không mất mạng; ngăn việc khóa người đang nổi sát trần vào một đòn không thể thoát.

Bốn trận thắng thường C3 dùng Classic, **ba tim mua hợp lệ từ đầu lượt**, không khiên/kỹ năng, không sửa máu boss hoặc xoá đạn/bật bất tử trong trận. Giữ nguyên 1.020/1.088/1.224/1.142,4 HP, sát thương và cửa sổ lõi production. Bot dùng A/D, nhảy/cúi/lực nâng/bơi/lặn và J; kết quả:

| Boss | Thời gian chiến đấu | Số lần trúng đòn thật |
|---|---:|---:|
| Tank | 59,5s | 1 |
| Gunship | 64,0s | 0 |
| Submarine | 72,5s | 2 |
| Sentinel | 71,4s | 0 |

Mỗi trận bắt đầu ngay trước mốc điểm để bỏ phần chạy dài; đây không phải lượt chạy liên tục đến 40k, cũng không phải bằng chứng thắng không trúng đòn/một mạng. Bot dự báo từ vật thể sống và dấu báo đã hiện, dùng bao thân bảo thủ; không đọc lịch chiêu đang ẩn hoặc sửa trạng thái sống để né.

Bài đường né pha 3 dựng máu 20% trước khi bắt đầu combat: ba boss gốc × C2/C4/C8. C2 giữ một mạng 50 giây; C4/C8 dùng ba tim tiêu chuẩn đấu tập 35 giây, không kỹ năng, hồi máu, miễn thương hoặc xoá đạn. C4 Tank/Gunship/Sentinel chịu 1/1/0 đòn; C8 là 0/0/0 trong mẫu này. Đây là thử sinh tồn qua nhiều họ chiêu, không phải thắng boss đầy máu cấp cao. Bài lướt liên hoàn cô lập họ lướt để kiểm tra từng nguồn báo, mũi húc thật, lùi về vô hại và huỷ chuỗi khi thắng.

## Chuyển cảnh và lưu dữ liệu

Bài thưởng/trở về có thể đặt boss 1 HP và tách đạn để kiểm tra ghi nhận đúng một lần; không dùng làm bằng chứng chiến đấu. Kiểm tra đấu tập bốn boss/ba nhân vật giữ JSON, ví/kỷ lục/lịch sử/kho, không mở khóa khi thử nhân vật chưa mua, không thưởng hoặc tiêu hàng.

Có **27 đường rời trận dưới nước** = 3 nhân vật × 3 vị trí X × 3 độ sâu. Chân giữ dưới mặt nước khi đang bơi, bám đúng dốc bờ khi lên, không giật hình; **1.368 khung dốc** được kiểm tra. Camera/thế giới chỉ tiến qua cổng sau boss; mũ cất khi lên bờ, pause đóng băng và chỉ nối lại điểm/tài nguyên sau khi qua cổng.

## Lệnh vẽ, input và âm thanh

**64 nhóm render** ghi lệnh production qua phép toán affine, nhiều canvas/letterbox. Bao gồm tâm/UV/alpha/va chạm, pose ba nhân vật, cổng/đấu trường, kính mũ trong suốt, cột tên lửa và điểm phóng ! cục bộ. Kiểm tra marker tại nguồn vật thể thật, không phủ cả làn; laser ngắm mảnh khác beam sống. Bài dưới nước đối chiếu chân nhân vật với đường dốc đã biến đổi camera.

Các bài BẢN14 giữ vị trí/kích thước skyline và hai lưới đường khi đổi mây; pause giữ màu, restart đặt lại; nền muộn có tốc độ riêng trong khi hố vật lý vẫn đi theo mô phỏng; mưa bão giữ độ tối/chớp mạnh trong map, chỉ hòa dần 3 giây khi đổi map. Đêm vẫn phủ đen hoàn toàn 2–3 giây theo luật gameplay.

**48 nhóm input** gọi controller production bằng thiết bị/tọa độ giả: bàn phím, nhiều ngón, bấm xuyên modal, bay/bơi/lặn/bắn, chặn thao tác lúc chuyển cảnh/pause/menu/chết. **13 nhóm audio** kiểm tra sự kiện theo ID và mẫu PCM: một tiếng mỗi đợt, hướng âm, tiếng hạ boss, cơ cấu/bơi/thở, mute/pause/mất tiêu điểm/menu/huỷ controller; không lặp lại sau khi tiếp tục.

Các stubs không raster hoá ảnh, chạy Unity events, chạm thiết bị, phát âm qua mixer hoặc build Unity. Kiểm tra biên dịch dùng chữ ký API Unity giả cho toàn bộ runtime mới và script cũ. **Vẫn cần thử Play Mode trong Unity Editor**, đặc biệt cảm giác né/độ khó, chuyển map, lên bờ, chất lượng hình và âm thanh trên thiết bị thật.

## Hồi quy BẢN 15

Kiểm tra lệnh vẽ đối chiếu trực tiếp độ tối mạnh và chớp sáng với đồng hồ sấm, qua nhiều chu kỳ 1,8 giây; lớp tối vẫn hòa hết khi rời map trong 3 giây. Có 36 trường hợp nét ngang/dọc với nhiều hướng, độ dày và canvas, giữ nguyên đường chéo và vị trí cưa/tên lửa. HUD được vẽ lặp thật để kiểm tra tái dùng cùng chuỗi và cập nhật khi điểm/ví đổi; nút vẫn hoạt động.

Ba nhóm lưu dữ liệu dùng bộ đếm tại stubs JSON/PlayerPrefs: 500 thông báo sao không serialize/ghi từng lần, checkpoint cố định không bị đẩy lùi mãi và ghi đúng dữ liệu mới nhất một lần; hoãn ghi khi đang đổi map, gần bẫy, có tên lửa hoặc đang nhảy; pause/mất tiêu điểm/về menu/thoát ép lưu trước hạn. Đây là kiểm tra lời gọi và thời điểm, không đo độ trễ đĩa thật.

Bài âm thanh mới warm hai observer với warning/đạn/beam/vật cản rồi gọi 10.000 frame: **0 byte managed** trong đoạn đo bằng `GC.GetAllocatedBytesForCurrentThread`. Phép đo chỉ áp dụng các observer production trong .NET, không phải toàn bộ trò chơi hoặc Unity Audio.
