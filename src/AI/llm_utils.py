import google.generativeai as genai
import json
import logging
import re
from config import GEMINI_API_KEY, MODEL_NAME, generation_config

# Configure Gemini


genai.configure(api_key=GEMINI_API_KEY)

model = genai.GenerativeModel(
    model_name=MODEL_NAME,
    generation_config=generation_config )



# Main Function


def analyze_resume(prompt: str) -> dict:
    try:
        logging.info("Sending request to Gemini...")

        response = model.generate_content(prompt)

        if not response or not response.text:
            raise ValueError("Empty response from LLM")

        raw_text = response.text.strip()

        cleaned_json = clean_response(raw_text)

        logging.info("LLM response parsed successfully.")
        return cleaned_json

    except Exception as e:
        logging.error(f"LLM Error: {e}")
        raise


# Clean & Parse JSON Safely


def clean_response(response_text: str) -> dict:
    try:

        response_text = response_text.replace("```json", "")
        response_text = response_text.replace("```", "").strip()


        json_match = re.search(r"\{[\s\S]*\}", response_text)

        if not json_match:
            raise ValueError("No valid JSON found in response.")

        json_str = json_match.group()

        return json.loads(json_str)

    except json.JSONDecodeError as e:
        logging.error(f"JSON Parsing Error: {e}")
        raise ValueError("Invalid JSON returned by model.")