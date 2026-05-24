"""
LLM Utility — Gemini wrapper (single API key)
"""
import json
import logging
import re
import threading
import time
from typing import Dict

from google import genai
from google.api_core.exceptions import ResourceExhausted, TooManyRequests
from google.genai import types as genai_types
import json_repair

from core.config import Config


def repair_json_string(text: str) -> str:
    text = re.sub(r"```(?:json)?\s*", "", text).strip()
    text = re.sub(r"\s*```", "", text).strip()
    text = re.sub(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]", "", text)

    if not text.startswith(("{", "[")):
        match = re.search(r"[{\[]", text)
        if match:
            text = text[match.start():]

    text = text.strip()
    if text.endswith("}") or text.endswith("]"):
        return text

    last_brace = text.rfind("}")
    last_bracket = text.rfind("]")
    end_pos = max(last_brace, last_bracket)
    if end_pos != -1:
        text = text[:end_pos + 1]

    return text


def safe_json_load(text: str) -> Dict:
    try:
        return json.loads(text)
    except json.JSONDecodeError as e:
        logging.warning(f"Standard JSON parse failed: {e}")

    try:
        fixed = re.sub(r",\s*([}\]])", r"\1", text)
        return json.loads(fixed)
    except json.JSONDecodeError:
        pass

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


def call_llm(prompt: str, config: dict, label: str, model: str) -> Dict:
    raw_config = {k: v for k, v in config.items() if k != "thinking_config"}
    thinking_budget = config.get("thinking_config", {}).get("thinking_budget", None)
    if thinking_budget is not None:
        raw_config["thinking_config"] = genai_types.ThinkingConfig(thinking_budget=thinking_budget)
    generate_config = genai_types.GenerateContentConfig(**raw_config)

    MAX_ATTEMPTS = 6
    TIMEOUT = Config.LLM_TIMEOUT_SECONDS
    current_prompt = prompt
    json_hint_added = False

    client = genai.Client(api_key=Config.GEMINI_API_KEY)

    for attempt in range(MAX_ATTEMPTS):
        try:
            logging.info(f"[{label}] Attempt {attempt+1}/{MAX_ATTEMPTS}")

            result_holder: Dict = {}
            error_holder: Dict = {}

            def _call():
                try:
                    resp = client.models.generate_content(
                        model=model,
                        contents=current_prompt,
                        config=generate_config,
                    )
                    result_holder["response"] = resp
                except Exception as exc:
                    error_holder["error"] = exc

            thread = threading.Thread(target=_call, daemon=True)
            thread.start()
            thread.join(timeout=TIMEOUT)

            if thread.is_alive():
                logging.warning(f"[{label}] Attempt {attempt+1} timed out after {TIMEOUT}s")
                continue

            if "error" in error_holder:
                raise error_holder["error"]

            response = result_holder.get("response")
            if not response or not response.text:
                raise ValueError("Empty response from model")

            raw_text = response.text.strip()
            logging.debug(f"[{label}] RAW (first 500):\n{raw_text[:500]}")

            cleaned = repair_json_string(raw_text)
            parsed = safe_json_load(cleaned)

            if isinstance(parsed, list):
                if len(parsed) == 1 and isinstance(parsed[0], dict):
                    parsed = parsed[0]
                else:
                    raise ValueError("Parsed JSON is a list, expected a dict")

            logging.info(f"[{label}] Success on attempt {attempt+1}")
            return parsed

        except (ResourceExhausted, TooManyRequests) as e:
            logging.warning(f"[{label}] Rate limit on attempt {attempt+1}: {e}")
            time.sleep(65)
            continue

        except ValueError as e:
            logging.warning(f"[{label}] JSON issue on attempt {attempt+1}: {e}")
            if not json_hint_added:
                current_prompt += (
                    "\n\nREMINDER: Your previous response could not be parsed as JSON. "
                    "Return ONLY a valid JSON object. No markdown, no explanation, no preamble."
                )
                json_hint_added = True
            time.sleep(1)
            continue

        except Exception as e:
            error_str = str(e)

            if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str:
                logging.warning(f"[{label}] Quota exceeded on attempt {attempt+1}")
                time.sleep(65)
                continue

            if "503" in error_str or "unavailable" in error_str.lower():
                logging.warning(f"[{label}] Server busy on attempt {attempt+1}")
                time.sleep(3)
                continue

            logging.error(f"[{label}] Unexpected error on attempt {attempt+1}: {e}")
            time.sleep(1)
            continue

    raise Exception(f"[{label}] All {MAX_ATTEMPTS} attempts exhausted")


def analyze_resume(prompt: str) -> Dict:
    return call_llm(prompt, Config.CV_GENERATION_CONFIG, "CV Analysis", Config.CV_MODEL)


def analyze_content(prompt: str) -> Dict:
    return call_llm(prompt, Config.QUESTIONS_GENERATION_CONFIG, "Questions Generation", Config.QUESTIONS_MODEL)


def analyze_feedback(prompt: str) -> Dict:
    return call_llm(prompt, Config.FEEDBACK_GENERATION_CONFIG, "Feedback Analysis", Config.FEEDBACK_MODEL)
