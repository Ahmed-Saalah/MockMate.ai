import logging


from prompts.cv import build_prompt
from utils.llm import analyze_resume
from schemas.cv import validate_cv_output, ResumeAnalysis
from pydantic import ValidationError

def run_resume_analysis(cv_text: str, job_description: str):  # ← removed Optional
    if not cv_text or len(cv_text.strip()) == 0:
        logging.warning("CV text is empty. Proceeding with job_description only.")

    # Prevent very large prompt
    cv_text = cv_text[:15000]

    logging.info("Building prompt...")
    prompt = build_prompt(cv_text, job_description)  

    logging.info("Sending to LLM...")
    response = analyze_resume(prompt)

    logging.info("Validating JSON response...")

    try:
        response = validate_cv_output(response)
        validated = ResumeAnalysis(**response)
        return validated.dict()

    except ValidationError as e:
        logging.error("Invalid JSON returned from model.")
        raise ValueError("Model returned invalid JSON format.") from e