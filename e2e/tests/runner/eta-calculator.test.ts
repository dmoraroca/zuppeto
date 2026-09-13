import assert from 'node:assert/strict';
import test from 'node:test';
import { estimateRemainingDuration } from '../../application/eta-calculator.js';

test('ETA is absent until the sample is sufficient', () => {
  assert.equal(estimateRemainingDuration([10, 20], 3), undefined);
});

test('ETA uses the median simulated duration', () => {
  assert.deepEqual(estimateRemainingDuration([10, 30, 90], 4), { estimatedRemainingMs: 120, sampleSize: 3 });
});
