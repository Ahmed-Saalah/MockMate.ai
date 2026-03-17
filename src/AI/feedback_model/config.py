
import os
from dotenv import load_dotenv

load_dotenv()

class Config:
    GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
    MODEL_NAME = "gemini-2.5-flash"

    
    GENERATION_CONFIG = {
        "temperature": 0.1,
        "top_p": 0.95,
        "max_output_tokens": 2048,
        "response_mime_type": "application/json",
    }