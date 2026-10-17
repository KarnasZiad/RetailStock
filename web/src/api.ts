import type { Movement, Product, Reservation, StockItem, Store, Transfer } from './types';

export const API_BASE = import.meta.env.VITE_API_URL ?? '';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const problem = await response.json();
      message = problem.detail ?? problem.title ?? message;
    } catch {
      // body was not JSON
    }
    throw new Error(message);
  }

  return (await response.json()) as T;
}

const post = <T>(path: string, body?: unknown) =>
  request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });

const line = (storeId: string, productId: string) => `/api/stores/${storeId}/products/${productId}`;

export const api = {
  stores: () => request<Store[]>('/api/stores'),
  products: () => request<Product[]>('/api/products'),
  stock: () => request<StockItem[]>('/api/stock'),
  movements: (take = 30) => request<Movement[]>(`/api/movements?take=${take}`),
  transfers: () => request<Transfer[]>('/api/transfers'),

  receive: (storeId: string, productId: string, quantity: number, reference?: string) =>
    post<StockItem>(`${line(storeId, productId)}/receipts`, { quantity, reference }),
  sell: (storeId: string, productId: string, quantity: number, reference?: string) =>
    post<StockItem>(`${line(storeId, productId)}/sales`, { quantity, reference }),
  adjust: (storeId: string, productId: string, countedQuantity: number, reason?: string) =>
    post<StockItem>(`${line(storeId, productId)}/adjustments`, { countedQuantity, reason }),
  reserve: (storeId: string, productId: string, quantity: number, reference: string, ttlMinutes: number) =>
    post<Reservation>(`${line(storeId, productId)}/reservations`, { quantity, reference, ttlMinutes }),

  requestTransfer: (sourceStoreId: string, destinationStoreId: string, productId: string, quantity: number, note?: string) =>
    post<Transfer>('/api/transfers', { sourceStoreId, destinationStoreId, productId, quantity, note }),
  receiveTransfer: (id: string) => post<Transfer>(`/api/transfers/${id}/receive`),
  cancelTransfer: (id: string) => post<Transfer>(`/api/transfers/${id}/cancel`),
};
