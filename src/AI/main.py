
import logging

from prompt_builder import build_prompt
from llm_utils import analyze_resume
from pydantic import BaseModel, ValidationError
from typing import List, Optional
from validator import validate_output



class ResumeAnalysis(BaseModel):
    track_name: str
    level: str
    technical_skills: List[str]

# Core Analysis Function

def run_resume_analysis(cv_text: str, job_description: Optional[str] = None):

    if not cv_text or len(cv_text.strip()) == 0:
        raise ValueError("CV text extraction failed or empty file.")

    # Prevent very large prompt
    cv_text = cv_text[:15000]

    logging.info("Building prompt...")
    prompt = build_prompt(cv_text,job_description)

    logging.info("Sending to LLM...")
    response = analyze_resume(prompt)

    logging.info("Validating JSON response...")

    try:
        response = validate_output(response)
        validated = ResumeAnalysis(**response)

        return validated.dict()


    except ValidationError as e:
        logging.error("Invalid JSON returned from model.")
        raise ValueError("Model returned invalid JSON format.") from e



