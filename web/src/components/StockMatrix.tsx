import { stockKey, type Product, type StockItem, type Store } from '../types';

interface Props {
  stores: Store[];
  products: Product[];
  stock: Map<string, StockItem>;
  selected: { storeId: string; productId: string } | null;
  onSelect: (storeId: string, productId: string) => void;
}

export function StockMatrix({ stores, products, stock, selected, onSelect }: Props) {
  if (stores.length === 0) {
    return <p className="empty">Chargement du réseau de magasins…</p>;
  }

  return (
    <div className="matrix-scroll">
      <table className="matrix">
        <thead>
          <tr>
            <th className="product-col">Produit</th>
            {stores.map((s) => (
              <th key={s.id} title={s.name}>
                <span className="store-code">{s.code}</span>
                <span className="store-city">{s.city}</span>
              </th>
            ))}
            <th>Réseau</th>
          </tr>
        </thead>
        <tbody>
          {products.map((p) => {
            const total = stores.reduce((sum, s) => sum + (stock.get(stockKey(s.id, p.id))?.available ?? 0), 0);
            return (
              <tr key={p.id}>
                <th className="product-col">
                  <span className="product-name">{p.name}</span>
                  <span className="product-sku">{p.sku}</span>
                </th>
                {stores.map((s) => {
                  const item = stock.get(stockKey(s.id, p.id));
                  const isSelected = selected?.storeId === s.id && selected.productId === p.id;
                  // The button key changes on every update, which replays the flash animation.
                  return (
                    <td key={s.id}>
                      <button
                        type="button"
                        key={item?.updatedAt ?? 'none'}
                        className={`cell lvl-${item?.level ?? 'OutOfStock'} ${isSelected ? 'selected' : ''} flash`}
                        onClick={() => onSelect(s.id, p.id)}
                        title={item ? `Stock ${item.onHand} · réservé ${item.reserved} · seuil ${item.alertThreshold}` : 'Jamais référencé'}
                      >
                        <span className="available">{item?.available ?? '–'}</span>
                        {item && item.reserved > 0 && <span className="reserved">+{item.reserved} rés.</span>}
                      </button>
                    </td>
                  );
                })}
                <td className="total">{total}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
