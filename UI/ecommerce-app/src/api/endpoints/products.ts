import api from '@api/axios';
import type {
  ApiResponse,
  PagedResult,
  Product,
  ProductListItem,
  Category,
  InventoryResponse,
} from '@types/api';

// ─── Request shapes ───────────────────────────────────────────────────────────

export interface ProductsQuery {
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  brand?: string;
  isActive?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
  sort?: 'newest' | 'price_asc' | 'price_desc';
}

export interface SearchQuery {
  query: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  page?: number;
  pageSize?: number;
}

// ─── Endpoint functions ───────────────────────────────────────────────────────

export async function getProducts(
  params: ProductsQuery
): Promise<ApiResponse<PagedResult<ProductListItem>>> {
  const { data } = await api.get<ApiResponse<PagedResult<ProductListItem>>>('/api/products', {
    params,
  });
  return data;
}

export async function getProductById(id: number): Promise<ApiResponse<Product>> {
  const { data } = await api.get<ApiResponse<Product>>(`/api/products/${id}`);
  return data;
}

export async function getCategories(): Promise<ApiResponse<Category[]>> {
  const { data } = await api.get<ApiResponse<Category[]>>('/api/categories');
  return data;
}

export async function getInventoryByVariant(
  variantId: number
): Promise<ApiResponse<InventoryResponse>> {
  const { data } = await api.get<ApiResponse<InventoryResponse>>(
    `/api/inventory/variant/${variantId}`
  );
  return data;
}

export async function searchProducts(
  params: SearchQuery
): Promise<ApiResponse<PagedResult<ProductListItem>>> {
  const { data } = await api.get<ApiResponse<PagedResult<ProductListItem>>>(
    '/api/search/products',
    { params }
  );
  return data;
}