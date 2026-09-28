const AVATAR_COLORS = [
  "bg-brand-100 text-brand-700",
  "bg-violet-100 text-violet-700",
  "bg-amber-100 text-amber-700",
  "bg-emerald-100 text-emerald-700",
  "bg-pink-100 text-pink-700",
  "bg-sky-100 text-sky-700",
];

/** Simple deterministic hash so the same seed always maps to the same slot — not for security use. */
export function hashSeed(seed: string): number {
  return [...seed].reduce((sum, char) => sum + char.charCodeAt(0), 0);
}

export function avatarColorFor(seed: string) {
  return AVATAR_COLORS[hashSeed(seed) % AVATAR_COLORS.length];
}
