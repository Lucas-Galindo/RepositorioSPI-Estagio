/** Espelha os helpers hexToRgb/rgbToHex/darkenHex/hexToRgba do protótipo. */

export function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const n = parseInt(hex.replace("#", ""), 16);
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
}

export function darkenHex(hex: string, pct: number): string {
  const { r, g, b } = hexToRgb(hex);
  const clamp = (v: number) => Math.max(0, Math.min(255, Math.round(v)));
  return (
    "#" +
    [clamp(r * (1 - pct)), clamp(g * (1 - pct)), clamp(b * (1 - pct))]
      .map((v) => v.toString(16).padStart(2, "0"))
      .join("")
  );
}

export function hexToRgba(hex: string, alpha: number): string {
  const { r, g, b } = hexToRgb(hex);
  return `rgba(${r},${g},${b},${alpha})`;
}
