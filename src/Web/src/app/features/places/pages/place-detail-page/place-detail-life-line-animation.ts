interface RoutePoint {
  readonly x: number;
  readonly y: number;
  readonly distance: number;
}

const HERO_ROUTE_FRACTION = 0.19060951;
const MOVEMENT_DURATION_MS = 21_000;
const FINAL_PAUSE_MS = 3_000;
const FADE_DURATION_MS = 300;

export class PlaceDetailLifeLineAnimation {
  private westie: HTMLImageElement | null = null;
  private sheet: HTMLElement | null = null;
  private route: RoutePoint[] = [];
  private routeLength = 0;
  private animationFrame: number | null = null;
  private recalculationFrame: number | null = null;
  private cycleStartedAt: number | null = null;
  private suspendedAt: number | null = null;
  private sheetIsVisible = true;
  private reducedMotion = false;
  private resizeObserver: ResizeObserver | null = null;
  private intersectionObserver: IntersectionObserver | null = null;
  private motionPreference: MediaQueryList | null = null;

  private reachedEndInCurrentCycle = false;

  public constructor(
    private readonly host: HTMLElement,
    private readonly onReachedEnd: () => void = () => undefined
  ) {}

  public connect(westie: HTMLImageElement): void {
    this.disconnect();
    this.westie = westie;
    this.sheet = this.host.querySelector<HTMLElement>('.petiloc-detail__sheet');
    if (!this.sheet || typeof window === 'undefined') return;

    this.motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
    this.reducedMotion = this.motionPreference.matches;
    this.motionPreference.addEventListener('change', this.handleMotionPreferenceChange);
    document.addEventListener('visibilitychange', this.handleVisibilityChange);
    window.addEventListener('resize', this.requestRouteRecalculation, { passive: true });

    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(this.requestRouteRecalculation);
      this.resizeObserver.observe(this.sheet);
      const hero = this.host.querySelector<SVGSVGElement>('.petiloc-detail__hero-organic');
      const desktop = this.host.querySelector<SVGSVGElement>('.petiloc-detail__life-line--desktop');
      const mobile = this.host.querySelector<SVGSVGElement>('.petiloc-detail__life-line--mobile');
      if (hero) this.resizeObserver.observe(hero);
      if (desktop) this.resizeObserver.observe(desktop);
      if (mobile) this.resizeObserver.observe(mobile);
    }

    if (typeof IntersectionObserver !== 'undefined') {
      this.intersectionObserver = new IntersectionObserver(([entry]) => {
        this.sheetIsVisible = entry?.isIntersecting ?? false;
        this.updateRunState();
      });
      this.intersectionObserver.observe(this.sheet);
    }

    this.recalculateRoute();
    this.cycleStartedAt = performance.now();
    this.updateRunState();
  }

  public disconnect(): void {
    if (this.animationFrame !== null) cancelAnimationFrame(this.animationFrame);
    if (this.recalculationFrame !== null) cancelAnimationFrame(this.recalculationFrame);
    this.animationFrame = null;
    this.recalculationFrame = null;
    this.resizeObserver?.disconnect();
    this.intersectionObserver?.disconnect();
    this.motionPreference?.removeEventListener('change', this.handleMotionPreferenceChange);
    if (typeof document !== 'undefined') {
      document.removeEventListener('visibilitychange', this.handleVisibilityChange);
    }
    if (typeof window !== 'undefined') {
      window.removeEventListener('resize', this.requestRouteRecalculation);
    }
    this.restoreStaticPresentation();
    this.westie?.removeAttribute('data-animation-state');
    this.westie = null;
    this.sheet = null;
    this.route = [];
    this.routeLength = 0;
    this.cycleStartedAt = null;
    this.suspendedAt = null;
    this.reachedEndInCurrentCycle = false;
    this.resizeObserver = null;
    this.intersectionObserver = null;
    this.motionPreference = null;
  }

  private readonly handleVisibilityChange = (): void => this.updateRunState();

  private readonly handleMotionPreferenceChange = (event: MediaQueryListEvent): void => {
    this.reducedMotion = event.matches;
    if (!this.reducedMotion) this.cycleStartedAt = performance.now();
    this.updateRunState();
  };

  private readonly requestRouteRecalculation = (): void => {
    if (this.recalculationFrame !== null) return;
    this.recalculationFrame = requestAnimationFrame(() => {
      this.recalculationFrame = null;
      this.recalculateRoute();
      this.updateRunState();
    });
  };

  private recalculateRoute(): void {
    if (!this.sheet) return;
    this.applyAnimatedPresentation();
    const heroSvg = this.host.querySelector<SVGSVGElement>('.petiloc-detail__hero-organic');
    const heroPath = heroSvg?.querySelector<SVGPathElement>('#petiloc-hero-contour');
    const heroRoute = heroSvg?.querySelector<SVGUseElement>('.petiloc-detail__hero-route');
    const visibleBodySvg = Array.from(this.host.querySelectorAll<SVGSVGElement>('.petiloc-detail__life-line'))
      .find(svg => getComputedStyle(svg).display !== 'none');
    const bodyPath = visibleBodySvg?.querySelector<SVGPathElement>('path');
    const heroMatrix = heroSvg?.getScreenCTM();
    const bodyMatrix = bodyPath?.getScreenCTM();
    if (!heroPath || !bodyPath || !heroMatrix || !bodyMatrix) {
      this.route = [];
      this.routeLength = 0;
      return;
    }

    const sheetBox = this.sheet.getBoundingClientRect();
    const originX = sheetBox.left + this.sheet.clientLeft;
    const originY = sheetBox.top + this.sheet.clientTop;
    const points: RoutePoint[] = [];
    const declaredHeroFraction = Number.parseFloat(heroRoute?.dataset['routeFraction'] ?? '');
    const heroFraction = Number.isFinite(declaredHeroFraction) ? declaredHeroFraction : HERO_ROUTE_FRACTION;
    this.appendPath(points, heroPath, heroMatrix, heroFraction, 320, originX, originY);
    this.appendPath(points, bodyPath, bodyMatrix, 1, 640, originX, originY);
    this.route = points;
    this.routeLength = points.at(-1)?.distance ?? 0;
  }

  private appendPath(
    target: RoutePoint[],
    path: SVGPathElement,
    matrix: DOMMatrix,
    fraction: number,
    samples: number,
    originX: number,
    originY: number
  ): void {
    const length = path.getTotalLength() * fraction;
    for (let index = 0; index <= samples; index += 1) {
      const point = path.getPointAtLength(length * index / samples).matrixTransform(matrix);
      const x = point.x - originX;
      const y = point.y - originY;
      const previous = target.at(-1);
      target.push({
        x,
        y,
        distance: (previous?.distance ?? 0) + (previous ? Math.hypot(x - previous.x, y - previous.y) : 0)
      });
    }
  }

  private updateRunState(): void {
    if (!this.westie) return;
    if (this.reducedMotion) {
      this.stopFrame();
      this.restoreStaticPresentation();
      this.setAnimationState('reduced');
      return;
    }

    this.applyAnimatedPresentation();

    const canRun = !document.hidden && this.sheetIsVisible && this.routeLength > 0;
    if (!canRun) {
      if (this.suspendedAt === null) this.suspendedAt = performance.now();
      this.stopFrame();
      this.setAnimationState('suspended');
      return;
    }

    const now = performance.now();
    if (this.cycleStartedAt === null) this.cycleStartedAt = now;
    if (this.suspendedAt !== null) {
      this.cycleStartedAt += now - this.suspendedAt;
      this.suspendedAt = null;
    }
    if (this.animationFrame === null) this.animationFrame = requestAnimationFrame(this.animate);
  }

  private stopFrame(): void {
    if (this.animationFrame !== null) cancelAnimationFrame(this.animationFrame);
    this.animationFrame = null;
  }

  private readonly animate = (timestamp: number): void => {
    this.animationFrame = null;
    if (!this.westie || this.cycleStartedAt === null || this.routeLength <= 0) return;
    if (document.hidden || !this.sheetIsVisible || this.reducedMotion) {
      this.updateRunState();
      return;
    }

    const cycleDuration = MOVEMENT_DURATION_MS + FINAL_PAUSE_MS;
    const elapsed = (timestamp - this.cycleStartedAt) % cycleDuration;
    const moving = elapsed < MOVEMENT_DURATION_MS;
    if (!moving && !this.reachedEndInCurrentCycle) {
      this.reachedEndInCurrentCycle = true;
      this.onReachedEnd();
    } else if (moving && this.reachedEndInCurrentCycle) {
      this.reachedEndInCurrentCycle = false;
    }
    const progress = moving ? elapsed / MOVEMENT_DURATION_MS : 1;
    const point = this.pointAtDistance(this.routeLength * progress);
    if (point) {
      const tilt = Math.max(-5, Math.min(5, point.tangentDegrees));
      this.westie.style.transform = `translate3d(${point.x}px, ${point.y}px, 0) translate(-50%, -50%) rotate(${tilt}deg)`;
      this.westie.style.opacity = String(this.opacityFor(elapsed));
    }
    this.setAnimationState(moving ? 'moving' : 'paused');
    this.animationFrame = requestAnimationFrame(this.animate);
  };

  private pointAtDistance(distance: number): { x: number; y: number; tangentDegrees: number } | null {
    if (!this.route.length) return null;
    let low = 0;
    let high = this.route.length - 1;
    while (low < high) {
      const middle = Math.floor((low + high) / 2);
      if (this.route[middle]!.distance < distance) low = middle + 1;
      else high = middle;
    }
    const next = this.route[low]!;
    const previous = this.route[Math.max(0, low - 1)]!;
    const span = Math.max(Number.EPSILON, next.distance - previous.distance);
    const ratio = Math.max(0, Math.min(1, (distance - previous.distance) / span));
    return {
      x: previous.x + (next.x - previous.x) * ratio,
      y: previous.y + (next.y - previous.y) * ratio,
      tangentDegrees: Math.atan2(next.y - previous.y, next.x - previous.x) * 180 / Math.PI
    };
  }

  private opacityFor(elapsed: number): number {
    if (elapsed < FADE_DURATION_MS) return elapsed / FADE_DURATION_MS;
    const fadeOutStartsAt = MOVEMENT_DURATION_MS + FINAL_PAUSE_MS - FADE_DURATION_MS;
    if (elapsed > fadeOutStartsAt) return (MOVEMENT_DURATION_MS + FINAL_PAUSE_MS - elapsed) / FADE_DURATION_MS;
    return 1;
  }

  private setAnimationState(state: 'moving' | 'paused' | 'suspended' | 'reduced'): void {
    if (this.westie?.dataset['animationState'] !== state) {
      this.westie?.setAttribute('data-animation-state', state);
    }
  }

  private applyAnimatedPresentation(): void {
    if (!this.westie || this.reducedMotion) return;
    this.westie.style.top = '0';
    this.westie.style.right = 'auto';
    this.westie.style.bottom = 'auto';
    this.westie.style.left = '0';
    this.westie.style.width = innerWidth <= 720 ? '2.5rem' : '3.4rem';
    this.westie.style.willChange = 'transform, opacity';
  }

  private restoreStaticPresentation(): void {
    if (!this.westie) return;
    for (const property of ['top', 'right', 'bottom', 'left', 'width', 'will-change', 'transform', 'opacity']) {
      this.westie.style.removeProperty(property);
    }
  }
}
