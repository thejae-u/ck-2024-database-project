-- 데이터베이스 선택
USE game_db;

-- 테이블 생성 (기존 테이블 삭제 후 재생성하여 초기화 보장)
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS log;
DROP TABLE IF EXISTS user_item;
DROP TABLE IF EXISTS item_list;
DROP TABLE IF EXISTS userinfo;
SET FOREIGN_KEY_CHECKS = 1;

-- 유저 정보 테이블
CREATE TABLE userinfo (
    uid VARCHAR(50) PRIMARY KEY,
    cash INT DEFAULT 0
);

-- 아이템 리스트 테이블 (판매 중인 아이템)
CREATE TABLE item_list (
    id INT AUTO_INCREMENT PRIMARY KEY,
    uid VARCHAR(50),
    item VARCHAR(100),
    price INT,
    FOREIGN KEY (uid) REFERENCES userinfo(uid)
);

-- 유저 아이템 테이블 (소유 중인 아이템)
CREATE TABLE user_item (
    id INT AUTO_INCREMENT PRIMARY KEY,
    item_name VARCHAR(100),
    uid VARCHAR(50),
    FOREIGN KEY (uid) REFERENCES userinfo(uid)
);

-- 로그 테이블
CREATE TABLE log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 1. 유저 정보 초기화 (userinfo_reset.txt 기반)
INSERT INTO userinfo (uid, cash) VALUES 
('userA', 50000),
('userB', 100000),
('userC', 80000),
('userD', 120000),
('userE', 500000),
('userF', 100000),
('userG', 35000),
('userH', 1000000);

-- 2. 유저 소유 아이템 초기화 (user_item_reset.txt 기반)
INSERT INTO user_item (item_name, uid) VALUES 
('Sword', 'userA'),
('GunA', 'userB'),
('GunExtra', 'userD'),
('SpecialGun', 'userD'),
('LegendSword', 'userF'),
('HP_Potion', 'userC'),
('EXP_Potion', 'userA'),
('LegendWing', 'userD'),
('Rock', 'userH'),
('LegendBoxKey', 'userH'),
('NormalBoxKey', 'userG'),
('NormalBoxKey', 'userF'),
('HP_Potion', 'userF'),
('TeleportScroll', 'userA'),
('HandWeaponEnhanceScroll', 'userB'),
('BodyArmorEnhanceScroll', 'userB'),
('NormalAmmo', 'userG'),
('SepcialAmmo', 'userD'),
('LegendAmmo', 'userE'),
('SepcialAmmo', 'userA');

-- 3. 판매 등록 아이템 초기화 (item_list_reset.txt 기반)
INSERT INTO item_list (item, price, uid) VALUES 
('Sword', 3000, 'userA'),
('GunA', 10000, 'userB'),
('GunExtra', 8000, 'userD'),
('SpecialGun', 80000, 'userD'),
('LegendSword', 100000, 'userF'),
('HP_Potion', 3000, 'userC'),
('EXP_Potion', 10000, 'userA'),
('LegendWing', 250000, 'userD'),
('Rock', 1000, 'userH'),
('LegendBoxKey', 55000, 'userH'),
('NormalBoxKey', 15000, 'userG'),
('NormalBoxKey', 16000, 'userF'),
('HP_Potion', 3200, 'userF'),
('TeleportScroll', 1500, 'userA'),
('HandWeaponEnhanceScroll', 50000, 'userB'),
('BodyArmorEnhanceScroll', 30000, 'userB'),
('NormalAmmo', 1000, 'userG'),
('SepcialAmmo', 1500, 'userD'),
('LegendAmmo', 3000, 'userE'),
('SepcialAmmo', 1200, 'userA');
