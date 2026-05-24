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

    
    VOICE_GENERATION_CONFIG = {
        "temperature": 0.7,
        "top_p": 0.9,
        "top_k": 40,
        "max_output_tokens": 256,
        "candidate_count": 1,
        "thinking_config": {"thinking_budget": 0},  
    }

   
    VOICE_EVALUATION_CONFIG = {
        "response_mime_type": "application/json",
        "temperature": 0.2,
        "top_p": 0.9,
        "max_output_tokens": 1024,
        "thinking_config": {"thinking_budget": 0},
    }


    VOICE_MAX_USER_INPUT = 2000