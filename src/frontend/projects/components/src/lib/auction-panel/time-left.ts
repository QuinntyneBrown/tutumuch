/** How long an auction has left, as cards and the auction panel show it (L2-114). */
export interface TimeLeft {
  /** "Ends in 2d 4h", "Ends in 5h 10m", "Ends in 42 min" or "Ended". */
  text: string;
  /** Under an hour left: shown in the warning colour. */
  soon: boolean;
  ended: boolean;
}

const MINUTE = 60_000;

/** The time left until `endsAt` at `now`, in whole minutes, rounded down (L2-114 AC7). */
export function timeLeft(endsAt: string, now = Date.now()): TimeLeft {
  const left = Date.parse(endsAt) - now;
  if (left <= 0) return { text: 'Ended', soon: false, ended: true };
  const minutes = Math.floor(left / MINUTE);
  if (minutes < 60) return { text: `Ends in ${Math.max(minutes, 1)} min`, soon: true, ended: false };
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return { text: `Ends in ${hours}h ${minutes % 60}m`, soon: false, ended: false };
  return { text: `Ends in ${Math.floor(hours / 24)}d ${hours % 24}h`, soon: false, ended: false };
}

/** "Tue 27 Oct at 7:30 PM EDT" in the viewer's time zone. */
export function auctionTime(iso: string): string {
  const date = new Date(iso);
  const day = date.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short' });
  const time = date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', timeZoneName: 'short' });
  return `${day} at ${time}`;
}
