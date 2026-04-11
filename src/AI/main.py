from fastapi import FastAPI
import logging
from fastapi.responses import RedirectResponse

from api.routers import cv, feedback

# Configure root logger
logging.basicConfig(level=logging.INFO)

app = FastAPI(title="MockMate API Service")

@app.get("/", include_in_schema=False)
def root():
    return RedirectResponse(url="/docs")

app.include_router(cv.router, tags=["CV Analysis"])
app.include_router(feedback.router, tags=["Feedback Builder"])
