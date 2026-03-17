from fastapi import FastAPI, HTTPException
from feedback_service import generate_feedback
from feedback_validator import validate_feedback_structure

app = FastAPI()


@app.post("/generate-feedback")
def feedback_endpoint(interview_data: dict):
    try:
        feedback = generate_feedback(interview_data)

        validate_feedback_structure(feedback)

        return {
            "status": "success",
            "data": feedback
        }

    except ValueError as ve:
        raise HTTPException(status_code=400, detail=str(ve))

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))