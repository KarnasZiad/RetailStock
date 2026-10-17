import type { StockItem, Transfer } from '../types';

interface Props {
  items: StockItem[];
  transfers: Transfer[];
  storeCount: number;
}

export function KpiBar({ items, transfers, storeCount }: Props) {
  const onHand = items.reduce((sum, i) => sum + i.onHand, 0);
  const reserved = items.reduce((sum, i) => sum + i.reserved, 0);
  const outOfStock = items.filter((i) => i.level === 'OutOfStock').length;
  const low = items.filter((i) => i.level === 'Low').length;
  const inTransit = transfers.filter((t) => t.status === 'InTransit');

  return (
    <section className="kpis">
      <Kpi label="Unités en stock" value={onHand} hint={`${storeCount} magasins`} />
      <Kpi label="Unités réservées" value={reserved} hint="click & collect, e-réservation" />
      <Kpi label="Ruptures" value={outOfStock} tone={outOfStock > 0 ? 'danger' : undefined} hint="lignes à 0 disponible" />
      <Kpi label="Stocks bas" value={low} tone={low > 0 ? 'warning' : undefined} hint="sous le seuil d'alerte" />
      <Kpi
        label="Transferts en cours"
        value={inTransit.length}
        hint={`${inTransit.reduce((sum, t) => sum + t.quantity, 0)} unités en transit`}
      />
    </section>
  );
}

function Kpi({ label, value, hint, tone }: { label: string; value: number; hint: string; tone?: 'danger' | 'warning' }) {
  return (
    <div className={`kpi ${tone ?? ''}`}>
      <span className="kpi-label">{label}</span>
      <strong className="kpi-value">{value.toLocaleString('fr-FR')}</strong>
      <span className="kpi-hint">{hint}</span>
    </div>
  );
}
