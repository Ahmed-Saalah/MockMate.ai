import json
import logging
import re
from google import genai  
from config import GEMINI_API_KEY, MODEL_NAME, generation_config

client = genai.Client(api_key=GEMINI_API_KEY)


def analyze_resume(prompt: str) -> dict:
    try:
        logging.info("Sending request to Gemini via Google-GenAI SDK...")

        response = client.models.generate_content(
            model=MODEL_NAME,
            contents=prompt,
            config=generation_config
        )

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        raw_text = response.text.strip()

        # تنظيف وتحويل النص المستلم إلى JSON
        cleaned_json = clean_response(raw_text)

        logging.info("LLM response parsed successfully.")
        return cleaned_json

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise

# دالة تنظيف الـ JSON
def clean_response(response_text: str) -> dict:
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