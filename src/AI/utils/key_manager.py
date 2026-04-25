"""
Smart API Key Manager
- Per-key cooldown tracking (respects retry-after headers)
- Circuit breaker per key (opens after 3 consecutive failures)
- Least-recently-used key selection
- Thread-safe for use across parallel threads
"""
import logging
import threading
import time
from dataclasses import dataclass, field
from typing import Optional


@dataclass
class KeyState:
    key: str
    available_at: float = 0.0          # epoch timestamp when key is usable again
    consecutive_failures: int = 0
    circuit_open: bool = False
    circuit_open_until: float = 0.0    # auto-reset circuit after this time
    last_used: float = 0.0
    total_calls: int = 0
    total_failures: int = 0


class SmartKeyManager:
    """
    Thread-safe key manager with:
    - Per-key cooldown (set from retry-after header or default)
    - Circuit breaker: opens after CIRCUIT_OPEN_THRESHOLD failures,
      auto-resets after CIRCUIT_RESET_SECONDS
    - LRU selection: prefers key used least recently
    """

    CIRCUIT_OPEN_THRESHOLD = 3       # consecutive failures before circuit opens
    CIRCUIT_RESET_SECONDS = 120      # auto-reset open circuit after 2 min
    DEFAULT_COOLDOWN = 65            # seconds to wait after a 429 with no retry-after

    def __init__(self, keys: list[str]):
        if not keys:
            raise ValueError("No API keys provided")
        self._states: dict[str, KeyState] = {k: KeyState(key=k) for k in keys}
        self._lock = threading.Lock()
        logging.info(f"[KeyManager] Initialized with {len(keys)} keys")

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def get_available_key(self) -> Optional[str]:
        """Return the best available key (LRU among non-blocked keys), or None."""
        with self._lock:
            now = time.time()
            candidates = []
            for state in self._states.values():
                # Auto-reset circuit breaker if enough time has passed
                if state.circuit_open and now >= state.circuit_open_until:
                    logging.info(f"[KeyManager] Circuit reset for key ...{state.key[-6:]}")
                    state.circuit_open = False
                    state.consecutive_failures = 0

                if state.circuit_open:
                    continue
                if now < state.available_at:
                    continue
                candidates.append(state)

            if not candidates:
                return None

            # Prefer the key that was used least recently
            best = min(candidates, key=lambda s: s.last_used)
            best.last_used = now
            best.total_calls += 1
            return best.key

    def mark_rate_limited(self, key: str, retry_after: int = None):
        """Call when a 429 / RESOURCE_EXHAUSTED is received."""
        cooldown = retry_after if retry_after and retry_after > 0 else self.DEFAULT_COOLDOWN
        with self._lock:
            state = self._states.get(key)
            if not state:
                return
            state.available_at = time.time() + cooldown
            state.consecutive_failures += 1
            state.total_failures += 1
            logging.warning(
                f"[KeyManager] Key ...{key[-6:]} rate-limited "
                f"— cooldown {cooldown}s "
                f"(failures: {state.consecutive_failures})"
            )
            if state.consecutive_failures >= self.CIRCUIT_OPEN_THRESHOLD:
                state.circuit_open = True
                state.circuit_open_until = time.time() + self.CIRCUIT_RESET_SECONDS
                logging.error(
                    f"[KeyManager] Circuit OPEN for key ...{key[-6:]} "
                    f"— will auto-reset in {self.CIRCUIT_RESET_SECONDS}s"
                )

    def mark_server_error(self, key: str):
        """Call on 503 / server-side failures (short backoff, no circuit open)."""
        with self._lock:
            state = self._states.get(key)
            if not state:
                return
            state.available_at = time.time() + 5   # short 5-second pause
            state.consecutive_failures += 1
            state.total_failures += 1

    def mark_success(self, key: str):
        """Call after a successful API call — resets failure counters."""
        with self._lock:
            state = self._states.get(key)
            if not state:
                return
            state.consecutive_failures = 0
            state.circuit_open = False

    def seconds_until_available(self) -> float:
        """Return seconds until at least one key is available (0 if already available)."""
        with self._lock:
            now = time.time()
            waits = []
            for state in self._states.values():
                if state.circuit_open:
                    waits.append(state.circuit_open_until - now)
                else:
                    waits.append(max(0.0, state.available_at - now))
            if not waits:
                return 0.0
            return max(0.0, min(waits))

    def status(self) -> list[dict]:
        """Return human-readable status of all keys (for health endpoints)."""
        with self._lock:
            now = time.time()
            result = []
            for state in self._states.values():
                result.append({
                    "key_suffix": f"...{state.key[-6:]}",
                    "circuit_open": state.circuit_open,
                    "available_in_seconds": max(0.0, round(state.available_at - now, 1)),
                    "consecutive_failures": state.consecutive_failures,
                    "total_calls": state.total_calls,
                    "total_failures": state.total_failures,
                })
            return result