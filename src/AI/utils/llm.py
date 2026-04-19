import json
import logging
import re
import time
from typing import Dict

from google import genai
from google.api_core.exceptions import ResourceExhausted, TooManyRequests
import json_repair

from core.config import Config


# ====================== API KEY ROTATION ======================
_current_key_index = 0

def get_next_api_key() -> str:
    global _current_key_index

    if not Config.GEMINI_API_KEYS:
        raise ValueError("No GEMINI_API_KEYS found in .env")

    key = Config.GEMINI_API_KEYS[_current_key_index]
    _current_key_index = (_current_key_index + 1) % len(Config.GEMINI_API_KEYS)

    return key


def create_client() -> genai.Client:
    return genai.Client(api_key=get_next_api_key())


# ====================== JSON CLEANING ======================

def extract_json(text: str) -> str:
    match = re.search(r'\{.*\}', text, re.DOTALL)
    if not match:
        raise ValueError("No JSON found in response")
    return match.group()


def repair_json_string(text: str) -> str:
    # remove markdown
    text = re.sub(r"```json\s*|\s*```", "", text).strip()

    # remove invisible chars
    text = re.sub(r"[\x00-\x1f\x7f]", "", text)

    # Try to extract JSON only (handle both { and [)
    if not text.startswith('{') and not text.startswith('['):
        match = re.search(r'[\{\[].*[\}\]]', text, re.DOTALL)
        if match:
            text = match.group()
    
    # Ensure it ends properly
    text = text.strip()
    if not (text.endswith('}') or text.endswith(']')):
        # Try to find the last closing brace/bracket
        last_brace = text.rfind('}')
        last_bracket = text.rfind(']')
        end_pos = max(last_brace, last_bracket)
        if end_pos != -1:
            text = text[:end_pos + 1]

    # fix trailing commas
    text = re.sub(r",\s*}", "}", text)
    text = re.sub(r",\s*]", "]", text)

    # fix unquoted keys (basic)
    text = re.sub(r'(\w+):', r'"\1":', text)

    return text


def safe_json_load(text: str) -> Dict:
    try:
        return json.loads(text)

    except json.JSONDecodeError as e:
        logging.warning(f"JSON parse failed: {e}")

        try:
            # Try repairing with json_repair
            repaired = json_repair.loads(text)
            
            # If it repairs to a list, try to extract the dict
            if isinstance(repaired, list):
                if len(repaired) == 1 and isinstance(repaired[0], dict):
                    repaired = repaired[0]
                else:
                    # List with multiple items - likely malformed, raise error
                    raise ValueError(f"Repaired JSON is still a list with {len(repaired)} items")
            
            return repaired
            
        except Exception as e2:
            logging.error(f"JSON repair failed: {e2}")
            raise ValueError(f"Invalid JSON after repair: {e2}") from e2


# ====================== CORE ENGINE ======================

def call_llm(prompt: str, config: dict, label: str) -> Dict:

    max_attempts = len(Config.GEMINI_API_KEYS)

    for attempt in range(max_attempts):
        try:
            client = create_client()
            logging.info(f"{label} - Attempt {attempt+1}/{max_attempts}")

            response = client.models.generate_content(
                model=Config.MODEL_NAME,
                contents=prompt,
                config=config   # ✅ CLEAN (no override)
            )

            if not response or not response.text:
                raise ValueError("Empty response")

            raw_text = response.text.strip()
            logging.debug(f"RAW RESPONSE:\n{raw_text}")

            repaired = repair_json_string(raw_text)
            parsed = safe_json_load(repaired)

            # Ensure it's a dict, not a list
            if isinstance(parsed, list):
                if len(parsed) == 1 and isinstance(parsed[0], dict):
                    parsed = parsed[0]
                else:
                    raise ValueError("Parsed JSON is a list, expected dict")

            logging.debug(f"PARSED KEYS: {list(parsed.keys()) if isinstance(parsed, dict) else type(parsed)}")
            logging.debug(f"PARSED CODING: {parsed.get('codingQuestions', 'MISSING')}")

            logging.info(f"✅ {label} success")
            return parsed

        except (ResourceExhausted, TooManyRequests):
            logging.warning("Rate limit → switching key")
            time.sleep(2)
            continue

        except ValueError as e:
            logging.warning(f"{label} JSON issue: {e}")
            # Don't retry if it's already been modified multiple times
            if "IMPORTANT: Return ONLY valid JSON" not in prompt:
                prompt += "\nIMPORTANT: Return ONLY valid JSON. No text."
            time.sleep(1)
            continue

        except Exception as e:
            error_str = str(e)
            
            # Handle rate limit / quota exceeded
            if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str or "quota" in error_str.lower():
                logging.warning(f"Quota exceeded: {error_str}")
                raise Exception(f"API Quota exceeded for {label}. Please check your plan and try again later.")
            
            if "503" in error_str:
                logging.warning("Server busy → retry")
                time.sleep(3)
                continue

            logging.error(f"{label} error: {e}")
            continue

    raise Exception(f"All API keys failed for {label}")


# ====================== SERVICES ======================

def analyze_resume(prompt: str) -> Dict:
    return call_llm(prompt, Config.CV_GENERATION_CONFIG, "CV Analysis")


def analyze_content(prompt: str) -> Dict:
    return call_llm(prompt, Config.QUESTIONS_GENERATION_CONFIG, "Questions Generation")


def analyze_feedback(prompt: str) -> Dict:
    return call_llm(prompt, Config.FEEDBACK_GENERATION_CONFIG, "Feedback Analysis")