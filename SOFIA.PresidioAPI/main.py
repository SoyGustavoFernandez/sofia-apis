from fastapi import FastAPI
from pydantic import BaseModel
from presidio_analyzer import AnalyzerEngine, RecognizerRegistry, PatternRecognizer, Pattern
from presidio_analyzer.nlp_engine import NlpEngineProvider
from presidio_anonymizer import AnonymizerEngine

app = FastAPI(title="SOFIA Presidio API")

# --- REGLAS DEL EXPERTO ---
dni_regex = r"\b\d{8}\b"
ruc_regex = r"\b(?:10|15|17|20)\d{9}\b"
celular_regex = r"\b9\d{2}[\s\-\.]?\d{3}[\s\-\.]?\d{3}\b"

dni_context = ["dni", "documento", "identidad", "doc", "identificacion", "nro", "numero", "n°", "id"]
ruc_context = ["ruc", "registro", "unico", "contribuyente", "empresa", "razon", "social"]
celular_context = ["celular", "cel", "telefono", "tel", "movil", "fono", "telf", "contacto", "llamar"]

ALLOW_LIST = [
    "Paracetamol", "Amoxicilina", "Ibuprofeno", "Naproxeno", "Diclofenaco", "Ketorolaco", "Azitromicina",
    "Ciprofloxacino", "Ceftriaxona", "Losartan", "Captopril", "Enalapril", "Metformina", "Glibenclamida",
    "Omeprazol", "Pantoprazol", "Ranitidina", "Salbutamol", "Dexametasona", "Prednisona", "Loratadina",
    "Cetirizina", "Clorfenamina", "Aspirina", "Alprazolam", "Diazepam", "Tramadol", "Fluconazol",
    "Clindamicina", "Levofloxacino", "Meloxicam", "Celecoxib", "Clonazepam", "Atorvastatina", "Simvastatina",
    "Amlodipino", "Bisoprolol", "Furosemida", "Hidroclorotiazida", "Espironolactona", "Levotiroxina", "Insulina",
    "Metamizol", "Buscapina", "Panadol", "Apronax", "tableta", "tabletas", "tab", "tabs", "pastilla",
    "pastillas", "cápsula", "capsula", "cápsulas", "capsulas", "cap", "caps", "jarabe", "jbe", "ampolla",
    "ampollas", "amp", "inyección", "inyeccion", "inyectable", "crema", "pomada", "ungüento", "unguento",
    "gel", "loción", "locion", "gotas", "gts", "frasco", "fco", "sachet", "sobre", "sobres", "supositorio",
    "óvulo", "ovulo", "suspensión", "suspension", "solución", "solucion", "mg", "ml", "g", "gr", "cg", "ug",
    "mcg", "kg", "cc", "ui", "u.i.", "mEq", "vo", "v.o.", "iv", "i.v.", "im", "i.m.", "sc", "s.c.", "sl",
    "s.l.", "oral", "intravenosa", "intramuscular", "subcutanea", "sublingual", "topica", "tomar", "aplicar",
    "administrar", "c/", "cada", "horas", "h", "hrs", "c/8h", "c/12h", "c/24h", "c/6h", "c/4h", "desayuno",
    "almuerzo", "cena", "comidas", "ayunas", "días", "dias", "dosis", "cantidad", "receta", "indicaciones",
    "diagnóstico", "dx", "tratamiento", "tto", "cmp", "rne"
]

# --- CONFIGURACIÓN DE PRESIDIO ---

configuration = {
    "nlp_engine_name": "spacy",
    "models": [{"lang_code": "es", "model_name": "es_core_news_md"}],
}
provider = NlpEngineProvider(nlp_configuration=configuration)
nlp_engine = provider.create_engine()

registry = RecognizerRegistry()
registry.load_predefined_recognizers(nlp_engine=nlp_engine, languages=["es"])

# Registrando DNI
dni_pattern = Pattern(name="dni_pattern", regex=dni_regex, score=0.85)
dni_recognizer = PatternRecognizer(
    supported_entity="PE_DNI",
    supported_language="es",
    patterns=[dni_pattern],
    context=dni_context
)
registry.add_recognizer(dni_recognizer)

# Registrando RUC
ruc_pattern = Pattern(name="ruc_pattern", regex=ruc_regex, score=0.85)
ruc_recognizer = PatternRecognizer(
    supported_entity="PE_RUC",
    supported_language="es",
    patterns=[ruc_pattern],
    context=ruc_context
)
registry.add_recognizer(ruc_recognizer)

# Registrando Celular
celular_pattern = Pattern(name="celular_pattern", regex=celular_regex, score=0.85)
celular_recognizer = PatternRecognizer(
    supported_entity="PE_CELULAR",
    supported_language="es",
    patterns=[celular_pattern],
    context=celular_context
)
registry.add_recognizer(celular_recognizer)

analyzer = AnalyzerEngine(nlp_engine=nlp_engine, registry=registry, supported_languages=["es"])
anonymizer = AnonymizerEngine()

class TextoRequest(BaseModel):
    texto: str

@app.post("/api/anonimizar")
def anonimizar_texto(request: TextoRequest):
    # Analizar el texto con los recognizers y la lista de permitidos
    resultados = analyzer.analyze(
        text=request.texto, 
        language="es", 
        allow_list=ALLOW_LIST
    )
    
    # Anonimizar el texto basado en los resultados
    resultado_anonimizado = anonymizer.anonymize(text=request.texto, analyzer_results=resultados)
    
    # Devolver JSON esperado
    return {"TextoLimpio": resultado_anonimizado.text}
