"""
Lightweight in-memory cache with TTL.
No external dependencies — uses a plain dict protected by a lock.
Drop-in replacement for Redis for single-process deployments.

Usage:
    from utils.cache import cache
    cache.set("key", value, ttl=3600)
    value = cache.get("key")   # returns None on miss/expiry
"""
import hashlib
import logging
import threading
import time
from typing import Any, Optional


class TTLCache:
    def __init__(self, max_size: int = 512):
        self._store: dict[str, tuple[Any, float]] = {}   # key → (value, expires_at)
        self._lock = threading.Lock()
        self._max_size = max_size

    def get(self, key: str) -> Optional[Any]:
        with self._lock:
            entry = self._store.get(key)
            if entry is None:
                return None
            value, expires_at = entry
            if time.time() > expires_at:
                del self._store[key]
                return None
            return value

    def set(self, key: str, value: Any, ttl: int = 3600):
        with self._lock:
            if len(self._store) >= self._max_size:
                oldest_key = min(self._store, key=lambda k: self._store[k][1])
                del self._store[oldest_key]
                logging.debug(f"[Cache] Evicted oldest entry to make room")
            self._store[key] = (value, time.time() + ttl)

    def delete(self, key: str):
        with self._lock:
            self._store.pop(key, None)

    def clear(self):
        with self._lock:
            self._store.clear()

    def make_key(self, *parts: str) -> str:
        """Create a stable cache key from arbitrary string parts."""
        combined = "|".join(str(p) for p in parts)
        return hashlib.md5(combined.encode()).hexdigest()

    def stats(self) -> dict:
        with self._lock:
            now = time.time()
            alive = sum(1 for _, exp in self._store.values() if exp > now)
            return {"total": len(self._store), "alive": alive, "max_size": self._max_size}


cache = TTLCache(max_size=512)