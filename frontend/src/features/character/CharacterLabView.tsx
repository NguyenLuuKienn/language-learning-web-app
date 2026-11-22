import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { speechService } from '../../services/speechService';
import {
  SoundOutlined,
  EditOutlined,
  CheckCircleOutlined,
  TrophyOutlined,
  PlayCircleOutlined,
  ClearOutlined,
  BulbOutlined,
  ArrowLeftOutlined
} from '@ant-design/icons';
import { message } from 'antd';

export interface CharacterItem {
  char: string;
  romaji: string;
  name?: string;
  meaning?: string;
  strokes?: number;
  type?: string;
  exampleWord?: string;
  exampleMeaning?: string;
}

// ================= DỮ LIỆU KÝ TỰ CHUẨN CHO 4 NGÔN NGỮ =================

// 1. TIẾNG NHẬT: Hiragana, Katakana, Kanji N5
const JA_HIRAGANA: CharacterItem[] = [
  { char: 'あ', romaji: 'a', exampleWord: 'あさ (asa)', exampleMeaning: 'Buổi sáng' },
  { char: 'い', romaji: 'i', exampleWord: 'いぬ (inu)', exampleMeaning: 'Con chó' },
  { char: 'う', romaji: 'u', exampleWord: 'うみ (umi)', exampleMeaning: 'Biển cả' },
  { char: 'え', romaji: 'e', exampleWord: 'えき (eki)', exampleMeaning: 'Nhà ga' },
  { char: 'お', romaji: 'o', exampleWord: 'お茶 (ocha)', exampleMeaning: 'Trà' },
  { char: 'か', romaji: 'ka', exampleWord: 'かさ (kasa)', exampleMeaning: 'Cái ô / dù' },
  { char: 'き', romaji: 'ki', exampleWord: 'き (ki)', exampleMeaning: 'Cây cối' },
  { char: 'く', romaji: 'ku', exampleWord: 'くるま (kuruma)', exampleMeaning: 'Xe hơi' },
  { char: 'け', romaji: 'ke', exampleWord: 'けさ (kesa)', exampleMeaning: 'Sáng nay' },
  { char: 'こ', romaji: 'ko', exampleWord: 'こども (kodomo)', exampleMeaning: 'Trẻ em' },
  { char: 'さ', romaji: 'sa', exampleWord: 'さくら (sakura)', exampleMeaning: 'Hoa anh đào' },
  { char: 'し', romaji: 'shi', exampleWord: 'しお (shio)', exampleMeaning: 'Muối ăn' },
  { char: 'す', romaji: 'su', exampleWord: 'すし (sushi)', exampleMeaning: 'Món sushi' },
  { char: 'せ', romaji: 'se', exampleWord: 'せんせい (sensei)', exampleMeaning: 'Thầy cô giáo' },
  { char: 'そ', romaji: 'so', exampleWord: 'そら (sora)', exampleMeaning: 'Bầu trời' },
  { char: 'た', romaji: 'ta', exampleWord: 'たまご (tamago)', exampleMeaning: 'Quả trứng' },
  { char: 'ち', romaji: 'chi', exampleWord: 'ちず (chizu)', exampleMeaning: 'Bản đồ' },
  { char: 'つ', romaji: 'tsu', exampleWord: 'つき (tsuki)', exampleMeaning: 'Mặt trăng' },
  { char: 'て', romaji: 'te', exampleWord: 'て (te)', exampleMeaning: 'Bàn tay' },
  { char: 'と', romaji: 'to', exampleWord: 'ともだち (tomodachi)', exampleMeaning: 'Bạn bè' },
  { char: 'な', romaji: 'na', exampleWord: 'なつ (natsu)', exampleMeaning: 'Mùa hè' },
  { char: 'に', romaji: 'ni', exampleWord: 'にほん (nihon)', exampleMeaning: 'Nhật Bản' },
  { char: 'ぬ', romaji: 'nu', exampleWord: 'いぬ (inu)', exampleMeaning: 'Chó con' },
  { char: 'ね', romaji: 'ne', exampleWord: 'ねこ (neko)', exampleMeaning: 'Con mèo' },
  { char: 'の', romaji: 'no', exampleWord: 'のり (nori)', exampleMeaning: 'Rong biển' },
  { char: 'は', romaji: 'ha', exampleWord: 'はな (hana)', exampleMeaning: 'Bông hoa' },
  { char: 'ひ', romaji: 'hi', exampleWord: 'ひと (hito)', exampleMeaning: 'Con người' },
  { char: 'ふ', romaji: 'fu', exampleWord: 'ふじさん (fujisan)', exampleMeaning: 'Núi Phú Sĩ' },
  { char: 'へ', romaji: 'he', exampleWord: 'へや (heya)', exampleMeaning: 'Căn phòng' },
  { char: 'ほ', romaji: 'ho', exampleWord: 'ほし (hoshi)', exampleMeaning: 'Ngôi sao' },
  { char: 'ま', romaji: 'ma', exampleWord: 'まち (machi)', exampleMeaning: 'Thành phố' },
  { char: 'み', romaji: 'mi', exampleWord: 'みず (mizu)', exampleMeaning: 'Nước uống' },
  { char: 'む', romaji: 'mu', exampleWord: 'むし (mushi)', exampleMeaning: 'Côn trùng' },
  { char: 'め', romaji: 'me', exampleWord: 'め (me)', exampleMeaning: 'Đôi mắt' },
  { char: 'も', romaji: 'mo', exampleWord: 'もり (mori)', exampleMeaning: 'Khu rừng' },
  { char: 'や', romaji: 'ya', exampleWord: 'やま (yama)', exampleMeaning: 'Ngọn núi' },
  { char: 'ゆ', romaji: 'yu', exampleWord: 'ゆき (yuki)', exampleMeaning: 'Tuyết rơi' },
  { char: 'よ', romaji: 'yo', exampleWord: 'よる (yoru)', exampleMeaning: 'Ban đêm' },
  { char: 'ら', romaji: 'ra', exampleWord: 'らいげつ (raigetsu)', exampleMeaning: 'Tháng sau' },
  { char: 'り', romaji: 'ri', exampleWord: 'りんご (ringo)', exampleMeaning: 'Quả táo' },
  { char: 'る', romaji: 'ru', exampleWord: 'よる (yoru)', exampleMeaning: 'Đêm tối' },
  { char: 'れ', romaji: 're', exampleWord: 'れい (rei)', exampleMeaning: 'Số 0' },
  { char: 'ろ', romaji: 'ro', exampleWord: 'ろうそく (rousoku)', exampleMeaning: 'Cây nến' },
  { char: 'わ', romaji: 'wa', exampleWord: 'わたし (watashi)', exampleMeaning: 'Tôi' },
  { char: 'を', romaji: 'wo', exampleWord: '本を読む (hon wo yomu)', exampleMeaning: 'Đọc sách' },
  { char: 'ん', romaji: 'n', exampleWord: 'ほん (hon)', exampleMeaning: 'Quyển sách' }
];

const JA_KATAKANA: CharacterItem[] = [
  { char: 'ア', romaji: 'a', exampleWord: 'アメリカ (Amerika)', exampleMeaning: 'Nước Mỹ' },
  { char: 'イ', romaji: 'i', exampleWord: 'イギリス (Igirisu)', exampleMeaning: 'Nước Anh' },
  { char: 'ウ', romaji: 'u', exampleWord: 'ウェブ (Webu)', exampleMeaning: 'Trang web' },
  { char: 'エ', romaji: 'e', exampleWord: 'エレベーター (Erebētā)', exampleMeaning: 'Thang máy' },
  { char: 'オ', romaji: 'o', exampleWord: 'オフィス (Ofisu)', exampleMeaning: 'Văn phòng' },
  { char: 'カ', romaji: 'ka', exampleWord: 'カメラ (Kamera)', exampleMeaning: 'Máy ảnh' },
  { char: 'キ', romaji: 'ki', exampleWord: 'キーボード (Kībōdo)', exampleMeaning: 'Bàn phím' },
  { char: 'ク', romaji: 'ku', exampleWord: 'クラス (Kurasu)', exampleMeaning: 'Lớp học' },
  { char: 'ケ', romaji: 'ke', exampleWord: 'ケーキ (Kēki)', exampleMeaning: 'Bánh ngọt' },
  { char: 'コ', romaji: 'ko', exampleWord: 'コーヒー (Kōhī)', exampleMeaning: 'Cà phê' },
  { char: 'サ', romaji: 'sa', exampleWord: 'サッカー (Sakkā)', exampleMeaning: 'Bóng đá' },
  { char: 'シ', romaji: 'shi', exampleWord: 'シャツ (Shatsu)', exampleMeaning: 'Áo sơ mi' },
  { char: 'ス', romaji: 'su', exampleWord: 'スマート (Sumāto)', exampleMeaning: 'Thông minh' },
  { char: 'セ', romaji: 'se', exampleWord: 'セーター (Sētā)', exampleMeaning: 'Áo len' },
  { char: 'ソ', romaji: 'so', exampleWord: 'ソフト (Sofuto)', exampleMeaning: 'Phần mềm' },
  { char: 'タ', romaji: 'ta', exampleWord: 'タクシー (Takushī)', exampleMeaning: 'Xe taxi' },
  { char: 'チ', romaji: 'chi', exampleWord: 'チケット (Chiketto)', exampleMeaning: 'Vé' },
  { char: 'ツ', romaji: 'tsu', exampleWord: 'ツアー (Tsuā)', exampleMeaning: 'Chuyến du lịch' },
  { char: 'テ', romaji: 'te', exampleWord: 'テスト (Tesuto)', exampleMeaning: 'Bài kiểm tra' },
  { char: 'ト', romaji: 'to', exampleWord: 'トマト (Tomato)', exampleMeaning: 'Quả cà chua' }
];

const JA_KANJI_N5: CharacterItem[] = [
  { char: '日', romaji: 'nichi / hi', meaning: 'Nhật (Mặt trời, Ngày)', strokes: 4, exampleWord: '日本 (Nihon)', exampleMeaning: 'Nhật Bản' },
  { char: '月', romaji: 'getsu / tsuki', meaning: 'Nguyệt (Mặt trăng, Tháng)', strokes: 4, exampleWord: '今月 (Kongetsu)', exampleMeaning: 'Tháng này' },
  { char: '火', romaji: 'ka / hi', meaning: 'Hỏa (Lửa, Thứ ba)', strokes: 4, exampleWord: '火曜日 (Kayoubi)', exampleMeaning: 'Thứ Ba' },
  { char: '水', romaji: 'sui / mizu', meaning: 'Thủy (Nước, Thứ tư)', strokes: 4, exampleWord: '水曜日 (Suiyoubi)', exampleMeaning: 'Thứ Tư' },
  { char: '木', romaji: 'moku / ki', meaning: 'Mộc (Cây cối, Thứ năm)', strokes: 4, exampleWord: '木曜日 (Mokuyoubi)', exampleMeaning: 'Thứ Năm' },
  { char: '金', romaji: 'kin / kane', meaning: 'Kim (Vàng, Tiền, Thứ sáu)', strokes: 8, exampleWord: 'お金 (Okane)', exampleMeaning: 'Tiền bạc' },
  { char: '土', romaji: 'do / tsuchi', meaning: 'Thổ (Đất, Thứ bảy)', strokes: 3, exampleWord: '土曜日 (Doyoubi)', exampleMeaning: 'Thứ Bảy' },
  { char: '人', romaji: 'jin / hito', meaning: 'Nhân (Người)', strokes: 2, exampleWord: '日本人 (Nihonjin)', exampleMeaning: 'Người Nhật' },
  { char: '山', romaji: 'san / yama', meaning: 'Sơn (Núi)', strokes: 3, exampleWord: '富士山 (Fujisan)', exampleMeaning: 'Núi Phú Sĩ' },
  { char: '川', romaji: 'sen / kawa', meaning: 'Xuyên (Sông ngòi)', strokes: 3, exampleWord: 'ナイル川 (Nairugawa)', exampleMeaning: 'Sông Nile' },
  { char: '大', romaji: 'dai / oo-kii', meaning: 'Đại (To lớn)', strokes: 3, exampleWord: '大学 (Daigaku)', exampleMeaning: 'Đại học' },
  { char: '学', romaji: 'gaku / mana-bu', meaning: 'Học (Học tập)', strokes: 8, exampleWord: '学生 (Gakusei)', exampleMeaning: 'Học sinh' },
  { char: '先', romaji: 'sen / saki', meaning: 'Tiên (Trước)', strokes: 6, exampleWord: '先生 (Sensei)', exampleMeaning: 'Giáo viên' },
  { char: '生', romaji: 'sei / i-kiru', meaning: 'Sinh (Sinh sống, Sự sống)', strokes: 5, exampleWord: '生活 (Seikatsu)', exampleMeaning: 'Cuộc sống' },
  { char: '本', romaji: 'hon / moto', meaning: 'Bản (Sách, Gốc rễ)', strokes: 5, exampleWord: '本屋 (Honya)', exampleMeaning: 'Hiệu sách' }
];

// 2. TIẾNG TRUNG: Pinyin, Thanh điệu, Bộ thủ, Hán tự HSK
const ZH_PINYIN_TONES: CharacterItem[] = [
  { char: 'mā', romaji: 'mā (Thanh 1)', meaning: 'Thanh ngang cao (Cao độ 55), âm vang đều', exampleWord: '妈 (Mẹ)', exampleMeaning: 'Mẹ' },
  { char: 'má', romaji: 'má (Thanh 2)', meaning: 'Thanh sắc đi lên (Cao độ 35), vút từ trung bình lên cao', exampleWord: '麻 (Cây gai / Tê)', exampleMeaning: 'Tê cay' },
  { char: 'mǎ', romaji: 'mǎ (Thanh 3)', meaning: 'Thanh hỏi uốn lượn (Cao độ 214), xuống thấp rồi lên vừa', exampleWord: '马 (Con ngựa)', exampleMeaning: 'Con ngựa' },
  { char: 'mà', romaji: 'mà (Thanh 4)', meaning: 'Thanh huyền dứt khoát (Cao độ 51), rơi từ đỉnh cao xuống đáy', exampleWord: '骂 (Mắng mỏ)', exampleMeaning: 'La mắng' },
  { char: 'ma', romaji: 'ma (Thanh nhẹ)', meaning: 'Âm nhẹ ngắn, không dấu', exampleWord: '吗 (Trợ từ nghi vấn)', exampleMeaning: '...phải không?' }
];

const ZH_RADICALS: CharacterItem[] = [
  { char: '亻', romaji: 'rén zì páng', meaning: 'Bộ Nhân đứng (Chỉ người, hành vi con người)', exampleWord: '你 (Nǐ)', exampleMeaning: 'Bạn / Anh / Chị' },
  { char: '口', romaji: 'kǒu zì páng', meaning: 'Bộ Khẩu (Chỉ miệng, ăn uống, lời nói)', exampleWord: '吃 (Chī)', exampleMeaning: 'Ăn cơm' },
  { char: '氵', romaji: 'sān diǎn shuǐ', meaning: 'Bộ Tam chấm thủy (Chỉ nước, chất lỏng, sông hồ)', exampleWord: '江 (Jiāng)', exampleMeaning: 'Dòng sông' },
  { char: '木', romaji: 'mù zì páng', meaning: 'Bộ Mộc (Chỉ cây cối, gỗ, đồ đạc bằng gỗ)', exampleWord: '杯 (Bēi)', exampleMeaning: 'Cái cốc' },
  { char: '女', romaji: 'nǚ zì páng', meaning: 'Bộ Nữ (Chỉ phụ nữ, tính chất dịu dàng, quan hệ gia đình)', exampleWord: '好 (Hǎo)', exampleMeaning: 'Tốt đẹp' },
  { char: '讠', romaji: 'yán zì páng', meaning: 'Bộ Ngôn (Chỉ ngôn ngữ, lời nói, trao đổi)', exampleWord: '语 (Yǔ)', exampleMeaning: 'Ngôn ngữ' },
  { char: '饣', romaji: 'shí zì páng', meaning: 'Bộ Thực (Chỉ đồ ăn, thực phẩm, ăn uống)', exampleWord: '饭 (Fàn)', exampleMeaning: 'Cơm' },
  { char: '辶', romaji: 'zǒu zhī dǐ', meaning: 'Bộ Quai xước (Chỉ sự di chuyển, bước đi, khoảng cách)', exampleWord: '进 (Jìn)', exampleMeaning: 'Tiến vào' }
];

const ZH_HANZI_HSK: CharacterItem[] = [
  { char: '你', romaji: 'nǐ', meaning: 'Bạn, anh, chị (Đại từ ngôi 2)', strokes: 7, exampleWord: '你好 (Nǐ hǎo)', exampleMeaning: 'Xin chào' },
  { char: '好', romaji: 'hǎo', meaning: 'Tốt, đẹp, hay', strokes: 6, exampleWord: '很好 (Hěn hǎo)', exampleMeaning: 'Rất tốt' },
  { char: '我', romaji: 'wǒ', meaning: 'Tôi, mình (Đại từ ngôi 1)', strokes: 7, exampleWord: '我们 (Wǒmen)', exampleMeaning: 'Chúng tôi' },
  { char: '是', romaji: 'shì', meaning: 'Là (Động từ to be)', strokes: 9, exampleWord: '我是 (Wǒ shì)', exampleMeaning: 'Tôi là...' },
  { char: '大', romaji: 'dà', meaning: 'Lớn, to', strokes: 3, exampleWord: '大家 (Dàjiā)', exampleMeaning: 'Mọi người' },
  { char: '多', romaji: 'duō', meaning: 'Nhiều', strokes: 6, exampleWord: '多少 (Duōshao)', exampleMeaning: 'Bao nhiêu' },
  { char: '钱', romaji: 'qián', meaning: 'Tiền bạc', strokes: 10, exampleWord: '块钱 (Kuài qián)', exampleMeaning: 'Đồng tiền' },
  { char: '中', romaji: 'zhōng', meaning: 'Trung tâm, ở giữa, Trung Quốc', strokes: 4, exampleWord: '中国 (Zhōngguó)', exampleMeaning: 'Trung Quốc' },
  { char: '国', romaji: 'guó', meaning: 'Đất nước, quốc gia', strokes: 8, exampleWord: '国家 (Guójiā)', exampleMeaning: 'Quốc gia' },
  { char: '谢', romaji: 'xiè', meaning: 'Cảm ơn', strokes: 12, exampleWord: '谢谢 (Xièxie)', exampleMeaning: 'Cảm ơn' }
];

// 3. TIẾNG HÀN: Hangul Nguyên âm, Phụ âm, Khối âm
const KO_VOWELS: CharacterItem[] = [
  { char: 'ㅏ', romaji: 'a', meaning: 'Nguyên âm A', exampleWord: '아이 (a-i)', exampleMeaning: 'Em bé' },
  { char: 'ㅑ', romaji: 'ya', meaning: 'Nguyên âm YA', exampleWord: '야구 (ya-gu)', exampleMeaning: 'Bóng chày' },
  { char: 'ㅓ', romaji: 'eo', meaning: 'Nguyên âm Ơ / O mở', exampleWord: '어머니 (eo-meo-ni)', exampleMeaning: 'Mẹ' },
  { char: 'ㅕ', romaji: 'yeo', meaning: 'Nguyên âm YƠ', exampleWord: '여자 (yeo-ja)', exampleMeaning: 'Phụ nữ' },
  { char: 'ㅗ', romaji: 'o', meaning: 'Nguyên âm Ô tròn môi', exampleWord: '오이 (o-i)', exampleMeaning: 'Dưa chuột' },
  { char: 'ㅛ', romaji: 'yo', meaning: 'Nguyên âm YÔ', exampleWord: '요리 (yo-ri)', exampleMeaning: 'Nấu ăn' },
  { char: 'ㅜ', romaji: 'u', meaning: 'Nguyên âm U', exampleWord: '우유 (u-yu)', exampleMeaning: 'Sữa tươi' },
  { char: 'ㅠ', romaji: 'yu', meaning: 'Nguyên âm YU', exampleWord: '유리 (yu-ri)', exampleMeaning: 'Thủy tinh' },
  { char: 'ㅡ', romaji: 'eu', meaning: 'Nguyên âm Ư dẹt môi', exampleWord: '음악 (eum-ak)', exampleMeaning: 'Âm nhạc' },
  { char: 'ㅣ', romaji: 'i', meaning: 'Nguyên âm I', exampleWord: '이름 (i-reum)', exampleMeaning: 'Tên' }
];

const KO_CONSONANTS: CharacterItem[] = [
  { char: 'ㄱ', romaji: 'g / k', meaning: 'Phụ âm Kiyeok (Giống g/k)', exampleWord: '가방 (ga-bang)', exampleMeaning: 'Cặp sách' },
  { char: 'ㄴ', romaji: 'n', meaning: 'Phụ âm Nieun (Âm n)', exampleWord: '나무 (na-mu)', exampleMeaning: 'Cái cây' },
  { char: 'ㄷ', romaji: 'd / t', meaning: 'Phụ âm Digeut (Âm đ/t)', exampleWord: '다리 (da-ri)', exampleMeaning: 'Đôi chân / Cây cầu' },
  { char: 'ㄹ', romaji: 'r / l', meaning: 'Phụ âm Rieul (Âm r/l)', exampleWord: '라디오 (ra-di-o)', exampleMeaning: 'Đài phát thanh' },
  { char: 'ㅁ', romaji: 'm', meaning: 'Phụ âm Mieum (Âm m)', exampleWord: '모자 (mo-ja)', exampleMeaning: 'Cái mũ' },
  { char: 'ㅂ', romaji: 'b / p', meaning: 'Phụ âm Bieup (Âm b/p)', exampleWord: '바지 (ba-ji)', exampleMeaning: 'Cái quần' },
  { char: 'ㅅ', romaji: 's', meaning: 'Phụ âm Siot (Âm s nhẹ)', exampleWord: '사과 (sa-gwa)', exampleMeaning: 'Quả táo' },
  { char: 'ㅇ', romaji: 'ng / câm', meaning: 'Phụ âm Ieung (Đầu âm tiết câm, cuối là ng)', exampleWord: '우산 (u-san)', exampleMeaning: 'Cây dù' },
  { char: 'ㅈ', romaji: 'j', meaning: 'Phụ âm Jieut (Âm ch/gi)', exampleWord: '지도 (ji-do)', exampleMeaning: 'Bản đồ' },
  { char: 'ㅎ', romaji: 'h', meaning: 'Phụ âm Hieut (Âm h)', exampleWord: '하늘 (ha-neul)', exampleMeaning: 'Bầu trời' }
];

const KO_SYLLABLES: CharacterItem[] = [
  { char: '한', romaji: 'han', meaning: 'Hàn (Khối âm: ㅎ + ㅏ + ㄴ)', exampleWord: '한국 (Han-guk)', exampleMeaning: 'Hàn Quốc' },
  { char: '글', romaji: 'geul', meaning: 'Chữ viết (Khối âm: ㄱ + ㅡ + ㄹ)', exampleWord: '한글 (Han-geul)', exampleMeaning: 'Chữ Hangul' },
  { char: '안', romaji: 'an', meaning: 'An (Khối âm: ㅇ + ㅏ + ㄴ)', exampleWord: '안녕하세요', exampleMeaning: 'Xin chào' },
  { char: '녕', romaji: 'nyeong', meaning: 'Ninh (Khối âm: ㄴ + ㅕ + ㅇ)', exampleWord: '안녕 (An-nyeong)', exampleMeaning: 'Chào bạn' },
  { char: '사', romaji: 'sa', meaning: 'Tứ / Tạ (Khối âm: ㅅ + ㅏ)', exampleWord: '감사합니다', exampleMeaning: 'Cảm ơn' }
];

// 4. TIẾNG ANH: Bảng phiên âm quốc tế IPA (44 âm)
const EN_IPA_MONOPHTHONGS: CharacterItem[] = [
  { char: '/i:/', romaji: 'Long i', meaning: 'Nguyên âm i dài, miệng cười bè', exampleWord: 'sheep /ʃiːp/', exampleMeaning: 'Con cừu' },
  { char: '/ɪ/', romaji: 'Short i', meaning: 'Nguyên âm i ngắn, phát âm dứt khoát', exampleWord: 'ship /ʃɪp/', exampleMeaning: 'Con tàu biển' },
  { char: '/ʊ/', romaji: 'Short u', meaning: 'Nguyên âm u ngắn, môi hơi tròn', exampleWord: 'good /ɡʊd/', exampleMeaning: 'Tốt' },
  { char: '/u:/', romaji: 'Long u', meaning: 'Nguyên âm u dài, môi chu tròn sâu', exampleWord: 'shoot /ʃuːt/', exampleMeaning: 'Bắn súng' },
  { char: '/e/', romaji: 'Short e', meaning: 'Nguyên âm e ngắn, mở miệng vừa phải', exampleWord: 'bed /bed/', exampleMeaning: 'Cái giường' },
  { char: '/ə/', romaji: 'Schwa', meaning: 'Âm ơ nhẹ phổ biến nhất trong tiếng Anh', exampleWord: 'teacher /ˈtiːtʃə/', exampleMeaning: 'Giáo viên' },
  { char: '/ɜ:/', romaji: 'Long er', meaning: 'Nguyên âm ơ dài, cong lưỡi nhẹ', exampleWord: 'bird /bɜːd/', exampleMeaning: 'Con chim' },
  { char: '/ɔ:/', romaji: 'Long or', meaning: 'Nguyên âm o dài, tròn môi', exampleWord: 'door /dɔː/', exampleMeaning: 'Cánh cửa' },
  { char: '/æ/', romaji: 'A bẹt', meaning: 'Nguyên âm a bẹt, mở rộng miệng giữa a và e', exampleWord: 'cat /kæt/', exampleMeaning: 'Con mèo' },
  { char: '/ʌ/', romaji: 'Short a', meaning: 'Nguyên âm á ngắn, giật bụng', exampleWord: 'up /ʌp/', exampleMeaning: 'Lên trên' },
  { char: '/ɑ:/', romaji: 'Long a', meaning: 'Nguyên âm a dài, hạ hàm sâu', exampleWord: 'car /kɑː/', exampleMeaning: 'Ô tô' },
  { char: '/ɒ/', romaji: 'Short o', meaning: 'Nguyên âm o ngắn, dứt khoát', exampleWord: 'hot /hɒt/', exampleMeaning: 'Nóng' }
];

const EN_IPA_DIPHTHONGS: CharacterItem[] = [
  { char: '/eɪ/', romaji: 'ei', meaning: 'Nguyên âm đôi chuyển từ e sang i', exampleWord: 'day /deɪ/', exampleMeaning: 'Ngày' },
  { char: '/aɪ/', romaji: 'ai', meaning: 'Nguyên âm đôi chuyển từ a sang i', exampleWord: 'my /maɪ/', exampleMeaning: 'Của tôi' },
  { char: '/ɔɪ/', romaji: 'oi', meaning: 'Nguyên âm đôi chuyển từ o sang i', exampleWord: 'boy /bɔɪ/', exampleMeaning: 'Cậu bé' },
  { char: '/aʊ/', romaji: 'au', meaning: 'Nguyên âm đôi chuyển từ a sang u', exampleWord: 'cow /kaʊ/', exampleMeaning: 'Con bò' },
  { char: '/əʊ/', romaji: 'ou', meaning: 'Nguyên âm đôi chuyển từ ơ sang u', exampleWord: 'go /ɡəʊ/', exampleMeaning: 'Đi' },
  { char: '/ɪə/', romaji: 'ia', meaning: 'Nguyên âm đôi chuyển từ i sang ơ', exampleWord: 'near /nɪə/', exampleMeaning: 'Gần' },
  { char: '/eə/', romaji: 'ea', meaning: 'Nguyên âm đôi chuyển từ e sang ơ', exampleWord: 'hair /heə/', exampleMeaning: 'Mái tóc' }
];

const EN_IPA_CONSONANTS: CharacterItem[] = [
  { char: '/θ/', romaji: 'Voiceless th', meaning: 'Âm th thổi gió, đặt đầu lưỡi giữa 2 hàm răng', exampleWord: 'think /θɪŋk/', exampleMeaning: 'Suy nghĩ' },
  { char: '/ð/', romaji: 'Voiced th', meaning: 'Âm th rung họng, đặt đầu lưỡi giữa 2 hàm răng', exampleWord: 'this /ðɪs/', exampleMeaning: 'Cái này' },
  { char: '/ʃ/', romaji: 'sh', meaning: 'Âm s nặng, chu môi thổi hơi ra', exampleWord: 'shoe /ʃuː/', exampleMeaning: 'Chiếc giày' },
  { char: '/ʒ/', romaji: 'zh', meaning: 'Âm d rung, chu môi giống như trong vision', exampleWord: 'vision /ˈvɪʒn/', exampleMeaning: 'Tầm nhìn' },
  { char: '/tʃ/', romaji: 'ch', meaning: 'Âm ch bật hơi mạnh mẽ', exampleWord: 'chair /tʃeə/', exampleMeaning: 'Cái ghế' },
  { char: '/dʒ/', romaji: 'j', meaning: 'Âm j rung cổ họng', exampleWord: 'June /dʒuːn/', exampleMeaning: 'Tháng 6' },
  { char: '/ŋ/', romaji: 'ng', meaning: 'Âm ng ở cuối họng', exampleWord: 'sing /sɪŋ/', exampleMeaning: 'Ca hát' }
];

interface CharacterLabViewProps {
  target?: StudyTarget | null;
  onBack?: () => void;
}

export const CharacterLabView: React.FC<CharacterLabViewProps> = ({ target, onBack }) => {
  const { t } = useTranslation();
  // Xác định ngôn ngữ hiển thị mặc định dựa trên target.languageCode
  const initialLang = target?.languageCode?.toLowerCase() || 'ja';
  const [activeLang, setActiveLang] = useState<string>(
    ['ja', 'zh', 'ko', 'en'].includes(initialLang) ? initialLang : 'ja'
  );

  // Tab con của từng ngôn ngữ
  const [activeSubTab, setActiveSubTab] = useState<string>('tab1');

  // Ký tự đang được chọn để luyện tập viết
  const [selectedChar, setSelectedChar] = useState<CharacterItem>(JA_HIRAGANA[0]);

  // Trạng thái Canvas
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [strokeColor, setStrokeColor] = useState<string>('#1c1917'); // Đen mực tàu
  const [brushWidth, setBrushWidth] = useState<number>(10);
  const [canvasMode, setCanvasMode] = useState<'trace' | 'freehand'>('trace');
  const [evaluationResult, setEvaluationResult] = useState<{
    score: number;
    feedback: string;
    grade: 'A+' | 'A' | 'B' | 'Try Again';
  } | null>(null);

  // Animation nét viết
  const [isAnimating, setIsAnimating] = useState(false);

  // Đồng bộ khi target thay đổi từ bên ngoài
  useEffect(() => {
    if (target?.languageCode) {
      const code = target.languageCode.toLowerCase();
      if (['ja', 'zh', 'ko', 'en'].includes(code)) {
        setActiveLang(code);
        setActiveSubTab('tab1');
        if (code === 'ja') setSelectedChar(JA_HIRAGANA[0]);
        else if (code === 'zh') setSelectedChar(ZH_PINYIN_TONES[0]);
        else if (code === 'ko') setSelectedChar(KO_VOWELS[0]);
        else if (code === 'en') setSelectedChar(EN_IPA_MONOPHTHONGS[0]);
      }
    }
  }, [target?.languageCode]);

  // Cập nhật selectedChar khi đổi ngôn ngữ thủ công
  // Khởi tạo Canvas và lưới ô chữ điền (Tian Zi Ge / Kanji Grid)
  const drawGridAndTemplate = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Kích thước chuẩn
    const width = canvas.width;
    const height = canvas.height;

    // Xóa nền trắng ngà nhẹ
    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(0, 0, width, height);

    // Vẽ lưới ô kẻ (米字格)
    ctx.save();
    ctx.strokeStyle = '#FEE2E2'; // Đỏ nhạt chuẩn vở luyện chữ
    ctx.lineWidth = 1.5;

    // Đường viền ngoài
    ctx.strokeRect(10, 10, width - 20, height - 20);

    // Đường nét đứt chữ thập
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(width / 2, 10);
    ctx.lineTo(width / 2, height - 10);
    ctx.moveTo(10, height / 2);
    ctx.lineTo(width - 10, height / 2);

    // Đường nét đứt chéo góc
    ctx.moveTo(10, 10);
    ctx.lineTo(width - 10, height - 10);
    ctx.moveTo(width - 10, 10);
    ctx.lineTo(10, height - 10);
    ctx.stroke();
    ctx.restore();

    // Vẽ chữ mẫu mờ nếu ở chế độ Trace (Tô theo mẫu)
    if (canvasMode === 'trace' && selectedChar) {
      ctx.save();
      ctx.fillStyle = 'rgba(120, 113, 108, 0.16)'; // Màu xám mờ tao nhã
      ctx.font = 'bold 160px "Noto Serif CJK JP", "Noto Serif CJK SC", "Noto Serif KR", "Times New Roman", serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(selectedChar.char, width / 2, height / 2 + 8);
      ctx.restore();
    }
  }, [canvasMode, selectedChar]);

  useEffect(() => {
    drawGridAndTemplate();
    setEvaluationResult(null);
  }, [drawGridAndTemplate]);

  // Xóa bảng vẽ
  const handleClearCanvas = () => {
    drawGridAndTemplate();
    setEvaluationResult(null);
  };

  // Tương tác vẽ trên Canvas (Chuột & Cảm ứng trên điện thoại/máy tính bảng)
  const getCoordinates = (e: React.MouseEvent<HTMLCanvasElement> | React.TouchEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    if ('touches' in e) {
      return {
        x: (e.touches[0].clientX - rect.left) * scaleX,
        y: (e.touches[0].clientY - rect.top) * scaleY
      };
    }
    return {
      x: (e.clientX - rect.left) * scaleX,
      y: (e.clientY - rect.top) * scaleY
    };
  };

  const startDrawing = (e: React.MouseEvent<HTMLCanvasElement> | React.TouchEvent<HTMLCanvasElement>) => {
    setIsDrawing(true);
    const { x, y } = getCoordinates(e);
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = brushWidth;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
  };

  const draw = (e: React.MouseEvent<HTMLCanvasElement> | React.TouchEvent<HTMLCanvasElement>) => {
    if (!isDrawing) return;
    const { x, y } = getCoordinates(e);
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.lineTo(x, y);
    ctx.stroke();
  };

  const stopDrawing = () => {
    setIsDrawing(false);
  };

  // Chấm điểm nét vẽ (AI Stroke Evaluation)
  const handleEvaluateDrawing = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Lấy dữ liệu pixel để phân tích mật độ nét
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const data = imageData.data;
    let paintedPixels = 0;

    for (let i = 0; i < data.length; i += 4) {
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      // Nếu pixel không phải nền trắng/nền lưới đỏ nhạt
      if (r < 200 && g < 200 && b < 200) {
        paintedPixels++;
      }
    }

    if (paintedPixels < 150) {
      message.warning(t('characterLab.drawFirst'));
      return;
    }

    // Thuật toán đánh giá độ phủ & hình thái nét
    const ratio = paintedPixels / (canvas.width * canvas.height);
    let calculatedScore = 75;
    let feedback = t('characterLab.feedbackAverage');
    let grade: 'A+' | 'A' | 'B' | 'Try Again' = 'A';

    if (ratio > 0.025 && ratio < 0.12) {
      calculatedScore = Math.round(ratio * 100);
      feedback = t('characterLab.feedbackGood');
      grade = 'A+';
    } else if (ratio >= 0.12) {
      calculatedScore = Math.round(ratio * 100);
      feedback = t('characterLab.feedbackDense');
      grade = 'B';
    } else {
      calculatedScore = Math.round(ratio * 100);
      feedback = t('characterLab.feedbackSparse');
      grade = 'Try Again';
    }

    setEvaluationResult({
      score: calculatedScore,
      feedback,
      grade
    });

    message.info(t('characterLab.inkCoverage', { score: calculatedScore }));
  };

  // Phát âm ký tự
  const handleSpeakChar = (text: string) => {
    speechService.speak(text, activeLang, { rate: 0.85 });
  };

  // Mô phỏng hoạt ảnh nét viết (Stroke Animation)
  const handlePlayStrokeAnimation = () => {
    if (isAnimating) return;
    setIsAnimating(true);
    drawGridAndTemplate();

    let step = 0;
    const interval = setInterval(() => {
      step += 10;

      const canvas = canvasRef.current;
      if (canvas) {
        const ctx = canvas.getContext('2d');
        if (ctx) {
          drawGridAndTemplate();
          ctx.save();
          ctx.fillStyle = strokeColor;
          ctx.font = 'bold 160px "Noto Serif CJK JP", "Noto Serif CJK SC", "Noto Serif KR", serif';
          ctx.textAlign = 'center';
          ctx.textBaseline = 'middle';
          ctx.globalAlpha = Math.min(step / 100, 1);
          ctx.fillText(selectedChar.char, canvas.width / 2, canvas.height / 2 + 8);
          ctx.restore();
        }
      }

      if (step >= 100) {
        clearInterval(interval);
        setIsAnimating(false);
      }
    }, 100);
  };

  // Xác định danh sách ký tự theo ngôn ngữ và tab con
  let currentList: CharacterItem[] = [];
  if (activeLang === 'ja') {
    if (activeSubTab === 'tab1') currentList = JA_HIRAGANA;
    else if (activeSubTab === 'tab2') currentList = JA_KATAKANA;
    else currentList = JA_KANJI_N5;
  } else if (activeLang === 'zh') {
    if (activeSubTab === 'tab1') currentList = ZH_PINYIN_TONES;
    else if (activeSubTab === 'tab2') currentList = ZH_RADICALS;
    else currentList = ZH_HANZI_HSK;
  } else if (activeLang === 'ko') {
    if (activeSubTab === 'tab1') currentList = KO_VOWELS;
    else if (activeSubTab === 'tab2') currentList = KO_CONSONANTS;
    else currentList = KO_SYLLABLES;
  } else {
    if (activeSubTab === 'tab1') currentList = EN_IPA_MONOPHTHONGS;
    else if (activeSubTab === 'tab2') currentList = EN_IPA_DIPHTHONGS;
    else currentList = EN_IPA_CONSONANTS;
  }

  if (!target) return <div className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</div>;

  return (
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* HEADER & CHUYỂN ĐỔI NGÔN NGỮ TỨC THÌ */}
      <div className="bg-white border-notebook-2 rounded-2xl p-5 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          {onBack && (
            <button
              onClick={onBack}
              className="p-2 rounded-xl bg-white border border-stone-200 hover:bg-stone-100 transition cursor-pointer text-stone-700"
              title={t('common.back')}
            >
              <ArrowLeftOutlined />
            </button>
          )}
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="p-1.5 rounded-lg bg-rose-100 text-rose-700 text-base font-bold">
                <EditOutlined />
              </span>
              <h1 className="text-xl font-bold text-stone-900 tracking-tight">{t('characterLab.title')}</h1>
            </div>
            <p className="text-xs text-stone-600">{t('characterLab.subtitle')}</p>
          </div>
        </div>

        {/* 4 Nút Chuyển Ngôn Ngữ Nhanh */}
        <div className="rounded-xl border border-stone-200 bg-stone-100 px-3 py-2 text-xs font-bold">{activeLang === 'ja' ? t('libraryPage.japanese') : activeLang === 'zh' ? t('libraryPage.chinese') : activeLang === 'ko' ? t('libraryPage.korean') : t('characterLab.englishIpa')}</div>
      </div>

      {/* KHU VỰC CHÍNH: Khi là Tiếng Anh (IPA) mở rộng toàn màn hình 12 cột, khi là Nhật/Trung/Hàn thì chia 2 cột với Canvas */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* ================= CỘT TRÁI: DANH SÁCH KÝ TỰ ================= */}
        <div className={`${activeLang === 'en' ? 'lg:col-span-12' : 'lg:col-span-7'} bg-white border-notebook-2 rounded-2xl p-5 shadow-xs space-y-4 flex flex-col justify-between`}>
          <div>
            {/* Thanh Tab Danh Mục Phụ */}
            <div className="flex items-center gap-2 border-b border-stone-200 pb-3">
              {activeLang === 'ja' && (
                <>
                  <button
                    onClick={() => setActiveSubTab('tab1')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab1'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ja1')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab2')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab2'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ja2')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab3')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab3'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ja3')}</button>
                </>
              )}

              {activeLang === 'zh' && (
                <>
                  <button
                    onClick={() => setActiveSubTab('tab1')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab1'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.zh1')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab2')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab2'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.zh2')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab3')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab3'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.zh3')}</button>
                </>
              )}

              {activeLang === 'ko' && (
                <>
                  <button
                    onClick={() => setActiveSubTab('tab1')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab1'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ko1')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab2')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab2'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ko2')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab3')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab3'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.ko3')}</button>
                </>
              )}

              {activeLang === 'en' && (
                <>
                  <button
                    onClick={() => setActiveSubTab('tab1')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab1'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.en1')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab2')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab2'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.en2')}</button>
                  <button
                    onClick={() => setActiveSubTab('tab3')}
                    className={`px-3 py-1.5 rounded-lg text-xs font-bold cursor-pointer transition ${
                      activeSubTab === 'tab3'
                        ? 'bg-rose-50 text-rose-700 border border-rose-300'
                        : 'text-stone-600 hover:bg-stone-100'
                    }`}
                  >
                    {t('characterLab.en3')}</button>
                </>
              )}
            </div>

            {/* Lưới Thẻ Ký Tự */}
            <div className="grid grid-cols-4 sm:grid-cols-5 md:grid-cols-6 gap-2.5 pt-4 max-h-[460px] overflow-y-auto pr-1">
              {currentList.map((item, idx) => {
                const isSelected = selectedChar?.char === item.char;
                return (
                  <button
                    key={idx}
                    onClick={() => {
                      setSelectedChar(item);
                      handleSpeakChar(item.char);
                    }}
                    className={`p-2.5 rounded-xl border text-center transition flex flex-col items-center justify-between cursor-pointer group select-none ${
                      isSelected
                        ? 'bg-amber-100 border-amber-500 shadow-2xs ring-2 ring-amber-400/40'
                        : 'bg-stone-50/80 hover:bg-stone-100 border-stone-200'
                    }`}
                  >
                    <span className="text-2xl font-bold text-stone-900 group-hover:scale-110 transition-transform">
                      {item.char}
                    </span>
                    <span className="text-[10px] font-mono text-stone-600 mt-1 font-semibold truncate max-w-full">
                      {item.romaji}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Chi tiết ký tự được chọn */}
          {selectedChar && (
            <div className="mt-4 p-4 rounded-xl bg-amber-50/70 border border-amber-200/80 flex items-center justify-between gap-4">
              <div className="flex items-center gap-4">
                <div className="w-14 h-14 rounded-xl bg-white border border-amber-300 flex items-center justify-center text-3xl font-bold text-stone-900 shadow-2xs shrink-0">
                  {selectedChar.char}
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-bold text-stone-900">
                      {t('characterLab.pronunciation')}: <span className="font-mono text-rose-700">{selectedChar.romaji}</span>
                    </span>
                    {selectedChar.strokes && (
                      <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-stone-200 text-stone-700">
                        {t('characterLab.strokes', { count: selectedChar.strokes })}
                      </span>
                    )}
                  </div>
                  {selectedChar.meaning && (
                    <p className="text-xs text-stone-700 mt-0.5">{selectedChar.meaning}</p>
                  )}
                  {selectedChar.exampleWord && (
                    <p className="text-xs text-stone-600 mt-1">
                      {t('characterLab.example')}: <strong className="text-stone-900">{selectedChar.exampleWord}</strong>{' '}
                      {selectedChar.exampleMeaning && `(${selectedChar.exampleMeaning})`}
                    </p>
                  )}
                </div>
              </div>

              <button
                onClick={() => handleSpeakChar(selectedChar.char)}
                className="w-10 h-10 rounded-full bg-stone-900 text-white flex items-center justify-center hover:bg-stone-800 transition cursor-pointer shrink-0 shadow-xs"
                title={t('characterLab.listen')}
              >
                <SoundOutlined className="text-base" />
              </button>
            </div>
          )}
        </div>

        {/* ================= CỘT PHẢI: KHUNG LUYỆN NÉT CANVAS (Chỉ áp dụng cho Nhật, Trung, Hàn) ================= */}
        {activeLang !== 'en' && (
          <div className="lg:col-span-5 bg-white border-notebook-2 rounded-2xl p-5 shadow-xs space-y-4">
            <div className="flex items-center justify-between">
            <h2 className="text-sm font-bold text-stone-900 flex items-center gap-1.5">
              <BulbOutlined className="text-amber-500" />
              {t('characterLab.canvas')}
            </h2>

            {/* Chuyển Chế độ: Tô theo mẫu hoặc Viết tự do */}
            <div className="flex items-center gap-1 p-0.5 rounded-lg bg-stone-100 border border-stone-200 text-[11px] font-bold">
              <button
                onClick={() => setCanvasMode('trace')}
                className={`px-2.5 py-1 rounded-md transition cursor-pointer ${canvasMode === 'trace' ? 'bg-white shadow-2xs text-stone-900' : 'text-stone-500'}`}
              >{t('characterLab.trace')}</button>
              <button
                onClick={() => setCanvasMode('freehand')}
                className={`px-2.5 py-1 rounded-md transition cursor-pointer ${canvasMode === 'freehand' ? 'bg-white shadow-2xs text-stone-900' : 'text-stone-500'}`}
              >{t('characterLab.freehand')}</button>
            </div>
          </div>

          {/* Ô VẼ CANVAS Ô CHỮ ĐIỀN */}
          <div className="flex flex-col items-center">
            <div className="relative border-2 border-rose-300 rounded-2xl overflow-hidden shadow-inner bg-white select-none touch-none">
              <canvas
                ref={canvasRef}
                width={320}
                height={320}
                onMouseDown={startDrawing}
                onMouseMove={draw}
                onMouseUp={stopDrawing}
                onMouseLeave={stopDrawing}
                onTouchStart={startDrawing}
                onTouchMove={draw}
                onTouchEnd={stopDrawing}
                className="cursor-crosshair block"
              />

              {/* Watermark hướng dẫn mờ ở góc */}
              <span className="absolute bottom-2 right-2 text-[9px] font-mono text-stone-400 pointer-events-none">
                Tian Zi Ge Grid
              </span>
            </div>

            {/* Thanh Công Cụ Nét Bút */}
            <div className="w-full flex items-center justify-between mt-3 px-1">
              {/* Chọn Màu Mực Thư Pháp */}
              <div className="flex items-center gap-1.5">
                {[
                  { color: '#1c1917', label: t('characterLab.blackInk') },
                  { color: '#1e3a8a', label: t('characterLab.blueInk') },
                  { color: '#b91c1c', label: t('characterLab.redInk') }
                ].map((item) => (
                  <button
                    key={item.color}
                    onClick={() => setStrokeColor(item.color)}
                    style={{ backgroundColor: item.color }}
                    className={`w-5 h-5 rounded-full cursor-pointer transition ${
                      strokeColor === item.color ? 'ring-2 ring-offset-2 ring-stone-900 scale-110' : ''
                    }`}
                    title={item.label}
                  />
                ))}
              </div>

              {/* Chọn Độ Dày Nét */}
              <div className="flex items-center gap-1 text-[11px] font-mono text-stone-600">
                <button
                  onClick={() => setBrushWidth(6)}
                  className={`px-2 py-0.5 rounded border ${brushWidth === 6 ? 'bg-stone-900 text-white font-bold' : 'bg-white'}`}
                >{t('characterLab.thin')}</button>
                <button
                  onClick={() => setBrushWidth(10)}
                  className={`px-2 py-0.5 rounded border ${brushWidth === 10 ? 'bg-stone-900 text-white font-bold' : 'bg-white'}`}
                >{t('characterLab.medium')}</button>
                <button
                  onClick={() => setBrushWidth(16)}
                  className={`px-2 py-0.5 rounded border ${brushWidth === 16 ? 'bg-stone-900 text-white font-bold' : 'bg-white'}`}
                >{t('characterLab.bold')}</button>
              </div>

              {/* Nút Xóa Nhanh */}
              <button
                onClick={handleClearCanvas}
                className="px-2.5 py-1 text-xs font-semibold text-stone-600 hover:text-stone-900 hover:bg-stone-100 rounded-lg transition flex items-center gap-1 cursor-pointer"
              ><ClearOutlined /><span>{t('characterLab.clear')}</span>
              </button>
            </div>
          </div>

          {/* CÁC NÚT THAO TÁC ĐÁNH GIÁ & HOẠT ẢNH */}
          <div className="space-y-2 pt-2">
            <div className="grid grid-cols-2 gap-2">
              <button
                onClick={handlePlayStrokeAnimation}
                disabled={isAnimating}
                className="py-2.5 px-3 rounded-xl border border-stone-300 bg-white hover:bg-stone-50 font-bold text-xs text-stone-800 transition flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50"
              >
                <PlayCircleOutlined className="text-amber-600" />
                <span>{isAnimating ? t('characterLab.animating') : t('characterLab.animate')}</span>
              </button>

              <button
                onClick={handleEvaluateDrawing}
                className="py-2.5 px-3 rounded-xl bg-stone-900 text-white font-bold text-xs hover:bg-stone-800 transition flex items-center justify-center gap-1.5 cursor-pointer shadow-xs"
              ><CheckCircleOutlined className="text-emerald-400" /><span>{t('characterLab.evaluate')}</span>
              </button>
            </div>

            {/* BẢNG KẾT QUẢ ĐÁNH GIÁ NÉT BÚT */}
            {evaluationResult && (
              <div className="p-3.5 rounded-xl bg-gradient-to-br from-stone-900 to-stone-800 text-white shadow-md space-y-2 animate-in fade-in duration-300">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <TrophyOutlined className="text-amber-400 text-lg" />
                    <span className="text-xs font-bold uppercase tracking-wider text-stone-300">
                      {t('characterLab.result')}
                    </span>
                  </div>
                  <span className="text-base font-extrabold text-amber-300 font-mono">
                    {t('characterLab.inkCoverage', { score: evaluationResult.score })}
                  </span>
                </div>

                <p className="text-xs text-stone-300 leading-relaxed">
                  {evaluationResult.feedback}
                </p>

                <div className="flex items-center justify-between pt-1 border-t border-stone-700/80 text-[11px] text-stone-400 font-mono">
                  <span>{t('characterLab.coverageHint')}</span>
                  <span>{t('characterLab.gridBalance')}</span>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  </div>
);
};
