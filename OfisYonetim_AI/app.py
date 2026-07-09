from fastapi import FastAPI
from pydantic import BaseModel
from openai import OpenAI

app = FastAPI()

# Hugging Face üzerindeki Llama 3 modelini OpenAI standartlarında çağırıyoruz
client = OpenAI(
    base_url="https://router.huggingface.co/v1",
    api_key="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" # hf_ ile başlayan token'ını buraya tırnak içine yapıştır
)

class GelenMesaj(BaseModel):
    message: str

@app.post("/api/chat")
async def chat_et(veri: GelenMesaj):
    kullanici_mesaji = veri.message
    
    try:
        # Yapay zekaya rolünü ve kullanıcının mesajını iletiyoruz
        completion = client.chat.completions.create(
            model="meta-llama/Meta-Llama-3-8B-Instruct",
            messages=[
                {
                    "role": "system", 
                    "content": "Sen Ofis Yönetim Sistemi chatbot asistanısın. Kısa, samimi, kurumsal ve Türkçe cevaplar ver."
                },
                {
                    "role": "user", 
                    "content": kullanici_mesaji
                }
            ],
            max_tokens=80,      # Cevap süresini kısaltmak için token'ı biraz düşürdük (Çok daha hızlı yanıt verecek)
            temperature=0.7     # Botun daha yaratıcı ve esnek cevaplar vermesini sağlar
        )
        
        # Yapay zekanın ürettiği cevabı alıyoruz
        bot_cevabi = completion.choices[0].message.content.strip()
        
    except Exception as e:
        # Herhangi bir hata durumunda sistemin çökmemesi için güvenli bir cevap dönüyoruz
        bot_cevabi = "Şu an isteklerinizi işleyemiyorum, lütfen tekrar deneyin."

    return {"response": bot_cevabi}