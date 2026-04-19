from fastapi import FastAPI
import logging
from fastapi.responses import RedirectResponse

from api.routers import cv, feedback
from api.routers.questions_full import router as questions_full_router
from api.routers.questions import router as questions_only_router

# Configure root logger
logging.basicConfig(level=logging.INFO)

app = FastAPI(title="MockMate API Service")

@app.get("/", include_in_schema=False)
def root():
    return RedirectResponse(url="/docs")

app.include_router(cv.router, tags=["CV Analysis"])
app.include_router(feedback.router, tags=["Feedback Builder"])
app.include_router(questions_full_router, tags=["Full Interview"])
#app.include_router(questions_only_router, tags=["Questions Only"])