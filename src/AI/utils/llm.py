import json
import logging
import re
from google import genai
from core.config import Config

client = genai.Client(api_key=Config.GEMINI_API_KEY)


def analyze_resume(prompt: str) -> dict:
    try:
        logging.info("Sending request to Gemini via Google-GenAI SDK...")

        response = client.models.generate_content(
            model=Config.MODEL_NAME,
            contents=prompt,
            config=Config.CV_GENERATION_CONFIG
        )

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        raw_text = response.text.strip()

        # تنظيف وتحويل النص المستلم إلى JSON
        cleaned_json = clean_cv_response(raw_text)

        logging.info("LLM response parsed successfully.")
        return cleaned_json

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise


def clean_cv_response(response_text: str) -> dict:
    try:
        # إزالة زوائد Markdown مثل ```json
        response_text = re.sub(r"```json\s?|\s?```", "", response_text).strip()

        # البحث عن محتوى الـ JSON الفعلي بين الأقواس { }
        json_match = re.search(r"\{[\s\S]*\}", response_text)

        if not json_match:
            raise ValueError("No valid JSON found in response.")

        json_str = json_match.group()

        return json.loads(json_str)

    except json.JSONDecodeError as e:
        logging.error(f"JSON Parsing Error: {e}")
        raise ValueError("Invalid JSON returned by model.")


def analyze_content(prompt: str) -> dict:
    try:
        response = client.models.generate_content(
            model=Config.MODEL_NAME,
            contents=prompt,
            config=Config.FEEDBACK_GENERATION_CONFIG
        )

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        return clean_feedback_response(response.text)

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise


def clean_feedback_response(text: str) -> dict:
    try:
        start = text.find("{")
        end = text.rfind("}")

        if start == -1 or end == -1:
            raise ValueError("No JSON found")

        return json.loads(text[start:end+1])

    except Exception:
        raise ValueError("Invalid JSON from model")
