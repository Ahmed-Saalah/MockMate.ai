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

    return data


def generate_interview_questions(cv_analysis: dict, job_description: str = None) -> dict:
    logging.info(f"Generating interview questions for {cv_analysis.get('track_name')} - {cv_analysis.get('level')}")

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