from fastapi import FastAPI
from fastapi.responses import RedirectResponse, JSONResponse
import logging

from api.routers import cv, feedback
from api.routers.questions_full import router as questions_full_router
from api.routers.voice_interview import router as voice_interview_router

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)

app = FastAPI(title="MockMate API Service")


@app.get("/", include_in_schema=False)
def root():
    return RedirectResponse(url="/docs")


@app.get("/health", tags=["System"])
def health():
    return JSONResponse({"status": "ok"})


app.include_router(cv.router, tags=["CV Analysis"])
app.include_router(feedback.router, tags=["Feedback Builder"])
app.include_router(questions_full_router, tags=["Full Interview"])
app.include_router(voice_interview_router, tags=["Voice Interview"])
