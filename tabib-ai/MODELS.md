# 🧠 دليل نماذج TabibAI

دليل شامل لاختيار وتحميل وتحسين النماذج المحلية (GGUF) لمشروع TabibAI.

---

## 1️⃣ جدول مقارنة النماذج الموصى بها

| النموذج | الحجم (Q4_K_M) | RAM المطلوبة | العربية | الإنجليزية | الأفضل لـ |
|---------|----------------|--------------|---------|------------|----------|
| **gemma2-2b** (مضمّن حالياً) | 1.6 GB | 4 GB | ⭐⭐ | ⭐⭐⭐ | الأجهزة الضعيفة، تجربة سريعة |
| **Qwen2.5-3B-Instruct** | 1.4 GB | 4 GB | ⭐⭐⭐ | ⭐⭐⭐ | محادثة عامة + كود، أفضل اختيار صغير |
| **Llama-3.2-3B-Instruct** | 1.4 GB | 4 GB | ⭐⭐ | ⭐⭐⭐⭐ | الإنجليزية والمحادثة |
| **Phi-3.5-mini-instruct** | 1.5 GB | 4 GB | ⭐⭐ | ⭐⭐⭐⭐ | البرمجة والاستدلال المنطقي |
| **Qwen2.5-7B-Instruct** | 3.5 GB | 8 GB | ⭐⭐⭐ | ⭐⭐⭐⭐ | الجودة العالية على أجهزة متوسطة |
| **ALLaM-7B-Instruct** | 3.5 GB | 8 GB | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | **العربية الفصحى** (سعودي - SDAIA) |
| **Jais-13B-chat** | 6.5 GB | 16 GB | ⭐⭐⭐⭐ | ⭐⭐⭐ | العربية الفصحى عالية الجودة |

**القاعدة:** اختر النموذج الأصغر الذي يناسب RAM جهازك — 2-3B للأجهزة ≤4GB، 7B لـ ≥8GB.

---

## 2️⃣ روابط التحميل المباشرة (Hugging Face)

كل الروابط مؤكدة ومباشرة (`resolve/main/...`) — استخدم `download-models.sh` أو نزّل يدوياً:

```text
# صغير (4GB RAM)
https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf
https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf
https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf

# متوسط (8GB RAM)
https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF/resolve/main/qwen2.5-7b-instruct-q4_k_m.gguf
https://huggingface.co/bartowski/ALLaM-AI_ALLaM-7B-Instruct-preview-GGUF/resolve/main/ALLaM-AI_ALLaM-7B-Instruct-preview-Q4_K_M.gguf

# كبير (16GB RAM)
https://huggingface.co/bartowski/jais-13b-chat-GGUF/resolve/main/jais-13b-chat-Q4_K_M.gguf
```

> 📂 ضع النماذج في: `tabib-ai/Resources/Models/`

---

## 3️⃣ التحميل الآلي

```bash
# تحميل نموذج واحد (باسم مختصر)
./download-models.sh qwen3

# تحميل أفضل نموذج لحجم RAM تلقائياً
./download-models.sh auto

# عرض القائمة
./download-models.sh list
```

السكريبت يتحقق من الملف بعد التحميل (الحد الأدنى للحجم) ويعيد التسمية لاسم النموذج.

---

## 4️⃣ دليل الترميز (Quantization)

| الترميز | الحجم مقابل الأصل | الجودة | متى تختاره |
|---------|-------------------|--------|------------|
| **Q8_0** | ~50% | ممتازة (شبه الأصل) | إذا تكفي الذاكرة |
| **Q6_K** | ~44% | ممتازة | أفضل توازن إن وُجد |
| **Q5_K_M** | ~38% | جيدة جداً | ذاكرة متوسطة |
| **Q4_K_M** | ~33% | جيدة — **الموصى به افتراضياً** | 4-8 GB RAM |
| **Q4_K_S** | ~31% | جيدة | إذا لم يتوفر Q4_K_M |
| **Q3_K_M** | ~27% | مقبولة — تظهر خسائر | ذاكرة حرجة فقط |
| **Q2_K** | ~22% | ضعيفة | ❌ لا يُنصح |

**القاعدة الذهبية:** Q4_K_M نقطة التوازن المثالية للتشغيل المحلي على الأجهزة المكتبية.

---

## 5️⃣ خطة التحسين المستقبلي (Fine-tuning)

### المرحلة الأولى — تخصيص التعليمات (Prompt Tuning)
- ضبط system prompts لكل وضع طبي (موجود حالياً في `MedicalAgent`)
- إضافة أمثلة قليل-shot (few-shot) بالعربية الطبية

### المرحلة الثانية — LoRA على بيانات طبية عربية
- **الأدوات:** [unsloth](https://github.com/unslothai/unsloth) (سريع) أو `llama.cpp` finetune
- **البيانات:** مجموعات مثل PubMed Arabic، ترجمات أوصاف الحالات، أسئلة مرضى-طبيب (يعيد التحقق من الرخصة)
- **النموذج الأساسي:** Qwen2.5-7B أو ALLaM-7B (دعم عربي أصلي)
- **المتطلبات:** GPU واحد ≥16GB VRAM (مثل RTX 4090) أو Collab Pro
- **المخرجات:** أوزان LoRA → دمجها وترميزها GGUF → تكبير النموذج القائم

### المرحلة التقييم
- مجموعة اختبار عربية طبية (50-100 حالة)
- مقارنة: نموذج أساسي vs مُحسّن (دقة المصطلحات، هدم التشخيص، السلامة)

---

## 6️⃣ ملاحظات تقنية

- المحرك: `LlamaCpp` NuGet binding (llama.cpp) — تحميل مباشر للـ GGUF بدون Ollama.
- حزمة llama.cpp داخل الحزمة قديمة نسبياً (يناير 2024)؛ إذا رُفض نموذج حديث، حدّث الحزمة أو حوّل النموذج بصيغة GGUF قديمة (`convert.py`).
- GPU: يُفعَّل تلقائياً (`GpuLayerCount=999`)؛ للإجبار على CPU عيّن `gpuLayers=0` عند الاستدعاء.
- القياس: `dotnet run -- --model-info` أو من الواجهة عبر الأمر `/model`.
