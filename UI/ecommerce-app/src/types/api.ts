// ─── Envelope ────────────────────────────────────────────────────────────────

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthResponse {
  userId: number;
  name: string;
  email: string;
  role: 'ADMIN' | 'CUSTOMER';
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiry: string;
}

// ─── Products ─────────────────────────────────────────────────────────────────

export interface Product {
  id: number;
  name: string;
  slug: string;
  description: string | null;
  brand: string | null;
  basePrice: number | null;
  isActive: boolean;
  categoryId: number | null;
  categoryName: string | null;
  createdAt: string;
  images: ProductImage[];
  variants: Variant[];
}

export interface ProductListItem {
  id: number;
  name: string;
  slug: string;
  brand: string | null;
  basePrice: number | null;
  isActive: boolean;
  categoryId: number | null;
  categoryName: string | null;
  primaryImageUrl: string | null;
}

export interface ProductImage {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
}

export interface Variant {
  id: number;
  sku: string;
  color: string | null;
  size: string | null;
  price: number | null;
  stockQuantity: number;
  availableQuantity: number;
}

// ─── Categories ───────────────────────────────────────────────────────────────

export interface Category {
  id: number;
  name: string;
  slug: string | null;
  parentId: number | null;
  parentName: string | null;
  children: Category[];
}

// ─── Cart ─────────────────────────────────────────────────────────────────────

export interface CartResponse {
  cartId: number;
  userId: number;
  items: CartItem[];
  totalPrice: number;
}

export interface CartItem {
  id: number;
  variantId: number;
  sku: string;
  productName: string | null;
  color: string | null;
  size: string | null;
  unitPrice: number | null;
  quantity: number;
  lineTotal: number;
}

// ─── Orders ───────────────────────────────────────────────────────────────────

export type OrderStatus = 'PENDING' | 'PAID' | 'SHIPPED' | 'DELIVERED' | 'CANCELLED';

export interface Order {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  status: OrderStatus;
  subtotalAmount: number;
  discountAmount: number;
  totalAmount: number;
  couponCode: string | null;
  paymentStatus: string | null;
  createdAt: string;
  items: OrderItem[];
  shippingAddress: Address | null;
}

export interface OrderItem {
  id: number;
  sku: string | null;
  productName: string | null;
  color: string | null;
  size: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

// ─── Address ──────────────────────────────────────────────────────────────────

export interface Address {
  id: number;
  addressLine1: string;
  addressLine2?: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
  postalCode: string | null;
}

// ─── Payments ─────────────────────────────────────────────────────────────────

export type PaymentStatus = 'PENDING' | 'COMPLETED' | 'FAILED' | 'REFUNDED';

export interface Payment {
  id: number;
  orderId: number;
  paymentMethod: string | null;
  transactionId: string | null;
  amount: number | null;
  status: PaymentStatus;
  failureReason: string | null;
  paidAt: string | null;
}

// ─── Reviews ──────────────────────────────────────────────────────────────────

export interface Review {
  id: number;
  userId: number;
  userName: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
}

// ─── Inventory ────────────────────────────────────────────────────────────────

export interface InventoryResponse {
  id: number;
  variantId: number;
  sku: string | null;
  productId: number;
  stockQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  updatedAt: string;
}

// ─── Coupon ───────────────────────────────────────────────────────────────────

export type CouponDiscountType = 'PERCENTAGE' | 'FLAT';

export interface Coupon {
  id: number;
  code: string;
  discountType: CouponDiscountType;
  discountValue: number;
  minOrderAmount: number | null;
  maxDiscount: number | null;
  expiryDate: string | null;
  usageLimit: number | null;
  usedCount: number;
}

export interface CouponValidationResponse {
  isValid: boolean;
  discountAmount: number;
  message: string | null;
}
