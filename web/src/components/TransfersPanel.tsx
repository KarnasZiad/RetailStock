import { useState } from 'react';
import { api } from '../api';
import type { Product, Store, Transfer } from '../types';

export interface TransferDraft {
  sourceStoreId: string;
  destinationStoreId: string;
  productId: string;
  quantity: number;
}

interface Props {
  stores: Store[];
  products: Product[];
  transfers: Transfer[];
  draft: TransferDraft | null;
  onDraftChange: (draft: TransferDraft | null) => void;
  storeById: Map<string, Store>;
  productById: Map<string, Product>;
  onChanged: () => Promise<void>;
  notify: (tone: 'success' | 'danger', text: string) => void;
}

const emptyDraft: TransferDraft = { sourceStoreId: '', destinationStoreId: '', productId: '', quantity: 1 };

const statusLabels: Record<Transfer['status'], string> = {
  InTransit: 'En transit',
  Received: 'Reçu',
  Cancelled: 'Annulé',
};

export function TransfersPanel({ stores, products, transfers, draft, onDraftChange, storeById, productById, onChanged, notify }: Props) {
  const [busy, setBusy] = useState(false);
  const form = draft ?? emptyDraft;
  const set = (patch: Partial<TransferDraft>) => onDraftChange({ ...form, ...patch });
  const valid = form.sourceStoreId && form.destinationStoreId && form.productId && form.quantity > 0;

  const act = async (operation: () => Promise<unknown>, success: string) => {
    setBusy(true);
    try {
      await operation();
      notify('success', success);
      await onChanged();
    } catch (e) {
      notify('danger', (e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const submit = () =>
    act(async () => {
      await api.requestTransfer(form.sourceStoreId, form.destinationStoreId, form.productId, form.quantity, 'Rééquilibrage');
      onDraftChange(null);
    }, 'Transfert expédié');

  return (
    <section className="card" id="transfers">
      <h2>Transferts inter-magasins</h2>
      <div className="transfer-form">
        <select value={form.sourceStoreId} onChange={(e) => set({ sourceStoreId: e.target.value })} aria-label="Magasin source">
          <option value="">Depuis…</option>
          {stores.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <select value={form.destinationStoreId} onChange={(e) => set({ destinationStoreId: e.target.value })} aria-label="Magasin destination">
          <option value="">Vers…</option>
          {stores.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <select value={form.productId} onChange={(e) => set({ productId: e.target.value })} aria-label="Produit">
          <option value="">Produit…</option>
          {products.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <input type="number" min={1} value={form.quantity} onChange={(e) => set({ quantity: Number(e.target.value) })} aria-label="Quantité" />
        <button type="button" disabled={!valid || busy} onClick={submit}>
          Expédier
        </button>
      </div>

      {transfers.length === 0 ? (
        <p className="empty">Aucun transfert pour le moment.</p>
      ) : (
        <table className="list">
          <thead>
            <tr>
              <th>Produit</th>
              <th>Trajet</th>
              <th>Qté</th>
              <th>Statut</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {transfers.slice(0, 8).map((t) => (
              <tr key={t.id}>
                <td>{productById.get(t.productId)?.name}</td>
                <td>
                  {storeById.get(t.sourceStoreId)?.code} → {storeById.get(t.destinationStoreId)?.code}
                </td>
                <td>{t.quantity}</td>
                <td>
                  <span className={`pill status-${t.status}`}>{statusLabels[t.status]}</span>
                </td>
                <td className="row-actions">
                  {t.status === 'InTransit' && (
                    <>
                      <button type="button" className="link" disabled={busy} onClick={() => act(() => api.receiveTransfer(t.id), 'Transfert réceptionné')}>
                        Réceptionner
                      </button>
                      <button type="button" className="link danger" disabled={busy} onClick={() => act(() => api.cancelTransfer(t.id), 'Transfert annulé')}>
                        Annuler
                      </button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
