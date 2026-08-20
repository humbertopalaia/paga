import { TestBed } from '@angular/core/testing';
import { ThemeService, Theme } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;
  let getItemSpy: jasmine.Spy;
  let setItemSpy: jasmine.Spy;
  let matchMediaSpy: jasmine.Spy;

  function mockMatchMedia(prefersDark: boolean): void {
    matchMediaSpy.and.callFake((query: string) => ({
      matches: prefersDark,
      media: query,
      onchange: null,
      addEventListener: jasmine.createSpy('addEventListener'),
      removeEventListener: jasmine.createSpy('removeEventListener'),
      dispatchEvent: jasmine.createSpy('dispatchEvent'),
      addListener: jasmine.createSpy('addListener'),
      removeListener: jasmine.createSpy('removeListener'),
    }));
  }

  function createService(): ThemeService {
    return TestBed.inject(ThemeService);
  }

  beforeEach(() => {
    getItemSpy = spyOn(Storage.prototype, 'getItem').and.returnValue(null);
    setItemSpy = spyOn(Storage.prototype, 'setItem');
    matchMediaSpy = spyOn(window, 'matchMedia');
    mockMatchMedia(false);

    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => {
    document.documentElement.removeAttribute('data-theme');
    TestBed.resetTestingModule();
  });

  function setupTestBed(): void {
    TestBed.configureTestingModule({});
  }

  describe('initial theme resolution', () => {
    it('should default to light when no localStorage and system prefers light', () => {
      mockMatchMedia(false);
      getItemSpy.and.returnValue(null);
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('light');
    });

    it('should default to dark when no localStorage and system prefers dark', () => {
      mockMatchMedia(true);
      getItemSpy.and.returnValue(null);
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('dark');
    });

    it('should use localStorage value when present (dark)', () => {
      getItemSpy.and.returnValue('dark');
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('dark');
    });

    it('should use localStorage value when present (light)', () => {
      getItemSpy.and.returnValue('light');
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('light');
    });

    it('should ignore invalid localStorage value and fallback to system preference', () => {
      getItemSpy.and.returnValue('invalid-value');
      mockMatchMedia(true);
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('dark');
    });
  });

  describe('toggle()', () => {
    it('should toggle from light to dark', () => {
      mockMatchMedia(false);
      getItemSpy.and.returnValue(null);
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('light');
      service.toggle();
      expect(service.theme()).toBe('dark');
    });

    it('should toggle from dark to light', () => {
      getItemSpy.and.returnValue('dark');
      setupTestBed();
      service = createService();

      expect(service.theme()).toBe('dark');
      service.toggle();
      expect(service.theme()).toBe('light');
    });
  });

  describe('persistence and DOM sync', () => {
    it('should persist to localStorage on toggle', () => {
      mockMatchMedia(false);
      getItemSpy.and.returnValue(null);
      setupTestBed();
      service = createService();
      TestBed.flushEffects();

      setItemSpy.calls.reset();
      service.toggle();
      TestBed.flushEffects();

      expect(setItemSpy).toHaveBeenCalledWith('paga-theme', 'dark');
    });

    it('should update data-theme attribute on toggle', () => {
      mockMatchMedia(false);
      getItemSpy.and.returnValue(null);
      setupTestBed();
      service = createService();
      TestBed.flushEffects();

      expect(document.documentElement.getAttribute('data-theme')).toBe('light');

      service.toggle();
      TestBed.flushEffects();

      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    });
  });
});
