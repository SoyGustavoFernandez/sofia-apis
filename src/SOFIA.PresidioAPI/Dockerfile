FROM python:3.10.21-slim@sha256:9d53d8d4c0e882f61913025db53b3aec4ef74336082a9ab47d8a14e9e8329b00

WORKDIR /app

RUN pip install --upgrade pip

COPY requirements.txt requirements-model.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Model wheel is verified by hash; its dependencies (spaCy) come from requirements.txt
RUN pip install --no-cache-dir --no-deps --require-hashes -r requirements-model.txt

COPY main.py .

RUN useradd --uid 10001 --no-create-home --shell /usr/sbin/nologin presidio
USER 10001

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
