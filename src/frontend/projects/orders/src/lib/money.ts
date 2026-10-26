/** "$102.34": checkout and order pages always show cents (mocks pages/checkout). */
export function formatMoney(value: string): string {
  return `$${Number(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
