export interface BrowserTarget {
  readonly key: 'chrome' | 'firefox' | 'webkit' | 'edge';
  readonly name: 'Chrome' | 'Firefox' | 'WebKit' | 'Edge';
  readonly engine: 'Blink' | 'Gecko' | 'WebKit';
  readonly runsDirectoryName?: string;
}

export const chromeTarget: BrowserTarget = { key: 'chrome', name: 'Chrome', engine: 'Blink' };
export const firefoxTarget: BrowserTarget = { key: 'firefox', name: 'Firefox', engine: 'Gecko', runsDirectoryName: 'firefox' };
export const webkitTarget: BrowserTarget = { key: 'webkit', name: 'WebKit', engine: 'WebKit', runsDirectoryName: 'webkit' };
export const edgeTarget: BrowserTarget = { key: 'edge', name: 'Edge', engine: 'Blink', runsDirectoryName: 'edge' };
