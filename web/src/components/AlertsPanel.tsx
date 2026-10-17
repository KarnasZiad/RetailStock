import { levelLabels, type Product, type StockItem, type Store } from '../types';

interface Props {
  items: StockItem[];
  storeById: Map<string, Store>;
  productById: Map<string, Product>;
  onSuggestTransfer: (item: StockItem) => void;
}

export function AlertsPanel({ items, storeById, productById, onSuggestTransfer }: Props) {
  const alerts = items
    .filter((i) => i.level !== 'Ok')
    .sort((a, b) => (a.level === b.level ? a.available - b.available : a.level === 'OutOfStock' ? -1 : 1));

  return (
    <section className="card">
      <div className="card-header">
        <h2>Alertes de rupture</h2>
        <span className="badge">{alerts.length}</span>
      </div>
      {alerts.length === 0 ? (
        <p className="empty">Aucune alerte : tous les stocks sont au-dessus de leur seuil.</p>
      ) : (
        <ul className="alerts">
          {alerts.slice(0, 12).map((a) => (
            <li key={a.id} className={`alert lvl-border-${a.level}`}>
              <div>
                <strong>{productById.get(a.productId)?.name}</strong>
                <span>
                  {storeById.get(a.storeId)?.name} · {a.available} dispo / seuil {a.alertThreshold}
                </span>
              </div>
              <div className="alert-actions">
                <span className={`pill pill-${a.level}`}>{levelLabels[a.level]}</span>
                <button type="button" className="link" onClick={() => onSuggestTransfer(a)}>
                  Rééquilibrer
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
