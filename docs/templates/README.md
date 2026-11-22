# JSON mẫu cho nội dung học

## Hành trình bài học (`lessonSteps`)

Mỗi `units[].lessons[]` có thể khai báo `lessonSteps` để quyết định thứ tự học. Nếu bỏ trường này, hệ thống suy ra bước giới thiệu rồi lần lượt các phần có dữ liệu: từ vựng, ngữ pháp, nghe, nói, đọc, viết, kiểm tra. Mẫu tải từ admin có sẵn bước giới thiệu và từ vựng; khi thêm kỹ năng hoặc quiz, người soạn phải thêm bước tương ứng vào `lessonSteps` để chúng trở thành một phần bắt buộc của hành trình. Có thể bỏ hẳn `lessonSteps` để dùng thứ tự suy ra tự động.

Ví dụ một bài có ngữ pháp và quiz:

```json
"lessonSteps": [
  { "id": "intro", "type": "introduction", "title": "Giới thiệu", "order": 0, "required": true, "activityRefs": [], "completionPolicy": { "kind": "viewed" } },
  { "id": "words", "type": "vocabulary", "title": "Từ vựng", "order": 1, "required": true, "activityRefs": [], "completionPolicy": { "kind": "session_completed" } },
  { "id": "grammar", "type": "grammar", "title": "Ngữ pháp", "order": 2, "required": true, "activityRefs": ["UUID của grammarTopic"], "completionPolicy": { "kind": "all_correct" } },
  { "id": "quiz", "type": "quiz", "title": "Kiểm tra", "order": 3, "required": true, "activityRefs": [], "completionPolicy": { "kind": "quiz_passed" } }
]
```

`activityRefs` của ngữ pháp, nghe, nói, đọc và viết phải là các `id` tồn tại trong mảng nội dung tương ứng của chính bài đó. Với từ vựng và quiz, để `[]` để hệ thống lấy toàn bộ nội dung của bài. Mọi bài cần một bước `introduction` bắt buộc. Các điều kiện hiện có: `viewed`, `session_completed`, `all_correct`, `practiced`, `submission_required`, `quiz_passed` (quiz đạt ít nhất 70%). Hoàn thành phiên từ vựng chỉ hoàn thành bước từ vựng; bài học chỉ hoàn thành khi tất cả bước bắt buộc đạt điều kiện. Lịch ôn SRS tiếp tục chạy độc lập sau khi hoàn thành bài.

## Luyện thi IELTS, TOEIC và phỏng vấn

Trong trang quản trị của từng khóa học, mở mục **IELTS**, **TOEIC** hoặc **Phỏng vấn** riêng. Mỗi mục có danh sách, tạo/sửa/xóa, nhập JSON và tải mẫu tương ứng. IELTS và TOEIC đều dùng mảng `certificateExams`, nhưng mỗi lần nhập chỉ nhận các đề có trường `certificate` đúng với mục đang mở. Phỏng vấn dùng `interviewScenarios`. Import thêm mục mới và cập nhật mục trùng ID trong mục được chọn; giữ nguyên các mục khác và nội dung bài học.

Mỗi đề phải khớp `level` của khóa học. Có thể đặt `durationMinutes` từ 1 đến 240 cho đề thi; hết giờ BE tự nộp các câu đã lưu và câu bỏ trống nhận 0 điểm. Listening/Reading cần `options`, `correctIndex` và âm thanh hoặc đoạn đọc tương ứng. Speaking/Writing cần `rubric`; phỏng vấn cần `profession`, `rubric` và `turns`. ID đề và câu hỏi phải là UUID riêng, không trùng nhau. Người học có thể ghi âm câu nói; admin nghe và chấm thủ công. Bản chép lời không tự chấm phát âm hay sinh điểm IELTS/TOEIC chính thức.


## Mẫu tải từ admin và vòng học từ vựng

`decks` trong JSON giáo trình hiện để `[]` và chỉ được giữ để đọc các file cũ. Import khóa học không tạo bộ thẻ cá nhân hay công khai. Người học tạo bộ thẻ trong **Bộ thẻ của tôi** và quyết định có công khai lên **Bộ thẻ cộng đồng** hay không. Từ hoàn thành trong vòng học được đưa vào bộ thẻ SRS riêng của người học.

Mẫu khóa học tải từ admin cho **tiếng Anh** nay có ba thẻ minh họa hoàn chỉnh trong `units[].lessons[].flashcards`: `apply`, `experience`, `skill`. Mỗi thẻ có `front`, `back`, `meanings`, loại từ, phiên âm, câu ví dụ và bản dịch, `clozeSentence` chứa `___`, cùng `acceptedAnswers`. Thay ba thẻ bằng từ vựng đúng trình độ, Unit và chủ đề của khóa học trước khi import. Với ngôn ngữ khác, mẫu giữ ô trống để người soạn điền nội dung đúng ngôn ngữ.

Vòng học lấy tối đa ba từ theo thứ tự trong mỗi bài; không cần khai báo sẵn câu hỏi nghe chọn nghĩa, ghép cặp hay nghe gõ từ trong JSON. BE tạo các bước đó từ thông tin thẻ, rồi lưu kết quả và đưa từ hoàn thành vào lịch ôn tập. `audioUrl` là tùy chọn; khi trống, trình duyệt dùng phát âm tổng hợp. Các phần kỹ năng, quiz và bài đầu vào trong mẫu vẫn cần được biên soạn trước khi import.

- Tải JSON mẫu **Khóa học** từ trang quản trị sau khi tạo khóa học. File tải xuống có sẵn đúng ID khóa học, ngôn ngữ, trình độ và lĩnh vực; điền các trường còn trống trước khi import. Không dùng ID từ khóa học khác.
- [vocabulary-practice-en-example.json](vocabulary-practice-en-example.json) là **mẫu mảng flashcards** cho một bài tiếng Anh theo nghề. Sao chép các phần tử vào units[].lessons[].flashcards trong JSON khóa học đã tải; đây không phải file khóa học độc lập để import trực tiếp.
- meanings là các nghĩa phân biệt; back là nghĩa chính. example và exampleTranslation là một cặp câu. clozeSentence phải là **câu có ngữ cảnh**, chứa đúng vị trí ___; tránh câu mơ hồ như “She is my ___” nếu nhiều từ đều có thể điền. Màn điền từ hiển thị nghĩa chính, loại từ và bản dịch ví dụ làm gợi ý, nhưng không hiển thị từ đáp án trước khi chấm. acceptedAnswers liệt kê các cách viết đúng cho chỗ trống. audioUrl để trống khi dùng phát âm của trình duyệt, hoặc điền đường dẫn âm thanh hợp lệ.
- BE từ chối clozeSentence thiếu ___, đáp án rỗng hoặc quá 10 đáp án. JSON cũ không có các trường luyện tập mới vẫn import được; các từ đó chưa đủ dữ liệu để tham gia trọn vẹn luồng học mới cho tới khi admin bổ sung.
- Bài nghe, nói, đọc, viết, ngữ pháp và quiz vẫn phải có nội dung hợp lệ theo cấu trúc JSON khóa học hiện tại. FE kiểm tra trước khi gửi và BE kiểm tra lại trước khi ghi DB.

## Kiểm tra nội dung và dữ liệu tiến độ

Admin mở tab **Chất lượng nội dung** và bấm **Quét nội dung** sau khi import. Bảng kiểm tra chỉ đọc JSON đã lưu, báo bài thiếu `summary`, thiếu `lessonSteps`, nội dung kỹ năng không có bước tương ứng và flashcard thiếu `example`, `exampleTranslation` hoặc `clozeSentence` có `___`. Sửa JSON nguồn rồi import lại theo ID khóa học hiện tại.

`LearningSession`, `LearningEvidence`, liên kết nguồn thẻ SRS và ID chống gửi trùng được BE tạo khi người học thao tác; chúng không phải trường cần thêm vào mẫu JSON khóa học. Mẫu import hiện tại vẫn dùng `units[].lessons[]` và `lessonSteps` theo schema hiện hành.

Sau khi bài có phiên học, tiến độ, lượt quiz hoặc phiên từ vựng, giữ nguyên `lessonSteps[].id`, `lessonSteps[].type`, `required`, `completionPolicy`, `activityRefs` và các mảng hoạt động được đánh giá (`flashcards`, `grammarTopics`, `listeningExercises`, `speakingExercises`, `readingArticles`, `writingPrompts`, `quizQuestions`). API sửa bài từ chối thay đổi chúng để kết quả cũ không được tính nhầm cho nội dung mới. Có thể sửa phần giới thiệu và tên bước. Nếu chỉ cần đổi ID bước, dùng thao tác **Chuyển ID bước học có tiến độ** trong admin; thao tác này cập nhật checkpoint liên quan trong một transaction. Khi cần thay nội dung học, hãy tạo bài mới. Các trường tiến độ và gợi ý luyện lại được hệ thống tính từ lần làm bài, không nhập bằng JSON.
