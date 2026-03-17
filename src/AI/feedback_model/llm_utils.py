from google import genai
from config import Config
import json
import logging

client = genai.Client(api_key=Config.GEMINI_API_KEY)


def analyze_content(prompt: str) -> dict:
    try:
        response = client.models.generate_content(
            model=Config.MODEL_NAME,
            contents=prompt,
            config=Config.GENERATION_CONFIG
        )

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        return clean_response(response.text)

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise


def clean_response(text: str) -> dict:
    try:
        start = text.find("{")
        end = text.rfind("}")

        if start == -1 or end == -1:
            raise ValueError("No JSON found")

        return json.loads(text[start:end+1])

    except Exception:
        raise ValueError("Invalid JSON from model")