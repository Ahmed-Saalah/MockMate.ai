import pdfplumber
import logging

def extract_text_from_pdf(file_path: str) -> str:
    try:
        text = ""
        with pdfplumber.open(file_path) as pdf:
            for page in pdf.pages:
                page_text = page.extract_text()
                if page_text:
                    text += page_text + "\n"

        text = text.strip()

        if not text:
            raise ValueError("PDF extraction returned empty text.")

        return text

    except Exception as e:
        logging.error(f"PDF Extraction Error: {e}")
        raise
