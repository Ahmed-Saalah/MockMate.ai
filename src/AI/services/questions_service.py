import logging
import asyncio
from concurrent.futures import ThreadPoolExecutor

from prompts.questions import build_mcq_prompt, build_coding_prompt, build_questions_prompt
from utils.llm import analyze_content
from utils.cache import cache
from schemas.questions import InterviewQuestions, MCQQuestion, CodingQuestion
from pydantic import ValidationError



def validate_questions(data: InterviewQuestions) -> InterviewQuestions:
    if len(data.mcqQuestions) != 8:
        raise ValueError(f"Expected 8 MCQ, got {len(data.mcqQuestions)}")
    if len(data.codingQuestions) != 2:
        raise ValueError(f"Expected 2 coding questions, got {len(data.codingQuestions)}")

    for q in data.mcqQuestions:
        correct_count = sum(1 for opt in q.options if opt.isCorrect)
        if correct_count != 1:
            raise ValueError(f"MCQ '{q.title}' must have exactly 1 correct answer, got {correct_count}")
        if len(q.options) < 2:
            raise ValueError(f"MCQ '{q.title}' must have at least 2 options")

    for q in data.codingQuestions:
        for tmpl in q.templates:
            if "{{USER_CODE}}" not in tmpl.driverCode:
                if "{USER_CODE}" in tmpl.driverCode:
                    logging.warning(f"driverCode '{q.title}' lang={tmpl.languageId}: auto-fixed single-brace USER_CODE")
                    tmpl.driverCode = tmpl.driverCode.replace("{USER_CODE}", "{{USER_CODE}}")
                else:
                    raise ValueError(
                        f"driverCode '{q.title}' lang={tmpl.languageId}: missing {{{{USER_CODE}}}} placeholder"
                    )

            if tmpl.languageId == 71:
                for bad in ['if _name_ == "_main_":', "if _name_ == '_main_':"]:
                    if bad in tmpl.driverCode:
                        logging.warning(f"driverCode '{q.title}' lang=71: auto-fixed single-underscore __main__")
                        tmpl.driverCode = tmpl.driverCode.replace(bad, 'if __name__ == "__main__":')

            if not any(m in tmpl.defaultCode for m in ["# Write your code here", "// Write your code here"]):
                logging.warning(f"defaultCode '{q.title}' lang={tmpl.languageId}: missing skeleton comment")

            if tmpl.defaultCode.count("class ") > 1:
                logging.warning(f"defaultCode '{q.title}' lang={tmpl.languageId}: multiple class definitions detected")

    return data



def _generate_mcq(cv_analysis: dict, job_description: str, max_retries: int = 3) -> list:
    """Generate 8 MCQ questions — runs in a thread."""
    # Cache: same cv+jd → same MCQs
    ck = cache.make_key("mcq", str(cv_analysis.get("track_name")),
                         str(cv_analysis.get("level")), str(job_description)[:200])
    cached = cache.get(ck)
    if cached is not None:
        logging.info("MCQ served from cache")
        return [MCQQuestion(**q) for q in cached]

    prompt = build_mcq_prompt(cv_analysis, job_description)
    current_prompt = prompt

    for attempt in range(1, max_retries + 1):
        try:
            logging.info(f"MCQ attempt {attempt}/{max_retries}")
            raw = analyze_content(current_prompt)

            mcq_list = raw.get("mcqQuestions", [])
            if len(mcq_list) != 8:
                raise ValueError(f"Expected 8 MCQ, got {len(mcq_list)}")

            validated = [MCQQuestion(**q) for q in mcq_list]

            for q in validated:
                correct = sum(1 for o in q.options if o.isCorrect)
                if correct != 1:
                    raise ValueError(f"MCQ '{q.title}' has {correct} correct answers, expected 1")
                if len(q.options) < 2:
                    raise ValueError(f"MCQ '{q.title}' must have at least 2 options")

            logging.info("✅ MCQ generation successful")
            cache.set(ck, [q.model_dump() for q in validated], ttl=3600)
            return validated

        except (ValidationError, ValueError) as e:
            logging.warning(f"MCQ attempt {attempt} failed: {e}")
            if attempt == max_retries:
                raise ValueError(f"MCQ generation failed after {max_retries} retries") from e
            current_prompt = prompt + (
                f"\n\nPREVIOUS ATTEMPT FAILED:\n{e}\n"
                "Fix the issue and return ONLY valid JSON."
            )

        except Exception as e:
            logging.error(f"MCQ unexpected error on attempt {attempt}: {e}")
            raise



def _generate_coding(cv_analysis: dict, job_description: str, max_retries: int = 5) -> list:
    """Generate 2 coding questions — runs in a thread."""
    ck = cache.make_key("coding", str(cv_analysis.get("track_name")),
                         str(cv_analysis.get("level")), str(job_description)[:200])
    cached = cache.get(ck)
    if cached is not None:
        logging.info("Coding questions served from cache")
        return [CodingQuestion(**q) for q in cached]

    prompt = build_coding_prompt(cv_analysis, job_description)
    current_prompt = prompt

    for attempt in range(1, max_retries + 1):
        try:
            logging.info(f"Coding attempt {attempt}/{max_retries}")
            raw = analyze_content(current_prompt)

            coding_list = raw.get("codingQuestions", [])
            if len(coding_list) != 2:
                raise ValueError(f"Expected 2 coding questions, got {len(coding_list)}")

            validated = [CodingQuestion(**q) for q in coding_list]

            for q in validated:
                for tmpl in q.templates:
                    if "{{USER_CODE}}" not in tmpl.driverCode:
                        if "{USER_CODE}" in tmpl.driverCode:
                            logging.warning(f"Auto-fixed single-brace USER_CODE in '{q.title}' lang={tmpl.languageId}")
                            tmpl.driverCode = tmpl.driverCode.replace("{USER_CODE}", "{{USER_CODE}}")
                        else:
                            if tmpl.languageId == 71 and 'if __name__' in tmpl.driverCode:
                                tmpl.driverCode = tmpl.driverCode.replace(
                                    'if __name__', '{{USER_CODE}}\n\nif __name__', 1
                                )
                                logging.warning(f"Injected missing {{{{USER_CODE}}}} in '{q.title}' lang=71")
                            elif tmpl.languageId == 51 and 'public class Program' in tmpl.driverCode:
                                tmpl.driverCode = tmpl.driverCode.replace(
                                    'public class Program', '{{USER_CODE}}\n\npublic class Program', 1
                                )
                                logging.warning(f"Injected missing {{{{USER_CODE}}}} in '{q.title}' lang=51")
                            else:
                                raise ValueError(f"driverCode '{q.title}' lang={tmpl.languageId}: missing {{{{USER_CODE}}}}")

                    if tmpl.languageId == 71:
                        for bad in ['if _name_ == "_main_":', "if _name_ == '_main_':"]:
                            if bad in tmpl.driverCode:
                                tmpl.driverCode = tmpl.driverCode.replace(bad, 'if __name__ == "__main__":')

            logging.info("Coding generation successful")
            cache.set(ck, [q.model_dump() for q in validated], ttl=3600)
            return validated

        except (ValidationError, ValueError) as e:
            logging.warning(f"Coding attempt {attempt} failed: {e}")
            if attempt == max_retries:
                raise ValueError(f"Coding generation failed after {max_retries} retries") from e
            current_prompt = prompt + (
                f"\n\nPREVIOUS ATTEMPT FAILED:\n{e}\n"
                "Fix the issue and return ONLY valid JSON."
            )

        except Exception as e:
            logging.error(f"Coding unexpected error on attempt {attempt}: {e}")
            raise


async def generate_interview_questions_parallel(cv_analysis: dict, job_description: str = None) -> dict:
    """
    Runs MCQ and coding generation in parallel using a thread pool.
    Total time ≈ max(mcq_time, coding_time) instead of mcq_time + coding_time.
    """
    track = cv_analysis.get("track_name", "Unknown")
    level = cv_analysis.get("level", "Unknown")
    logging.info(f"Parallel questions generation — {track} / {level}")

    loop = asyncio.get_event_loop()

    with ThreadPoolExecutor(max_workers=2) as pool:
        mcq_future = loop.run_in_executor(pool, _generate_mcq, cv_analysis, job_description)
        coding_future = loop.run_in_executor(pool, _generate_coding, cv_analysis, job_description)

        mcq_questions, coding_questions = await asyncio.gather(mcq_future, coding_future)

    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))

    result = InterviewQuestions(
        trackName=cv_analysis.get("track_name", ""),
        seniorityLevel=seniority_level,
        detectedSkills=cv_analysis.get("technical_skills", []),
        mcqQuestions=mcq_questions,
        codingQuestions=coding_questions,
    )

    logging.info("Parallel interview questions generated successfully")
    return result.model_dump()


def generate_interview_questions(cv_analysis: dict, job_description: str = None) -> dict:
    """Sync version — used by the questions-only endpoint."""
    logging.info(
        f"Generating interview questions for "
        f"{cv_analysis.get('track_name')} - {cv_analysis.get('level')}"
    )

    prompt = build_questions_prompt(cv_analysis, job_description)
    current_prompt = prompt
    MAX_RETRIES = 3

    for attempt in range(1, MAX_RETRIES + 1):
        try:
            logging.info(f"Questions attempt {attempt}/{MAX_RETRIES}")
            raw_response = analyze_content(current_prompt)
            validated = InterviewQuestions(**raw_response)
            validated = validate_questions(validated)

            logging.info("Interview questions generated successfully")
            return validated.model_dump()

        except (ValidationError, ValueError) as e:
            logging.warning(f"Attempt {attempt} failed validation: {e}")
            if attempt == MAX_RETRIES:
                raise ValueError("Model returned invalid questions format after retries") from e
            current_prompt = prompt + (
                f"\n\nPREVIOUS ATTEMPT FAILED VALIDATION:\n{e}\n"
                "Fix the issue and return ONLY valid JSON matching the required structure exactly."
            )

        except Exception as e:
            logging.error(f"Questions generation error on attempt {attempt}: {e}")
            raise