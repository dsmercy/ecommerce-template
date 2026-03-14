-- =============================================================================
--  ECOMMERCE DATABASE — SEED SCRIPT
--  Target  : MySQL 8.0  |  utf8mb4_unicode_ci
--  Schema  : ecommerce_schema.sql (provided)
--
--  FIXES vs previous version:
--    1. `rows` → `row_count`  (rows is reserved in MySQL 8.0)
--    2. payments INSERT includes created_at  (schema has NOT NULL DEFAULT)
--    3. inventory INSERT omits id column  (AUTO_INCREMENT, let DB assign)
--       actually kept explicit IDs — all match schema column order exactly
--
--  TEST CREDENTIALS:
--    Admin    → admin@ecommerce.com    / Admin@123
--    Customer → john.doe@example.com  / Customer@123
--    Customer → jane.smith@example.com / Customer@123
--    Customer → bob.johnson@example.com / Customer@123
--    Customer → alice.w@example.com   / Customer@123
--
--  BCrypt work factor 11. Verify at https://bcrypt-generator.com
-- =============================================================================

USE ecommerce_db;
SET FOREIGN_KEY_CHECKS = 0;

-- ── clean slate ───────────────────────────────────────────────────────────────
TRUNCATE TABLE reviews;
TRUNCATE TABLE payments;
TRUNCATE TABLE order_items;
TRUNCATE TABLE orders;
TRUNCATE TABLE cart_items;
TRUNCATE TABLE carts;
TRUNCATE TABLE inventory;
TRUNCATE TABLE product_variants;
TRUNCATE TABLE product_images;
TRUNCATE TABLE products;
TRUNCATE TABLE categories;
TRUNCATE TABLE addresses;
TRUNCATE TABLE coupons;
TRUNCATE TABLE users;

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
-- 1. USERS
--    Schema cols: id, name, email, password_hash, phone, role,
--                 created_at, updated_at, deleted_at
-- =============================================================================
INSERT INTO users
    (id, name, email, password_hash, phone, role, created_at, updated_at, deleted_at)
VALUES
(1, 'Super Admin',    'admin@ecommerce.com',       '$2a$11$rBnbh5Nr4BbEjVgFCzjOP.Kk3oZRCdGBbCmFQDensDfwI.tNC.vMm', '+1-800-000-0001', 'ADMIN',    NOW(), NOW(), NULL),
(2, 'John Doe',       'john.doe@example.com',      '$2a$11$Qu3w5RtzSmKK2CaRyLn7o.1YoVdHv.6EbSFNm0LvRlANyiJZe/Vry', '+1-555-100-2001', 'CUSTOMER', NOW(), NOW(), NULL),
(3, 'Jane Smith',     'jane.smith@example.com',    '$2a$11$Qu3w5RtzSmKK2CaRyLn7o.1YoVdHv.6EbSFNm0LvRlANyiJZe/Vry', '+1-555-100-2002', 'CUSTOMER', NOW(), NOW(), NULL),
(4, 'Bob Johnson',    'bob.johnson@example.com',   '$2a$11$Qu3w5RtzSmKK2CaRyLn7o.1YoVdHv.6EbSFNm0LvRlANyiJZe/Vry', '+1-555-100-2003', 'CUSTOMER', NOW(), NOW(), NULL),
(5, 'Alice Williams', 'alice.w@example.com',       '$2a$11$Qu3w5RtzSmKK2CaRyLn7o.1YoVdHv.6EbSFNm0LvRlANyiJZe/Vry', '+1-555-100-2004', 'CUSTOMER', NOW(), NOW(), NULL);

-- =============================================================================
-- 2. ADDRESSES
--    Schema cols: id, user_id, address_line1, address_line2, city, state,
--                 postal_code, country, is_default, created_at
-- =============================================================================
INSERT INTO addresses
    (id, user_id, address_line1, address_line2, city, state, postal_code, country, is_default, created_at)
VALUES
(1, 2, '123 Main Street',      'Apt 4B',    'New York',     'NY', '10001',    'USA', 1, NOW()),
(2, 2, '456 Broadway',          NULL,        'New York',     'NY', '10013',    'USA', 0, NOW()),
(3, 3, '789 Sunset Boulevard',  'Suite 100', 'Los Angeles',  'CA', '90028',    'USA', 1, NOW()),
(4, 4, '10 Downing Street',     NULL,        'London',       NULL, 'SW1A 2AA', 'UK',  1, NOW()),
(5, 5, '1 Infinite Loop',       NULL,        'Cupertino',    'CA', '95014',    'USA', 1, NOW()),
(6, 1, '100 Admin Plaza',       'Floor 5',   'San Francisco','CA', '94105',    'USA', 1, NOW());

-- =============================================================================
-- 3. CATEGORIES
--    Schema cols: id, name, slug, parent_id, created_at
-- =============================================================================
INSERT INTO categories
    (id, name, slug, parent_id, created_at)
VALUES
-- Top-level
(1,  'Electronics',       'electronics',       NULL, NOW()),
(2,  'Clothing',          'clothing',          NULL, NOW()),
(3,  'Home & Garden',     'home-garden',       NULL, NOW()),
(4,  'Sports & Outdoors', 'sports-outdoors',   NULL, NOW()),
(5,  'Books',             'books',             NULL, NOW()),
-- Electronics → children
(6,  'Smartphones',       'smartphones',       1,    NOW()),
(7,  'Laptops',           'laptops',           1,    NOW()),
(8,  'Audio',             'audio',             1,    NOW()),
(9,  'Cameras',           'cameras',           1,    NOW()),
-- Clothing → children
(10, 'Men''s Clothing',   'mens-clothing',     2,    NOW()),
(11, 'Women''s Clothing', 'womens-clothing',   2,    NOW()),
(12, 'Footwear',          'footwear',          2,    NOW()),
-- Home → children
(13, 'Furniture',         'furniture',         3,    NOW()),
(14, 'Kitchen',           'kitchen',           3,    NOW()),
-- Sports → children
(15, 'Running',           'running',           4,    NOW()),
(16, 'Fitness Equipment', 'fitness-equipment', 4,    NOW());

-- =============================================================================
-- 4. PRODUCTS
--    Schema cols: id, category_id, name, slug, description, brand,
--                 base_price, is_active, created_at, updated_at, deleted_at
-- =============================================================================
INSERT INTO products
    (id, category_id, name, slug, description, brand, base_price, is_active, created_at, updated_at, deleted_at)
VALUES
-- Smartphones (cat 6)
(1,  6,  'iPhone 15 Pro',           'iphone-15-pro',
     'The most powerful iPhone ever with A17 Pro chip, titanium design, and a 48MP main camera.',
     'Apple',       999.00, 1, NOW(), NOW(), NULL),
(2,  6,  'Samsung Galaxy S24 Ultra', 'samsung-galaxy-s24-ultra',
     'Galaxy AI is here. Embedded S Pen, 200MP camera, and Snapdragon 8 Gen 3.',
     'Samsung',    1299.99, 1, NOW(), NOW(), NULL),
(3,  6,  'Google Pixel 8 Pro',       'google-pixel-8-pro',
     'The smartest Pixel yet with Google AI built in. Best-in-class photo and video.',
     'Google',      899.00, 1, NOW(), NOW(), NULL),
-- Laptops (cat 7)
(4,  7,  'MacBook Pro 14',           'macbook-pro-14',
     'Built for Apple silicon. M3 Pro chip, Liquid Retina XDR display, up to 22 hours battery.',
     'Apple',      1999.00, 1, NOW(), NOW(), NULL),
(5,  7,  'Dell XPS 15',              'dell-xps-15',
     'Premium laptop with Intel Core i9, OLED display, NVIDIA GeForce RTX 4060.',
     'Dell',       1799.99, 1, NOW(), NOW(), NULL),
(6,  7,  'ThinkPad X1 Carbon',       'thinkpad-x1-carbon',
     'Business ultrabook. Intel vPro, military-grade durability, exceptional keyboard.',
     'Lenovo',     1499.00, 1, NOW(), NOW(), NULL),
-- Audio (cat 8)
(7,  8,  'Sony WH-1000XM5',          'sony-wh-1000xm5',
     'Industry-leading noise cancellation. 30-hour battery, multipoint connection.',
     'Sony',        349.99, 1, NOW(), NOW(), NULL),
(8,  8,  'AirPods Pro 2nd Gen',      'airpods-pro-2nd-gen',
     'Active Noise Cancellation, Transparency mode, Personalised Spatial Audio.',
     'Apple',       249.00, 1, NOW(), NOW(), NULL),
-- Men Clothing (cat 10)
(9,  10, 'Classic Oxford Shirt',     'classic-oxford-shirt',
     'Premium 100% cotton Oxford shirt. Regular fit, button-down collar.',
     'Ralph Lauren',  89.50, 1, NOW(), NOW(), NULL),
(10, 10, 'Slim-Fit Chino Pants',     'slim-fit-chino-pants',
     'Stretch cotton chinos with a modern slim fit. Machine washable.',
     'Levi''s',       59.99, 1, NOW(), NOW(), NULL),
-- Women Clothing (cat 11)
(11, 11, 'Floral Wrap Dress',        'floral-wrap-dress',
     'Lightweight crepe wrap dress with V-neck and flutter sleeves.',
     'Zara',          69.99, 1, NOW(), NOW(), NULL),
-- Footwear (cat 12)
(12, 12, 'Air Max 270',              'air-max-270',
     'Nike first lifestyle Air Max shoe. Max Air unit, breathable upper.',
     'Nike',         150.00, 1, NOW(), NOW(), NULL),
(13, 12, 'Stan Smith Sneakers',      'stan-smith-sneakers',
     'The iconic low-top sneaker. Perforated 3-Stripes, leather upper.',
     'Adidas',        90.00, 1, NOW(), NOW(), NULL),
-- Furniture (cat 13)
(14, 13, 'Ergonomic Office Chair',   'ergonomic-office-chair',
     'Lumbar support, adjustable armrests, mesh back for all-day comfort.',
     'Herman Miller', 899.00, 1, NOW(), NOW(), NULL),
-- Kitchen (cat 14)
(15, 14, 'Instant Pot Duo 7-in-1',  'instant-pot-duo-7-in-1',
     'Pressure cooker, slow cooker, rice cooker, steamer, saute, yogurt maker, warmer.',
     'Instant Pot',   79.99, 1, NOW(), NOW(), NULL),
-- Running (cat 15)
(16, 15, 'UltraBoost 23',            'ultraboost-23',
     'Adidas BOOST midsole for incredible energy return. Continental rubber outsole.',
     'Adidas',       190.00, 1, NOW(), NOW(), NULL),
-- Soft-deleted product (tests the deleted_at filter)
(17, 6,  'OnePlus 12 Discontinued',  'oneplus-12-discontinued',
     'This product has been removed from the catalogue.',
     'OnePlus',      799.00, 0, NOW(), NOW(), NOW());

-- =============================================================================
-- 5. PRODUCT IMAGES
--    Schema cols: id, product_id, image_url, is_primary, sort_order, created_at
-- =============================================================================
INSERT INTO product_images
    (id, product_id, image_url, is_primary, sort_order, created_at)
VALUES
(1,  1,  'https://ecommercestorage.blob.core.windows.net/media/products/1/iphone15pro-black.jpg',    1, 1, NOW()),
(2,  1,  'https://ecommercestorage.blob.core.windows.net/media/products/1/iphone15pro-silver.jpg',   0, 2, NOW()),
(3,  1,  'https://ecommercestorage.blob.core.windows.net/media/products/1/iphone15pro-detail.jpg',   0, 3, NOW()),
(4,  2,  'https://ecommercestorage.blob.core.windows.net/media/products/2/s24ultra-phantom.jpg',     1, 1, NOW()),
(5,  2,  'https://ecommercestorage.blob.core.windows.net/media/products/2/s24ultra-cream.jpg',       0, 2, NOW()),
(6,  3,  'https://ecommercestorage.blob.core.windows.net/media/products/3/pixel8pro-obsidian.jpg',   1, 1, NOW()),
(7,  4,  'https://ecommercestorage.blob.core.windows.net/media/products/4/macbookpro14-silver.jpg',  1, 1, NOW()),
(8,  4,  'https://ecommercestorage.blob.core.windows.net/media/products/4/macbookpro14-space.jpg',   0, 2, NOW()),
(9,  5,  'https://ecommercestorage.blob.core.windows.net/media/products/5/xps15-platinum.jpg',       1, 1, NOW()),
(10, 6,  'https://ecommercestorage.blob.core.windows.net/media/products/6/x1carbon-black.jpg',       1, 1, NOW()),
(11, 7,  'https://ecommercestorage.blob.core.windows.net/media/products/7/wh1000xm5-black.jpg',      1, 1, NOW()),
(12, 7,  'https://ecommercestorage.blob.core.windows.net/media/products/7/wh1000xm5-silver.jpg',     0, 2, NOW()),
(13, 8,  'https://ecommercestorage.blob.core.windows.net/media/products/8/airpodspro2-white.jpg',    1, 1, NOW()),
(14, 9,  'https://ecommercestorage.blob.core.windows.net/media/products/9/oxford-white.jpg',         1, 1, NOW()),
(15, 9,  'https://ecommercestorage.blob.core.windows.net/media/products/9/oxford-blue.jpg',          0, 2, NOW()),
(16, 9,  'https://ecommercestorage.blob.core.windows.net/media/products/9/oxford-pink.jpg',          0, 3, NOW()),
(17, 10, 'https://ecommercestorage.blob.core.windows.net/media/products/10/chino-khaki.jpg',         1, 1, NOW()),
(18, 10, 'https://ecommercestorage.blob.core.windows.net/media/products/10/chino-navy.jpg',          0, 2, NOW()),
(19, 11, 'https://ecommercestorage.blob.core.windows.net/media/products/11/wrap-dress-floral.jpg',   1, 1, NOW()),
(20, 12, 'https://ecommercestorage.blob.core.windows.net/media/products/12/airmax270-white.jpg',     1, 1, NOW()),
(21, 12, 'https://ecommercestorage.blob.core.windows.net/media/products/12/airmax270-black.jpg',     0, 2, NOW()),
(22, 13, 'https://ecommercestorage.blob.core.windows.net/media/products/13/stansmith-white.jpg',     1, 1, NOW()),
(23, 14, 'https://ecommercestorage.blob.core.windows.net/media/products/14/officechairl-black.jpg',  1, 1, NOW()),
(24, 15, 'https://ecommercestorage.blob.core.windows.net/media/products/15/instantpot-duo.jpg',      1, 1, NOW()),
(25, 16, 'https://ecommercestorage.blob.core.windows.net/media/products/16/ultraboost23-white.jpg',  1, 1, NOW()),
(26, 16, 'https://ecommercestorage.blob.core.windows.net/media/products/16/ultraboost23-black.jpg',  0, 2, NOW());

-- =============================================================================
-- 6. PRODUCT VARIANTS
--    Schema cols: id, product_id, sku, color, size, price, created_at
-- =============================================================================
INSERT INTO product_variants
    (id, product_id, sku, color, size, price, created_at)
VALUES
-- iPhone 15 Pro (1)
(1,  1, 'APPL-IP15P-128-BLK', 'Black Titanium',   '128GB',   999.00, NOW()),
(2,  1, 'APPL-IP15P-256-BLK', 'Black Titanium',   '256GB',  1099.00, NOW()),
(3,  1, 'APPL-IP15P-256-WHT', 'White Titanium',   '256GB',  1099.00, NOW()),
(4,  1, 'APPL-IP15P-512-NAT', 'Natural Titanium', '512GB',  1299.00, NOW()),
-- Samsung Galaxy S24 Ultra (2)
(5,  2, 'SAMS-S24U-256-PHB',  'Phantom Black',    '256GB',  1299.99, NOW()),
(6,  2, 'SAMS-S24U-512-PHB',  'Phantom Black',    '512GB',  1419.99, NOW()),
(7,  2, 'SAMS-S24U-256-CRM',  'Cream',            '256GB',  1299.99, NOW()),
-- Pixel 8 Pro (3)
(8,  3, 'GOOG-PX8P-128-OBS',  'Obsidian',         '128GB',   899.00, NOW()),
(9,  3, 'GOOG-PX8P-256-BAY',  'Bay',              '256GB',   999.00, NOW()),
-- MacBook Pro 14 (4)
(10, 4, 'APPL-MBP14-M3P-SLV', 'Silver',           'M3 Pro', 1999.00, NOW()),
(11, 4, 'APPL-MBP14-M3M-SLV', 'Silver',           'M3 Max', 2499.00, NOW()),
(12, 4, 'APPL-MBP14-M3P-SPC', 'Space Black',      'M3 Pro', 1999.00, NOW()),
-- Dell XPS 15 (5)
(13, 5, 'DELL-XPS15-I9-PLT',  'Platinum Silver',  'i9 32GB',1799.99, NOW()),
(14, 5, 'DELL-XPS15-I7-PLT',  'Platinum Silver',  'i7 16GB',1499.99, NOW()),
-- ThinkPad X1 Carbon (6)
(15, 6, 'LNVO-X1C-I7-BLK',   'Deep Black',       'i7 16GB',1499.00, NOW()),
(16, 6, 'LNVO-X1C-I5-BLK',   'Deep Black',       'i5 16GB',1299.00, NOW()),
-- Sony WH-1000XM5 (7)
(17, 7, 'SONY-WH1KM5-BLK',   'Black',            NULL,      349.99, NOW()),
(18, 7, 'SONY-WH1KM5-SLV',   'Silver',           NULL,      349.99, NOW()),
-- AirPods Pro 2 (8)
(19, 8, 'APPL-APP2-WHT',      'White',            NULL,      249.00, NOW()),
-- Oxford Shirt (9)
(20, 9, 'RL-OXF-WHT-S',      'White', 'S',  89.50, NOW()),
(21, 9, 'RL-OXF-WHT-M',      'White', 'M',  89.50, NOW()),
(22, 9, 'RL-OXF-WHT-L',      'White', 'L',  89.50, NOW()),
(23, 9, 'RL-OXF-BLU-M',      'Blue',  'M',  89.50, NOW()),
(24, 9, 'RL-OXF-PNK-M',      'Pink',  'M',  89.50, NOW()),
-- Chino Pants (10)
(25,10, 'LV-CHN-KHK-30',     'Khaki', '30x30', 59.99, NOW()),
(26,10, 'LV-CHN-KHK-32',     'Khaki', '32x30', 59.99, NOW()),
(27,10, 'LV-CHN-NVY-32',     'Navy',  '32x30', 59.99, NOW()),
(28,10, 'LV-CHN-NVY-34',     'Navy',  '34x30', 59.99, NOW()),
-- Floral Wrap Dress (11)
(29,11, 'ZR-WD-FLR-XS',      'Floral Print', 'XS', 69.99, NOW()),
(30,11, 'ZR-WD-FLR-S',       'Floral Print', 'S',  69.99, NOW()),
(31,11, 'ZR-WD-FLR-M',       'Floral Print', 'M',  69.99, NOW()),
(32,11, 'ZR-WD-FLR-L',       'Floral Print', 'L',  69.99, NOW()),
-- Air Max 270 (12)
(33,12, 'NK-AM270-WHT-8',    'White', 'US 8',  150.00, NOW()),
(34,12, 'NK-AM270-WHT-9',    'White', 'US 9',  150.00, NOW()),
(35,12, 'NK-AM270-WHT-10',   'White', 'US 10', 150.00, NOW()),
(36,12, 'NK-AM270-BLK-9',    'Black', 'US 9',  150.00, NOW()),
-- Stan Smith (13)
(37,13, 'AD-SS-WHT-8',       'White/Green', 'US 8',  90.00, NOW()),
(38,13, 'AD-SS-WHT-9',       'White/Green', 'US 9',  90.00, NOW()),
(39,13, 'AD-SS-WHT-10',      'White/Green', 'US 10', 90.00, NOW()),
-- Office Chair (14)
(40,14, 'HM-AERN-BLK',       'Black',    NULL, 899.00, NOW()),
(41,14, 'HM-AERN-GRY',       'Graphite', NULL, 949.00, NOW()),
-- Instant Pot (15)
(42,15, 'IP-DUO-6QT',        'Stainless', '6 Qt',  79.99, NOW()),
(43,15, 'IP-DUO-8QT',        'Stainless', '8 Qt',  99.99, NOW()),
-- UltraBoost 23 (16)
(44,16, 'AD-UB23-WHT-8',     'Cloud White', 'US 8',  190.00, NOW()),
(45,16, 'AD-UB23-WHT-9',     'Cloud White', 'US 9',  190.00, NOW()),
(46,16, 'AD-UB23-BLK-9',     'Core Black',  'US 9',  190.00, NOW()),
(47,16, 'AD-UB23-BLK-10',    'Core Black',  'US 10', 190.00, NOW());

-- =============================================================================
-- 7. INVENTORY
--    Schema cols: id, variant_id, stock_quantity, reserved_quantity, updated_at
--    NOTE: no created_at in schema — intentionally omitted
-- =============================================================================
INSERT INTO inventory
    (id, variant_id, stock_quantity, reserved_quantity, updated_at)
VALUES
( 1,  1, 45, 2, NOW()),
( 2,  2, 30, 0, NOW()),
( 3,  3, 25, 0, NOW()),
( 4,  4, 10, 1, NOW()),
( 5,  5, 60, 3, NOW()),
( 6,  6, 20, 0, NOW()),
( 7,  7, 35, 0, NOW()),
( 8,  8, 40, 0, NOW()),
( 9,  9, 15, 0, NOW()),
(10, 10, 18, 1, NOW()),
(11, 11,  8, 0, NOW()),
(12, 12, 12, 0, NOW()),
(13, 13, 22, 0, NOW()),
(14, 14, 30, 0, NOW()),
(15, 15, 25, 0, NOW()),
(16, 16, 20, 0, NOW()),
(17, 17,100, 5, NOW()),
(18, 18, 80, 0, NOW()),
(19, 19,150, 8, NOW()),
(20, 20, 50, 0, NOW()),
(21, 21, 75, 2, NOW()),
(22, 22, 60, 0, NOW()),
(23, 23, 45, 0, NOW()),
(24, 24, 30, 0, NOW()),
(25, 25, 40, 0, NOW()),
(26, 26, 55, 0, NOW()),
(27, 27, 35, 0, NOW()),
(28, 28, 25, 0, NOW()),
(29, 29, 20, 0, NOW()),
(30, 30, 35, 0, NOW()),
(31, 31, 40, 2, NOW()),
(32, 32, 15, 0, NOW()),
(33, 33, 30, 0, NOW()),
(34, 34, 45, 0, NOW()),
(35, 35, 38, 0, NOW()),
(36, 36, 22, 0, NOW()),
(37, 37, 60, 0, NOW()),
(38, 38, 55, 0, NOW()),
(39, 39, 48, 0, NOW()),
(40, 40, 12, 0, NOW()),
(41, 41,  5, 0, NOW()),
(42, 42,200, 0, NOW()),
(43, 43,150, 0, NOW()),
(44, 44, 50, 0, NOW()),
(45, 45, 45, 0, NOW()),
(46, 46, 40, 0, NOW()),
(47, 47, 35, 0, NOW());

-- =============================================================================
-- 8. COUPONS
--    Schema cols: id, code, discount_type, discount_value, min_order_amount,
--                 max_discount, expiry_date, usage_limit, usage_count,
--                 is_active, created_at
-- =============================================================================
INSERT INTO coupons
    (id, code, discount_type, discount_value, min_order_amount, max_discount, expiry_date, usage_limit, usage_count, is_active, created_at)
VALUES
(1, 'WELCOME10', 'PERCENTAGE', 10.00,   0.00,  50.00, DATE_ADD(NOW(), INTERVAL 1 YEAR),  1000, 0, 1, NOW()),
(2, 'SAVE50',    'FLAT',       50.00, 200.00,  50.00, DATE_ADD(NOW(), INTERVAL 6 MONTH),  500, 0, 1, NOW()),
(3, 'TECH20',    'PERCENTAGE', 20.00, 500.00, 200.00, DATE_ADD(NOW(), INTERVAL 3 MONTH),  200, 0, 1, NOW()),
(4, 'FREESHIP',  'FLAT',       15.00,  50.00,  15.00, DATE_ADD(NOW(), INTERVAL 1 YEAR),  9999, 0, 1, NOW()),
(5, 'SUMMER25',  'PERCENTAGE', 25.00, 100.00, 100.00, DATE_ADD(NOW(), INTERVAL 2 MONTH),  300, 0, 1, NOW()),
(6, 'EXPIRED',   'FLAT',       30.00,   0.00,  30.00, DATE_SUB(NOW(), INTERVAL 1 DAY),    100, 0, 1, NOW()),
(7, 'VIP30',     'PERCENTAGE', 30.00, 300.00, 150.00, DATE_ADD(NOW(), INTERVAL 1 YEAR),   100, 0, 1, NOW());

-- =============================================================================
-- 9. CARTS
--    Schema cols: id, user_id, created_at, updated_at
--    (uq_carts_user: one cart per user — admin has no cart by design)
-- =============================================================================
INSERT INTO carts
    (id, user_id, created_at, updated_at)
VALUES
(1, 2, NOW(), NOW()),
(2, 3, NOW(), NOW()),
(3, 4, NOW(), NOW());

-- =============================================================================
-- 10. CART ITEMS
--     Schema cols: id, cart_id, variant_id, quantity, added_at
-- =============================================================================
INSERT INTO cart_items
    (id, cart_id, variant_id, quantity, added_at)
VALUES
-- John Doe cart (1)
(1, 1, 1,  1, NOW()),   -- iPhone 15 Pro 128GB Black
(2, 1, 10, 1, NOW()),   -- MacBook Pro M3 Pro Silver
-- Jane Smith cart (2)
(3, 2, 19, 2, NOW()),   -- AirPods Pro x2
(4, 2, 30, 1, NOW()),   -- Floral Wrap Dress S
-- Bob Johnson cart (3)
(5, 3, 17, 1, NOW());   -- Sony WH-1000XM5 Black

-- =============================================================================
-- 11. ORDERS
--     Schema cols: id, user_id, coupon_id, status, subtotal_amount,
--                  discount_amount, total_amount, shipping_address_id,
--                  notes, created_at
-- =============================================================================
INSERT INTO orders
    (id, user_id, coupon_id, status, subtotal_amount, discount_amount, total_amount, shipping_address_id, notes, created_at)
VALUES
(1, 2, 1,    'DELIVERED', 1099.00, 109.90,  989.10, 1, NULL,                             DATE_SUB(NOW(), INTERVAL 30 DAY)),
(2, 3, 2,    'PAID',       349.99,  50.00,  299.99, 3, 'Please leave at door.',          DATE_SUB(NOW(), INTERVAL 7  DAY)),
(3, 4, NULL, 'PENDING',    150.00,   0.00,  150.00, 4, NULL,                             DATE_SUB(NOW(), INTERVAL 1  DAY)),
(4, 5, 4,    'SHIPPED',    190.00,  15.00,  175.00, 5, NULL,                             DATE_SUB(NOW(), INTERVAL 10 DAY)),
(5, 2, NULL, 'CANCELLED',   89.50,   0.00,   89.50, 1, NULL,                             DATE_SUB(NOW(), INTERVAL 60 DAY)),
(6, 1, 3,    'PENDING',   1999.00, 200.00, 1799.00, 6, 'Test order for API validation.', NOW());

-- =============================================================================
-- 12. ORDER ITEMS
--     Schema cols: id, order_id, variant_id, sku, product_name, color, size,
--                  unit_price, quantity, line_total
-- =============================================================================
INSERT INTO order_items
    (id, order_id, variant_id, sku, product_name, color, size, unit_price, quantity, line_total)
VALUES
(1, 1, 2,  'APPL-IP15P-256-BLK', 'iPhone 15 Pro',      'Black Titanium', '256GB',   1099.00, 1, 1099.00),
(2, 2, 17, 'SONY-WH1KM5-BLK',   'Sony WH-1000XM5',    'Black',          NULL,        349.99, 1,  349.99),
(3, 3, 33, 'NK-AM270-WHT-8',    'Air Max 270',         'White',          'US 8',      150.00, 1,  150.00),
(4, 4, 44, 'AD-UB23-WHT-8',     'UltraBoost 23',       'Cloud White',    'US 8',      190.00, 1,  190.00),
(5, 5, 21, 'RL-OXF-WHT-M',      'Classic Oxford Shirt','White',          'M',          89.50, 1,   89.50),
(6, 6, 10, 'APPL-MBP14-M3P-SLV','MacBook Pro 14',      'Silver',         'M3 Pro',   1999.00, 1, 1999.00);

-- =============================================================================
-- 13. PAYMENTS
--     Schema cols: id, order_id, payment_method, transaction_id, amount,
--                  status, failure_reason, paid_at, created_at
--     FIX: created_at was missing in previous seed version
-- =============================================================================
INSERT INTO payments
    (id, order_id, payment_method, transaction_id, amount, status, failure_reason, paid_at, created_at)
VALUES
(1, 1, 'CREDIT_CARD', 'TXN-20240101-001',  989.10, 'COMPLETED', NULL,                               DATE_SUB(NOW(), INTERVAL 30 DAY), DATE_SUB(NOW(), INTERVAL 30 DAY)),
(2, 2, 'PAYPAL',      'TXN-20240201-002',  299.99, 'COMPLETED', NULL,                               DATE_SUB(NOW(), INTERVAL 7  DAY), DATE_SUB(NOW(), INTERVAL 7  DAY)),
(3, 3, 'CREDIT_CARD', NULL,                150.00, 'PENDING',   NULL,                               NULL,                             DATE_SUB(NOW(), INTERVAL 1  DAY)),
(4, 4, 'CREDIT_CARD', 'TXN-20240115-004',  175.00, 'COMPLETED', NULL,                               DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY)),
(5, 5, 'CREDIT_CARD', 'TXN-20231201-005',   89.50, 'REFUNDED',  NULL,                               DATE_SUB(NOW(), INTERVAL 59 DAY), DATE_SUB(NOW(), INTERVAL 59 DAY)),
(6, 6, 'CREDIT_CARD', 'TXN-FAIL-001',     1799.00, 'FAILED',    'Card declined: insufficient funds.',NULL,                            NOW()),
(7, 6, 'CREDIT_CARD', NULL,               1799.00, 'PENDING',   NULL,                               NULL,                             NOW());

-- =============================================================================
-- 14. REVIEWS
--     Schema cols: id, user_id, product_id, rating, comment, created_at
-- =============================================================================
INSERT INTO reviews
    (id, user_id, product_id, rating, comment, created_at)
VALUES
( 1, 2,  1, 5, 'Absolutely love this phone. The camera is insane and the titanium build feels premium.', DATE_SUB(NOW(), INTERVAL 20 DAY)),
( 2, 3,  1, 4, 'Great phone overall. Battery life could be better but the performance is top-notch.',    DATE_SUB(NOW(), INTERVAL 15 DAY)),
( 3, 4,  1, 5, 'Best iPhone I have ever owned. Worth every penny.',                                      DATE_SUB(NOW(), INTERVAL  5 DAY)),
( 4, 2,  4, 5, 'M3 Pro chip is a beast. Handles everything I throw at it with ease.',                   DATE_SUB(NOW(), INTERVAL 25 DAY)),
( 5, 5,  4, 4, 'Excellent machine. The display is stunning. Wish it had more ports though.',             DATE_SUB(NOW(), INTERVAL  8 DAY)),
( 6, 3,  7, 5, 'Noise cancellation is in a league of its own. Perfect for long flights.',               DATE_SUB(NOW(), INTERVAL  3 DAY)),
( 7, 4,  7, 4, 'Sound quality is amazing. Slightly heavy for long wearing sessions.',                   DATE_SUB(NOW(), INTERVAL 12 DAY)),
( 8, 2,  8, 4, 'Great ANC and sound quality. Seamless Apple ecosystem integration.',                    DATE_SUB(NOW(), INTERVAL  6 DAY)),
( 9, 3,  9, 5, 'Perfect fit and quality fabric. Looks sharp in the office.',                            DATE_SUB(NOW(), INTERVAL 18 DAY)),
(10, 4,  9, 3, 'Good quality but runs a bit large. Size down if between sizes.',                        DATE_SUB(NOW(), INTERVAL 10 DAY)),
(11, 5, 12, 5, 'Super comfortable for all-day wear. Got many compliments too!',                         DATE_SUB(NOW(), INTERVAL  4 DAY)),
(12, 2, 16, 5, 'Best running shoes I have ever owned. Energy return is incredible.',                    DATE_SUB(NOW(), INTERVAL  7 DAY)),
(13, 3, 16, 4, 'Great for long runs. True to size. Highly recommend.',                                  DATE_SUB(NOW(), INTERVAL  2 DAY));

-- =============================================================================
-- VERIFICATION
-- FIX: `rows` is a reserved word in MySQL 8.0 — use `row_count` instead
-- =============================================================================
SELECT 'users'           AS tbl, COUNT(*) AS row_count FROM users
UNION ALL SELECT 'addresses',        COUNT(*) FROM addresses
UNION ALL SELECT 'categories',       COUNT(*) FROM categories
UNION ALL SELECT 'products',         COUNT(*) FROM products
UNION ALL SELECT 'product_images',   COUNT(*) FROM product_images
UNION ALL SELECT 'product_variants', COUNT(*) FROM product_variants
UNION ALL SELECT 'inventory',        COUNT(*) FROM inventory
UNION ALL SELECT 'coupons',          COUNT(*) FROM coupons
UNION ALL SELECT 'carts',            COUNT(*) FROM carts
UNION ALL SELECT 'cart_items',       COUNT(*) FROM cart_items
UNION ALL SELECT 'orders',           COUNT(*) FROM orders
UNION ALL SELECT 'order_items',      COUNT(*) FROM order_items
UNION ALL SELECT 'payments',         COUNT(*) FROM payments
UNION ALL SELECT 'reviews',          COUNT(*) FROM reviews;