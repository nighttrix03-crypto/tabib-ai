import json, urllib.request, sys, time, os

# نظام التعامل الذكي مع النماذج - يحل مشكلة نفاذ الذاكرة والتوقف على الأجهزة ذات الموارد المحدودة

OLLAMA_URL = os.environ.get("TABIB_OLLAMA_URL", "http://127.0.0.1:11435").rstrip("/")

# قائمة طبقات النماذج حسب الأفضلية والتخزين
MODEL_HIERARCHY = [
    # الطبقة الأولى - الأفضل للتشخيص الطبي (تحتاج ≥8 جيجابايت رام)
    {"name": "medgemma1.5:latest", "size_gb": 3.3, "min_ram_mb": 8000, "priority": 1},
    
    # الطبقة الثانية - توازن جيد (تحتاج 2-8 جيجابايت رام)
    {"name": "gemma:2b", "size_gb": 1.3, "min_ram_mb": 4000, "priority": 2},
    {"name": "gemma3:270m", "size_gb": 0.2, "min_ram_mb": 2000, "priority": 3},
    
    # الطبقة الثالثة - أساسي للتوجيه (أي جهاز تقريبًا)
    {"name": "qwen:0.5b", "size_gb": 0.4, "min_ram_mb": 512, "priority": 3}
];

# دوال كشف قدرات الجهاز
import psutil

def get_system_info():
    """الحصول على معلومات نظام المستخدم"""
    memory = psutil.virtual_memory()
    cpu_count = psutil.cpu_count()
    return {
        "total_ram_mb": memory.total // (1024 * 1024),
        "available_ram_mb": memory.available // (1024 * 1024),
        "cpu_count": cpu_count,
        "cpu_percent": psutil.cpu_percent(interval=0.1)
    };

def get_installed_models():
    """قراءة النماذج المثبتة فعلياً من السيرفر (GET /api/tags)."""
    try:
        with urllib.request.urlopen(f"{OLLAMA_URL}/api/tags", timeout=10) as resp:
            data = json.loads(resp.read().decode("utf-8"))
        return {m.get("name", "") for m in data.get("models", [])}
    except Exception as e:
        print(f"⚠️ تعذّر قراءة قائمة النماذج المثبتة: {str(e)[:100]}")
        return set()

def model_is_installed(name, installed):
    """يتحقق من وجود النموذج (يدعم صيغة :latest)."""
    return any(m == name or m.startswith(name + ":") or name.startswith(m + ":") or m.startswith(name)
               for m in installed if m)

def select_optimal_model():
    """اختيار أنسب نموذج لقدرات الجهاز الحالية"""
    try:
        system_info = get_system_info();
        print(f"📊 معلومات الجهاز: {system_info['total_ram_mb']:,} ميجابايت رام إجمالي ({system_info['available_ram_mb']:,} ميجابايت متاح الآن)");

        installed = get_installed_models();
        print(f"📦 النماذج المثبتة: {', '.join(sorted(installed)) or 'لا شيء'}");

        models_tried = [];

        for model in MODEL_HIERARCHY:
            # بوابة المستوى: صنف الجهاز (الرام الإجمالي) كما في مواصفات الطبقة
            if system_info["total_ram_mb"] < model["min_ram_mb"]:
                print(f"⏭️ تخطي {model['name']} - صنف الجهاز أصغر من الطبقة (الإجمالي {system_info['total_ram_mb']:,} < {model['min_ram_mb']:,} ميجابايت)");
                models_tried.append({"name": model["name"], "status": "skipped", "reason": "جهاز أصغر من متطلبات الطبقة"});
                continue;

            # النموذج يجب أن يكون مثبتاً فعلاً على السيرفر
            if not model_is_installed(model["name"], installed):
                print(f"⏭️ تخطي {model['name']} - غير مثبت (نفّذ: ollama pull {model['name']})");
                models_tried.append({"name": model["name"], "status": "skipped", "reason": "غير مثبت"});
                continue;

            # بوابة الأمان: RAM المتاح الآن يجب أن يكفي حجم النموذج + هامش 40%
            required_mb = int(model["size_gb"] * 1024 * 1.4);
            if system_info["available_ram_mb"] < required_mb:
                print(f"⏭️ تخطي {model['name']} - RAM المتاح لا يكفي للتحميل الآن ({system_info['available_ram_mb']:,} < {required_mb:,} ميجابايت)");
                models_tried.append({"name": model["name"], "status": "skipped", "reason": "RAM متاح غير كافٍ"});
                continue;

            print(f"✅ اختيار {model['name']} (الطبقة {model['priority']}, الإجمالي ≥{model['min_ram_mb']:,} والمتاح ≥{required_mb:,} ميجابايت)");
            models_tried.append({"name": model["name"], "status": "selected", "layer": model["priority"]});
            return model["name"];

        # fallback - أصغر نموذج مثبت فعلاً
        for model in reversed(MODEL_HIERARCHY):
            if model_is_installed(model["name"], installed):
                print(f"⚠️ كل الطبقات متجاوزة - fallback إلى {model['name']} (الطبقة {model['priority']})");
                return model["name"];

        print("🚨 لا يوجد أي نموذج مثبت على السيرفر");
        return MODEL_HIERARCHY[-1]["name"]; # محاولة أصغر نموذج كملاذ أخير

    except Exception as e:
        print(f"🚨 خطأ في اختيار النموذج: {str(e)[:100]}... استخدام fallback");
        return MODEL_HIERARCHY[-1]["name"]; # أصغر نموذج كملاذ أخير

PROMPT_SYSTEM = (
    "أنت مساعد معلومات صحية تثقيفي باللغة العربية، لا طبيب ولا نظام تشخيص معتمد. "
    "اتبع مبدأ السلامة: عند علامة خطر محتملة، ابدأ بتوجيه واضح لطلب المساعدة العاجلة ولا تؤخره بالأسئلة. "
    "لا تقدّم تشخيصاً نهائياً، ولا خطة علاج، ولا وصفة أو جرعات دوائية. لا تقل إن الحالة سليمة أو خطيرة على نحو جازم من بيانات ناقصة. "
    "لا تختلق نتائج أو مراجع أو نسب دقة. إذا تعذّر الاستنتاج فقل ذلك صراحة. حافظ على السرية ولا تطلب اسم المريض أو رقم هويته. "
    "الإسعافات الأولية غير الدوائية مسموحة ومطلوبة عند الطوارئ ولا تعدّ وصفة علاجية. "
    "عند وجود خطر فوري أو عند بُعد المسافة عن المستشفى أو صعوبة الوصول إليه: لا تكتفِ بجملة «راجع الطبيب» أو «استشر مختصاً» وحدها. "
    "قدّم أولاً الإسعافات الأولية الآمنة خطوة بخطوة (ما يمكن فعله الآن وما يجب تجنّبه)، ثم وجّه المستخدم للاتصال بالإسعاف "
    "ورقم الطوارئ المحلي في بلده (مثال: 1122 في الجزائر، 15 في المغرب، 997 في السعودية، 123 في مصر، 998 في الإمارات) "
    "إلى أقرب مركز صحي، مع شرح ما يجب فعله أثناء الانتظار أو النقل. وضّح أن ذلك إسعاف أولي لا يغني عن تقييم مختص. "
    "لا تبدأ الرد بالنصيحة العامة عند وجود إجراء عاجل آمن يمكن تنفيذه فوراً. "
    "قدّم ملخصاً تعليمياً منظماً للمعلومات الصحية، واذكر احتمالات عامة لا تشخيصاً، والأسئلة المفيدة لطرحها على المختص.\n\n"
    "نسّق الرد بعناوين قصيرة: ١) ملخص المعلومات، ٢) ما يمكن ملاحظته أو فهمه، ٣) احتمالات عامة مع مستوى يقين منخفض أو متوسط فقط، "
    "٤) معلومات ناقصة وأسئلة للمختص، ٥) متى يلزم طلب مساعدة عاجلة إن كان السياق يدعم ذلك."
)

CASE = (
    "مجال المساعدة: علامات الخطر والطوارئ.\nالعمر: 30.\n"
    "سبب الاستشارة والأعراض:\nحادث سيارة في منطقة نائية. أنا مع صديقي المصاب. أقرب مستسفى يبعد ساعتين بالسيارة "
    "ولا يوجد إسعاف في المحيط. المصاب فاقد الوعي لكنه يتنفس، ونزيف قوي من ساقه اليمنى عند الكسر المحتمل. "
    "ما الذي يجب أن أفعله الآن بالضبط خطوة بخطوة؟\n\n"
    "أجب باللغة العربية الواضحة. اذكر ما تدعمه المعلومات وما لا يمكن استنتاجه منها."
)

# اختيار النموذج الذكي
OPTIMAL_MODEL = select_optimal_model()

body = {
    "model": OPTIMAL_MODEL,
    "messages": [
        {"role": "system", "content": PROMPT_SYSTEM},
        {"role": "user", "content": CASE}
    ],
    "stream": False,
    "keep_alive": "10m",
    "options": {"temperature": 0.15, "num_ctx": 8192, "num_thread": 4, "num_predict": 480}
}

started = time.time()
req = urllib.request.Request(
    OLLAMA_URL + "/api/chat",
    data=json.dumps(body, ensure_ascii=False).encode("utf-8"),
    headers={"Content-Type": "application/json"},
    method="POST")
try:
    with urllib.request.urlopen(req, timeout=2400) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    answer = data.get("message", {}).get("content", "")
    with open("/tmp/emergency-answer.txt", "w", encoding="utf-8") as fh:
        fh.write(answer)
    print(f"OK elapsed={time.time()-started:.1f}s chars={len(answer)}")
except Exception as exc:
    with open("/tmp/emergency-answer.txt", "w", encoding="utf-8") as fh:
        fh.write("ERROR: " + repr(exc))
    print("ERROR:", repr(exc))
    sys.exit(1)
