import os
from dotenv import load_dotenv

load_dotenv()

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")

MAX_INPUT_CHARS = 12000
MODEL_NAME = "gemini-2.5-flash"
TEMPERATURE = 0.2

generation_config={
    "response_mime_type": "application/json",
    "temperature": 0.1,
    "top_p": 0.95,
    "max_output_tokens": 800
}