export interface EtaEstimate { readonly estimatedRemainingMs: number; readonly sampleSize: number; }
export function estimateRemainingDuration(durations: readonly number[], remaining: number, minimumSampleSize = 3): EtaEstimate | undefined {
  if (remaining <= 0 || durations.length < minimumSampleSize) return undefined;
  const sorted = [...durations].sort((a, b) => a - b);
  const index = Math.floor(sorted.length / 2);
  const median = sorted.length % 2 === 0 ? (sorted[index - 1] + sorted[index]) / 2 : sorted[index];
  return { estimatedRemainingMs: Math.round(median * remaining), sampleSize: durations.length };
}
