# DINO CITY

Game Unity 2D chạy vô tận: khủng long di chuyển trái/phải, nhảy kép, cúi tránh bẫy, gom sao và mua vật phẩm. Có menu tiếng Việt, sáu map chạy, bốn boss, cửa hàng, kỷ lục và lịch sử lượt chơi. Hình ảnh gốc từ `Assets.zip` được giữ lại cùng nền, bẫy, hoạt ảnh và vật phẩm pixel art mới.

## Mở và cập nhật

1. Dùng **Unity 6000.4.0f1**, revision `8cf496087c8f`, theo `ProjectSettings/ProjectVersion.txt` gốc.
2. Unity Hub → **Add → Add project from disk**, chọn thư mục chứa `Assets`, `Packages`, `ProjectSettings`.
3. Chờ Package Manager hoàn tất. Project giữ package gốc từ bản bạn gửi, gồm URP **17.4.0**, Input System **1.19.0**, UGUI **2.0.0**, Test Framework **1.6.0** và các gói 2D.
4. Mở `Assets/CityDino/Scenes/CityRunner.unity`, hoặc menu **City Dino → Open City Runner Scene**, rồi nhấn **Play**.
5. Chọn tab **Game**, tỷ lệ **16:9**, ví dụ 1280 × 720. Giao diện tự co theo cửa sổ; điện thoại dùng màn hình ngang.

**Nếu project trên máy đã chạy được:** đóng Unity, giải nén ZIP mới rồi chép và thay thế riêng thư mục **`Assets/CityDino`** vào project đang dùng. Giữ `Packages` và `ProjectSettings` đang chạy được. Mở lại, chờ import; menu phải hiện **BẢN 13**.

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
| Bắn khi đấu boss — cả ba con | Giữ J | Giữ BẮN |
| Kỹ năng hai con mới | E | CHIÊU |
| Bay khi đấu trực thăng | Giữ hoặc nhấn nhịp Space / ↑ / W; thả để hạ | Giữ hoặc chạm nhịp BAY |
| Bơi khi đấu tàu ngầm | Giữ/nhấn nhịp Space / ↑ / W để lên; thả để chìm, S / ↓ để lặn nhanh | BƠI ↑ / LẶN ↓ |

Mất tiêu điểm tự tạm dừng. Cảm ứng hỗ trợ nhiều ngón để giữ hướng và nhảy cùng lúc; nút hướng không kích hoạt nhảy. Cửa hàng/thành tích chặn thao tác bấm xuyên. Nhân vật di chuyển trong vùng X100–720 trên màn hình ảo, màn chơi vẫn tự cuộn.

## Sáu map và thử thách

Chọn **Ngẫu nhiên** hoặc một map khởi đầu ở menu. Mỗi chặng dài **35 giây**. Sáu map được xáo thành một lượt: đi hết sáu map trước khi xáo tiếp, không lặp ngay ở ranh giới hai lượt. Map được chọn chỉ cố định chặng đầu; lựa chọn được lưu và áp dụng cả khi Chạy lại.

| Map | Thử thách |
|---|---|
| Thành phố ban ngày | Nhà, vòi cứu hoả, gai và cưa tuần tra; lá bay, gió và nền nhiều lớp. |
| Phố tuyết | Thay cho Phố mưa cũ. Tuyết rơi, đường đóng băng; tăng tốc, phanh và đổi hướng có quán tính, trượt xa hơn trên không. Có băng nhọn nứt rồi rơi, cần tránh điểm rơi hoặc vượt mảnh băng đã đáp. Nhặt cốc nóng để giữ ấm. |
| Nắng gắt | Hơi nóng, bụi sáng, ưu tiên gai đôi/ba và rào bậc; khe phun nóng báo đỏ rồi phun theo chu kỳ, tắt có thể chạy qua. Nhặt chai nước để chống mất nước. |
| Đêm neon | Ưu tiên cưa; đen hoàn toàn **2–3 giây**, thời lượng và khoảng nghỉ ngẫu nhiên. Báo trước 1,5 giây và fade 0,18 giây mỗi đầu; HUD/nút vẫn hiện. |
| Đường trên mây | Nhiều hố đơn, hố đôi có đảo nhỏ và cầu nứt rồi sập thành hố thật. Gió từng đợt đẩy sang trái, mạnh hơn khi nhảy; có vệt gió để nhận biết. Chọn làm map đầu thì 10 giây đầu chưa có hố. |
| Giông bão | Mưa dày luôn nhìn thấy; nhịp tối → tia sét/chớp sáng → bình thường, kèm sấm mỗi **1,8 giây** từ giây thứ 1. Không dùng đợt đen kín kéo dài của Đêm neon. Có biển quảng cáo bị gió cuốn theo quỹ đạo ngang/lên xuống; ưu tiên cưa, gai và bẫy di chuyển. |

Tuyết/nắng có thanh **Giữ ấm / Nước** từ 100. Hai giây đầu khi vào map chưa hao; sau đó giảm lần lượt 7 / 6 mỗi giây. Đồ tiếp tế sinh cách nhau khoảng 3,7–4,6 giây, tránh vùng quét của bẫy dưới đất. **BẢN 08 hồi thật từ từ:** nước thêm tổng 30 điểm trong 3 giây, cốc nóng thêm 35 trong 3,5 giây, tốc độ hồi 10 điểm/giây; thời tiết vẫn làm hao thanh trong lúc hồi. Nhặt tiếp cộng lượng hồi đang chờ, tối đa 60 nước / 70 giữ ấm, không tăng tốc độ hồi vô hạn. Thanh tối đa 100; đổi map xoá lượng hồi cũ và đặt lại thanh. Cạn thanh mất một mạng; chỉ khi thực sự mất mạng và còn sống mới hồi 45 để có thời gian tìm đồ. Bình dự trữ tự dùng một lần khi cạn: cứu ngay 10 điểm rồi thêm 50 trong 5 giây, không nhảy lên đầy bình.

Hố, cầu sập, hình vẽ và kiểm tra mặt đường dùng chung dữ liệu. Cầu có vết nứt tăng dần khi tiến đến gần, sập trước vùng điều khiển của nhân vật. Rơi mất một mạng và hồi an toàn nếu còn mạng; hết mạng dừng hẳn.

## Bẫy mới BẢN 07

- **Băng rơi:** xuất hiện ở Phố tuyết, treo phía trên với vết nứt, vụn băng và vạch đánh dấu điểm rơi. Sau cảnh báo, mảnh băng rơi theo vị trí thực rồi nằm trên mặt đường để nhảy qua. Dấu báo không gây sát thương; phần băng thật kiểm tra theo tam giác chúc xuống.
- **Khe phun nóng:** xuất hiện ở Nắng gắt. Miệng khe báo đỏ trước khi phun, có khoảng tắt an toàn giữa các đợt. Chỉ cột nóng đang hiện mới gây sát thương; vùng va chạm thu hẹp về ngọn và dùng đúng chiều cao đang vẽ, không tính cả hình chữ nhật trống.
- **Biển quảng cáo bị cuốn:** xuất hiện ở Giông bão, báo bằng dây/vệt gió và màu nhạt trước khi thành vật cản. Biển dao động ngang và lên/xuống, giữ hình nằm ngang cùng vùng va chạm để không tái hiện lỗi ảnh xoay lệch collider. Biên độ/tần số tăng dần theo thời gian như bẫy chuyển động khác.
- **Khoảng hồi phục:** tuyết giãn khoảng giữa các cụm theo ít nhất 1 giây tiếp cận để có thời gian phanh; bão giãn theo 0,9 giây. Các map khác giữ nhịp cũ; khoảng cách còn tính chiều rộng, vùng quét và gió. Không ghép ba bẫy mới với một hố hoặc bẫy bay trong cùng cụm.
- Nước/cốc nóng và powerup tránh cả vùng hoạt động tương lai của bẫy mới. Cảnh báo, hình và sát thương dùng chung trạng thái mô phỏng; pause đóng băng chúng, khiên và miễn thương vẫn áp dụng cho va chạm thật.

Ba loại mở sau **15 giây tổng thời gian chạy**. Khi vào map tương ứng, sau ít nhất 4 giây làm quen sẽ có bẫy đặc trưng trong một lượt sinh cụm bình thường; các lần sau tiếp tục xáo ngẫu nhiên. Để thử nhanh, chọn Tuyết/Nắng/Bão làm map đầu và chạy qua mốc 15 giây. Thời gian kích hoạt khe nóng/biển cuốn tăng nhịp theo tốc độ cuộn để chúng không đi qua nhân vật khi vẫn còn ở trạng thái báo trước trong lượt dài. Khe nóng có bốn nhịp chu kỳ để không luôn tắt ở cùng vị trí X. Va chạm ba bẫy mới kiểm tra theo đường bao thật của khung khủng long, tính cả đầu, đuôi, cổ trống, hướng nhìn và tư thế nhảy/cúi.

## Nhân vật và kỹ năng BẢN 08

**Cả ba khủng long đều bắn được khi đấu boss.** Hai con mới có kỹ năng riêng dùng thêm trong lúc chạy hoặc chiến đấu.

| Nhân vật | Giá mở vĩnh viễn | Kỹ năng E / CHIÊU |
|---|---:|---|
| Cổ Dài | Miễn phí | Không có kỹ năng riêng; né, nhảy/cúi và bắn cơ bản. |
| Giáp Sừng — Triceratops | 2.500 sao | Giáp 3 giây, đỡ một đòn; nếu đỡ đạn boss sẽ phản 24 sát thương. Hồi chiêu 30 giây. |
| Tốc Ảnh — Raptor | 5.000 sao | Lướt theo hướng đang di chuyển hoặc hướng nhìn trong 0,35 giây, tốc độ ngang 900; miễn va chạm trong lúc lướt. Hồi 16 giây. |

Mua/chọn trong cửa sổ **Nhân vật** ở menu hoặc cửa hàng. Mua một lần mở vĩnh viễn và tự trang bị; không mất nhân vật khi chết. Không đổi nhân vật giữa lượt. Mua/chọn sau khi chết chuẩn bị lượt tới, hình của lượt vừa chết được giữ nguyên. Giáp/lướt không cứu rơi hố hoặc cạn nước/giữ ấm. Kỹ năng ưu tiên trước khiên tiêu hao; giữ nguyên luật tim dùng theo lượt. Hồi chiêu chạy trong cả đường chạy lẫn đấu trường và đứng yên khi pause. Nhân vật có ảnh chạy, cúi và nhảy riêng; hình và va chạm dùng cùng vị trí/đường bao alpha, không chỉ đổi màu con gốc.

## Boss và đấu tập — BẢN 13

Boss xuất hiện mỗi **10.000 điểm hiển thị**: 10k, 20k, 30k…; ×2 điểm giúp đến mốc sớm hơn. Mỗi mốc kích hoạt đúng một lần. Cảnh chuyển kéo dài **4,8 giây**, tự đưa nhân vật từ vị trí đang chạy tới đấu trường, chưa nhận sát thương hay bắn trong cảnh chuyển. Thời tiết không che đòn boss.

| Mốc đầu | Boss / khu vực | Đòn có báo trước |
|---|---|---|
| 10k | Xe tăng K-01 / khu phong tỏa | Loạt pháo, pháo đánh dấu đất, mìn lăn, drone tấn công bắn vỡ, húc tới, tên lửa hai phía và mưa tên lửa. |
| 20k | Trực thăng H-03 / bầu trời | Cổng điện có khe bay qua, đạn tỏa, laser khóa độ cao, mìn bay, bổ nhào và tên lửa từ hai bên/phía trên. |
| 30k | Tàu ngầm N-04 / cảng chìm | Ngư lôi trước/sau, đàn cá máy, thủy lôi nổi, sonar quét, bom chìm và đột kích. |
| 40k | Vệ binh G-02 / nhà máy | Laser thấp/cao, sóng điện dậm đất, cột hồ quang, drone chắn đường, lao đấm, cầu điện thấp/cao và tên lửa hai phía. |

Ở mốc **20k**, khủng long chạy tới nhặt **ván phản lực hai động cơ**, ván trượt vào dưới chân và mở ra trong nửa giây rồi từ từ cất cánh; thành phố và mặt đường lùi xuống dưới, chuyển sang đấu trường trên trời. Ván vẽ phía trước ở dưới chân, luồng đẩy ở phía sau, nên nhân vật không che mất thiết bị. Giữ **Space / W / ↑** để nâng độ cao, thả để hạ hoặc nhấn thành nhịp; vẫn dùng A/D và J để né/bắn. Có giới hạn độ cao an toàn, không dùng cúi trong trận bay. Khi thắng sẽ hạ cánh trở về đường chạy. Xe tăng và vệ binh có cảnh đi qua trạm phong tỏa/cổng nhà máy, chiến đấu trên mặt đất.

Menu có **Đấu tập boss**: chọn một trong bốn boss, một trong ba khủng long và mức **Làm quen / Tiêu chuẩn**. Có thể thử hai nhân vật chưa mua riêng trong đấu tập, không mở khóa hay đổi nhân vật đã chọn cho lượt chạy. Làm quen có 5 tim, boss còn 65% máu, đạn chậm hơn và báo trước lâu hơn; Tiêu chuẩn có 3 tim với máu/nhịp đòn bình thường. Mặc định mở ở cấp 3, có thể chọn thử cấp boss 1 / 2 / 3 / 5 / 8; phím R tập lại giữ nguyên cấp. Kết quả hiện cấp boss, thời gian, số lần trúng đòn và cho đấu lại hoặc đổi bài tập. Cấp đấu tập không thay đổi lượt chạy thật, HUD phân biệt CẤP với PHA máu 1–3. Đấu tập **không tiêu đồ đã mua, không thưởng/trừ sao, không ghi kỷ lục hay lịch sử lượt chạy**.

Giữ **J / BẮN** để bắn đạn năng lượng tự hướng tới boss, cả ba nhân vật dùng cùng cơ chế; nếu có vật cản phá được phía trước, đạn ưu tiên phá vật cản. Có nhịp bắn 0,24 giây; mỗi viên trúng lúc lõi mở gây 8 sát thương. Boss đổi giữa giáp và hở lõi, thể hiện bằng màu/lớp khóa ở lõi. Có ba pha theo máu: trên 65%, trên 35% đến 65% và không quá 35%; pha cuối hiện CUỒNG NỘ trên HUD. Chu kỳ chọn chuỗi đòn lần lượt **3,9 / 3,5 / 3,2 giây** với boss đất và **4,6 / 4,2 / 3,8 giây** với trực thăng; mỗi pha tăng độ phức tạp. Mọi đợt nguy hiểm có dấu báo riêng, nhìn thấy ít nhất 0,7 giây trước khi kích hoạt; đòn được xếp chờ chưa đến lượt không vẽ dấu báo hoặc gây sát thương. Nhịp trận độc lập với tốc độ chạy. Máu cơ bản cấp 1 là 750 / 800 / 900 / 840 tương ứng xe tăng / trực thăng / tàu ngầm / vệ binh. Boss trong lượt chạy thật bắt đầu ở **cấp 3**, tương đương đúng cấp 3 trong đấu tập. Bốn trận đầu lần lượt ở mốc 10k / 20k / 30k / 40k; sau đó xáo đủ bốn loại và tránh lặp liền nhau. Mỗi vòng bốn trận tăng một cấp: 10–40k cấp 3, 50–80k cấp 4, 90–120k cấp 5… Máu nhân `1 + 0,18 × (cấp − 1)`, tiếp tục tăng qua các vòng sau; số cấp không dừng ở ba pha máu. Tốc độ đạn tăng từ từ theo logarit cấp, tối đa thêm 65%; nhịp giữa các đợt tối đa nhanh thêm 35%. Số lượt lặp tăng theo cấp, mỗi họ chiêu có giới hạn riêng; các đợt có cảnh báo và khoảng né để giữ số vật thể hữu hạn.

Ở mốc **30k**, khủng long tiến tới nhặt **mũ lặn**, đội lên trước khi nhảy khỏi cầu cảng, rồi chìm dần vào đấu trường dưới nước. Kính mũ trong suốt giữ thấy mặt nhân vật; không dùng ván bay dưới nước. Giữ/nhấn nhịp **Space / W / ↑ / BƠI** để bơi lên, thả để chìm chậm, giữ **S / ↓ / LẶN** để lặn nhanh; A/D và J vẫn di chuyển/bắn. Tàu ngầm gọi đàn cá máy từ hai phía theo làn có khoảng trống, bắn ngư lôi, thả thủy lôi nổi bắn vỡ được, khóa làn bằng sonar, rải bom chìm và lao đột kích. Các đợt có báo trước riêng, khoảng né hoặc hành lang trống. Khi thắng, nhân vật bơi về phía trước, nổi lên/cất mũ trước khi qua cổng trên bờ và tiếp tục lượt chạy; không tính điểm hay hao thanh sinh tồn trong trận này.

Chuyển động và phản ứng chiến đấu:

- **Xe tăng:** nén hệ treo/nâng nòng khi tích lực, giật nòng và chạy xích khi bắn, húc thật vào làn người chơi rồi lùi về; loạt pháo tăng từ 2 lên 3 rồi 4 viên, các đợt pháo khóa 1–3 vị trí dồn dập hơn theo pha.
- **Vệ binh:** khuỵu chân/tích điện, lao đấm, đáp xuống rồi hồi thế; ghép laser thấp/cao với sóng điện dậm đất. Cầu điện có lõi tròn, quầng sáng và tia hồ quang, kiểm tra va chạm tròn; cột điện là các nhánh sét động, drone là turbine mang trường plasma.
- **Trực thăng:** đổi góc thân, mở giá tên lửa, bổ nhào vào độ cao đã đánh dấu rồi kéo lên, có lửa động cơ và vệt gió; trận bay yêu cầu đổi độ cao qua khe cổng, né đạn tỏa và rời khỏi làn laser đã khóa.
- **Tàu ngầm:** bốn tư thế thân tàu, mở ống phóng/nâng sonar, chân vịt và van xả bọt; ngư lôi, cá máy và thủy lôi dùng hình riêng, vùng nổ bom chìm là một vòng nước thực tế.
- **Hiệu ứng:** xích/cơ cấu chuyển động, cánh quạt, luồng động cơ, tích năng, chớp nòng, lóe khi trúng đạn, bụi đáp và tia lửa khi bị hạ. Hình boss dùng chính X/Y đang mô phỏng; hiệu ứng không làm hình lệch vùng trúng đạn. Pause/chết giữ nguyên cả chuyển động và hiệu ứng.

Mỗi boss có **bốn ảnh tư thế riêng**: sẵn sàng, chuẩn bị, tung đòn và hồi thế. Đòn lao có vùng đánh ở mũi húc/nắm đấm/đầu máy bay được tô năng lượng rõ; chỉ phần này gây sát thương trong giai đoạn lao, khi chuẩn bị hoặc lùi về không gây sát thương thân. Đòn lao có ít nhất 1,25 giây báo trước và được chọn ngẫu nhiên giữa dừng giữa sân hoặc quét qua hết mép trái. Loại quét dài đi thật ra ngoài màn hình, không dừng ở X100; mũi tên kép chỉ quét hết sân, dấu chặn chỉ điểm dừng của lượt ngắn. Mỗi lượt khóa kiểu và điểm dừng từ lúc báo, không đổi giữa chừng. Các cấp cao có chuỗi lao/đạn/cổng/bắn tỏa nhiều lượt; chiêu đặc biệt giữ đủ thời gian để boss trở về trước lần lao kế tiếp.

**Tên lửa gọng kìm** vào thật từ mép trái và phải, có mũi tên chỉ hướng; trận trên trời chừa khe cao 190 đơn vị ở cấp 1, thu hẹp dần còn 175 ở cấp cao. **Mưa tên lửa** báo riêng từng dải rơi, giữ một hành lang rộng 240 đơn vị ở cấp 1, giảm dần còn 210 ở cấp cao, gần vị trí người chơi khi khóa mục tiêu, không đổi theo người chơi sau khi báo; cạnh hành lang được đánh dấu xanh. Cầu điện robot đi thấp/cao xen kẽ, có thời gian đổi nhảy/cúi giữa các đợt. Các pha sau tăng số đợt, vẫn giữ cảnh báo và đường né.

Trong đấu trường, **điểm, số mét, tốc độ chạy, đồng hồ đổi map, hao/hồi nước/giữ ấm và thời lượng ×2/nam châm đều đóng băng**. Đạn, chiêu boss, hồi chiêu nhân vật và thời gian miễn thương dùng đồng hồ chiến đấu riêng. Tim và khiên đang có được mang vào; không tự hồi tim khi vào boss. Hết mạng kết thúc lượt như cũ; Pause đóng băng cả trận.

Hạ boss thưởng **300 / 600 / 550 / 450 sao** tương ứng xe tăng / trực thăng / tàu ngầm / vệ binh, các vòng sau thêm sao có giới hạn; trả đúng một lần, không cộng điểm chạy. Sau **1,6 giây hiệu ứng hạ boss**, có **3,2 giây tiến về phía trước qua cổng sau boss**. Máy quay đi cùng nhân vật, đấu trường lùi ra bên trái và đường phố tiếp theo hiện từ bên phải; không quay lại cổng vào. Trận máy bay hạ cánh/cất ván, trận dưới nước nổi lên/cất mũ trước khi chạy qua cổng. Khi nối lại map đang chạy, giữ điểm/tốc độ/tài nguyên và đúng vị trí cuộn nền; chỉ lúc bắt đầu chạy tiếp mới tính điểm trở lại. Đường trở lại được dọn an toàn, có 2 giây bảo vệ và tiếp tế phù hợp gần phía trước khi đang ở tuyết/nắng. Nút bắn chỉ hoạt động lúc đang giao chiến; E/CHIÊU chỉ dành cho hai nhân vật đặc biệt.

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

## Tên lửa trong lượt chạy

Từ khoảng **20–28 giây**, hệ thống bắt đầu xếp các đợt tên lửa bay ngang từ mép phải: ngẫu nhiên **1–2 hoặc 3–4 quả**. Cả loạt được phóng cùng lúc nhưng xếp cách nhau theo chiều ngang để thấy từng quả và có thể nhảy/nhảy kép qua. Có **1,2 giây báo trước** bằng mũi tên/làn thấp và số chấm chỉ số quả; chỉ tên lửa thật mới gây sát thương. Nhân vật vẫn chưa được bắn ngoài trận boss.

Đợt tên lửa chờ các vật cản cũ đi khỏi, dành một đoạn đường trống để không ghép với hố hoặc làn bắt cúi; nước/cốc nóng và vật phẩm vẫn sinh bình thường. Tuyết/nắng có một đồ uống thay thế thật trong làn trống sau khi hạ chân hết loạt tên lửa, để bù cơ hội nhặt đồ bị mất khi phải nhảy; vẫn cần nhặt để hồi dần, không tự cộng thanh. Sau đợt, nghỉ rồi chọn khoảng tiếp theo 12–19 giây; càng chạy lâu càng thường gặp loạt 3–4 quả. Tên lửa tăng tốc dần cùng đường chạy. Map đêm hoãn đợt nếu khoảng tối sắp che mất cả loạt. Vào boss xoá đạn/cảnh báo, trở lại có ít nhất 5 giây nghỉ; pause/chết giữ nguyên hình và trạng thái. Khiên/giáp/lướt áp dụng như các va chạm vật lý khác, tên lửa không tự thưởng sao.

## Chạy lâu và tính điểm

- Tốc độ bắt đầu **360 đơn vị/giây**, tăng đều **2,2 đơn vị/giây mỗi giây**, không giới hạn và không nhảy bậc theo chặng. Sau 1/5/10 phút khoảng 1,37×/2,83×/4,67×. Khoảng 20.000 điểm ở mức 3,8× nếu chưa có ×2 điểm.
- Ngoài nhà/vòi/gai/rào/cưa/máy bay có cưa tuần tra, rào di chuyển ngang, máy bay lên xuống, hố đôi và cầu sập. Bẫy nâng cao mở sau 15/30 giây, cao thêm sau 60 giây; cụm được xáo và tránh lặp ngay.
- Bẫy sinh càng muộn càng di chuyển xa/nhanh: với `t` giây, biên độ nhân `1 + 0,35 × √(t/120)`, tần số nhân `1 + 0,4 × √(t/120)`. Khoảng cách cụm tính cả vùng quét và tốc độ tiếp cận. Bẫy bay giữ làn cúi.
- Cưa dùng va chạm tròn với đường bao alpha của đúng khung khủng long; gai dùng tam giác. Hình cưa quay quanh đúng tâm va chạm ở các tỷ lệ Game View. Bỏ chữ CÚI/NHẢY nổi trên bẫy; nút điều khiển vẫn ở dưới.
- **Ảnh chạy mới BẢN 07:** năm khung đạp đất/co chân/đẩy chân/lướt/tiếp đất được vẽ lại theo phong cách pixel art của ảnh nhảy/cúi. Các khung dùng chung kích thước và mốc đầu/chân để không co giãn hoặc giật; hình lật trái/phải và vùng va chạm lấy từ đúng khung đang vẽ. Giữ ba khung nhảy, ba khung cúi và tư thế khi chết. Năm ảnh chạy gốc vẫn được giữ để đối chiếu.
- Mỗi 20 đơn vị thế giới = 1 mét = 1 điểm cơ bản. Lưu kỷ lục, điểm lượt trước và 10 lượt gần nhất; thành tích hiện 5 lượt mới nhất. ×2 tăng điểm, số mét vẫn là quãng đường thật.
- Mô phỏng chia nhỏ bước theo tốc độ để giảm xuyên vật cản; đồng hồ/quãng đường dùng double. Không khẳng định người chơi có thể phản ứng kịp ở mọi tốc độ lớn tùy ý.

Menu mang tên **DINO CITY**, ít thông báo giữa màn; giữ HUD, thanh sinh tồn/vật phẩm, cảnh báo trước tối và kết quả lượt.

**Âm thanh BẢN 13:** có tiếng tích lực, pháo, laser, điện, tên lửa rít, lao tới, trúng đạn/nổ và chuỗi nổ khi kết liễu boss; trận tàu ngầm có sonar, ngư lôi, đàn cá, bọt thở, vào/rời nước và tiếng nổ chìm. Tên lửa từ trái/phải có hướng âm thanh tương ứng. Xích xe tăng, servo robot, cánh quạt trực thăng và động cơ ván bay dùng các vòng âm riêng với âm lượng nền nhỏ; bước chân bám theo thời gian di chuyển mô phỏng. Các dải tên lửa cùng đợt được gom tiếng để tránh chồng quá nhiều. Hiệu ứng hạ boss phát một lần mỗi trận; tạm dừng, mất tiêu điểm, tắt âm, chết hoặc về menu dừng âm đúng trạng thái. Giữ tiếng nhảy, sao, va chạm, mua hàng và sấm cũ; **M / nút nhạc** tắt toàn bộ. Âm thanh được tổng hợp tại runtime theo phong cách game, không cần tải thêm tệp âm.

## Dữ liệu và cấu trúc

Dùng Unity `PlayerPrefs`, khoá **`CityDino.Save.v1`** giữ nguyên, schema tăng lên 4. Giữ ví, kỷ lục, lịch sử, nâng thời lượng và tim/tiếp tế chưa dùng; lưu thêm sở hữu/lựa chọn nhân vật; kỷ lục `HighScore` gốc được nhập khi chưa có hồ sơ mới. Nâng tim vĩnh viễn bản cũ được hoàn **40/140/320 sao** theo cấp đúng một lần. Map đầu lưu ở `CityDino.StartDistrict`, âm thanh ở `CityDino.Muted`. Dữ liệu trên thiết bị hiện tại, chưa đồng bộ tài khoản hoặc có bảng xếp hạng trực tuyến.

- `Assets/CityDino/Core/RunnerSession.cs`: mô phỏng, sinh bẫy/map, sinh tồn, quán tính/gió và dữ liệu lưu.
- `Assets/CityDino/Core/RunnerItems.cs`: cửa hàng, kho hàng, ×2 điểm, nam châm và khiên.
- `Assets/CityDino/Core/RunnerCharacters.cs`, `RunnerCharacterGeometry.cs`: mở/chọn nhân vật, kỹ năng và đường bao riêng.
- `Assets/CityDino/Core/RunnerSurvival.cs`: hàng đợi hồi nước/giữ ấm từ từ.
- `Assets/CityDino/Core/RunnerBoss.cs`, `RunnerBossModels.cs`: mốc điểm, trận đấu, đạn/vùng báo và phần thưởng; `RunnerBossJourney.cs` quản lý đấu tập, chuyển cảnh và bay; `RunnerBossPatterns.cs`, `RunnerBossObstacleModels.cs` quản lý chuỗi đòn/vật cản; `RunnerBossMotion.cs`, `RunnerBossSpecials.cs` quản lý đòn lao và các đợt tên lửa/cầu điện; `RunnerBossDifficulty.cs` quản lý cấp, máu, nhịp và số lượt tung chiêu; `RunnerUnderwaterBoss.cs` quản lý bơi, mũ lặn và sáu chiêu hải chiến; `RunnerRunMissiles.cs` quản lý các loạt tên lửa trong lượt chạy.
- `Assets/CityDino/Core/RunnerGeometry.cs`: tư thế và đường bao va chạm dùng chung.
- `Assets/CityDino/Core/RunnerWeather.cs`: pha chớp và độ tối của bão.
- `Assets/CityDino/Core/RunnerBiomeHazards.cs`: trạng thái báo trước/kích hoạt, hình va chạm băng, khe nóng và biển cuốn.
- `Assets/CityDino/Runtime/CityRunnerController.cs`: input, menu/HUD, cửa hàng, lưu và âm thanh.
- `Assets/CityDino/Runtime/RunnerBossAudio.cs`: theo dõi dấu báo/đạn/vật cản theo ID, âm thanh boss/di chuyển, tổng hợp mẫu âm và dừng/dọn nguồn âm.
- `Assets/CityDino/Runtime/RunnerWorldView.cs`, `RunnerBossView.cs`, `RunnerBossTransitionView.cs`, `RunnerArt.cs`: vẽ thế giới/đấu trường/chuyển cảnh và ánh xạ atlas, cùng hệ toạ độ với core.
- `Assets/CityDino/Resources/CityDino`: ảnh gốc, font tiếng Việt, hai skyline, atlas bẫy, hành động, **`DinoRunAtlas.png`** cho năm khung chạy mới, **`TriceratopsAtlas.png`**, **`RaptorAtlas.png`**, **`BossAtlas.png`**, **`BiomeHazardAtlas.png`** cho ba bẫy theo map và **`PickupAtlas.png`** gồm nước/cốc nóng/khiên/nam châm/×2/bình giữ ấm.
- `Hoverboard.png` trong cùng thư mục: ván phản lực mới, ảnh nền trong suốt, lật theo hướng nhìn; không mở rộng vùng va chạm khủng long. `BossActionAtlas.png` gồm 12 tư thế cho ba boss; `RunnerBossRigView.cs` vẽ các hiệu ứng cơ khí/điện và dấu báo chiêu mới. `OrdnanceAtlas.png` gồm đạn pháo, drone tấn công, mìn và đầu phát plasma, thay các khối hộp bằng ảnh vũ khí/cơ khí riêng.
- `ArenaBackdropAtlas.png`, `ArenaGateAtlas.png`: nền kho hàng hoàng hôn/nhà máy lò phản ứng và hai khung cổng có lối đi trong suốt. `RunnerArenaScenery.cs` thêm đèn quét, hơi nước, bụi/tia lửa, sàn và cửa cuốn mở theo cảnh chuyển. Không phận máy bay giữ nền trời nhiều lớp, có sân đáp khi kết thúc.
- `SubmarineAtlas.png`, `DivingKitAtlas.png`, `UnderwaterBackdrop.png`: bốn tư thế tàu ngầm, mũ kính trong suốt/cá máy/ngư lôi/thủy lôi và cảng chìm. `RunnerUnderwaterView.cs`, `RunnerRunMissileView.cs` vẽ hải chiến và tên lửa chạy theo dữ liệu mô phỏng.
- `Assets/CityDino/Scenes/CityRunner.unity`: scene khởi động.

172 tệp của `Assets.zip` gốc cùng script/scene cũ được giữ nguyên để đối chiếu. Bản mới sửa lỗi đường cuộn không đồng nhất, nhiều script cùng kéo nền, rơi vô hạn và xoay cưa lệch collider.

## Kiểm tra

Cần .NET SDK 8; bộ console ngoài `Assets` chạy trực tiếp mã production, không cần Unity/NuGet:

```bash
./scripts/test-core.sh
./scripts/test-core.sh --input-mapping
./scripts/test-core.sh --render-contracts
./scripts/test-core.sh --audio-contracts
./scripts/test-core.sh --compile-unity
```

Windows có thể dùng `dotnet run --project Tests/RunnerCore.Tests.csproj --configuration Release`. Phạm vi, số kiểm tra và các lượt mô phỏng ở [Tests/README.md](Tests/README.md).

Môi trường đám mây **không có Unity Editor**. Stub kiểm tra biên dịch, phép toán input và lệnh vẽ; không chạy Play Mode, raster hoá ảnh, âm thanh, cảm ứng thật hoặc build Unity. Cần thử trong Unity:

1. Import hoàn tất, Console không lỗi biên dịch; menu hiện BẢN 13 và tiếng Việt đầy đủ.
2. Trái/phải, nhảy kép, cúi, đa chạm và pause; cửa hàng không bấm xuyên.
3. Tuyết trượt/cốc nóng/băng rơi, nắng/nước/khe phun, gió/hố đôi/cầu sập, đêm đen 2–3 giây, bão nháy có mưa/sấm/biển cuốn. Thử lúc cảnh báo, đang kích hoạt và khoảng tắt để phân biệt vùng thật gây sát thương.
4. Giá tim theo số mua, kho hàng lưu khi đóng game và dùng ở lượt sau; nâng thời lượng, ×2, nam châm và khiên.
5. Mua/chọn hai nhân vật, kiểm tra mở khóa vẫn giữ khi đóng/mở game; mỗi con dùng đúng kỹ năng, cả ba chỉ bắn được trong boss. Tới mốc boss, kiểm tra điểm/tốc độ/tài nguyên đứng yên, chuyển động/hiệu ứng của bốn boss và các pha 65%/35%; thắng thưởng sao một lần và quay lại an toàn.
6. Hoạt ảnh chạy mới: nhịp chân rõ, đầu/chân không giật, menu và trong lượt đều dùng ảnh mới; thử cưa lúc chạy/nhảy/cúi, hai hướng và nhiều kích thước Game View. Hết mạng dừng và lưu đúng một lần; chơi lại giữ ví/kỷ lục.
7. Đấu tập: thử bốn boss/cả nhân vật chưa mua, cả hai mức; đấu lại, đổi boss, thoát rồi kiểm tra ví/kho/nhân vật/kỷ lục giữ nguyên. Thử giữ và nhấn nhịp BAY đồng thời di chuyển/bắn trên điện thoại; pause không giữ kẹt lực nâng.
8. Chuyển cảnh: từ phố đi qua cổng với boss đất; nhặt ván → triển khai dưới chân → cất cánh → bầu trời ở 20k → hạ cánh sau thắng. Điểm và các thanh vẫn đứng yên suốt trận, không có mặt đường trong trận bay; ván/luồng đẩy nhìn rõ ở cả ba nhân vật và hai hướng.
9. Chọn các cấp boss trong Đấu tập, thử chuỗi tung nhiều lần và cả lướt ngắn/quét hết trái; khi thắng phải đi qua cổng sau boss, không quay lại cổng vào. Nền và mặt đường nối tiếp không giật. Thử trận 30k: đội mũ → nhảy khỏi cầu cảng → bơi/né/bắn dưới nước → nổi lên và qua cổng. Thử đủ tư thế boss; né đòn húc/lao đấm/bổ nhào, tên lửa từ trước/sau/phía trên, hành lang mưa tên lửa, cầu điện và hồ quang. Pause giữa đòn phải giữ cả hình, hiệu ứng và va chạm; khi thắng huỷ toàn bộ đòn đang chờ.
10. Nghe tiếng tích lực/khai hỏa/điện/lao tới, hướng tiếng tên lửa trái/phải, các cơ cấu di chuyển và chuỗi nổ kết liễu và bước chân khi tiến ra cổng. Thử M, pause, đổi cửa sổ, về menu và tập lại; không giữ tiếng máy chạy nền hoặc phát lại tiếng hạ boss sau khi tiếp tục.

Tên lửa chạy và trận tàu ngầm cần thử thêm trong Play Mode: kiểm tra mũ lặn trên cả ba nhân vật/hai hướng; giữ đồng thời BƠI/LẶN + hướng + BẮN bằng nhiều ngón, cảnh báo loạt 1–4 tên lửa, âm sonar/ngư lôi/bọt nước, mute/pause/chơi lại và kỷ lục/kho giữ nguyên sau đấu tập.
