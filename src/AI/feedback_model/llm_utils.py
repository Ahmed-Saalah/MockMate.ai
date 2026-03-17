import json
import logging
import re
from google import genai  # المكتبة الجديدة
from config import GEMINI_API_KEY, MODEL_NAME, generation_config

# في المكتبة الجديدة، لا نستخدم genai.configure
# بل نقوم بإنشاء Client مباشرة
client = genai.Client(api_key=GEMINI_API_KEY)

def analyze_resume(prompt: str) -> dict:
    """
    إرسال النص إلى Gemini باستخدام الـ Client الجديد.
    """
    try:
        logging.info("Sending request to Gemini...")

        # استخدام client.models.generate_content بدلاً من model.generate_content
        response = client.models.generate_content(
            model=MODEL_NAME,
            contents=prompt,
            config=generation_config
        )

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        raw_text = response.text.strip()

        # تنظيف النص المستلم وتحويله لـ JSON
        cleaned_json = clean_response(raw_text)

        logging.info("LLM response parsed successfully.")
        return cleaned_json

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise

def clean_response(response_text: str) -> dict:
    """
    تنظيف مخرجات الموديل لضمان استخراج JSON صحيح.
    """
    try:
        # إزالة علامات المارك داون ```json و ```
        response_text = re.sub(r"```json\s?|\s?```", "", response_text).strip()

        # البحث عن محتوى الـ JSON الفعلي داخل النص
        json_match = re.search(r"\{[\s\S]*\}", response_text)

        if not json_match:
            raise ValueError("No valid JSON found in response.")

        json_str = json_match.group()

        return json.loads(json_str)

    except json.JSONDecodeError as e:
        logging.error(f"JSON Parsing Error: {e}")
        raise ValueError("Invalid JSON returned by model.")