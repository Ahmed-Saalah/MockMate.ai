import logging
from prompts.questions import build_questions_prompt
from utils.llm import analyze_content
from schemas.questions import InterviewQuestions
from pydantic import ValidationError


def validate_questions(data: InterviewQuestions):
    if len(data.mcqQuestions) != 8:
        raise ValueError(f"Expected 8 MCQ, got {len(data.mcqQuestions)}")
    if len(data.codingQuestions) != 2:
        raise ValueError(f"Expected 2 coding questions, got {len(data.codingQuestions)}")

    for q in data.mcqQuestions:
        correct_count = sum(1 for opt in q.options if opt.isCorrect)
        if correct_count != 1:
            raise ValueError("Each MCQ must have exactly one correct answer")

    for q in data.codingQuestions:
        for tmpl in q.templates:

            # ── 1. {{USER_CODE}} double-brace auto-fix ──────────────────────
            if "{{USER_CODE}}" not in tmpl.driverCode:
                if "{USER_CODE}" in tmpl.driverCode:
                    logging.warning(
                        f"driverCode '{q.title}' lang={tmpl.languageId}: "
                        f"single-brace USER_CODE — auto-fixed to double braces"
                    )
                    tmpl.driverCode = tmpl.driverCode.replace("{USER_CODE}", "{{USER_CODE}}")
                else:
                    raise ValueError(
                        f"driverCode '{q.title}' lang={tmpl.languageId}: "
                        f"missing {{{{USER_CODE}}}} placeholder entirely"
                    )

            # ── 2. Python __main__ single-underscore auto-fix ────────────────
            # Model sometimes writes _name_ / _main_ with single underscores —
            # this is a runtime bug: the block will never execute.
            if tmpl.languageId == 71:
                bad_variants = [
                    'if _name_ == "_main_":',
                    "if _name_ == '_main_':",
                ]
                good_main = 'if __name__ == "__main__":'
                for bad in bad_variants:
                    if bad in tmpl.driverCode:
                        logging.warning(
                            f"driverCode '{q.title}' lang=71: "
                            f"single-underscore __main__ detected — auto-fixed"
                        )
                        tmpl.driverCode = tmpl.driverCode.replace(bad, good_main)

            # ── 3. defaultCode skeleton comment check ────────────────────────
            skeleton_markers = ["# Write your code here", "// Write your code here"]
            if not any(m in tmpl.defaultCode for m in skeleton_markers):
                logging.warning(
                    f"defaultCode '{q.title}' lang={tmpl.languageId}: "
                    f"missing skeleton comment — may contain full implementation"
                )

            # ── 4. Helper class leaked into defaultCode ──────────────────────
            # defaultCode should have exactly ONE class (Solution).
            # More than one means helper classes leaked in from driverCode.
            class_count = tmpl.defaultCode.count("class ")
            if class_count > 1:
                logging.warning(
                    f"defaultCode '{q.title}' lang={tmpl.languageId}: "
                    f"found {class_count} class definitions — helper classes "
                    f"belong in driverCode only, not defaultCode"
                )

    return data


def generate_interview_questions(cv_analysis: dict, job_description: str = None) -> dict:
    logging.info(
        f"Generating interview questions for "
        f"{cv_analysis.get('track_name')} - {cv_analysis.get('level')}"
    )

    prompt = build_questions_prompt(cv_analysis, job_description)

    try:
        raw_response = analyze_content(prompt)
        validated = InterviewQuestions(**raw_response)
        validated = validate_questions(validated)

        logging.info("✅ Interview questions generated successfully")
        return validated.model_dump()

    except ValidationError as e:
        logging.error(f"Schema validation failed: {e}")
        raise ValueError("Model returned invalid questions format") from e
    except Exception as e:
        logging.error(f"Questions generation error: {e}")
        raise
