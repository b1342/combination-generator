import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SESSION_ID } from '../../../../testing/fixtures';
import { SessionIdStorage } from './session-id-storage';

describe('SessionIdStorage', () => {
  const storage = new SessionIdStorage();

  beforeEach(() => sessionStorage.clear());
  afterEach(() => vi.restoreAllMocks());

  it('saves, reads and removes the id', () => {
    expect(storage.read()).toBeNull();

    storage.save(SESSION_ID);
    expect(storage.read()).toBe(SESSION_ID);

    storage.remove();
    expect(storage.read()).toBeNull();
  });

  it('removes a stored value that is not a GUID', () => {
    sessionStorage.setItem('combination-generator:sessionId', 'not-a-guid');

    expect(storage.read()).toBeNull();
    expect(sessionStorage.getItem('combination-generator:sessionId')).toBeNull();
  });

  it('keeps working when the browser blocks storage', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });

    expect(() => storage.save(SESSION_ID)).not.toThrow();
    expect(storage.read()).toBeNull();
  });
});
