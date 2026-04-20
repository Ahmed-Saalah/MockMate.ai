import json
import logging
import re
import threading
import time
from typing import Dict

from google import genai
from google.api_core.exceptions import ResourceExhausted, TooManyRequests
import json_repair

from core.config import Config


# ====================== API KEY ROTATION ======================
# Thread-safe key rotation using a lock to prevent race conditions
# under concurrent FastAPI requests.
_current_key_index = 0
_key_lock = threading.Lock()

def get_next_api_key() -> str:
    global _current_key_index

    if not Config.GEMINI_API_KEYS:
        raise ValueError("No GEMINI_API_KEYS found in .env")

    with _key_lock:
        key = Config.GEMINI_API_KEYS[_current_key_index]
        _current_key_index = (_current_key_index + 1) % len(Config.GEMINI_API_KEYS)

    return key


def create_client() -> genai.Client:
    return genai.Client(api_key=get_next_api_key())


# ====================== JSON CLEANING ======================

def repair_json_string(text: str) -> str:
    """
    Minimal, safe JSON cleanup. Only strips markdown fences and
    control characters — does NOT attempt regex-based structural
    repair, which corrupts already-valid JSON (e.g. the old
    unquoted-key regex broke URLs and pre-quoted keys).
    json_repair in safe_json_load handles structural issues.
    """
    # Strip markdown code fences
    text = re.sub(r"```(?:json)?\s*", "", text).strip()
    text = re.sub(r"\s*```", "", text).strip()

    # Remove invisible / control characters (keep \n \r \t which are valid in JSON strings)
    text = re.sub(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]", "", text)

    # If response has preamble before the JSON object, extract the JSON portion
    if not text.startswith('{') and not text.startswith('['):
        match = re.search(r'[\{\[]', text)
        if match:
            text = text[match.start():]

    # Trim trailing content after the last closing brace/bracket
    text = text.strip()
    if text.endswith('}') or text.endswith(']'):
        return text

    last_brace = text.rfind('}')
    last_bracket = text.rfind(']')
    end_pos = max(last_brace, last_bracket)
    if end_pos != -1:
        text = text[:end_pos + 1]

    return text


def safe_json_load(text: str) -> Dict:
    # First try: standard parse (fastest, works when model behaves)
    try:
        return json.loads(text)
    except json.JSONDecodeError as e:
        logging.warning(f"Standard JSON parse failed: {e}")

    # Second try: fix trailing commas then parse
    try:
        fixed = re.sub(r",\s*([}\]])", r"\1", text)
        return json.loads(fixed)
    except json.JSONDecodeError:
        pass

    # Third try: json_repair (handles truncated/malformed JSON)
    try:
        repaired = json_repair.loads(text)
        if isinstance(repaired, list):
            if len(repaired) == 1 and isinstance(repaired[0], dict):
                return repaired[0]
            raise ValueError(f"Repaired JSON is a list with {len(repaired)} items, expected a dict")
        return repaired
    except Exception as e2:
        logging.error(f"json_repair also failed: {e2}")
        raise ValueError(f"Could not parse JSON after all repair attempts: {e2}") from e2


# ====================== CORE ENGINE ======================

def call_llm(prompt: str, config: dict, label: str) -> Dict:
    """
    Retry strategy:
    - Up to MAX_TOTAL_ATTEMPTS total tries.
    - Rate-limit errors (429 / ResourceExhausted): rotate key, short sleep, retry.
    - JSON parse errors: add a clarifying note to the prompt, retry (same or next key).
    - Server errors (503): short sleep, retry.
    - Unknown errors: log and retry.
    - Raises only when all attempts are exhausted.
    """
    MAX_TOTAL_ATTEMPTS = max(len(Config.GEMINI_API_KEYS) * 2, 6)
    current_prompt = prompt
    json_hint_added = False

    for attempt in range(MAX_TOTAL_ATTEMPTS):
        try:
            client = create_client()
            logging.info(f"{label} - Attempt {attempt + 1}/{MAX_TOTAL_ATTEMPTS}")

            response = client.models.generate_content(
                model=Config.MODEL_NAME,
                contents=current_prompt,
                config=config,
            )

            if not response or not response.text:
                raise ValueError("Empty response from model")

            raw_text = response.text.strip()
            logging.debug(f"RAW RESPONSE:\n{raw_text[:500]}...")

            cleaned = repair_json_string(raw_text)
            parsed = safe_json_load(cleaned)

            if isinstance(parsed, list):
                if len(parsed) == 1 and isinstance(parsed[0], dict):
                    parsed = parsed[0]
                else:
                    raise ValueError("Parsed JSON is a list, expected a dict")

            logging.debug(f"PARSED KEYS: {list(parsed.keys()) if isinstance(parsed, dict) else type(parsed)}")
            logging.info(f"✅ {label} success on attempt {attempt + 1}")
            return parsed

        except (ResourceExhausted, TooManyRequests):
            logging.warning(f"{label} - Rate limit on attempt {attempt + 1}, rotating key...")
            time.sleep(2)
            continue

        except ValueError as e:
            logging.warning(f"{label} - JSON issue on attempt {attempt + 1}: {e}")
            # Add a one-time hint to the prompt to nudge the model
            if not json_hint_added:
                current_prompt += "\n\nREMINDER: Your previous response could not be parsed as JSON. Return ONLY a valid JSON object. No markdown, no explanation, no preamble."
                json_hint_added = True
            time.sleep(1)
            continue

        except Exception as e:
            error_str = str(e)

            if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str:
                logging.warning(f"{label} - Quota exceeded: {error_str[:120]}")
                delay_match = re.search(r"retryDelay.*?'(\d+)s'", error_str)
                delay = int(delay_match.group(1)) if delay_match else 3
                logging.info(f"Sleeping {delay}s before retry...")
                time.sleep(delay)
                continue

            if "503" in error_str:
                logging.warning(f"{label} - Server busy on attempt {attempt + 1}, retrying...")
                time.sleep(3)
                continue

            logging.error(f"{label} - Unexpected error on attempt {attempt + 1}: {e}")
            time.sleep(1)
            continue

    raise Exception(f"{label}: all {MAX_TOTAL_ATTEMPTS} attempts exhausted")


# ====================== SERVICES ======================

def analyze_resume(prompt: str) -> Dict:
    return call_llm(prompt, Config.CV_GENERATION_CONFIG, "CV Analysis")


def analyze_content(prompt: str) -> Dict:
    return call_llm(prompt, Config.QUESTIONS_GENERATION_CONFIG, "Questions Generation")


def analyze_feedback(prompt: str) -> Dict:
    return call_llm(prompt, Config.FEEDBACK_GENERATION_CONFIG, "Feedback Analysis")