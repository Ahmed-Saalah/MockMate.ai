from fastapi import FastAPI, UploadFile, File, Form, HTTPException
import tempfile
import os
import logging

from pdf_utils import extract_text_from_pdf
from main import run_resume_analysis

app = FastAPI(title="CV AI Analyzer Service")


@app.post("/analyze")
async def analyze_resume(
    cv_file: UploadFile = File(...),
    job_description: str = Form(None)
):
    try:
        logging.info("Receiving CV file...")

        # Save temporary file
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp:
            content = await cv_file.read()
            tmp.write(content)
            temp_path = tmp.name


        cv_text = extract_text_from_pdf(temp_path)


        os.remove(temp_path)

        if not cv_text.strip():
            raise HTTPException(status_code=400, detail="Empty CV text.")

        # Run analysis
        result = run_resume_analysis(cv_text, job_description)

        return {
            "status": "success",
            "data": result
        }

    except Exception as e:
        logging.error(f"API Error: {e}")
        raise HTTPException(status_code=500, detail=str(e))