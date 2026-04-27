import logging

from prompts.cv import build_prompt
from utils.llm import analyze_resume
from utils.cache import cache
from schemas.cv import validate_cv_output, ResumeAnalysis
from pydantic import ValidationError


def run_resume_analysis(cv_text: str, job_description: str) -> dict:
    if not cv_text or len(cv_text.strip()) == 0:
        logging.warning("CV text is empty. Proceeding with job_description only.")

    cv_text = cv_text[:15000]

    ck = cache.make_key("cv_analysis", cv_text[:500], job_description[:200])
    cached = cache.get(ck)
    if cached is not None:
        logging.info("CV analysis served from cache")
        return cached

    prompt = build_prompt(cv_text, job_description)
    MAX_RETRIES = 3

    for attempt in range(1, MAX_RETRIES + 1):
        try:
            logging.info(f"CV analysis attempt {attempt}/{MAX_RETRIES}")
            response = analyze_resume(prompt)
            response = validate_cv_output(response)
            validated = ResumeAnalysis(**response)
            result = validated.dict()
            logging.info("CV analysis successful")
            cache.set(ck, result, ttl=7200)
            return result

        except (ValidationError, ValueError) as e:
            logging.warning(f"CV attempt {attempt} failed validation: {e}")
            if attempt == MAX_RETRIES:
                raise ValueError("Model returned invalid CV format after retries") from e
            prompt = build_prompt(cv_text, job_description) + (
                f"\n\nPREVIOUS ATTEMPT FAILED:\n{e}\n"
                "Fix the issue and return ONLY valid JSON."
            )

        except Exception as e:
            logging.error(f"CV analysis error on attempt {attempt}: {e}")
            raise