# Tabib AI v1.1.0 — نسخة Windows مدمجة (ملف واحد للتشغيل + مثبت)

## المحتويات
- `TabibAI.exe` (73M) — التطبيق نفسه، ملف واحد self-contained، أيقونة مميزة مدمجة.
- `tabib-ai.ico` + `tabib-ai.png` — الأيقونة المميزة (صليب طبي + نبض + شارة AI).
- `Install-TabibAI.ps1` — سكريبت تثبيت ينشئ اختصار سطح المكتب + قائمة ابدأ تلقائياً.

## التشغيل السريع (بدون تثبيت)
نقرة مزدوجة على `TabibAI.exe`.

## التثبيت مع اختصار تلقائي (يحتاج صلاحية مسؤول)
```powershell
Right-click Install-TabibAI.ps1 > Run with PowerShell (as Admin)
```

## ملاحظة النموذج
النموذج `gemma2-2b.gguf` (1.6GB) لا يُضمَّن في exe لتقليل الحجم — يُنزَّل عند أول تشغيل
عبر المثبت `installer/dist/TabibAI-Setup.exe` أو يوضع يدوياً بجانب الـ exe في مجلد `Models/`.
