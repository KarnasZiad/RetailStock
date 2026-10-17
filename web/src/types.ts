export type StockLevel = 'Ok' | 'Low' | 'OutOfStock';

export type MovementType =
  | 'Receipt'
  | 'Sale'
  | 'Adjustment'
  | 'TransferOut'
  | 'TransferIn'
  | 'TransferReturn'
  | 'Reservation'
  | 'ReservationConfirmed'
  | 'ReservationReleased'
  | 'ReservationExpired';

export type TransferStatus = 'InTransit' | 'Received' | 'Cancelled';

export interface Store {
  id: string;
  code: string;
  name: string;
  city: string;
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  category: string;
  price: number;
}

export interface StockItem {
  id: string;
  storeId: string;
  productId: string;
  onHand: number;
  reserved: number;
  available: number;
  alertThreshold: number;
  level: StockLevel;
  updatedAt: string;
}

export interface Movement {
  id: string;
  storeId: string;
  productId: string;
  type: MovementType;
  onHandDelta: number;
  reservedDelta: number;
  onHandAfter: number;
  availableAfter: number;
  reference?: string | null;
  occurredAt: string;
}

export interface Transfer {
  id: string;
  sourceStoreId: string;
  destinationStoreId: string;
  productId: string;
  quantity: number;
  status: TransferStatus;
  note?: string | null;
  requestedAt: string;
  completedAt?: string | null;
}

export interface Reservation {
  id: string;
  quantity: number;
  reference: string;
  expiresAt: string;
  stock: StockItem;
}

export interface StockChangedMessage {
  stockItemId: string;
  storeId: string;
  productId: string;
  movement: MovementType;
  onHandDelta: number;
  reservedDelta: number;
  onHand: number;
  reserved: number;
  available: number;
  level: StockLevel;
  reference?: string | null;
  occurredAt: string;
}

export interface StockAlertMessage {
  stockItemId: string;
  storeId: string;
  productId: string;
  level: StockLevel;
  available: number;
  threshold: number;
  cleared: boolean;
  occurredAt: string;
}

export interface TransferUpdatedMessage {
  transferId: string;
  status: TransferStatus;
}

export const stockKey = (storeId: string, productId: string) => `${storeId}:${productId}`;

export const movementLabels: Record<MovementType, string> = {
  Receipt: 'Réception',
  Sale: 'Vente',
  Adjustment: 'Inventaire',
  TransferOut: 'Transfert sortant',
  TransferIn: 'Transfert reçu',
  TransferReturn: 'Transfert annulé',
  Reservation: 'Réservation',
  ReservationConfirmed: 'Retrait client',
  ReservationReleased: 'Réservation annulée',
  ReservationExpired: 'Réservation expirée',
};

export const levelLabels: Record<StockLevel, string> = {
  Ok: 'OK',
  Low: 'Stock bas',
  OutOfStock: 'Rupture',
};
