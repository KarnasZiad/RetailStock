import { movementLabels, type MovementType, type Product, type Store } from '../types';

export interface FeedEntry {
  id: string;
  storeId: string;
  productId: string;
  type: MovementType;
  onHandDelta: number;
  reservedDelta: number;
  available: number;
  reference?: string | null;
  occurredAt: string;
  live?: boolean;
}

interface Props {
  entries: FeedEntry[];
  storeById: Map<string, Store>;
  productById: Map<string, Product>;
}

const time = new Intl.DateTimeFormat('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

export function LiveFeed({ entries, storeById, productById }: Props) {
  return (
    <section className="card feed-card">
      <h2>Flux des mouvements</h2>
      {entries.length === 0 ? (
        <p className="empty">En attente de mouvements…</p>
      ) : (
        <ol className="feed">
          {entries.map((e) => {
            const delta = e.onHandDelta !== 0 ? e.onHandDelta : -e.reservedDelta;
            return (
              <li key={e.id} className={e.live ? 'live' : undefined}>
                <time>{time.format(new Date(e.occurredAt))}</time>
                <span className={`delta ${delta >= 0 ? 'up' : 'down'}`}>
                  {delta > 0 ? '+' : ''}
                  {delta}
                </span>
                <div className="feed-text">
                  <strong>{movementLabels[e.type]}</strong> · {productById.get(e.productId)?.name ?? '…'}
                  <span>
                    {storeById.get(e.storeId)?.name} · reste {e.available} dispo{e.reference ? ` · ${e.reference}` : ''}
                  </span>
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}
