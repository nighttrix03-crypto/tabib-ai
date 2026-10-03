# Tabib AI v1.1.0 — نسخة Windows مدمجة (ملف واحد للتشغيل + مثبت)

## 📥 التنزيل الرسمي
من صفحة [Releases](https://github.com/nighttrix03-crypto/tabib-ai/releases/latest):

| الملف | الحجم | يحتوي على |
|-------|-------|-----------|
| `TabibAI-Setup.exe` | 122 MB | **يحتوي `TabibAI.exe`** + مثبّت كامل + النموذج |
| `tabib-ai_1.1.0-1_amd64.deb` | 1.5 GB | نسخة Linux |
| `gemma2-2b.gguf` | 1.6 GB | النموذج |
| `TabibAI-source.zip` | 110 KB | الكود المصدري |

> ⚠️ **لا يوجد `TabibAI.exe` منفصل في الإصدار** — المثبّت يستخرجه تلقائياً أثناء التثبيت ويضعه مع اختصار على سطح المكتب وقائمة ابدأ.

## المحتويات (مجلد `merged-win/` في المستودع)
- `TabibAI.exe` (73M) — التطبيق نفسه، ملف واحد self-contained، أيقونة مميزة مدمجة.
- `TabibAI-Setup.exe` (122M) — المثبّت (يضم الـ exe).
- `tabib-ai.ico` + `tabib-ai.png` — الأيقونة المميزة (صليب طبي + نبض + شارة AI).
- `Install-TabibAI.ps1` — سكريبت تثبيت ينشئ اختصار سطح المكتب + قائمة ابدأ تلقائياً.

> ملاحظة: ملفات `.exe` في هذا المجلد **غير محفوظة في git** (أكبر من حد GitHub 100MB) — توزع عبر Release فقط.

## التشغيل السريع (بدون تثبيت)
شغّل `TabibAI-Setup.exe` — يختاره المستخدم ويستخرج التطبيق.

## التثبيت مع اختصار تلقائي (يحتاج صلاحية مسؤول)
```powershell
Right-click Install-TabibAI.ps1 > Run with PowerShell (as Admin)
```

## ملاحظة النموذج
النموذج `gemma2-2b.gguf` (1.6GB) مضمن داخل المثبّت وحزمة `.deb`.

