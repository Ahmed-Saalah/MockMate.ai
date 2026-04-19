import os
from dotenv import load_dotenv

load_dotenv()

class Config:
    
 

    GEMINI_API_KEYS = [
        os.getenv("GEMINI_API_KEY_1"),
        os.getenv("GEMINI_API_KEY_2"),
        os.getenv("GEMINI_API_KEY_3")
    ]
   
    GEMINI_API_KEYS = [k for k in GEMINI_API_KEYS if k]

    MODEL_NAME = "gemini-2.5-flash"
    
    # CV generation config
    CV_GENERATION_CONFIG = {
        "response_mime_type": "application/json",
        "temperature": 0.1,
        "top_p": 0.95,
    }

    # Feedback generation config
    FEEDBACK_GENERATION_CONFIG = {
        "temperature": 0.1,
        "top_p": 0.95,
        "max_output_tokens": 2048,
        "response_mime_type": "application/json",
    }
 
    QUESTIONS_GENERATION_CONFIG = {
        "temperature": 0.1, 
        "top_p": 0.95,
        "max_output_tokens":10000, 
        "response_mime_type": "application/json",
    }