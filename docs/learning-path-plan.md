# Kế hoạch học theo lộ trình, quiz, phỏng vấn và ôn tập ngắt quãng

Cập nhật lỗi 25/10/2026: DB đã tồn tại không tự nhận hai bảng `skill_practice_progresses` và `special_practice_attempts` từ `EnsureCreated`, khiến năm API tiến độ kỹ năng và catalog thi/phỏng vấn trả 500. Startup nay tạo bổ sung hai bảng theo schema hiện tại. Giao diện thi/phỏng vấn phân biệt lỗi tải với trạng thái chưa có bài và chỉ dùng thông báo cho người học. Câu điền từ hiển thị nghĩa, loại từ và bản dịch ví dụ làm gợi ý; JSON mẫu được hướng dẫn soạn câu có ngữ cảnh.

Cập nhật 25/10/2026: Chốt một nguồn lịch ôn là `CardSchedule` và một nguồn sự kiện là `ReviewEvent`. Mỗi bài từ vựng có **một phiên học**, chia nội bộ thành cụm tối đa ba từ; vòng 1 đúng hết thì hoàn thành bài ngay, còn sai thì vòng 2 chỉ lặp lại dạng bài/từ sai. Hoàn thành phiên tạo lịch SRS một lần cho từng từ nhưng chưa tính là một lượt ôn (`Repetitions = 0`, chưa có `LastReviewedAt`); từ đúng đến hạn sau một ngày, từ sai sau 10 phút. Tab Ôn tập tách hàng đợi **đến hạn** (đã có lịch và `DueAt <= hiện tại`) với **thẻ mới** (chưa có lịch); thẻ có lịch tương lai không xuất hiện ở hai hàng đợi. Bộ lọc bộ thẻ áp dụng đồng thời cho số liệu và hàng đợi. Một lượt đánh giá cập nhật lịch kế tiếp, vì vậy thẻ biến khỏi hàng đợi cũ và chỉ trở lại khi đến hạn. Phần phân nhóm ghi nhớ/giới hạn từ mới theo ngày vẫn chưa triển khai.

Cập nhật 24/10/2026: Bổ sung hai nhánh còn thiếu trong lộ trình: **Luyện thi chứng chỉ** theo chứng chỉ/phiên bản đề, và **Luyện phỏng vấn** theo nghề/trình độ. Hai nhánh có nội dung, phiên làm bài, tiến độ, cách đánh giá và JSON mẫu riêng; bài quiz cuối Lesson không được tự gọi là đề thi chứng chỉ. Xem mục 3.6–3.7 và phần dữ liệu/triển khai bên dưới.

Cập nhật 23/10/2026: Nghĩa trong bài ghép cặp được đảo để không nằm cùng hàng; đường nối bám vị trí nút khi đổi kích thước. SRS phân biệt chưa có thẻ đến hạn với phiên vừa hoàn tất và hiển thị lần ôn tiếp theo. Nghe, đọc, ngữ pháp lưu tiến độ từng bài vào DB và chỉ đánh dấu hoàn thành khi BE chấm đúng. Nói, viết lưu bài đã luyện/bản nháp và vị trí tiếp tục nhưng chưa tự đánh dấu đạt khi chưa đánh giá nội dung. Import khóa học không tạo bộ thẻ; `decks: []` trong mẫu chỉ giữ tương thích file cũ. Mỗi bài từ vựng vẫn chia thành nhóm tối đa ba từ khác nhau; UI hiện rõ từ của nhóm kế tiếp. Sau vòng đầu hoàn toàn đúng, người học được hoàn thành nhóm ngay. Nếu có lỗi, vòng ôn chỉ lặp lại dạng bài/từ đã sai, rồi mới luyện bù lỗi còn lại. Tiến độ và lịch SRS được lưu khi hoàn thành nhóm.

Trạng thái: đã duyệt hướng triển khai và đang thực hiện theo từng mốc. Những mục chưa đánh dấu hoàn thành vẫn là kế hoạch, chưa được coi là tính năng đã giao.

## Tiến độ triển khai

- Đã có hai tab **Luyện thi chứng chỉ** và **Luyện phỏng vấn**, liên kết từ Lộ trình. Admin quản lý riêng IELTS/TOEIC và phỏng vấn: tạo/sửa/xóa từng mục, biên soạn câu hỏi trên UI, nhập nhiều mục bằng JSON và tải mẫu. Import gộp theo ID và kiểm tra toàn bộ nội dung trước khi lưu. BE lưu snapshot đề, từng câu, tiếp tục phiên, tự nộp khi hết giờ theo `durationMinutes`, chấm câu khách quan; bài nói có thể ghi âm để admin nghe và chấm theo rubric. Đã có kiểm thử tích hợp cho phân quyền, CRUD, lưu/tiếp tục, hết giờ, ghi âm, nộp và chấm. Chưa có ngân hàng đề chuẩn hoặc quy đổi band/điểm chính thức.

- Đã thêm icon Sổ tay dùng toàn ứng dụng, lưu/xem/sửa/xóa ghi chú trong DB qua API có xác thực; chọn văn bản chỉ tạo bản nháp, phải bấm Lưu.
- Đã chuyển vị trí điều hướng: Ôn tập thay Bảng xếp hạng; Bảng xếp hạng nằm trong nhóm tiến độ/hệ thống. Tab Ôn tập đã có tổng số đến hạn, đang học và chưa gặp từ BE.
- Đã cập nhật JSON mẫu khóa học tải từ admin với exampleTranslation, clozeSentence và acceptedAnswers; BE validate các trường luyện tập mới nếu có. Có [mẫu ba từ tiếng Anh](templates/vocabulary-practice-en-example.json) và [hướng dẫn điền mẫu](templates/README.md).
- Đã có một phiên học từ mới cho mỗi bài, chia cụm tối đa ba từ, với vòng 1 gồm nghe chọn nghĩa → điền chỗ trống → ghép cặp → nghe gõ lại. Đúng hết vòng 1 có thể hoàn thành ngay; vòng 2 chỉ luyện lại câu sai. BE chốt câu hỏi, chấm từng bước và lưu vị trí/kết quả trong DB.
- Hoàn tất phiên sẽ tạo thẻ và lịch SRS theo từng từ trong cùng giao dịch DB, không tạo lịch trùng với thẻ đã có. Từ trả lời sai được lên lịch học lại sớm. Tab Ôn tập có hàng đợi thẻ đến hạn và thẻ mới chưa có lịch; hành động Học từ mới đi vào phiên học từ vựng riêng.
- Trang Lộ trình hiện lấy cấu trúc khóa/Unit/bài học đã xuất bản và trạng thái bài từ DB theo mục tiêu học hiện tại; hiển thị số bài hoàn thành và mở được bài tiếp theo. Nút Tiếp tục học ưu tiên bài đang học dở, sau đó tới bài chưa hoàn thành đầu tiên. Đây là tiến độ giáo trình thực tế, chưa phải hệ thống mẫu lộ trình theo nghề/mục tiêu có phiên bản.
- Đã chặn API tự đánh dấu bài Completed và bỏ nút tự hoàn thành ở FE. BE tự ghi hoàn thành khi đạt quiz hoặc hoàn thành phiên từ vựng của bài không có quiz; cập nhật lịch SRS và tiến độ bài trong cùng giao dịch. Việc nộp lại quiz đạt không cộng trùng thống kê ngày. Các kỹ năng khác vẫn cần quy tắc bằng chứng và đánh giá riêng trước khi tính hoàn thành.
- Đã chỉnh lại điểm vào theo phản hồi UI: tab Khóa học trong sidebar đổi thành **Từ vựng** và chuyển vào nhóm Từ vựng & Ôn tập, đặt cạnh tab Ôn tập. Tab Từ vựng hiển thị các Unit/bài có nhóm từ, tiến độ nhóm, số thẻ SRS đến hạn và nút ôn; chọn nhóm mở ngay phiên học theo vòng lặp. Màn giới thiệu từng từ có âm thanh, nghĩa, ví dụ và nút Tiếp tục một lần bấm; kết thúc một nhóm có thể tiếp tục nhóm kế tiếp cùng bài. Tab Ôn tập mở hàng đợi đến hạn trên tất cả bộ thẻ theo hạn sớm nhất và có lối quay về học từ mới. Trang chi tiết giáo trình cũ ở `/curriculum` cho các liên kết tiếp tục học/quiz, không còn là màn chính của tab Từ vựng. BE chia cả nhóm cuối 1–2 từ; với JSON cũ, chỉ suy ra câu điền từ khi câu ví dụ thật sự chứa từ cần học. Bài thiếu dữ liệu vẫn báo rõ để admin sửa, không tạo bài sai.
- Build FE và biên dịch BE đã chạy sau các chỉnh sửa gần nhất. Bộ integration test mới kiểm tra phiên nhiều cụm và tính nhất quán hàng đợi SRS; lần chạy test gần nhất bị dừng vì test host không kết nối được, nên chưa xác nhận tất cả test đều đạt. Chưa kiểm thử trực quan trên trình duyệt với DB MySQL thực tế.
- Còn triển khai: mẫu lộ trình và mục tiêu nghề/người mới bắt đầu lưu DB, bằng chứng hoàn thành nghe/nói/đọc/viết và từng chặng; quiz tự do và bài cuối chặng; ngân hàng đề IELTS/TOEIC được biên soạn và kiểm định, quy đổi điểm theo bộ đề, giới hạn ngày và phân nhóm SRS chuyên sâu; kiểm thử E2E toàn bộ hành trình trên MySQL thật.

## 1. Nguyên tắc và phạm vi

- Tái sử dụng sidebar Lộ trình, giáo trình Course → Unit → Lesson, mục tiêu học đã lưu trong DB, bài luyện bốn kỹ năng, bài kiểm tra, deck và lịch SRS hiện có.
- Mục tiêu muốn đạt tới được lưu riêng với trình độ hiện tại dùng để kiểm soát quyền truy cập. Chọn mục tiêu B1 không tự mở khóa giáo trình B1.
- Mọi nội dung, câu trả lời, tiến độ, lịch ôn và kết quả kiểm tra lưu trong DB. BE quyết định quyền truy cập, thứ tự bước và trạng thái hoàn thành; FE chỉ hiển thị và gửi thao tác.
- Giao diện theo hệ thống thiết kế hiện có. Hai ảnh tham khảo chỉ định chức năng: học thẻ lật và quiz từng câu, có âm thanh, tiến độ, thoát/tiếp tục và kết quả.
- Nội dung được gắn mã ngôn ngữ, trình độ, lĩnh vực/nghề nghiệp và thứ tự Unit. Không trộn các trình độ hay ngôn ngữ trong cùng một lộ trình.
- Tab **Ôn tập** chiếm vị trí Bảng xếp hạng trong điều hướng học tập. Bảng xếp hạng chuyển xuống khu vực Thống kê/Cộng đồng và không chiếm chỗ của hoạt động học hằng ngày. Giao diện mới dùng hệ thống thiết kế của ứng dụng; các ảnh chỉ mô tả chức năng.
- **Sổ tay** có nút nổi cố định bên phải trên mọi trang; chỉ lưu từ hoặc ghi chú khi người dùng chủ động xác nhận. Việc bôi đen văn bản không tự lưu.

## 2. Các loại lộ trình

| Loại | Đầu vào | Nội dung đề xuất | Điều kiện |
| --- | --- | --- | --- |
| Người mới bắt đầu | Ngôn ngữ, thời gian học/ngày, mục tiêu giao tiếp | Nền tảng A1 theo thứ tự Unit, từ vựng và cấu trúc cơ bản | Tuân thủ quy tắc xác định trình độ/placement hiện hành |
| Nâng trình độ | Trình độ hiện tại đã xác nhận, trình độ đích, kỹ năng ưu tiên | Giáo trình kế tiếp theo thứ tự, bổ trợ kỹ năng yếu | Chỉ mở nội dung trong phạm vi trình độ được phép |
| Theo nghề nghiệp | Trình độ hiện tại, nghề/vai trò, mục tiêu sử dụng, kỹ năng ưu tiên | Từ vựng và tình huống nghề nghiệp cộng với giáo trình nền; cuối chặng có phỏng vấn | Nghề nghiệp phải có nội dung đã xuất bản phù hợp ngôn ngữ và trình độ |

Mỗi người dùng có một lộ trình đang hoạt động cho mỗi ngôn ngữ; các lộ trình cũ giữ lịch sử. Đổi mục tiêu hoặc trình độ tạo phiên bản lộ trình mới hay tạm dừng lộ trình cũ sau khi người dùng xác nhận, không xóa kết quả đã học.

## 3. Luồng học chính

**Xác định mục tiêu → Từ vựng → Hiểu cấu trúc → Luyện nghe, nói, đọc, viết → Bài kiểm tra cuối chặng → Phỏng vấn theo nghề và trình độ.**

SRS chạy song song sau khi người học gặp từ vựng, được nhắc mỗi ngày và có thể mở từ mọi bước. SRS không khóa việc chuyển sang bài học kế tiếp.

### 3.1. Xác định mục tiêu

1. Người học chọn ngôn ngữ, loại lộ trình, nghề/vai trò nếu có, trình độ muốn đạt, thời gian học/ngày và kỹ năng ưu tiên.
2. BE đọc trình độ/placement hiện tại và các khóa học đã xuất bản, trả những lộ trình có thể bắt đầu và phần đang bị khóa kèm lý do.
3. Người học xem trước các chặng, thời lượng ước tính và yêu cầu hoàn thành rồi bắt đầu. BE lưu lựa chọn và phiên bản nội dung.
4. Sidebar Lộ trình hiển thị chặng hiện tại, bước kế tiếp, số bước đã xong, bài đang học dở và số thẻ đến hạn. Nút “Tiếp tục học” dẫn đúng hoạt động chưa hoàn thành gần nhất.
5. Nếu chưa có giáo trình/nội dung phù hợp, hiển thị trạng thái thiếu nội dung rõ ràng; không tạo lộ trình rỗng hay dùng dữ liệu mẫu.

### 3.2. Học từ vựng theo hướng đã chọn

1. Mỗi chặng lấy từ các bài thuộc Unit đúng thứ tự, lọc theo ngôn ngữ/trình độ/nghề và từ vựng đã xuất bản.
2. **Thay luồng học từ vựng hiện tại** bằng phiên học chủ động ở mục 4. Chọn tối đa ba từ mới trong cùng chủ đề/Unit cho một nhóm; sau mỗi nhóm chạy chuỗi bài tập và lặp lại chuỗi đó hai vòng. Tiếp tục nhóm ba từ kế tiếp cho tới hết phiên.
3. Khi mở mỗi từ, phát âm thanh nếu trình duyệt cho phép tự phát; luôn hiện ngay từ, phiên âm, loại từ, nghĩa phù hợp, các nghĩa khác, ví dụ có bản dịch và nút nghe lại. Nếu trình duyệt chặn tự phát, hiện nút phát rõ ràng; không chặn người học tiếp tục.
4. “Đã xem” chỉ ghi nhận tiếp xúc. Từ chỉ được tính học xong phiên khi hoàn tất các lượt kiểm tra; câu sai được hỏi lại trong phiên và đưa vào ôn SRS sau phiên theo quy tắc mục 5.
5. Từ vựng thiếu nghĩa, đáp án phân biệt được, ví dụ/âm thanh cần cho dạng bài tương ứng hoặc các trường bắt buộc không được dùng để sinh bài đó. Admin thấy danh sách nội dung cần bổ sung.

### 3.3. Hiểu cấu trúc

1. Hiển thị quy tắc/ngữ pháp theo bài học và ngữ cảnh nghề nghiệp khi có: mẫu câu, giải thích, ví dụ đúng/sai, biến thể và trường hợp sử dụng.
2. Người học làm vài bài áp dụng trực tiếp (chọn đáp án/điền từ/sắp xếp nếu dữ liệu hỗ trợ). BE chấm các dạng có đáp án xác định.
3. Kết quả và bài làm dở được lưu; chưa đạt thì gợi ý quay lại đúng cấu trúc liên quan. Không dùng việc chỉ mở trang làm bằng chứng hoàn thành.

### 3.4. Luyện bốn kỹ năng

| Kỹ năng | Hoạt động | Bằng chứng hoàn thành |
| --- | --- | --- |
| Nghe | Nghe đoạn ngắn/hội thoại phân vai, trả lời câu hỏi | Lượt làm và điểm do BE chấm |
| Nói/phát âm | Ghi âm hoặc trả lời theo lượt, phát lại và nhận phản hồi | Bản ghi, trạng thái đánh giá, điểm/rubric |
| Đọc | Đọc đoạn văn, câu hỏi hiểu ý và từ trong ngữ cảnh | Lượt làm và điểm do BE chấm |
| Viết | Viết theo đề, lưu bản nháp, nộp và xem nhận xét | Bài nộp, trạng thái đánh giá, điểm/rubric |

Một kỹ năng chỉ hoàn thành khi có bằng chứng đạt ngưỡng. Với nói/viết, nếu chưa có bộ chấm tự động đủ tin cậy thì trạng thái là “Chờ đánh giá”; admin/giáo viên chấm theo rubric. Không tự cho đạt vì đã tải tệp hoặc nhập văn bản. Lộ trình hiển thị rõ kỹ năng nào đã đạt, cần làm lại hoặc đang chờ.

### 3.5. Bài kiểm tra cuối chặng

1. Khi bốn kỹ năng và các bước nền tảng đạt yêu cầu, BE mở bài kiểm tra của chặng/Unit. Đây là bài đánh giá lộ trình, tách khỏi bài placement và quiz ôn từ.
2. Bài có câu nghe, đọc và phần nói, viết nếu chặng yêu cầu; câu hỏi và thang điểm được chốt theo phiên bản lúc bắt đầu lượt thi.
3. BE chấm phần khách quan, gộp kết quả nói/viết sau đánh giá, tính điểm từng kỹ năng và tổng điểm. Ngưỡng đạt do admin cấu hình theo mẫu lộ trình.
4. Trượt thì chỉ rõ kỹ năng/chủ đề yếu, gợi ý bài ôn tương ứng, cho thi lại theo chính sách cấu hình. Đạt thì mở chặng tiếp theo. Lượt cũ giữ nguyên.

### 3.6. Phỏng vấn theo nghề nghiệp và trình độ

1. Sau chặng phù hợp, chọn tình huống phỏng vấn đã xuất bản cho ngôn ngữ, trình độ và nghề/vai trò: ví dụ giới thiệu bản thân, kinh nghiệm, xử lý tình huống.
2. Hệ thống hỏi từng lượt; người học trả lời bằng văn bản hoặc ghi âm. Lưu phiên, câu hỏi, phản hồi và thời gian để tiếp tục sau khi rời trang.
3. Đánh giá bằng rubric về mức phù hợp nội dung, từ vựng nghề, cấu trúc, độ rõ ràng và phát âm (chỉ khi có âm thanh và công cụ đánh giá tương ứng). Trả nhận xét cụ thể và gợi ý luyện lại.
4. Kết quả phỏng vấn là một nhánh luyện tập riêng. Chỉ chấm tự động khi dịch vụ đánh giá thực sự hoạt động; nếu không, chuyển chờ người chấm. Không hiển thị điểm AI giả.

### 3.7. Luyện thi chứng chỉ IELTS và TOEIC

1. Người học chọn chứng chỉ và đề theo ngôn ngữ, trình độ, mục tiêu đang học. Đề IELTS và TOEIC được quản lý riêng với quiz cuối bài; mỗi đề có nhiều phần và câu hỏi, đáp án, điểm tối đa và rubric.
2. Bắt đầu đề tạo một phiên cố định. Câu trả lời lưu từng bước để tiếp tục sau khi rời trang. Nộp bài chấm ngay câu nghe/đọc có đáp án xác định; câu nói/viết chuyển cho admin chấm thủ công và nhận xét.
3. Hiển thị điểm thô và trạng thái chờ chấm hoặc đã chấm. Không quy đổi thành band IELTS hay điểm TOEIC chính thức khi chưa có bộ đề và quy tắc quy đổi được kiểm chứng.
4. Quản trị viên nhập đề qua JSON mẫu, xem hàng đợi chấm tự luận, chấm từng câu trong giới hạn điểm và gửi phản hồi cho người học. Nội dung cũ của phiên đã bắt đầu không thay đổi khi đề được cập nhật.

## 4. Phiên học từ mới, quiz và thẻ lật

### Luyện thi chứng chỉ và phỏng vấn: phạm vi triển khai đầu tiên

- **IELTS:** đề luyện theo Listening, Reading, Writing và Speaking. Người học làm từng câu, hệ thống lưu đáp án để tiếp tục; BE chấm câu nghe/đọc có đáp án rõ ràng. Viết/nói chuyển chờ admin chấm theo rubric. Điểm trả về là điểm thô của bài luyện, chưa phải band IELTS.
- **TOEIC:** nội dung tách đề Listening & Reading và Speaking & Writing. Cơ chế lưu, chấm và nhận xét giống IELTS. Chưa quy đổi điểm luyện sang thang điểm TOEIC chính thức.
- **Phỏng vấn:** tình huống theo nghề và trình độ, nhiều lượt hỏi, trả lời bằng văn bản, bản chép lời hoặc ghi âm. Phiên lưu từng lượt để tiếp tục. Admin nghe bản ghi, chấm theo rubric và gửi nhận xét. Bản chép lời không phải phép đo phát âm.
- Nội dung được quản lý bằng hai mảng `certificateExams` và `interviewScenarios` trong JSON khóa học. Admin có CRUD riêng cho từng đề/tình huống, form biên soạn câu hỏi, import JSON theo từng loại và mẫu tải về. Phiên làm bài lưu snapshot nội dung để thay JSON không đổi đề đã bắt đầu; bản làm từ đề bị xóa vẫn truy cập được. Đề có thể đặt `durationMinutes` để BE tự nộp khi hết giờ.
- Trên sidebar và lộ trình có lối vào hai nhánh. Khi không có nội dung phù hợp với ngôn ngữ, trình độ và mục tiêu, hiện trạng thái trống rõ ràng. Một bài quiz Lesson không được tự coi là đề IELTS/TOEIC.


**Phiên học từ mới là luồng chính**, thay cách học từ vựng hiện tại. Quiz tự do và thẻ lật là chế độ bổ sung cho deck/kho từ; không thay thế các bước bắt buộc của phiên.

### 4.1. Nhịp của một phiên học từ mới

Một nhóm gồm tối đa ba từ mới. Với nhóm có ba từ, trình tự cố định:

1. **Gặp từ 1 → từ 2 → từ 3:** lần lượt mở thẻ đầy đủ thông tin và âm thanh như mục 3.2. Người học nghe lại, đọc ví dụ rồi bấm Tiếp tục.
2. **Vòng luyện 1:** nghe và chọn nghĩa → điền từ vào chỗ trống theo câu có ngữ cảnh → ghép cặp từ ngôn ngữ đang học với nghĩa tiếng Việt → nghe và gõ lại từ.
3. **Vòng luyện 2:** lặp lại đúng bốn dạng bài theo thứ tự trên cho cùng ba từ, nhưng đảo thứ tự từ/câu, đáp án nhiễu và ví dụ khi có dữ liệu khác. Không lặp nguyên câu hỏi và vị trí đáp án.
4. **Phản hồi nhóm:** hiển thị từng từ đã đúng/sai ở mỗi dạng, giải thích, phát âm lại; câu sai được luyện bù trước khi kết thúc nhóm. Sau đó chuyển sang nhóm ba từ tiếp theo.

Nhóm cuối có một hoặc hai từ vẫn dùng cùng thứ tự, với số cặp/đáp án phù hợp; BE không tạo lựa chọn giả hay câu ghép cặp vô nghĩa để đủ số lượng. Nếu thiếu dữ liệu hợp lệ cho một dạng, BE báo nội dung thiếu để admin bổ sung và không âm thầm đánh dấu đạt dạng đó. Một phiên nên chia thành các nhóm ngắn để có thể dừng; BE lưu vị trí đến cấp độ nhóm, vòng và câu.

**Cách chấm:** bốn dạng đều có kết quả riêng. Chuẩn hóa câu gõ theo quy tắc của từng ngôn ngữ (Unicode, khoảng trắng, chữ hoa/thường; dấu/biến thể chính tả chỉ chấp nhận khi nội dung khai báo). Điền chỗ trống có thể có nhiều đáp án hợp lệ do admin khai báo. Ghép cặp chỉ kết thúc khi ghép đúng toàn bộ; lượt sai vẫn được ghi. Kết quả BE quyết định hoàn tất nhóm; mở thẻ hoặc bấm Tiếp tục không tự tính là nắm vững.

### 4.2. Quiz tự do và thẻ lật

- Quiz độc lập trên deck/chủ đề có phiên ngắn, mặc định 10 câu, tối đa 20 nếu đủ dữ liệu; hiển thị câu hiện tại/tổng số, thanh tiến độ và kết quả. Nó không làm thay bài kiểm tra cuối chặng hay hai vòng luyện bắt buộc ở trên.
- Thẻ lật để xem hai mặt, nghe, tiến/lùi và xem đầy đủ nghĩa/ví dụ; việc lật thẻ không cộng điểm quiz hoặc tự đổi lịch SRS.
- Câu hỏi và thứ tự được BE tạo, chốt theo phiên; không gửi đáp án đúng trước khi công bố. Các lựa chọn khác nhau, cùng ngôn ngữ/trình độ, ưu tiên cùng loại từ/chủ đề. BE kiểm tra quyền, chấm, lưu câu đang làm để thoát/tiếp tục trên thiết bị khác và chống nộp trùng.
- Kết quả hiển thị từ yếu và nút học lại/ôn SRS. Quiz tự do không tự ghi một lượt SRS “Good”; chỉ lượt ôn SRS hoặc quy tắc chuyển từ mới sang SRS ở mục 5 mới đổi lịch.

## 5. Hoàn thiện ôn tập ngắt quãng (SRS)

Hiện hệ thống đã có lịch theo Again/Hard/Good/Easy và lưu DB. Bổ sung **tab Ôn tập** ở vị trí Bảng xếp hạng hiện tại; Bảng xếp hạng chuyển xuống Thống kê/Cộng đồng. Tab Ôn tập là điểm vào chính cho việc ôn hằng ngày và học từ mới, dùng UI hiện có của ứng dụng.

1. Đầu tab có hai hành động nổi bật: **Ôn từ đến hạn** và **Học từ mới**. Hiển thị ba số riêng: đến hạn ngay, đang học/chưa tới hạn, chưa từng gặp trong phạm vi nội dung người học được phép học. Số “chưa từng gặp” không tính toàn bộ kho ngôn ngữ hay trình độ bị khóa.
2. Bên dưới có phân bố thẻ theo các giai đoạn ghi nhớ (có thể trình bày thành năm nhóm như ý tưởng trong ảnh). Nhóm phải được định nghĩa bằng trạng thái/lịch thực trong BE; không gán nhãn hộp Leitner nếu thuật toán vẫn là lịch Again/Hard/Good/Easy. Bấm vào nhóm xem danh sách từ, ngày ôn kế tiếp và nguồn bài học.
3. “Học từ mới” lấy từ chưa gặp theo lộ trình/Unit hiện tại, chủ đề và giới hạn ngày; chạy đúng phiên ba từ và hai vòng luyện ở mục 4.1. Khi nhóm hoàn tất, BE tạo lịch SRS một lần cho từng mục từ/nghĩa. Từ chưa đạt vào bước học lại sớm. Từ đã có trong deck/SRS không bị tạo lịch trùng.
4. “Ôn từ đến hạn” ưu tiên thẻ quá hạn rồi thẻ đến hạn hôm nay, giới hạn hợp lý mỗi phiên. Người học xem/nghe → tự nhớ → trả lời dạng bài phù hợp hoặc lật để kiểm tra → chọn Again/Hard/Good/Easy; phản hồi đúng/sai của bài tập không tự biến thành rating nếu chưa định nghĩa ánh xạ rõ ràng. BE ghi review event và tính due date trong cùng giao dịch, có client event ID chống gửi trùng.
5. Hàng đợi hằng ngày tách “Đến hạn”, “Đang học lại” và “Mới”. Khi không còn thẻ đến hạn, hiển thị “Đã ôn xong hôm nay” cùng thời điểm dự kiến tiếp theo và nút học từ mới nếu còn quota. Người học có thể xem từ đã học mà không thay đổi lịch.
6. Again đưa thẻ vào bước học lại ngắn; hạn chế các thẻ cùng một từ xuất hiện sát nhau. Mỗi ngày có giới hạn thẻ mới và lượt ôn do người học cấu hình trong giới hạn BE; thẻ đến hạn không bị ẩn vì đã đạt quota từ mới.
7. Giữ lịch cũ hoặc chuyển đổi có kiểm tra, không reset tiến độ. Dashboard, sidebar và “Tiếp tục học” lấy số từ cùng nguồn; ngày tính theo múi giờ người dùng. BE cung cấp dự báo 7 ngày và tỷ lệ nhớ được định nghĩa rõ, không trộn tỷ lệ làm quiz.

### 5.1. Sổ tay dùng trên mọi trang

1. Một icon Sổ tay nổi ở cạnh phải, giữ vị trí dễ mở khi cuộn nhưng không che nút quan trọng hoặc nội dung trên màn hình nhỏ. Bấm icon mở panel Sổ tay; có thể xem, tìm, tạo, sửa và xóa ghi chú tự do.
2. Khi người dùng bôi đen một từ hoặc cụm từ trong nội dung học, chỉ hiện hành động nhỏ **“Thêm vào Sổ tay”** gần vùng chọn. Bỏ chọn, chuyển trang hoặc bấm ra ngoài thì hành động biến mất. Không gọi API tạo ghi chú khi chỉ bôi đen.
3. Bấm “Thêm vào Sổ tay” mở bản nháp với văn bản đã chọn, ngôn ngữ, nguồn trang/bài học và một đoạn ngữ cảnh ngắn nếu có. Người dùng có thể sửa từ, nhập nghĩa và ghi chú riêng; chỉ nút **Lưu** mới ghi vào DB. Nếu chọn văn bản không thuộc nội dung học (ví dụ tiêu đề/nút), chỉ lấy đúng văn bản được chọn, không suy đoán nghĩa.
4. Mở Sổ tay trực tiếp cho phép ghi chú trống hoặc tự nhập từ/cụm từ, không cần bôi đen trước. Có thể gắn ghi chú vào từ vựng trong DB nếu tìm thấy, nhưng ghi chú tự do vẫn hoạt động khi không có mục từ tương ứng.
5. Ghi chú thuộc riêng tài khoản, lưu trên BE, đồng bộ giữa thiết bị. BE giới hạn độ dài, kiểm tra quyền sở hữu và lọc nội dung hiển thị để tránh chèn mã. Khi chưa đăng nhập, icon vẫn có mặt trên trang công khai; thao tác lưu yêu cầu đăng nhập và giữ bản nháp trong phiên hiện tại để người dùng không mất nội dung vừa viết.
6. Sổ tay, deck và SRS là ba chức năng khác nhau. Lưu vào Sổ tay không tự tạo flashcard hay lịch ôn; từ một mục sổ tay có nút riêng **“Thêm vào ôn tập”** nếu mục từ đủ dữ liệu và người dùng được phép học. Các mục trùng văn bản/ngôn ngữ được gợi ý hợp nhất, không âm thầm ghi đè ghi chú cũ.

## 6. Dữ liệu và quản trị

- **Mẫu lộ trình:** ngôn ngữ, trình độ đầu vào/đích, lĩnh vực/nghề, phiên bản, trạng thái nháp/xuất bản, thứ tự Unit/bước, ngưỡng đạt và chính sách thi lại.
- **Lộ trình người học:** mục tiêu riêng, mẫu/phiên bản đã chọn, trạng thái hoạt động/tạm dừng/hoàn thành, vị trí tiếp tục và thời gian.
- **Tiến độ bước:** tham chiếu đúng Lesson, QuizAttempt, bài nộp kỹ năng hoặc lượt phỏng vấn; trạng thái được BE tính từ bằng chứng. Không có API cho FE tự đánh dấu đạt.
- **VocabularySession/Group/ExerciseAttempt:** lưu ba từ đã chọn, hai vòng, thứ tự bài tập, câu đang làm, kết quả từng dạng và bài luyện bù; chốt phiên bản nội dung khi bắt đầu.
- **QuizSession/QuestionSnapshot/Answer:** lưu câu hỏi và đáp án đã phát hành theo phiên quiz tự do, bài đang làm dở, kết quả và lịch sử.
- **InterviewScenario/Attempt/Turn/Evaluation:** nội dung và rubric theo nghề/trình độ, câu trả lời, trạng thái chấm.
- **SRS:** dùng CardSchedule/ReviewEvent hiện có; bổ sung ngữ cảnh nguồn và các trạng thái cần thiết bằng migration, tránh tạo hệ lịch thứ hai.
- **NotebookEntry:** user, văn bản/từ đã chọn hoặc ghi chú tự do, ngôn ngữ, nghĩa và ghi chú riêng, liên kết từ vựng tùy chọn, nguồn trang/bài học tùy chọn, thời gian tạo/sửa. Bản nháp chưa bấm Lưu không tạo bản ghi.
- Admin có UI tạo/sửa/xem trước/xuất bản mẫu lộ trình, tình huống phỏng vấn, rubric và ngưỡng đạt; nhập/xuất JSON có template và validate ở BE. Bản đã xuất bản được version để sửa nội dung không làm đổi kết quả cũ.
- **Cập nhật toàn bộ JSON mẫu đang tải từ trang admin** và cung cấp mẫu riêng cho lộ trình, từ vựng/phiên học mới, bài kiểm tra và tình huống phỏng vấn. Mẫu từ vựng phải chỉ rõ ngôn ngữ, trình độ, Unit/thứ tự, loại từ, phiên âm, các nghĩa, câu ví dụ có bản dịch, nguồn âm thanh hoặc cấu hình phát âm, đáp án điền từ hợp lệ và dữ liệu đủ để tạo lựa chọn nghe/chọn nghĩa, ghép cặp, nghe/gõ lại. Mẫu bài kiểm tra có câu cho đủ bốn kỹ năng và rubric; mẫu nghề có vai trò, trình độ, các lượt hỏi và tiêu chí chấm. Trường mới cần mô tả ý nghĩa, tính bắt buộc và ví dụ hợp lệ.
- Import JSON được BE validate cả cấu trúc lẫn logic tham chiếu: đúng khóa học/ngôn ngữ/trình độ, Unit/bài tồn tại và đúng thứ tự, từ và nghĩa không trùng gây nhập nhằng, đủ dữ liệu cho từng dạng bài, đáp án hợp lệ, ngưỡng điểm/rubric nhất quán. Trả lỗi có đường dẫn tới trường sai; kiểm tra toàn bộ trước khi ghi DB, tránh import nửa chừng. JSON cũ được hỗ trợ chuyển đổi có kiểm tra hoặc trả lỗi hướng dẫn nâng cấp, không âm thầm coi nội dung thiếu là đạt.
- Danh mục nghề nghiệp có mã ổn định và tên đa ngôn ngữ. Nội dung UI dùng i18n vi/en; nội dung học giữ đúng ngôn ngữ bài học.

## 7. API và kiểm soát

- API đọc mẫu lộ trình khả dụng; tạo mục tiêu/lộ trình; đọc lộ trình hoạt động với bước kế tiếp và bằng chứng; tạm dừng/đổi mục tiêu.
- API phiên học từ mới: tạo phiên theo lộ trình/Unit, đọc bước hiện tại, lưu đáp án từng dạng, tiếp tục phiên và lấy kết quả nhóm. BE chốt thứ tự ba từ × hai vòng, xác minh user/quyền/nội dung và chấm từng đáp án.
- API phiên quiz tự do: tạo phiên, đọc phiên, lưu đáp án, nộp, đọc kết quả. BE xác minh user, ngôn ngữ, trình độ, quyền bài học, thời hạn và tính toàn vẹn snapshot.
- API tab Ôn tập: tóm tắt đến hạn/đang học/chưa gặp, phân bố giai đoạn ghi nhớ, hàng đợi đến hạn và từ mới trong quota. Review và chuyển từ mới vào lịch giữ idempotency.
- API Sổ tay: phân trang/tìm kiếm mục của chính người dùng, tạo/sửa/xóa, liên kết mục từ và hành động riêng để thêm vào SRS; BE validate dữ liệu và quyền sở hữu trên từng thao tác.
- API tình huống/lượt phỏng vấn và đánh giá; quyền chấm thủ công dành cho admin/giáo viên được phân quyền.
- Truy vấn trả dữ liệu tổng hợp cho dashboard/sidebar trong ít lần gọi; phân trang bài, tình huống và lịch sử; chỉ lấy nội dung chi tiết khi mở bài.

## 8. Thứ tự triển khai đề xuất

1. **Nền dữ liệu và admin:** chốt schema, migration, cập nhật JSON mẫu hiện có và thêm template lộ trình/từ vựng/nghề, validate import ở BE, cấu hình rubric và ngưỡng; UI quản trị và dữ liệu mẫu để import (không seed khóa học mặc định).
2. **Mục tiêu và lộ trình:** chọn mục tiêu, gợi ý theo quyền/trình độ, sidebar tiến độ, bước kế tiếp và tiếp tục học đa thiết bị.
3. **Tab Ôn tập, Sổ tay và luồng từ mới:** chuyển vị trí Bảng xếp hạng; tóm tắt đúng phạm vi học, icon Sổ tay trên mọi trang với thao tác lưu chủ động, phiên ba từ × hai vòng, đủ bốn dạng bài, âm thanh, tiếp tục phiên và đồng bộ SRS.
4. **Các bước học:** nối từ vựng, cấu trúc và bốn kỹ năng hiện có vào bằng chứng hoàn thành; xử lý bài nói/viết chờ chấm.
5. **Quiz, thẻ lật và bài cuối chặng:** phiên ngắn, tiếp tục, chấm BE, hiển thị lỗi, mở khóa chặng.
6. **SRS chuyên sâu:** hàng đợi đến hạn/đang học/mới, giai đoạn ghi nhớ, giới hạn ngày, thống kê nhất quán, chuyển đổi lịch cũ.
7. **Phỏng vấn:** nội dung nghề theo trình độ, lượt hỏi/đáp, rubric, lưu lịch sử, phản hồi.
8. **Kiểm thử và tối ưu:** E2E luồng học, quyền truy cập, refresh/tiếp tục, dữ liệu cũ, hiệu năng truy vấn, i18n, mobile và accessibility.

## 9. Tiêu chí nghiệm thu

- Người mới bắt đầu, người nâng trình độ và người học theo nghề nhận đúng lộ trình có nội dung; mục tiêu đích không mở quyền truy cập trước khi đủ điều kiện.
- Hoàn thành từng bước dựa trên dữ liệu BE; F5 hoặc đăng nhập thiết bị khác vẫn tiếp tục đúng câu/bài; đổi mục tiêu giữ lịch sử.
- Bốn kỹ năng có kết quả riêng. Phần chưa chấm hiển thị “Chờ đánh giá”; bài cuối chặng chỉ đạt khi đủ ngưỡng cấu hình.
- Tab Ôn tập thay vị trí Bảng xếp hạng; Bảng xếp hạng có lối vào mới ở Thống kê/Cộng đồng. Ba số và phân bố ghi nhớ khớp dữ liệu DB trong đúng ngôn ngữ/trình độ được phép.
- Phiên học từ mới lần lượt hiện đủ thông tin và âm thanh cho tối đa ba từ, sau đó nghe chọn nghĩa → điền chỗ trống → ghép cặp → nghe gõ từ, lặp đủ hai vòng. Câu sai được luyện bù; rời trang rồi quay lại tiếp tục đúng bước.
- Quiz tự do có phiên 10–20 câu, thẻ lật, âm thanh khi có nguồn, kết quả đúng, không lộ đáp án qua API và không cộng sai tiến độ SRS.
- SRS tách đến hạn/đang học/mới; rating và việc tạo lịch từ mới chỉ cập nhật một lần; số liệu dashboard khớp hàng đợi và lịch sử.
- Icon Sổ tay hiện trên mọi trang; bôi đen không tạo bản ghi. Chỉ bấm Thêm rồi Lưu mới ghi DB; mở icon có thể viết ghi chú tự do. Ghi chú đồng bộ giữa thiết bị, riêng tư theo tài khoản và không tự thêm vào deck/SRS.
- Phỏng vấn đúng nghề/trình độ, lưu lượt trả lời và nhận xét có nguồn chấm rõ ràng.
- Admin sửa và xuất bản nội dung trên web hoặc JSON template; nội dung lỗi bị BE từ chối với lỗi cụ thể trước khi xuất bản.
- Mỗi JSON mẫu tải từ admin import được vào đúng loại nội dung sau khi điền dữ liệu bắt buộc; lỗi import nêu chính xác trường và không ghi bản dữ liệu dở dang. Nội dung import hỗ trợ đủ bốn dạng luyện từ mới, bốn kỹ năng và tình huống nghề theo phạm vi mẫu.

## 10. Các quyết định cần chốt khi duyệt

1. Đề xuất tách **trình độ hiện tại đã xác nhận** và **trình độ mục tiêu** như trên. Đây là điều kiện để mục tiêu dài hạn không phá quy tắc mở khóa.
2. Theo yêu cầu mới, phiên học từ mới dùng nhóm ba từ và lặp bốn dạng bài hai vòng; quiz tự do vẫn mặc định 10 câu/phiên (tối đa 20). Ngưỡng đạt bài cuối chặng do admin cấu hình, có thể dùng 70% tổng và tối thiểu 60% mỗi kỹ năng làm giá trị khởi đầu.
3. Đề xuất phỏng vấn và bài nói/viết hỗ trợ chấm thủ công theo rubric trước; chỉ bật chấm tự động khi có dịch vụ đánh giá và kiểm định chất lượng thực tế.
# Danh mục khóa học trong quản trị

Admin quản lý trình độ theo khung CEFR/JLPT/HSK/TOPIK và lĩnh vực trong **Quản trị → Chương trình học → Quản lý trình độ & lĩnh vực**. Mỗi mục có mã cố định, tên và mô tả tiếng Việt/Anh, thứ tự hiển thị; có thể thêm, sửa tên/mô tả/thứ tự, và xóa khi chưa được khóa học sử dụng. Khi tạo khóa học, chọn trình độ và lĩnh vực sẽ tự điền tiêu đề theo mẫu `Trình độ - Tên lĩnh vực` (ví dụ `A1 - Giao tiếp Đời sống`); admin vẫn có thể sửa tiêu đề. Danh mục được lưu trong cơ sở dữ liệu và dùng cho lựa chọn khóa học, mục tiêu học và kiểm tra hợp lệ ở API.

