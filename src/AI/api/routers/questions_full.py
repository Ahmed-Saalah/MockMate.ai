import asyncio
import logging
import os
import tempfile

from fastapi import APIRouter, File, Form, HTTPException, UploadFile

from services.cv_service import run_resume_analysis
from services.questions_service import generate_interview_questions_parallel
from utils.pdf import extract_text_from_pdf

router = APIRouter(prefix="/interview", tags=["Full Interview"])


@router.post("/full", summary="Generate Full Interview (CV + JD)")
async def generate_full_interview(
    cv_file: UploadFile = File(...),
    job_description: str = Form(...)
):
    try:
        logging.info("📥 Starting Full Interview Generation...")

        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            content = await cv_file.read()
            tmp.write(content)
            temp_path = tmp.name

        cv_text = extract_text_from_pdf(temp_path)
        os.remove(temp_path)

        if not cv_text.strip():
            logging.warning("CV is empty — will rely on JD only for analysis")
            cv_text = ""

        cv_analysis = await asyncio.get_event_loop().run_in_executor(
            None, run_resume_analysis, cv_text, job_description
        )

        questions = await generate_interview_questions_parallel(
            cv_analysis=cv_analysis,
            job_description=job_description,
        )

        return questions

    except HTTPException:
        raise
    except Exception as e:
        logging.error(f"Full interview error: {e}")
        raise HTTPException(status_code=500, detail=str(e))
