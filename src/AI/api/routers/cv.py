from fastapi import APIRouter, UploadFile, File, Form, HTTPException
import tempfile
import os
import logging

from utils.pdf import extract_text_from_pdf
from services.cv_service import run_resume_analysis

router = APIRouter()

@router.post("/analyze")
async def analyze_resume_endpoint(
    cv_file: UploadFile = File(...),
    job_description: str = Form(..., description="Full job description text (required)")
):
    try:
        logging.info("Receiving CV file and job description...")

        # Save temporary file
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            content = await cv_file.read()
            tmp.write(content)
            temp_path = tmp.name

        cv_text = extract_text_from_pdf(temp_path)
        os.remove(temp_path)

        if not cv_text.strip():
            logging.warning("CV text is empty, but job_description is provided → continuing with JD only")

        # Run analysis (job_description is now required)
        result = run_resume_analysis(cv_text, job_description)

        return {
            "status": "success",
            "data": result
        }

    except Exception as e:
        logging.error(f"API Error: {e}")
        raise HTTPException(status_code=500, detail=str(e))