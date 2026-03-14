-- =============================================================================
--  ECOMMERCE DATABASE — MySQL 8.0 Production Schema
--  Character set : utf8mb4 / utf8mb4_unicode_ci
--  Engine        : InnoDB (all tables)
--  Features used : CHECK constraints, generated columns, window-friendly indexes,
--                  ON DELETE / ON UPDATE rules, performance indexes
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. DATABASE
-- -----------------------------------------------------------------------------
CREATE DATABASE IF NOT EXISTS ecommerce_db
    CHARACTER SET  utf8mb4
    COLLATE        utf8mb4_unicode_ci;

USE ecommerce_db;

-- Ensure foreign-key enforcement is on (default in MySQL 8, but explicit)
SET FOREIGN_KEY_CHECKS = 1;


-- =============================================================================
-- 1. USERS
-- =============================================================================
CREATE TABLE users (
    id            BIGINT          NOT NULL AUTO_INCREMENT,
    name          VARCHAR(150)    NOT NULL,
    email         VARCHAR(200)    NOT NULL,
    password_hash VARCHAR(255)    NOT NULL,
    phone         VARCHAR(20)         NULL,
    role          ENUM('ADMIN','CUSTOMER')
                                  NOT NULL DEFAULT 'CUSTOMER',
    created_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                           ON UPDATE CURRENT_TIMESTAMP,
    deleted_at    DATETIME            NULL,

    CONSTRAINT pk_users          PRIMARY KEY (id),
    CONSTRAINT uq_users_email    UNIQUE      (email),

    -- Soft-delete partial index: only index active records
    INDEX idx_users_email_active (email, deleted_at),
    INDEX idx_users_role         (role),
    INDEX idx_users_deleted_at   (deleted_at)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Registered users — soft delete via deleted_at';


-- =============================================================================
-- 2. ADDRESSES
-- =============================================================================
CREATE TABLE addresses (
    id            BIGINT          NOT NULL AUTO_INCREMENT,
    user_id       BIGINT          NOT NULL,
    address_line1 VARCHAR(255)    NOT NULL,
    address_line2 VARCHAR(255)        NULL,
    city          VARCHAR(100)        NULL,
    state         VARCHAR(100)        NULL,
    postal_code   VARCHAR(20)         NULL,
    country       VARCHAR(100)        NULL,
    is_default    TINYINT(1)      NOT NULL DEFAULT 0,
    created_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_addresses         PRIMARY KEY (id),
    CONSTRAINT fk_addresses_user    FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_addresses_default CHECK (is_default IN (0, 1)),

    INDEX idx_addresses_user    (user_id),
    INDEX idx_addresses_default (user_id, is_default)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Shipping / billing addresses per user';


-- =============================================================================
-- 3. CATEGORIES  (self-referencing tree)
-- =============================================================================
CREATE TABLE categories (
    id         BIGINT       NOT NULL AUTO_INCREMENT,
    name       VARCHAR(150) NOT NULL,
    slug       VARCHAR(150)     NULL,
    parent_id  BIGINT           NULL,
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_categories        PRIMARY KEY (id),
    CONSTRAINT uq_categories_slug   UNIQUE      (slug),
    CONSTRAINT fk_categories_parent FOREIGN KEY (parent_id)
        REFERENCES categories(id) ON DELETE SET NULL ON UPDATE CASCADE,

    INDEX idx_categories_parent (parent_id)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Product category tree — unlimited depth via parent_id';


-- =============================================================================
-- 4. PRODUCTS
-- =============================================================================
CREATE TABLE products (
    id          BIGINT           NOT NULL AUTO_INCREMENT,
    category_id BIGINT               NULL,
    name        VARCHAR(255)     NOT NULL,
    slug        VARCHAR(255)         NULL,
    description TEXT                 NULL,
    brand       VARCHAR(150)         NULL,
    base_price  DECIMAL(10,2)        NULL,
    is_active   TINYINT(1)       NOT NULL DEFAULT 1,
    created_at  DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          ON UPDATE CURRENT_TIMESTAMP,
    deleted_at  DATETIME             NULL,

    CONSTRAINT pk_products          PRIMARY KEY (id),
    CONSTRAINT uq_products_slug     UNIQUE      (slug),
    CONSTRAINT fk_products_category FOREIGN KEY (category_id)
        REFERENCES categories(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT chk_products_price   CHECK (base_price IS NULL OR base_price >= 0),
    CONSTRAINT chk_products_active  CHECK (is_active IN (0, 1)),

    INDEX idx_products_category   (category_id),
    INDEX idx_products_name       (name),
    INDEX idx_products_brand      (brand),
    INDEX idx_products_active     (is_active, deleted_at),
    INDEX idx_products_deleted_at (deleted_at),
    -- Composite for catalogue listing (active + category + price sort)
    INDEX idx_products_listing    (is_active, category_id, base_price)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Product catalogue — soft delete via deleted_at';


-- =============================================================================
-- 5. PRODUCT IMAGES
-- =============================================================================
CREATE TABLE product_images (
    id         BIGINT       NOT NULL AUTO_INCREMENT,
    product_id BIGINT       NOT NULL,
    image_url  VARCHAR(500) NOT NULL,
    is_primary TINYINT(1)   NOT NULL DEFAULT 0,
    sort_order SMALLINT     NOT NULL DEFAULT 0
                            COMMENT 'Display order within product gallery',
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_product_images         PRIMARY KEY (id),
    CONSTRAINT fk_product_images_product FOREIGN KEY (product_id)
        REFERENCES products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_product_images_primary CHECK (is_primary IN (0, 1)),

    INDEX idx_product_images_product (product_id),
    INDEX idx_product_images_primary (product_id, is_primary)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Azure Blob Storage URLs for product images';


-- =============================================================================
-- 6. PRODUCT VARIANTS  (SKU / colour / size)
-- =============================================================================
CREATE TABLE product_variants (
    id         BIGINT        NOT NULL AUTO_INCREMENT,
    product_id BIGINT        NOT NULL,
    sku        VARCHAR(100)  NOT NULL,
    color      VARCHAR(50)       NULL,
    size       VARCHAR(50)       NULL,
    price      DECIMAL(10,2)     NULL
               COMMENT 'Override price; falls back to products.base_price when NULL',
    created_at DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_product_variants         PRIMARY KEY (id),
    CONSTRAINT uq_product_variants_sku     UNIQUE      (sku),
    CONSTRAINT fk_product_variants_product FOREIGN KEY (product_id)
        REFERENCES products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_product_variants_price  CHECK (price IS NULL OR price >= 0),

    INDEX idx_product_variants_product (product_id),
    -- Composite for variant filtering by attribute
    INDEX idx_product_variants_attrs   (product_id, color, size)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='SKU-level product variants (size, colour, etc.)';


-- =============================================================================
-- 7. INVENTORY  (one row per variant)
-- =============================================================================
CREATE TABLE inventory (
    id                BIGINT   NOT NULL AUTO_INCREMENT,
    variant_id        BIGINT   NOT NULL,
    stock_quantity    INT      NOT NULL DEFAULT 0,
    reserved_quantity INT      NOT NULL DEFAULT 0,
    updated_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                               ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_inventory            PRIMARY KEY (id),
    CONSTRAINT uq_inventory_variant    UNIQUE      (variant_id),
    CONSTRAINT fk_inventory_variant    FOREIGN KEY (variant_id)
        REFERENCES product_variants(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT chk_inventory_stock     CHECK (stock_quantity    >= 0),
    CONSTRAINT chk_inventory_reserved  CHECK (reserved_quantity >= 0),
    CONSTRAINT chk_inventory_available CHECK (stock_quantity >= reserved_quantity),

    INDEX idx_inventory_variant    (variant_id),
    -- Low-stock monitoring queries
    INDEX idx_inventory_low_stock  (stock_quantity)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Real-time stock levels per variant';


-- =============================================================================
-- 8. COUPONS
--    (created before orders so the FK below can reference it)
-- =============================================================================
CREATE TABLE coupons (
    id               BIGINT          NOT NULL AUTO_INCREMENT,
    code             VARCHAR(100)    NOT NULL,
    discount_type    ENUM('PERCENTAGE','FLAT')
                                     NOT NULL,
    discount_value   DECIMAL(10,2)   NOT NULL,
    min_order_amount DECIMAL(10,2)       NULL,
    max_discount     DECIMAL(10,2)       NULL,
    expiry_date      DATETIME            NULL,
    usage_limit      INT                 NULL,
    usage_count      INT             NOT NULL DEFAULT 0
                     COMMENT 'Tracks total redemptions',
    is_active        TINYINT(1)      NOT NULL DEFAULT 1,
    created_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_coupons               PRIMARY KEY (id),
    CONSTRAINT uq_coupons_code          UNIQUE      (code),
    CONSTRAINT chk_coupons_value        CHECK (discount_value   >  0),
    CONSTRAINT chk_coupons_min_order    CHECK (min_order_amount IS NULL OR min_order_amount >= 0),
    CONSTRAINT chk_coupons_max_discount CHECK (max_discount     IS NULL OR max_discount     >= 0),
    CONSTRAINT chk_coupons_pct_range    CHECK (
        discount_type != 'PERCENTAGE' OR discount_value <= 100
    ),
    CONSTRAINT chk_coupons_usage_limit  CHECK (usage_limit  IS NULL OR usage_limit  > 0),
    CONSTRAINT chk_coupons_usage_count  CHECK (usage_count  >= 0),
    CONSTRAINT chk_coupons_active       CHECK (is_active IN (0, 1)),

    INDEX idx_coupons_code      (code),
    INDEX idx_coupons_active    (is_active, expiry_date),
    INDEX idx_coupons_expiry    (expiry_date)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Discount coupons — PERCENTAGE or FLAT off order total';


-- =============================================================================
-- 9. ORDERS
-- =============================================================================
CREATE TABLE orders (
    id                  BIGINT         NOT NULL AUTO_INCREMENT,
    user_id             BIGINT         NOT NULL,
    coupon_id           BIGINT             NULL,
    status              ENUM('PENDING','PAID','SHIPPED','DELIVERED','CANCELLED')
                                       NOT NULL DEFAULT 'PENDING',
    subtotal_amount     DECIMAL(12,2)  NOT NULL DEFAULT 0.00
                        COMMENT 'Pre-discount total',
    discount_amount     DECIMAL(12,2)  NOT NULL DEFAULT 0.00,
    total_amount        DECIMAL(12,2)  NOT NULL DEFAULT 0.00
                        COMMENT 'Final amount charged',
    shipping_address_id BIGINT             NULL,
    notes               TEXT               NULL,
    created_at          DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_orders              PRIMARY KEY (id),
    CONSTRAINT fk_orders_user         FOREIGN KEY (user_id)
        REFERENCES users(id) ON UPDATE CASCADE,
    CONSTRAINT fk_orders_coupon       FOREIGN KEY (coupon_id)
        REFERENCES coupons(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_orders_address      FOREIGN KEY (shipping_address_id)
        REFERENCES addresses(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT chk_orders_subtotal    CHECK (subtotal_amount  >= 0),
    CONSTRAINT chk_orders_discount    CHECK (discount_amount  >= 0),
    CONSTRAINT chk_orders_total       CHECK (total_amount     >= 0),

    INDEX idx_orders_user       (user_id),
    INDEX idx_orders_status     (status),
    INDEX idx_orders_coupon     (coupon_id),
    INDEX idx_orders_created_at (created_at),
    -- Composite for user order history page (most recent first)
    INDEX idx_orders_user_date  (user_id, created_at DESC)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Customer orders';


-- =============================================================================
-- 10. ORDER ITEMS  (snapshot at purchase time)
-- =============================================================================
CREATE TABLE order_items (
    id           BIGINT        NOT NULL AUTO_INCREMENT,
    order_id     BIGINT        NOT NULL,
    variant_id   BIGINT            NULL
                 COMMENT 'NULL-safe: kept even if variant is later removed',
    sku          VARCHAR(100)      NULL
                 COMMENT 'Snapshot of variant SKU at purchase time',
    product_name VARCHAR(255)      NULL
                 COMMENT 'Snapshot of product name at purchase time',
    color        VARCHAR(50)       NULL
                 COMMENT 'Snapshot of colour at purchase time',
    size         VARCHAR(50)       NULL
                 COMMENT 'Snapshot of size at purchase time',
    unit_price   DECIMAL(10,2) NOT NULL
                 COMMENT 'Actual price charged per unit',
    quantity     INT           NOT NULL,
    line_total   DECIMAL(12,2) NOT NULL
                 COMMENT 'unit_price × quantity',

    CONSTRAINT pk_order_items        PRIMARY KEY (id),
    CONSTRAINT fk_order_items_order  FOREIGN KEY (order_id)
        REFERENCES orders(id) ON DELETE CASCADE ON UPDATE CASCADE,
    -- No hard FK on variant_id — intentional snapshot pattern
    CONSTRAINT chk_order_items_qty   CHECK (quantity   >  0),
    CONSTRAINT chk_order_items_price CHECK (unit_price >= 0),
    CONSTRAINT chk_order_items_total CHECK (line_total >= 0),

    INDEX idx_order_items_order   (order_id),
    INDEX idx_order_items_variant (variant_id),
    INDEX idx_order_items_sku     (sku)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Immutable line-item snapshot of each order';


-- =============================================================================
-- 11. PAYMENTS
-- =============================================================================
CREATE TABLE payments (
    id             BIGINT         NOT NULL AUTO_INCREMENT,
    order_id       BIGINT         NOT NULL,
    payment_method VARCHAR(100)       NULL,
    transaction_id VARCHAR(255)       NULL,
    amount         DECIMAL(12,2)  NOT NULL,
    status         ENUM('PENDING','COMPLETED','FAILED','REFUNDED')
                                  NOT NULL DEFAULT 'PENDING',
    failure_reason VARCHAR(500)       NULL
                   COMMENT 'Gateway error message on FAILED status',
    paid_at        DATETIME           NULL,
    created_at     DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_payments          PRIMARY KEY (id),
    CONSTRAINT fk_payments_order    FOREIGN KEY (order_id)
        REFERENCES orders(id) ON UPDATE CASCADE,
    CONSTRAINT uq_payments_txn      UNIQUE (transaction_id),
    CONSTRAINT chk_payments_amount  CHECK (amount >= 0),

    INDEX idx_payments_order        (order_id),
    INDEX idx_payments_status       (status),
    INDEX idx_payments_transaction  (transaction_id),
    INDEX idx_payments_paid_at      (paid_at)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Payment records — one order may have multiple attempts';


-- =============================================================================
-- 12. CARTS
-- =============================================================================
CREATE TABLE carts (
    id         BIGINT   NOT NULL AUTO_INCREMENT,
    user_id    BIGINT   NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_carts       PRIMARY KEY (id),
    CONSTRAINT uq_carts_user  UNIQUE      (user_id)
                              COMMENT 'One active cart per user',
    CONSTRAINT fk_carts_user  FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,

    INDEX idx_carts_user (user_id)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Active shopping cart (optionally mirrored in Redis)';


-- =============================================================================
-- 13. CART ITEMS
-- =============================================================================
CREATE TABLE cart_items (
    id         BIGINT   NOT NULL AUTO_INCREMENT,
    cart_id    BIGINT   NOT NULL,
    variant_id BIGINT   NOT NULL,
    quantity   INT      NOT NULL,
    added_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_cart_items          PRIMARY KEY (id),
    CONSTRAINT uq_cart_items_variant  UNIQUE      (cart_id, variant_id),
    CONSTRAINT fk_cart_items_cart     FOREIGN KEY (cart_id)
        REFERENCES carts(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_cart_items_variant  FOREIGN KEY (variant_id)
        REFERENCES product_variants(id) ON UPDATE CASCADE,
    CONSTRAINT chk_cart_items_qty     CHECK (quantity > 0),

    INDEX idx_cart_items_cart    (cart_id),
    INDEX idx_cart_items_variant (variant_id)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Items in a user shopping cart';


-- =============================================================================
-- 14. REVIEWS
-- =============================================================================
CREATE TABLE reviews (
    id         BIGINT   NOT NULL AUTO_INCREMENT,
    user_id    BIGINT   NOT NULL,
    product_id BIGINT   NOT NULL,
    rating     TINYINT  NOT NULL,
    comment    TEXT         NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_reviews           PRIMARY KEY (id),
    CONSTRAINT uq_reviews_user_prod UNIQUE      (user_id, product_id)
                                    COMMENT 'One review per user per product',
    CONSTRAINT fk_reviews_user      FOREIGN KEY (user_id)
        REFERENCES users(id) ON UPDATE CASCADE,
    CONSTRAINT fk_reviews_product   FOREIGN KEY (product_id)
        REFERENCES products(id) ON UPDATE CASCADE,
    CONSTRAINT chk_reviews_rating   CHECK (rating BETWEEN 1 AND 5),

    INDEX idx_reviews_product    (product_id),
    INDEX idx_reviews_user       (user_id),
    INDEX idx_reviews_rating     (product_id, rating)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='User product reviews — one per user/product pair';


-- =============================================================================
-- VIEWS  (optional helpers — non-materialised)
-- =============================================================================

-- Available stock per variant (stock minus reserved)
CREATE OR REPLACE VIEW v_inventory_available AS
SELECT
    i.id,
    i.variant_id,
    pv.sku,
    pv.product_id,
    i.stock_quantity,
    i.reserved_quantity,
    (i.stock_quantity - i.reserved_quantity) AS available_quantity,
    i.updated_at
FROM inventory     i
JOIN product_variants pv ON pv.id = i.variant_id;

-- Active product catalogue with primary image and category
CREATE OR REPLACE VIEW v_product_catalogue AS
SELECT
    p.id,
    p.name,
    p.slug,
    p.brand,
    p.base_price,
    p.is_active,
    c.id         AS category_id,
    c.name       AS category_name,
    c.slug       AS category_slug,
    (SELECT pi.image_url
     FROM   product_images pi
     WHERE  pi.product_id = p.id
       AND  pi.is_primary  = 1
     LIMIT 1)   AS primary_image_url,
    p.created_at
FROM products  p
LEFT JOIN categories c ON c.id = p.category_id
WHERE p.deleted_at IS NULL
  AND p.is_active   = 1;

-- Order summary with payment status
CREATE OR REPLACE VIEW v_order_summary AS
SELECT
    o.id            AS order_id,
    o.user_id,
    u.name          AS user_name,
    u.email         AS user_email,
    o.status        AS order_status,
    o.subtotal_amount,
    o.discount_amount,
    o.total_amount,
    o.created_at,
    cp.code         AS coupon_code,
    (SELECT MAX(py.status)
     FROM   payments py
     WHERE  py.order_id = o.id
       AND  py.status   = 'COMPLETED') AS payment_status
FROM orders  o
JOIN users   u  ON u.id  = o.user_id
LEFT JOIN coupons cp ON cp.id = o.coupon_id;


-- =============================================================================
-- STORED PROCEDURES
-- =============================================================================

DELIMITER $$

-- Reserve stock when an order is placed
CREATE PROCEDURE sp_reserve_stock(
    IN  p_variant_id BIGINT,
    IN  p_quantity   INT,
    OUT p_success    TINYINT
)
BEGIN
    DECLARE v_available INT DEFAULT 0;

    START TRANSACTION;

    SELECT (stock_quantity - reserved_quantity)
    INTO   v_available
    FROM   inventory
    WHERE  variant_id = p_variant_id
    FOR UPDATE;

    IF v_available >= p_quantity THEN
        UPDATE inventory
        SET    reserved_quantity = reserved_quantity + p_quantity,
               updated_at        = NOW()
        WHERE  variant_id = p_variant_id;

        SET p_success = 1;
        COMMIT;
    ELSE
        SET p_success = 0;
        ROLLBACK;
    END IF;
END$$

-- Deduct stock and release reservation on successful payment
CREATE PROCEDURE sp_deduct_stock(
    IN p_variant_id BIGINT,
    IN p_quantity   INT
)
BEGIN
    UPDATE inventory
    SET    stock_quantity    = stock_quantity    - p_quantity,
           reserved_quantity = reserved_quantity - p_quantity,
           updated_at        = NOW()
    WHERE  variant_id = p_variant_id;
END$$

-- Release reservation on cancelled/failed order (no stock deduction)
CREATE PROCEDURE sp_release_reservation(
    IN p_variant_id BIGINT,
    IN p_quantity   INT
)
BEGIN
    UPDATE inventory
    SET    reserved_quantity = GREATEST(0, reserved_quantity - p_quantity),
           updated_at        = NOW()
    WHERE  variant_id = p_variant_id;
END$$

DELIMITER ;


-- =============================================================================
-- PERFORMANCE INDEXES  (supplemental)
-- =============================================================================

-- Products
CREATE INDEX idx_products_slug       ON products         (slug);
CREATE INDEX idx_products_base_price ON products         (base_price);

-- Variants
CREATE INDEX idx_variants_sku        ON product_variants (sku);
CREATE INDEX idx_variants_product    ON product_variants (product_id);

-- Inventory

-- Orders
CREATE INDEX idx_orders_status_user  ON orders           (status, user_id);

-- Payments
CREATE INDEX idx_payments_order_stat ON payments         (order_id, status);

-- Reviews
CREATE INDEX idx_reviews_prod_rating ON reviews          (product_id, rating);


-- =============================================================================
-- END OF SCHEMA
-- =============================================================================