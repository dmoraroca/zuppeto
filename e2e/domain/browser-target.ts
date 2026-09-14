export interface BrowserTarget {
  readonly key: 'chrome' | 'firefox' | 'webkit';
  readonly name: 'Chrome' | 'Firefox' | 'WebKit';
  readonly engine: 'Blink' | 'Gecko' | 'WebKit';
  readonly runsDirectoryName?: string;
}

export const chromeTarget: BrowserTarget = { key: 'chrome', name: 'Chrome', engine: 'Blink' };
export const firefoxTarget: BrowserTarget = { key: 'firefox', name: 'Firefox', engine: 'Gecko', runsDirectoryName: 'firefox' };
export const webkitTarget: BrowserTarget = { key: 'webkit', name: 'WebKit', engine: 'WebKit', runsDirectoryName: 'webkit' };
