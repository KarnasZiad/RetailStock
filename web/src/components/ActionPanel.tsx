import { useState } from 'react';
import { api } from '../api';
import { levelLabels, type Product, type StockItem, type Store } from '../types';

interface Props {
  item?: StockItem;
  store?: Store;
  product?: Product;
  onDone: (message: string) => void;
}

type Action = 'receive' | 'sell' | 'reserve' | 'adjust';

export function ActionPanel({ item, store, product, onDone }: Props) {
  const [quantity, setQuantity] = useState(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!store || !product) {
    return (
      <section className="card">
        <h2>Opérations magasin</h2>
        <p className="empty">Sélectionnez une case du tableau pour réceptionner, vendre, réserver ou inventorier.</p>
      </section>
    );
  }

  const run = async (action: Action) => {
    setBusy(true);
    setError(null);
    try {
      const ref = `${action.toUpperCase()}-${Math.floor(Math.random() * 90000 + 10000)}`;
      if (action === 'receive') await api.receive(store.id, product.id, quantity, ref);
      if (action === 'sell') await api.sell(store.id, product.id, quantity, ref);
      if (action === 'reserve') await api.reserve(store.id, product.id, quantity, ref, 30);
      if (action === 'adjust') await api.adjust(store.id, product.id, quantity, 'Inventaire tournant');
      onDone(`${labels[action]} enregistrée · ${product.name} · ${store.name}`);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="card">
      <h2>Opérations magasin</h2>
      <div className="selection">
        <strong>{product.name}</strong>
        <span>
          {store.name} · {product.sku}
        </span>
      </div>
      {item && (
        <dl className="figures">
          <div>
            <dt>En stock</dt>
            <dd>{item.onHand}</dd>
          </div>
          <div>
            <dt>Réservé</dt>
            <dd>{item.reserved}</dd>
          </div>
          <div>
            <dt>Disponible</dt>
            <dd className={`txt-${item.level}`}>{item.available}</dd>
          </div>
          <div>
            <dt>État</dt>
            <dd className={`txt-${item.level}`}>{levelLabels[item.level]}</dd>
          </div>
        </dl>
      )}
      <label className="field">
        Quantité
        <input type="number" min={0} value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} />
      </label>
      <div className="actions">
        <button type="button" disabled={busy} onClick={() => run('receive')}>
          Réception
        </button>
        <button type="button" disabled={busy} onClick={() => run('sell')}>
          Vente
        </button>
        <button type="button" disabled={busy} onClick={() => run('reserve')}>
          Réserver 30 min
        </button>
        <button type="button" className="secondary" disabled={busy} onClick={() => run('adjust')}>
          Inventaire = {quantity}
        </button>
      </div>
      {error && <p className="error">{error}</p>}
    </section>
  );
}

const labels: Record<Action, string> = {
  receive: 'Réception',
  sell: 'Vente',
  reserve: 'Réservation',
  adjust: 'Inventaire',
};
