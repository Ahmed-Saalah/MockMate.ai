from fastapi import APIRouter, UploadFile, File, Form, HTTPException
import tempfile
import os
import logging

from utils.pdf import extract_text_from_pdf
from services.cv_service import run_resume_analysis
from services.questions_service import generate_interview_questions

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
            raise HTTPException(status_code=400, detail="Empty or unreadable CV")

        cv_analysis = run_resume_analysis(cv_text, job_description)

        questions = generate_interview_questions(cv_analysis=cv_analysis, job_description=job_description)

        return questions

    except Exception as e:
        logging.error(f"❌ Full interview error: {e}")
        raise HTTPException(status_code=500, detail=str(e))