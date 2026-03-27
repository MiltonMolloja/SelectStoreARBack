FROM python:3.13-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY reader.py .

# La sesión se persiste en un volumen
VOLUME /app/session

CMD ["python", "reader.py"]
