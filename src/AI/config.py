import os
from dotenv import load_dotenv

load_dotenv()

GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")

MAX_INPUT_CHARS = 15000
MODEL_NAME = "gemini-2.5-flash"


generation_config={
    "response_mime_type": "application/json",
    "temperature": 0.1,
    "top_p": 0.95,

}