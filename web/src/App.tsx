import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from './api';
import { useStockHub } from './useStockHub';
import { ActionPanel } from './components/ActionPanel';
import { AlertsPanel } from './components/AlertsPanel';
import { KpiBar } from './components/KpiBar';
import { LiveFeed, type FeedEntry } from './components/LiveFeed';
import { StockMatrix } from './components/StockMatrix';
import { TransfersPanel, type TransferDraft } from './components/TransfersPanel';
import { stockKey, type Product, type StockItem, type Store, type Transfer } from './types';

interface Toast {
  id: number;
  tone: 'warning' | 'danger' | 'success';
  text: string;
}

export default function App() {
  const [stores, setStores] = useState<Store[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [stock, setStock] = useState<Map<string, StockItem>>(new Map());
  const [feed, setFeed] = useState<FeedEntry[]>([]);
  const [transfers, setTransfers] = useState<Transfer[]>([]);
  const [selected, setSelected] = useState<{ storeId: string; productId: string } | null>(null);
  const [draft, setDraft] = useState<TransferDraft | null>(null);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [error, setError] = useState<string | null>(null);

  const storeById = useMemo(() => new Map(stores.map((s) => [s.id, s])), [stores]);
  const productById = useMemo(() => new Map(products.map((p) => [p.id, p])), [products]);

  const loadStock = useCallback(async () => {
    const items = await api.stock();
    setStock(new Map(items.map((i) => [stockKey(i.storeId, i.productId), i])));
  }, []);

  const loadTransfers = useCallback(async () => setTransfers(await api.transfers()), []);

  const loadAll = useCallback(async () => {
    try {
      const [s, p, movements] = await Promise.all([api.stores(), api.products(), api.movements(40)]);
      setStores(s);
      setProducts(p);
      setFeed(
        movements.map((m) => ({
          id: m.id,
          storeId: m.storeId,
          productId: m.productId,
          type: m.type,
          onHandDelta: m.onHandDelta,
          reservedDelta: m.reservedDelta,
          available: m.availableAfter,
          reference: m.reference,
          occurredAt: m.occurredAt,
        })),
      );
      await Promise.all([loadStock(), loadTransfers()]);
      setError(null);
    } catch (e) {
      setError(`API injoignable : ${(e as Error).message}`);
    }
  }, [loadStock, loadTransfers]);

  useEffect(() => {
    void loadAll();
  }, [loadAll]);

  const pushToast = useCallback((tone: Toast['tone'], text: string) => {
    const id = Date.now() + Math.random();
    setToasts((t) => [...t.slice(-3), { id, tone, text }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 5000);
  }, []);

  const status = useStockHub({
    onStockChanged: (m) => {
      const key = stockKey(m.storeId, m.productId);
      setStock((current) => {
        const previous = current.get(key);
        if (!previous) {
          void loadStock();
          return current;
        }
        const next = new Map(current);
        next.set(key, {
          ...previous,
          onHand: m.onHand,
          reserved: m.reserved,
          available: m.available,
          level: m.level,
          updatedAt: m.occurredAt,
        });
        return next;
      });
      setFeed((f) =>
        [
          {
            id: `${m.stockItemId}-${m.occurredAt}-${m.movement}`,
            storeId: m.storeId,
            productId: m.productId,
            type: m.movement,
            onHandDelta: m.onHandDelta,
            reservedDelta: m.reservedDelta,
            available: m.available,
            reference: m.reference,
            occurredAt: m.occurredAt,
            live: true,
          },
          ...f,
        ].slice(0, 60),
      );
    },
    onStockAlert: (m) => {
      const where = `${productById.get(m.productId)?.name ?? 'Produit'} · ${storeById.get(m.storeId)?.name ?? 'magasin'}`;
      if (m.cleared) pushToast('success', `Stock rétabli : ${where}`);
      else if (m.level === 'OutOfStock') pushToast('danger', `Rupture : ${where}`);
      else pushToast('warning', `Stock bas (${m.available}) : ${where}`);
    },
    onTransferUpdated: () => void loadTransfers(),
    onResync: () => void loadAll(),
  });

  const items = useMemo(() => [...stock.values()], [stock]);
  const selectedItem = selected ? stock.get(stockKey(selected.storeId, selected.productId)) : undefined;

  const suggestTransfer = (item: StockItem) => {
    // Rebalancing: take from the store that has the most available units of this product.
    const donor = items
      .filter((i) => i.productId === item.productId && i.storeId !== item.storeId)
      .sort((a, b) => b.available - a.available)[0];
    const quantity = Math.max(1, item.alertThreshold * 2 - item.available);
    setDraft({
      sourceStoreId: donor?.storeId ?? '',
      destinationStoreId: item.storeId,
      productId: item.productId,
      quantity: donor ? Math.min(quantity, Math.max(1, donor.available - donor.alertThreshold)) : quantity,
    });
    document.getElementById('transfers')?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="logo" aria-hidden>
            ▤
          </span>
          <div>
            <h1>RetailStock</h1>
            <p>Pilotage des stocks multi-magasins en temps réel</p>
          </div>
        </div>
        <span className={`hub-status ${status}`}>
          <span className="dot" />
          {status === 'connected' ? 'Temps réel actif' : status === 'reconnecting' ? 'Reconnexion…' : status === 'connecting' ? 'Connexion…' : 'Hors ligne'}
        </span>
      </header>

      {error && <div className="banner danger">{error}</div>}

      <main>
        <KpiBar items={items} transfers={transfers} storeCount={stores.length} />

        <div className="layout">
          <section className="card matrix-card">
            <div className="card-header">
              <h2>Stock disponible par magasin</h2>
              <span className="legend">
                <i className="lvl-Ok" /> OK <i className="lvl-Low" /> Stock bas <i className="lvl-OutOfStock" /> Rupture
              </span>
            </div>
            <StockMatrix
              stores={stores}
              products={products}
              stock={stock}
              selected={selected}
              onSelect={(storeId, productId) => setSelected({ storeId, productId })}
            />
          </section>

          <aside className="side">
            <ActionPanel
              item={selectedItem}
              store={selected ? storeById.get(selected.storeId) : undefined}
              product={selected ? productById.get(selected.productId) : undefined}
              onDone={(text) => pushToast('success', text)}
            />
            <AlertsPanel items={items} storeById={storeById} productById={productById} onSuggestTransfer={suggestTransfer} />
          </aside>
        </div>

        <div className="layout">
          <TransfersPanel
            stores={stores}
            products={products}
            transfers={transfers}
            draft={draft}
            onDraftChange={setDraft}
            storeById={storeById}
            productById={productById}
            onChanged={loadTransfers}
            notify={pushToast}
          />
          <LiveFeed entries={feed} storeById={storeById} productById={productById} />
        </div>
      </main>

      <div className="toasts" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast ${t.tone}`}>
            {t.text}
          </div>
        ))}
      </div>
    </div>
  );
}
