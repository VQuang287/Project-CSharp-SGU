/*
  Seed data: Phố Ẩm Thực Vĩnh Khánh - Quận 4, TP.HCM
  Chạy sau khi đã tạo database
*/

USE [TourMapAdmin];
GO

-- Xóa POI cũ nếu có (optional)
-- DELETE FROM Pois WHERE Title LIKE N'%Vĩnh Khánh%';

-- Thêm POI Phố Ẩm Thực Vĩnh Khánh
INSERT INTO [dbo].[Pois] (
    [Id], [Title], [Description], [Latitude], [Longitude], [RadiusMeters], 
    [Priority], [IsActive], [ImageUrl], [AudioUrl], [MapLink],
    [DescriptionEn], [DescriptionZh], [DescriptionKo], [DescriptionJa], [DescriptionFr],
    [TtsScriptVi], [TtsScriptEn], [UpdatedAt]
)
VALUES
-- 1. Đầu phố Vĩnh Khánh (Ngã tư Vĩnh Khánh - Hoàng Diệu)
(
    NEWID(),
    N'Đầu Phố Ẩm Thực Vĩnh Khánh',
    N'Điểm bắt đầu phố ẩm thực Vĩnh Khánh nổi tiếng. Nơi đây tập trung hàng chục quán ốc, hải sản tươi sống với giá bình dân. Đặc biệt nổi tiếng vào buổi tối khi các quán bắt đầu nhộn nhịp.',
    10.7649, 106.6958, 50, 1, 1,
    N'https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800',
    NULL, N'https://maps.google.com/?q=10.7649,106.6958',
    N'Entrance of Vinh Khanh Food Street. Famous seafood street with dozens of fresh seafood restaurants at affordable prices. Best visited in the evening.',
    N'美食街入口。著名的海鲜街，有数十家价格实惠的新鲜海鲜餐厅。晚上最热闹。',
    N'빈칸 음식거리 입구. 저렴한 가격에 신선한 해산물을 제공하는 수십 개의 레스토랑이 있는 유명한 해산물 거리. 저녁에 방문하기 가장 좋습니다.',
    N'ビンカン美食街の入り口。手頃な価格で新鮮なシーフードを提供する数十店舗が集まる有名なシーフード街。夕方が最も賑やかです。',
    N'Entrée de la rue gastronomique Vinh Khanh. Rue des fruits de mer célèbre avec des dizaines de restaurants de fruits de mer frais à prix abordables.',
    N'Bạn đang ở đầu phố ẩm thực Vĩnh Khánh. Đây là thiên đường hải sản với hàng chục quán ốc, tôm, cua, ghẹ tươi ngon. Hãy thưởng thức các món đặc sản như ốc hương xào me, tôm nướng muối ớt, hay cua rang me.',
    N'Welcome to Vinh Khanh Food Street! This is a seafood paradise with dozens of restaurants serving fresh snails, shrimp, and crabs.',
    GETDATE()
),

-- 2. Quán Ốc Loan (nổi tiếng nhất)
(
    NEWID(),
    N'Quán Ốc Loan - Vĩnh Khánh',
    N'Quán ốc nổi tiếng nhất phố Vĩnh Khánh, hoạt động hơn 20 năm. Chuyên các món ốc hương, ốc len, ốc móng tay xào me, nướng mỡ hành. Giá bình dân, phục vụ nhanh.',
    10.7652, 106.6961, 30, 2, 1,
    N'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=800',
    NULL, N'https://maps.google.com/?q=10.7652,106.6961',
    N'Oc Loan Restaurant - Most famous snail shop on Vinh Khanh Street, operating for over 20 years. Specializes in butter garlic snails, grilled snails with scallion oil.',
    N'螺餐厅-美食街上最著名的螺蛳店，经营超过20年。专门提供黄油蒜香螺蛳和葱油烤螺蛳。',
    N'옥 롄 레스토랑 - 빈칸 거리에서 가장 유명한 달팽이 가게, 20년 이상 영업. 버터 마늘 달팽이, 파 기름 구운 달팽이 전문.',
    N'オック・ロアン・レストラン - ビンカン通りで最も有名なエスカルゴ店、20年以上営業。バターガーリックスネイル、ネギ油焼きスネイルが名物。',
    N'Restaurant Oc Loan - Le magasin d escargots le plus célèbre de la rue Vinh Khanh, en activité depuis plus de 20 ans. Spécialisé dans les escargots au beurre ail.',
    N'Bạn đang đứng trước Quán Ốc Loan - huyền thoại của phố Vĩnh Khánh. Quán nổi tiếng với ốc hương xào me chua ngọt, ốc len nướng mỡ hành thơm lừng. Đây là địa điểm không thể bỏ qua khi đến phố ẩm thực này.',
    N'You are standing at Oc Loan Restaurant - a legend of Vinh Khanh Street. Famous for sweet and sour butter garlic snails and grilled snails with scallion oil.',
    GETDATE()
),

-- 3. Quán Hải Sản 123
(
    NEWID(),
    N'Hải Sản 123 - Vĩnh Khánh',
    N'Quán hải sản tươi sống với bể chứa lớn. Khách chọn hải sản tươi rồi chế biến theo yêu cầu: nướng, hấp, xào, rang me. Đặc biệt có tôm càng xanh, cua thịt, ghẹ.',
    10.7655, 106.6965, 30, 2, 1,
    N'https://images.unsplash.com/photo-1534939561126-855b8675edd7?w=800',
    NULL, N'https://maps.google.com/?q=10.7655,106.6965',
    N'Seafood 123 Restaurant - Fresh seafood with large tanks. Customers choose fresh seafood and customize cooking: grilled, steamed, stir-fried, or tamarind sauce.',
    N'海鲜123餐厅-有大型水箱的新鲜海鲜餐厅。顾客选择新鲜海鲜并定制烹饪方式：烧烤、蒸煮、炒制或罗望子酱。',
    N'씨푸드 123 레스토랑 - 대형 수조를 갖춘 신선한 해산물 전문점. 신선한 해산물을 선택하여 구이, 찜, 볶음, 타마린드 소스 등으로 요리합니다.',
    N'シーフード123レストラン - 大型水槽付きの新鮮なシーフードレストラン。新鮮なシーフードを選んで、焼き、蒸し、炒め、タマリンドソースなどの調理法で注文できます。',
    N'Restaurant Fruits de Mer 123 - Fruits de mer frais avec grands réservoirs. Les clients choisissent des fruits de mer frais et personnalisent la cuisson.',
    N'Hải Sản 123 - nơi hải sản tươi roi rói trong bể kính. Bạn có thể chọn tôm càng, cua, ghẹ tươi và yêu cầu chế biến theo ý thích: nướng mọi, hấp bia, rang me, hay xào tỏi.',
    N'Welcome to Seafood 123 - where seafood is fresh in glass tanks. Choose your live blue claw crabs, rock crabs, or mantis shrimp and customize the cooking method.',
    GETDATE()
),

-- 4. Quán Lẩu Cua Đồng
(
    NEWID(),
    N'Lẩu Cua Đồng Vĩnh Khánh',
    N'Quán chuyên các loại lẩu: lẩu cua đồng, lẩu hải sản, lẩu Thái. Nước lẩu đậm đà, cua đồng béo ngậy. Phù hợp cho nhóm 4-6 người. Giá trung bình 150-200k/người.',
    10.7650, 106.6955, 30, 3, 1,
    N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800',
    NULL, N'https://maps.google.com/?q=10.7650,106.6955',
    N'Field Crab Hotpot Restaurant - Specializes in field crab hotpot, seafood hotpot, Thai hotpot. Rich broth, fatty crabs. Good for groups of 4-6 people.',
    N'田蟹火锅店-专门提供田蟹火锅、海鲜火锅、泰式火锅。浓郁的汤底，肥美的螃蟹。适合4-6人的团体。',
    N'들판 게 전골 레스토랑 - 들판 게 전골, 해산물 전골, 태국 전골 전문. 진한 국물, 기름진 게. 4-6인 그룹에 적합.',
    N'田んぼカニ鍋レストラン - 田んぼカニ鍋、シーフード鍋、タイ鍋が名物。濃厚なスープ、脂身のあるカニ。4〜6人のグループに最適。',
    N'Restaurant Fondue aux Crabes des Champs - Spécialisé dans la fondue aux crabes des champs, fruits de mer, thaïlandaise. Bouillon riche, crabes gras.',
    N'Lẩu Cua Đồng - nơi bạn thưởng thức nồi lẩu cua đồng béo ngậy với nước lẩu đậm đà. Cua đồng tươi, thịt ngọt, kết hợp với rau muống, bông bí, bún tươi.',
    N'Field Crab Hotpot - enjoy a rich pot of fatty field crabs with flavorful broth. Fresh sweet crab meat with morning glory, pumpkin flowers, and fresh noodles.',
    GETDATE()
),

-- 5. Quán Bún Riêu Cua
(
    NEWID(),
    N'Bún Riêu Cua Vĩnh Khánh',
    N'Quán bún riêu cua truyền thống, nước dùng ninh từ cua đồng, có thêm riêu cua, đậu hũ, rau muống. Hương vị đậm đà, chua cay tùy khẩu vị. Giá 40-60k/tô.',
    10.7645, 106.6952, 25, 3, 1,
    N'https://images.unsplash.com/photo-1548943487-a2e4e43b4853?w=800',
    NULL, N'https://maps.google.com/?q=10.7645,106.6952',
    N'Traditional Crab Noodle Soup - broth simmered from field crabs, with crab paste, tofu, morning glory. Rich flavor, sour and spicy to taste.',
    N'传统蟹肉粉餐厅-用田蟹熬制的汤底，配有蟹膏、豆腐、通心菜。浓郁的味道，可根据口味调整酸辣度。',
    N'전통 게 국수 레스토랑 - 들판 게로 끓인 국물, 게 페이스트, 두부, 아침 영광 채소가 들어갑니다. 진한 맛, 신맛과 매운맛 조절 가능.',
    N'伝統的なカニ麺スープ - 田んぼカニで煮込んだスープに、カニペースト、豆腐、空芯菜をトッピング。濃厚な味わい、酸辣調整可能。',
    N'Restaurant Traditionnel Soupe de Nouilles aux Crabes - Bouillon mijoté aux crabes des champs, avec pâte de crabe, tofu, légumes. Goût riche.',
    N'Bún Riêu Cua - món ăn dân dã nhưng đầy hương vị quê nhà. Nước dùng ninh từ cua đồng, gạch cua béo ngậy, đậu hũ chiên giòn, rau muống chần tái.',
    N'Crab Noodle Soup - a rustic dish full of hometown flavors. Broth simmered from field crabs, fatty crab roe, crispy fried tofu, blanched morning glory.',
    GETDATE()
),

-- 6. Quán Chè Thái Lan
(
    NEWID(),
    N'Chè Thái Lan Vĩnh Khánh',
    N'Quán chè Thái nổi tiếng với chè thái sầu riêng, chè khúc bạch, chè khoai dẻo. Ngọt thanh, thơm mát. Giải khát hoàn hảo sau bữa ăn hải sản cay nồng. Giá 25-40k/ly.',
    10.7657, 106.6968, 25, 3, 1,
    N'https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=800',
    NULL, N'https://maps.google.com/?q=10.7657,106.6968',
    N'Thai Dessert Shop - Famous for Thai durian sweet soup, white taro pearls, chewy sweet potato dessert. Sweet and refreshing. Perfect after spicy seafood.',
    N'泰式甜品店-以泰式榴莲甜汤、白芋圆、糯甜薯甜品闻名。清甜爽口。辛辣海鲜后的完美解腻选择。',
    N'태국 디저트 가게 - 태국 두리안 단팥죽, 백색 타로 펄, 쫀득한 고구마 디저트로 유명. 달콤하고 상큼. 매운 해산물 후 완벽한 간식.',
    N'タイデザート店 - タイ風ドリアンスイートスープ、白いタロイモ、モチモチのスイートポテトデザートで有名。甘く爽やか。辛いシーフード後の完璧なデザート。',
    N'Pâtisserie Thaïlandaise - Célèbre pour la soupe sucrée thaïlandaise au durian, perles de taro blanc. Doux et rafraîchissant. Parfait après les fruits de mer épicés.',
    N'Chè Thái Lan - món tráng miệng thanh mát sau bữa tiệc hải sản. Chè thái sầu riêng béo ngậy, chè khúc bạch dai dai, chè khoai dẻo thơm bùi. Giải nhiệt hoàn hảo!',
    N'Thai Dessert - the perfect cooling treat after a seafood feast. Thai durian sweet soup with fatty durian, chewy white taro pearls, and fragrant sweet potato balls.',
    GETDATE()
),

-- 7. Quán Bánh Xèo Miền Tây
(
    NEWID(),
    N'Bánh Xèo Miền Tây Vĩnh Khánh',
    N'Bánh xèo Nam Bộ giòn rụm, nhân tôm thịt, giá sống, rau thơm. Cuốn bánh tráng chấm nước mắm chua ngọt. Đặc sản miền Tây giữa lòng Sài Gòn. Giá 15-25k/cái.',
    10.7643, 106.6950, 25, 3, 1,
    N'https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800',
    NULL, N'https://maps.google.com/?q=10.7643,106.6950',
    N'Western Vietnam Crispy Pancake - Crispy rice pancake with shrimp and pork filling, bean sprouts, herbs. Wrapped in rice paper with sweet and sour fish sauce.',
    N'越南西部煎饼-酥脆的米煎饼，内含虾肉馅料、豆芽、香草。用米纸包裹蘸酸甜鱼露。',
    N'베트남 서부 바삭한 팬케이크 - 새우와 돼지고기, 숙주나물, 허브가 들어간 바삭한 쌀 팬케이크. 라이스 페이퍼로 싸서 달콤새콤한 피쉬 소스에 찍어 먹습니다.',
    N'ベトナム西部クリスピーパンケーキ - エビと豚肉、もやし、ハーブ入りのサクサクライスパンケーキ。ライスペーパーで包んで甘酸っぱいフィッシュソースで召し上がれ。',
    N'Crêpe Croustillante du Vietnam Occidental - Crêpe de riz croustillante avec garniture de crevettes et porc. Enveloppée dans du papier de riz avec sauce poisson aigre-douce.',
    N'Bánh Xèo Miền Tây - tiếng xèo xèo vui tai, mùi nghệ thơm lừng, vỏ bánh giòn tan. Cuốn với rau sống, chấm nước mắm pha chua ngọt là đúng bài!',
    N'Western Vietnam Crispy Pancake - the delightful sizzling sound, fragrant turmeric aroma, crispy shell. Wrap with fresh herbs and dip in sweet and sour fish sauce.',
    GETDATE()
),

-- 8. Quán Gỏi Cá Mai
(
    NEWID(),
    N'Gỏi Cá Mai Vĩnh Khánh',
    N'Chuyên gỏi cá mai tươi sống - đặc sản biển Phan Thiết. Cá mai trộn đu đủ xanh, rau thơm, đậu phộng rang, chấm nước mắm me. Tươi ngon, không tanh.',
    10.7659, 106.6970, 30, 2, 1,
    N'https://images.unsplash.com/photo-1534604973900-c43ab4c2e0ab?w=800',
    NULL, N'https://maps.google.com/?q=10.7659,106.6970',
    N'Fresh Mai Fish Salad - Specialty from Phan Thiet sea. Fresh mai fish with green papaya, herbs, roasted peanuts, tamarind fish sauce. Fresh and not fishy.',
    N'新鲜鲻鱼沙拉-潘切海特产。新鲜鲻鱼配青木瓜、香草、烤花生、罗望子鱼露。新鲜无腥味。',
    N'신선한 마이 생선 샐러드 - 판티엣 바다 특산물. 신선한 마이 생선에 청파파야, 허브, 볶은 땅콩, 타마린드 피쉬 소스. 신선하고 비린내 없음.',
    N'新鮮マイ魚サラダ - ファンティエット海の特産品。新鮮なマイ魚に青パパイヤ、ハーブ、ローストピーナッツ、タマリンドフィッシュソース。新鮮で魚臭くない。',
    N'Salade de Poisson Mai Frais - Spécialité de la mer de Phan Thiet. Poisson mai frais avec papaye verte, herbes, cacahuètes grillées, sauce poisson tamarin.',
    N'Gỏi Cá Mai - đặc sản biển Phan Thiết ngay giữa Sài Gòn. Cá mai tươi roi rói, trộn đu đủ bào sợi, rau thơm, đậu phộng rang giòn. Nước chấm me chua ngọt đặc trưng.',
    N'Fresh Mai Fish Salad - a Phan Thiet seafood specialty in Saigon. Super fresh fish with shredded green papaya, herbs, crispy roasted peanuts. Signature sweet and sour tamarind sauce.',
    GETDATE()
),

-- 9. Quán Bia Tươi Hải Sản
(
    NEWID(),
    N'Bia Tươi Hải Sản Vĩnh Khánh',
    N'Quán nhậu bia tươi kèm hải sản nướng. Bia Đức, bia Séc nhập khẩu. Mực nướng sa tế, tôm nướng muối ớt, sò lông nướng mỡ hành. Không gian thoáng mát.',
    10.7641, 106.6948, 35, 3, 1,
    N'https://images.unsplash.com/photo-1575037614876-c38a767c7d99?w=800',
    NULL, N'https://maps.google.com/?q=10.7641,106.6948',
    N'Draft Beer & Seafood - German and Czech imported draft beer. Grilled squid with sate, salt and pepper shrimp, grilled scallops with scallion oil. Open airy space.',
    N'生啤海鲜-德国和捷克进口生啤。烤沙爹鱿鱼、盐胡椒虾、烤葱油扇贝。开放通风的空间。',
    N'생맥주와 해산물 - 독일 및 체코 수입 생맥주. 사테 소스 오징어 구이, 소금 고추 새우 구이, 파 기름 가리비 구이. 개방적이고 통풍이 잘 되는 공간.',
    N'ドラフトビールとシーフード - ドイツとチェコ産の輸入ドラフトビール。サテソースのイカ焼き、塩胡椒エビ、ネギ油ホタテ焼き。開放的で風通しの良い空間。',
    N'Bière Pression et Fruits de Mer - Bière pression importée d Allemagne et de Tchéquie. Calmar grillé au saté, crevettes au sel et poivre. Espace aéré et ouvert.',
    N'Bia Tươi Hải Sản - nơi bạn thưởng thức bia Đức, bia Séc mát lạnh cùng hải sản nướng thơm nức. Mực sa tế cay nồng, tôm muối ớt đậm đà, sò lông béo ngậy.',
    N'Draft Beer & Seafood - enjoy ice-cold German and Czech beer with fragrant grilled seafood. Spicy sate squid, savory salt and pepper shrimp, fatty grilled scallops.',
    GETDATE()
),

-- 10. Quán Cơm Tấm Sườn Bì Chả
(
    NEWID(),
    N'Cơm Tấm Sườn Bì Chả Vĩnh Khánh',
    N'Cơm tấm sườn nướng chả cua đặc trưng Sài Gòn. Sườn nướng mật ong, bì thái chỉ, chả trứng hấp. Cơm tấm dẻo thơm, canh khổ qua nhồi thịt. Giá 40-60k/dĩa.',
    10.7639, 106.6946, 25, 3, 1,
    N'https://images.unsplash.com/photo-1604382354936-7c7e0a1f9d9e?w=800',
    NULL, N'https://maps.google.com/?q=10.7639,106.6946',
    N'Broken Rice with Grilled Pork - Saigon signature: grilled honey pork chop, shredded pork skin, steamed egg meatloaf. Fragrant sticky rice, bitter melon soup.',
    N'碎米饭配烤猪肉-西贡招牌：蜂蜜烤猪排、切丝猪皮、蒸蛋肉饼。香糯碎米饭、苦瓜汤。',
    N'부서진 쌀과 구운 돼지고기 - 사이공 명물: 꿀 돼지고기 스테이크, 채썬 돼지껍질, 찐 계란 미트로프. 향기롭고 찰진 쌀, 여주 수프.',
    N'割れた米と焼き豚 - サイゴンの名物：ハニーポークチョップ、千切りポークスキン、蒸し卵ミートローフ。香り高くモチモチの米、ゴーヤスープ。',
    N'Riz Cassé avec Porc Grillé - Spécialité de Saigon: côtelette de porc grillée au miel, peau de porc, pain de viande aux œufs cuit à la vapeur. Riz parfumé et collant.',
    N'Cơm Tấm Sườn Bì Chả - đặc sản Sài Gòn không thể bỏ qua. Sườn nướng mật ong vàng ươm, bì trộn thính giòn sần sật, chả trứng mềm mịn. Đĩa cơm đầy đặn, no lòng!',
    N'Broken Rice with Grilled Pork - a must-try Saigon specialty. Golden honey-grilled pork chop, crispy shredded pork skin with toasted rice powder, soft steamed egg meatloaf.',
    GETDATE()
);

GO

PRINT N'Đã thêm 10 POI phố ẩm thực Vĩnh Khánh thành công!';
GO
