import os
from dotenv import load_dotenv

load_dotenv()


class Config:

    GEMINI_API_KEYS = [
        os.getenv("GEMINI_API_KEY_1"),
        os.getenv("GEMINI_API_KEY_2"),
        os.getenv("GEMINI_API_KEY_3"),
        os.getenv("GEMINI_API_KEY_4"),
        os.getenv("GEMINI_API_KEY_5"),
        os.getenv("GEMINI_API_KEY_6"),
    ]
    GEMINI_API_KEYS = [k for k in GEMINI_API_KEYS if k]

    MODEL_NAME = "gemini-2.5-flash"

    LLM_TIMEOUT_SECONDS = 90

    CV_GENERATION_CONFIG = {
        "response_mime_type": "application/json",
        "temperature": 0.1,
        "top_p": 0.95,
        "max_output_tokens": 512,
        "thinking_config": {"thinking_budget": 0},
    }

    FEEDBACK_GENERATION_CONFIG = {
        "response_mime_type": "application/json",
        "temperature": 0.1,
        "top_p": 0.95,
        "max_output_tokens": 2048,
        "thinking_config": {"thinking_budget": 0},
    }

    QUESTIONS_GENERATION_CONFIG = {
        "response_mime_type": "application/json",
        "temperature": 0.1,
        "top_p": 0.95,
        "max_output_tokens": 6000,
        "thinking_config": {"thinking_budget": 1024},
    }