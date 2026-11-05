# Kiểm tra gameplay ngoài Unity

Cần **.NET SDK 8**. Từ thư mục gốc dự án:

```bash
./scripts/test-core.sh
```

Console chạy trực tiếp các file trong `Assets/CityDino/Core/` bằng C# 9,
không dùng bản sao logic game, không cần thư viện kiểm thử bên ngoài.
Bộ kiểm tra có **139 nhóm hành vi**, gồm:

- Nhảy kép, coyote time, cúi, di chuyển trái/phải trên đất/trên không, giới hạn màn hình và input không hợp lệ.
- Va chạm, nhặt đồ, mặt đường và hồi sau rơi dùng vị trí X hiện tại; chết chỉ ghi kết quả một lần và đóng băng tư thế.
- Tốc độ tăng từ từ không giới hạn, vượt 20.000 điểm, đồng hồ 1/10 giờ và va chạm thích ứng ở tốc độ 8.280/79.560.
- Ngẫu nhiên có seed, khoảng nghỉ giữa các cụm tính cả vùng quét chuyển động; không ghép làn cúi và làn nhảy bất khả thi.
- Gai dùng tam giác; cưa dùng vòng tròn và bóng dáng theo alpha nhân vật, bỏ qua góc/cổ trong suốt; cả hai hướng, sáu khung nhảy/cúi và độ nhấp nhô.
- Năm khung chạy mới dùng alpha thật từ `DinoRunAtlas.png`: đầu giữ vị trí, chân đổi tư thế; kiểm tra đầu/cổ trống ở cả năm khung và hai hướng, rìa răng cưa và bob theo vùng ảnh mới.
- Cưa/rào tuần tra và máy bay dao động; biên độ/tần số tăng theo thời gian, vẫn giữ làn cúi; không trôi sai, pause/reset và bước thời gian nhất quán.
- Tim hiếm, nhặt sao một lần, giới hạn ví/mạng; tim mua dùng một lượt giá **100/300/600**; hoàn nâng cấp v1 đúng một lần và chuyển lưu v2 sang v3 giữ ví/kỷ lục/lịch sử/tim dự trữ.
- Chọn map khởi đầu hoặc ngẫu nhiên; túi sáu map đi đủ sáu map trước khi lặp, không lặp liền nhau ở ranh giới hai túi.
- Đêm có cảnh báo và tối đen hoàn toàn ngẫu nhiên 2–3 giây; chỉ đêm dùng blackout này, pause giữ nguyên và đổi map/chơi lại đặt lại.
- Bão mưa có chớp mỗi 1,8 giây sau mở đầu 1 giây, pha chớp/tia sét dùng chung danh tính sấm; mức tối thay đổi liên tục; tuyết và các map khác không sấm chớp.
- Tuyết có quán tính khi thả/phanh/đổi hướng; gió trên mây đẩy mạnh hơn trên không và có thể điều khiển ngược để giữ X.
- Map mây có 10 giây mở đầu an toàn, sinh nhiều hố, hố kép và nền nứt/sập; nền sập là hố thật và hồi sau rơi không lặp vô hạn.
- Nhiệt/nước có 2 giây nghỉ đầu map, hao đúng tốc độ; nước hồi 30 trong 3 giây, đồ nóng hồi 35 trong 3,5 giây bằng hàng đợi hữu hạn, không nhảy thanh tức thì. Hồi và hao chạy đồng thời; nhặt tiếp chỉ thêm lượng chờ, pause/đấu trường dừng cả hai, chuyển map đặt lại hàng đợi.
- Bình nước/gói ấm dùng một lần giá **150**, khiên dùng một lần giá **200**; mua lưu vào lượt sau, tiêu kho khi bắt đầu. Bình cứu 10 ngay và hồi thêm 50 trong 5 giây, không lặp lại ở lần cạn tiếp theo.
- Khiên chặn đúng một va chạm vật lý, có khoảng miễn thương ngắn; không cứu rơi hố.
- ×2 chỉ nhân quãng đường ghi điểm trong thời gian hiệu lực, gồm bước hết hạn lẻ; không nhân điểm cũ, quãng đường thật hoặc ví sao.
- Nam châm chỉ hút sao trong bán kính, không hút tim/nước; thời gian hết hạn lẻ, gia hạn có giới hạn, pause/chơi lại đúng trạng thái.
- Hai nâng cấp thời lượng **200/400/700/1.100**, từ 8 lên tối đa 16 giây; ba loại powerup sinh ngẫu nhiên trong làn trống.
- Băng rơi, khe phun nóng và biển cuốn mở từ giây thứ 15, chỉ sinh trong map tương ứng và có một lần giới thiệu trong lượt sinh cụm bình thường.
- Cảnh báo không gây sát thương; băng có hình tam giác chúc xuống, cột nóng dùng đúng chiều cao/tam giác đang hiện, miệng khe nguội vô hại. Cả ba bẫy dùng đường bao alpha nhân vật ở hai hướng và mọi tư thế hành động.
- Biển cuốn dùng vị trí dao động thật, không trôi sau nhiều chu kỳ; giai đoạn báo trước/hoạt động đóng băng khi pause và đặt lại khi chơi lại.
- Nhịp khe nóng/biển cuốn tăng theo tốc độ, vẫn có cảnh báo nhìn thấy và trạng thái nguy hiểm thật ở các mốc 600/1.200/3.600 giây; khe có bốn biến thể nhịp.
- Khoảng phục hồi tuyết/bão tăng để dành chỗ phanh và quan sát; tiếp tế/powerup tránh vùng quét của cả ba loại bẫy mới.
- Giáp Sừng/Tốc Ảnh mở vĩnh viễn ở giá 2.500/5.000, chọn trước lượt và không đổi nhân vật chết khi chọn cho lượt sau; dữ liệu cũ chuyển schema 4 giữ ví, tim, nâng cấp và thành tích.
- Giáp đỡ một đòn vật lý trước khi dùng khiên mua; phản đúng một đạn boss. Lướt dùng chuyển động X thật, bảo vệ va chạm trong 0,35 giây; hai kỹ năng có hồi chiêu, đóng băng khi pause và không cứu rơi/cạn thanh.
- Cả ba khủng long đều bắn được chỉ trong giai đoạn chiến đấu boss; Classic không có kỹ năng riêng. Kiểm tra nhịp bắn, projectile thật, intro/victory/menu/pause/death chặn bắn.
- Mốc boss dựa trên điểm hiển thị kể cả ×2; bốn boss đầu Tank/Gunship/Submarine/Sentinel ở mốc 10k/20k/30k/40k đúng thứ tự, các vòng sau xáo đủ bốn loại, không lặp sát nhau. Mốc đã xử lý không tái kích hoạt, kể cả khi điểm chạm giới hạn int.
- Đấu trường giữ điểm/quãng đường/tốc độ/đồng hồ map/nước/thân nhiệt/hồi tài nguyên/×2/nam châm; kỹ năng và đạn vẫn chạy theo đồng hồ chiến đấu. Tạm dừng dừng toàn bộ; tốc độ đạn và chu kỳ boss không đổi khi tốc độ chạy đã là 8.280.
- Boss chết chỉ thưởng sao một lần, không cộng điểm chạy hoặc hồi tim; trở lại làn sạch với bảo vệ ngắn. Tuyết/nắng có đồ uống thay thế nhặt thật, không tự hồi thanh 45 khi miễn thương bỏ qua sát thương.
- Bốn boss đầy máu cấp 3 được khủng long miễn phí hạ bằng đạn và né thật, một mạng, không dùng kỹ năng, không xoá đạn hoặc bật bất tử; chiêu có cảnh báo trước rồi mới tạo vùng sát thương thật.

Các bài va chạm tạo tình huống cụ thể trong danh sách thực thể rồi chạy bước
mô phỏng production. Fixture cơ bản chọn map ngày để tách điều kiện thời tiết.
Các bài đo sinh cụm loại vật cản sau khi ghi đủ cụm, loại powerup và giữ đầy
thanh sinh tồn để chỉ đo generator; đồng thời đặt mốc boss ở int.MaxValue để tách cơ chế chạy. Các bài đồng hồ/tốc độ kéo dài dùng cùng kiểu cô lập. Chúng không được dùng làm bằng chứng sống sót hoặc đánh boss.

Kiểm tra cụm ghi **12 seed × 215 giây**, mở đầu trên đủ sáu map. Phát lại
**131 hình học thực tế × 4 tốc độ 360/900/1.800/3.600 × 2 biên X = 1.048 tình huống**,
tìm nhịp nhảy/nhảy kép/cúi bằng va chạm thật. Mẫu muộn sinh trên **6 map × 3 mốc
600/1.200/3.600 giây × 20 giây**, phát lại 21 hình học chuyển động ở tốc độ
thực, hai biên X và bốn pha (**168 tình huống**). Ba bẫy theo map được phát lại
riêng thêm **36 tình huống** ở ba mốc muộn và hai biên X, trong đó biển cuốn
thử đủ bốn pha chuyển động ngang.

Bài chạy liên tục dùng **6 seed × 2 vị trí X × 215 giây**, mỗi seed bắt đầu ở
một map, giữ vật cản và nguồn nước/đồ ấm production, không bất tử hoặc hồi thanh
miễn phí. Powerup khiên/nam châm/×2 được loại khỏi bài này để tránh che lỗi.
Bot bù gió theo X mục tiêu, tìm nhịp nhảy có dung sai ±25 ms và bốn pha chuyển
động ngang. Với bẫy theo map, replay giữ nguyên vị trí X và đồng hồ giai đoạn;
bot nhảy dựa trên khoảng cách đang thay đổi, dùng cùng quy tắc trong replay và
lượt liên tục. Có **37.064 khung hình đồ tiếp tế hiện trong màn hình** được kiểm tra
không giao vật cản/hố thật trong suốt đường đi, kể cả sau thời điểm sinh. Đây là kiểm tra đường vượt tồn tại, không thay đánh giá độ khó bởi
người chơi và không khẳng định mọi tốc độ lớn tùy ý đều dễ phản ứng.

Bài sinh tồn riêng chạy **12 seed × 2 map tuyết/nắng × 34 giây**, giữ hao thanh,
nhịp sinh, quãng đường di chuyển và va chạm nhặt đồ thật; chỉ bỏ vật cản để tách
độ đủ của nguồn tiếp tế. Nó ghi nhận **208 lần sinh và 192 lần nhặt hồi thanh**,
không tự hồi tài nguyên. Bài tiếp cận muộn thử vị trí sinh production trước/sau
các cụm thật tại 600/1.200/3.600 giây, kiểm tra vùng quét và thời gian tiếp cận;
đây là kiểm tra bố trí, không phải lượt chơi muộn hoàn chỉnh. Có 216 vị trí
sinh được thử, thời gian tiếp cận bằng cuộn đường dài nhất là 0,36 giây.

Đo mây trên **12 seed × 2 mốc 0/90 giây × 34 giây** có **256/422 cụm thuộc nhóm
hố (60,7%)**, gồm 59 hố kép và 84 nền sập. Túi map được kiểm tra thêm ba vòng
sáu map cho cả sáu lựa chọn khởi đầu và chế độ ngẫu nhiên.

Kiểm tra boss tạo lượt chạy ngay trước **mốc điểm thật**, chỉ rút ngắn phần chạy trước trận. Bốn trận thường bắt đầu **cấp 3** theo thứ tự Tank/Gunship/Submarine/Sentinel, giữ nguyên **1.020 / 1.088 / 1.224 / 1.142,4 HP**, đạn, vật cản, cảnh báo, cửa sổ lộ lõi và sát thương production. Bot Classic dùng lệnh A/D, nhảy/cúi hoặc lực nâng/bơi/lặn và bắn thật, không vật phẩm/kỹ năng, không sửa máu, miễn thương hoặc xoá đạn. Cả bốn trận không trúng đòn, kết thúc khoảng **61,1 / 64,3 / 77,2 / 76,6 giây** chiến đấu. Đây là bằng chứng cho các trận riêng sau khi đến mốc, không phải lượt chạy liên tục đến 40k.

Bài truy vết cảnh báo quan sát **122 đạn địch và 71 vùng laser/nổ**, mỗi đợt liên kết bằng `SourceTelegraphId` với chính cảnh báo đã hiển thị đủ thời gian; không chỉ nhớ loại chiêu. Các bài đầu ra/ba pha dựng máu và bảo vệ người chơi để đo đủ chuỗi, **không phải bằng chứng né/sống sót**. Các bài gia đình boss cũ dùng đấu tập cấp 1 rõ ràng để tách chúng khỏi thay đổi boss thường bắt đầu cấp 3. Bài nhịp đạn ở tốc độ chạy 360/8.280 cũng được cô lập như vậy; đòn đặc biệt có khoảng hồi riêng.

Bài đường né muộn dựng pha 3 ở mốc boss 7/8/9. Hai vị trí X100/X720 được đặt **trước khi vượt mốc**, nên đoạn tiếp cận ghi nhận đúng mép xuất phát rồi tự đưa nhân vật vào làn chiến đấu X260 theo cơ chế mới. Sáu trường hợp sống 15 giây bằng né và bắn phá vật cản thật, một mạng, không kỹ năng, miễn thương, xoá đạn hoặc hồi máu. Bot chỉ giữ bắn khi có vật cản phá được phía trước; không tự đổi vị trí sau khi bắt đầu. Đây là bằng chứng có đường vượt với các lệnh hợp lệ, không khẳng định mọi người chơi sẽ phản ứng giống bot. Chuỗi bốn đạn pháo được vượt bằng nhảy kép sớm khi đang đi lên, rồi hạ xuống khoảng nghỉ; không chờ đến cuối cú nhảy để tiêu lần nhảy thứ hai vào đợt kế tiếp.

BẢN 10 thêm mười nhóm: đấu tập đủ bốn boss × ba nhân vật, kể cả thử nhân vật chưa mua mà giữ nguyên toàn bộ JSON lưu/stock và không phát `SaveChanged`; thua/chơi lại không ghi lịch sử; mức Làm quen/Chuẩn thay đổi đúng máu, mạng, nhịp và cảnh báo; thắng tập không thưởng sao; tiếp cận/nhặt thiết bị/cất cánh/hạ cánh liên tục, không tính điểm hay hao thanh; giữ/thả/nhấn Space, giới hạn độ cao, A/D, pause; nguồn cảnh báo/vòng đời vật cản; đạn thật phá vật cản rồi mới chạm boss; va chạm mìn tròn và dọn vật cản. Bài bay thuần tuý bảo vệ nhân vật để tách kiểm tra điều khiển khỏi chiêu boss. Bài va chạm/trạng thái dùng tình huống dựng rõ ràng; các trận đầy máu ở trên giữ nguyên luật sát thương.

BẢN 11 thêm **sáu nhóm**: động tác nạp/lướt/thu hồi dùng vị trí vật lý thật, đầu húc chỉ hoạt động khi tấn công và nằm trong vùng đã báo; chạm phần húc mất mạng thật nhưng rút về vô hại; tên lửa có nguồn báo trước riêng ở cả hai mép và trên cao; mưa tên lửa giữ nguyên hành lang trống qua các đợt; lôi cầu dùng va chạm tròn bỏ qua góc ảnh trong suốt; pause/thắng/chơi lại đóng băng và xoá chiêu đúng lúc. Bài đo 65 giây mỗi boss ghi **11 tên lửa từ trước, 9 từ sau, 28 rơi từ trên và 6 lôi cầu** trong các chu kỳ đã chọn. Bài hình học/hit dựng vị trí cụ thể và dọn các chiêu không liên quan để tách luật cần kiểm tra; các trận thắng đầy máu không dùng cách cô lập đó.

BẢN 12 thêm **bảy nhóm**: cấp boss tăng theo vòng bốn mốc, máu tiếp tục tăng vượt giới hạn cũ; các cấp 1/2/4/8 có đạn nhanh hơn và nhiều đợt thật hơn; lựa chọn lướt ngắn/quét hết sang trái có seed, khóa đích từ cảnh báo, thật sự vượt vị trí X100 rồi ra ngoài mép trái; từng lượt lướt liên hoàn có nguồn báo riêng và hồi về vô hại; chọn cấp trong đấu tập giữ nguyên JSON lưu/kho; đường né ở cấp muộn; rời đấu trường về phía trước qua cổng sau boss.

Phép đo đầu ra **75 giây ở cấp 1/2/4/8** ghi **38/72/85/112** nguy hiểm ở xe tăng, **22/38/45/55** ở robot và **39/73/90/112** ở trực thăng. Chúng bảo vệ người chơi để ghi đủ chuỗi, không dùng làm bằng chứng sống sót. Các phép đo cấp 32/128 xác nhận máu tiếp tục tăng; số đợt và nhịp có giới hạn để giữ cảnh báo rõ và số thực thể hữu hạn.

Bài đường né cấp muộn dùng **ba boss gốc × cấp 2/4/8** (50 giây ở cấp 2/4, 110 giây ở cấp 8), một mạng, không kỹ năng, miễn thương, xoá đạn hoặc hồi máu. Fixture đặt boss vào pha 3 bằng 20% máu ban đầu để thử chuỗi cuối trận, nên đây là bằng chứng đường né chứ không phải thắng boss đầy máu cấp cao. Bot dùng A/D sau dấu báo để đưa các tên lửa đối đầu gặp nhau gần X665 rồi nhảy qua từng cặp; khi cột điện đang chặn đường thoát khỏi dấu báo kế tiếp, bot dừng bước và dùng nhảy kép thật để vượt cột. Trực thăng dùng lực nâng giữ/thả và di chuyển ngang thật. Các trận thắng đầy máu cấp 3 ở trên không dựng pha hoặc thay máu.

Bài rời đấu trường dựng trạng thái thắng riêng rồi kiểm tra cả bốn boss: quãng đường thế giới và camera chỉ tăng về phía trước, nhân vật đi qua cổng ở X1320 rồi màn hình ổn định tại X220 trong phố kế tiếp. Điểm/quãng đường ghi điểm/tài nguyên/mạng đứng yên suốt đoạn chuyển; pause giữ cả camera và nhân vật. Trực thăng thử thêm X100/X260/X720 từ độ cao tối đa: hạ dần, cất ván và chạm đất trước cổng rồi chạy qua, không giật xuống đất hoặc quay lại cửa vào. Đây là kiểm tra chuyển cảnh, không phải bằng chứng thắng trận.

BẢN 13 thêm **11 nhóm**: boss thường bắt đầu cấp 3, tăng theo vòng bốn boss nhưng đấu tập dùng đúng cấp đã chọn; loạt tên lửa chạy 1–4 quả phóng cùng lúc sau cảnh báo 1,2 giây; va chạm tròn/tốc độ cao/giáp/khiên/lướt; pause/chết/reset/vào boss và khoảng nghỉ trở lại; đoạn đường trống không ghép hố/làn cúi và giữ tiếp tế; né tên lửa bằng lệnh thật; đội mũ → nhảy khỏi cầu cảng → lặn; giữ nổi/lặn/nhấn bơi và ưu tiên lặn khi cùng giữ hai nút; sáu chiêu tàu ngầm có nguồn báo hoàn chỉnh, thủy lôi tròn bắn phá được; mặt nước/cổng ra và bảo toàn JSON lưu/kho.

Bài tên lửa ghi **59 loạt thật**, phủ đủ số lượng 1/2/3/4. Bài đường né giữ một mạng với **4 số quả × 3 mốc tốc độ 0/600/3.600 giây × 2 vị trí X = 24 trường hợp**, không xoá đạn hay miễn thương. Lượt chạy liên tục **6 seed × 2 X × 215 giây** giữ tên lửa, bẫy, thời tiết và đồ uống production; bot bù gió, né tên lửa và tìm nhịp nhảy chịu được cả hai hướng nhìn/±25ms. Đồ uống bổ sung sau loạt tên lửa là vật thể thật, cần hạ chân/nhặt và hồi dần, không trực tiếp cấp tài nguyên. Các phép đo riêng nguồn tiếp tế và tỷ lệ hố vô hiệu hoá tên lửa để chỉ đo đúng cơ chế mục tiêu; lượt liên tục không làm vậy.

Các fixture thưởng/chuyển cảnh và chuỗi 12 mốc đặt boss còn 1 HP, loại đạn địch rồi kết liễu bằng đạn người chơi thật để tách ghi nhận thưởng/trở về; chúng không phải bằng chứng vượt boss. Bài đầy máu nói trên không dùng cách cô lập này. Bài nhịp boss so sánh cùng đòn ở tốc độ chạy lưu 360 và 8.280; bài trở về map tuyết/nắng giữ tài nguyên 0 rồi nhặt đồ uống production, không cấp hồi miễn phí.

Có thể chỉ chạy một nhóm khi chẩn đoán lỗi:

```bash
./scripts/test-core.sh --only 'seeded full'
```

Console ghi rõ những bài không được chọn chưa chạy; không truyền tham số sẽ
chạy toàn bộ 139 nhóm.

## Ánh xạ input và đa điểm chạm

```bash
./scripts/test-core.sh --input-mapping
```

**45 kiểm tra** gọi trực tiếp `ApplyPointer`/`ReadPointers` controller production
bằng tọa độ và thiết bị giả: trái/phải, hai hướng triệt tiêu, hai ngón di chuyển
cùng nhảy, crouch, HUD, letterbox và scale màn hình. Thêm J cho cả ba nhân vật, E cho hai nhân vật đặc biệt, ba ngón di chuyển/nhảy/bắn, nút kỹ năng không nhảy, intro/victory/pause và chặn Space/R/J/E xuyên qua cửa hàng/thành tích/chọn nhân vật. Thêm giữ/thả/nhấn Space để bay, BAY cảm ứng cùng di chuyển/bắn, không cúi/nhảy kép trên trời, chặn input trong chuyển cảnh, modal đấu tập và nút thử nhân vật chưa mua. Đây là kiểm tra logic/API tối thiểu; không kiểm tra sự kiện Unity, cảm ứng thật hoặc input từ hệ điều hành.

BẢN 12 kiểm tra cấp đấu tập được giữ khi bắt đầu/tập lại và khóa thao tác trong đoạn tiến ra cổng sau boss. BẢN 11 kiểm tra thêm điều khiển trong khi cả ba boss lao tới với cả ba nhân vật; fixture này bảo vệ nhân vật để tách ánh xạ input khỏi sát thương, không dùng làm bằng chứng thắng boss.

## Hợp đồng toạ độ vẽ

```bash
./scripts/test-core.sh --render-contracts
```

**56 nhóm kiểm tra** gọi `RunnerWorldView.Draw` production và ghi lệnh vẽ:

- Tâm cưa đúng tâm va chạm qua 5 góc xoay × 5 scale/offset; đầu đường không lệch khi letterbox.
- Mỗi loại vật cản có ảnh, không nhãn CÚI/NHẢY nổi; nền sập/hố cắt mặt đường đúng vùng clip.
- Tuyết có mặt đường phủ tuyết và bông tuyết; mưa bão vẽ trên lớp tối và chớp theo đúng pha.
- Blackout đêm phủ RGB=0, alpha=1, không có vật thể thế giới vẽ đè sau.
- Năm pickup mới dùng vùng ảnh atlas riêng, đúng vị trí/UV; gió giữ nguyên khi pause.
- Sáu khung nhảy/cúi đúng UV và hướng lật; ảnh/va chạm dùng cùng tư thế.
- Ba bẫy mới dùng vùng atlas riêng tại đúng vị trí vật lý qua nhiều scale/letterbox; cảnh báo khác trạng thái hoạt động, khe nguội không vẽ cột sát thương và chiều cao cột khớp dữ liệu core.
- Giai đoạn bẫy theo map và hình biển di chuyển không chạy tiếp bằng thời gian ambient khi pause; blackout đêm vẫn phủ kín các vật thể mới.
- Năm khung chạy dùng đúng UV atlas và vị trí trong khung 434×354 chung, lật cả ảnh lẫn vị trí theo hướng; menu dùng ảnh mới, pause/chết giữ khung và có fallback khi thiếu atlas.
- Ba đấu trường tách hẳn cuộn/thời tiết, boss/đạn/dấu ngắm và lõi ở đúng hình học core qua nhiều canvas; cảnh báo khác vùng đang gây sát thương, pause/chết giữ hình.
- Boss BẢN 09 đi vào, tuần tra, giật pháo và bay theo X/Y thật ở nhiều canvas; xích/servo/cánh quạt chạy theo đồng hồ chiến đấu, phản ứng từ đạn trúng/đợt bắn/dậm đất và khói/tia lửa lúc hạ boss đóng băng khi pause/chết.
- Đợt xếp chờ không vẽ sớm; các dấu cảnh báo có nét riêng, drone thấp dùng hình học thật; HUD hiện pha, chiêu và màu thanh máu đúng trạng thái.
- Hai nhân vật mới dùng đúng texture/UV/vị trí cho đủ 11 khung logic, lật cả vị trí và ảnh, menu và trận dùng cùng dữ liệu; HUD hiện máu boss/điểm dừng và modal thực sự vô hiệu hoá nút phía sau.
- Chuyển cảnh giữ nền cũ rồi đưa đường phố xuống dần khi cất cánh; đấu trường trực thăng không vẽ mặt đường. Thiết bị bay nhặt/đeo dùng đúng UV, nhân vật và vật cản bay dùng cùng hình học core; menu đấu tập/kết quả có vùng bấm riêng.
- BẢN 11: bốn ảnh động tác boss đọc đúng ô PNG/alpha và có ảnh dự phòng; hình lao tới cùng vùng húc vật lý, lôi cầu tròn với cung điện, mũi/lửa đuôi tên lửa theo hướng bay, mưa tên lửa chỉ báo đúng các dải thật và giữ trống hành lang né. Pause/chết giữ cả khung động tác và hiệu ứng.
- BẢN 12: ảnh đạn/drone/mìn và đầu phát plasma dùng đúng ô atlas; lõi va chạm tròn không xoay lệch tâm; dấu lướt ngắn/quét hết sân khác nhau; cấp boss hiển thị riêng pha máu. Cổng dùng lối alpha trong suốt; sau thắng, cổng sau boss và phố đích dịch sang trái theo máy quay, phố vào từ bên phải; cả ba boss qua ba canvas, máy bay đáp/cất ván trước cổng, pause giữ nguyên cảnh.
- BẢN 13: cảnh nhận mũ/nhảy/lặn, kính trong suốt tại đầu cả ba loài/hai hướng/nhiều canvas, bốn tư thế tàu ngầm, cá/ngư lôi/thủy lôi/vòng nổ nước theo hình học thật, nổi lên trước cổng. Tên lửa chạy có cảnh báo vô hại và thân riêng từng quả; hình/điều khiển/âm thanh dừng khi pause và không mở súng ở ngoài boss.
- Bộ giải mã PNG bằng thư viện .NET chuẩn đọc alpha ảnh thật, độc lập với bảng mask: 1.007 điểm trong phần đặc và 2.278 điểm trống qua hai nhân vật × sáu tư thế × hai hướng khớp với va chạm tròn production. Đây là phân tích pixel nguồn, không phải Unity rasterization.

Stub dùng phép toán affine `System.Numerics`, texture giả lấy kích thước từ
PNG thật và ghi lệnh draw, **không raster hoá ảnh hoặc chạy Unity**. Hợp đồng
xoay tham chiếu [UnityCsReference GUIUtility.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.4/Modules/IMGUI/GUIUtility.cs#L404).

## Âm thanh boss và di chuyển

```bash
./scripts/test-core.sh --audio-contracts
```

**12 nhóm kiểm tra** chạy bộ theo dõi sự kiện âm thanh và controller production qua API stubs: dấu báo/đợt đạn phát một lần, gom các dải mưa tên lửa, hướng âm trái/phải, laser/vùng nổ/vật cản, kết liễu đúng một lần và không phát lại khi bỏ tắt tiếng/tiếp tục. Theo dõi ID hữu hạn trong các chuỗi boss dài; kiểm tra vòng âm cơ cấu/ván bay và dừng khi mute, pause, mất tiêu điểm, về menu hoặc huỷ controller. Bước chân chỉ phát khi ở mặt đất và đang di chuyển phù hợp. Mẫu PCM của các hiệu ứng và vòng âm cơ khí/dưới nước được kiểm tra hữu hạn, có tín hiệu, biên độ được giới hạn; âm kết liễu có bốn đợt nổ. BẢN 13 kiểm tra thêm tiếng tên lửa chạy một lần mỗi loạt, sonar/ngư lôi/đàn cá, vào/rời nước và nổ chìm; tiếng máy tàu/bọt thở dừng ngay khi mute/pause/rời trận. Các lượt lướt liên hoàn cấp cao phát riêng một tiếng mỗi lượt, không phát lại sau mute/pause; bước chân rời đấu trường chỉ phát sau thời gian nổ và khi đã chạm đất.

Các fixture ghi sự kiện nguồn âm và dữ liệu PCM, **không phát ra thiết bị âm thanh hoặc đánh giá chất lượng nghe trong Unity**. Các vòng audit boss có bảo vệ người chơi để quan sát đủ chiêu; chúng không phải bằng chứng né/thắng trận.

## Kiểm tra biên dịch tích hợp

```bash
./scripts/test-core.sh --compile-unity
```

Lệnh chỉ biên dịch toàn bộ script runtime mới và script cũ bằng chữ ký API Unity
ở `UnityCompile/UnityStubs.cs`; không chạy Unity Editor, không chứng minh import,
serialize, input thật, đồ họa, âm thanh hoặc build Unity hoạt động. Vẫn cần kiểm
tra Play Mode theo README chính. Stub và console test ở ngoài `Assets` nên Unity
không nhập chúng vào trò chơi.
