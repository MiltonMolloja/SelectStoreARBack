FROM python:3.13-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY reader.py .

# La sesión de Telethon se persiste en /app/data/session.session
RUN mkdir -p /app/data
VOLUME /app/data

CMD ["python", "reader.py"]
