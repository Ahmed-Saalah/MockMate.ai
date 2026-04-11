from fastapi import FastAPI
import logging

from api.routers import cv, feedback

# Configure root logger
logging.basicConfig(level=logging.INFO)

app = FastAPI(title="MockMate API Service")

app.include_router(cv.router, tags=["CV Analysis"])
app.include_router(feedback.router, tags=["Feedback Builder"])
