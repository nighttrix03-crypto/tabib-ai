# طبيب AI | Tabib AI

مساعد صحي عربي للتثقيف والمناقشة. يعمل تطبيق Windows محلياً مع MedGemma عبر Ollama، ولا يحتاج إلى مفتاح API.

## 📦 هيكل المستودع

| المسار | الوصف |
|--------|-------|
| [`tabib-ai/`](tabib-ai/) | **المشروع الكامل** — كود المصدر لكل المنصات |
| [`tabib-ai/linux/`](tabib-ai/linux/) | نسخة Linux (Avalonia UI، .NET 10) |
| [`tabib-ai/installer/`](tabib-ai/installer/) | مثبّت Windows (WinForms) |
| [`tabib-ai/merged-win/`](tabib-ai/merged-win/) | نسخة Windows المدمجة + سكريبت اختصار تلقائي |
| [`tabib-ai/deb-package/`](tabib-ai/deb-package/) | حزمة Debian مع أيقونة واختصار سطح مكتب تلقائي |
| [`tabib-ai/MODELS.md`](tabib-ai/MODELS.md) | دليل النماذج والترميز |
| [`FIX_SUMMARY.md`](FIX_SUMMARY.md) | توثيق إصلاح ثغرة `SelectedModelOption` |
| [`verify_fix.py`](verify_fix.py) | سكريبت التحقق من الإصلاح |

## تنزيل Windows

نزّل أحدث إصدار من صفحة [Releases](https://github.com/nighttrix03-crypto/tabib-ai/releases/latest) وشغّل `TabibAI-Setup.exe`. يثبت المعالج التطبيق في حساب المستخدم، وينشئ اختصاراً على سطح المكتب وقائمة ابدأ، وينزّل Ollama الرسمي ويتحقق من توقيعه، ثم ينزّل MedGemma بعد موافقة المستخدم على شروط Google.

يلزم Windows x64 واتصال بالإنترنت أثناء الإعداد الأول وتخزين إضافي للنموذج بحجم يقارب 3.3 GB. بعد الإعداد، يجري التحليل محلياً على جهاز المستخدم. لا تُرسل الحالات إلى خادم هذا المشروع.

## التحقق من التنزيل

تحقق من سلامة المثبّت بعد تنزيله:

```powershell
Get-FileHash .\TabibAI-Setup.exe -Algorithm SHA256
```

قارن الناتج بسطر `TabibAI-Setup.exe` في ملف `SHA256SUMS-v1.1.0.txt` المرفق مع الإصدار.

> **ملاحظة:** المثبّت `TabibAI-Setup.exe` **يحتوي على `TabibAI.exe`** (يُستخرج تلقائياً أثناء التثبيت)، لذلك لا يوجد ملف EXE منفصل في الإصدار. يستخرج المثبّت التطبيق ويضعه مع اختصار على سطح المكتب وقائمة ابدأ.

## تنزيل Linux (Debian / Ubuntu / Mint)

نزّل `tabib-ai_1.1.0-1_amd64.deb` من صفحة [Releases](https://github.com/nighttrix03-crypto/tabib-ai/releases/latest) ثم:

```bash
sudo dpkg -i tabib-ai_1.1.0-1_amd64.deb
sudo apt-get install -f
```

الاختصار على سطح المكتب وقائمة التطبيقات يُنشأ **تلقائياً** بعد التثبيت، مع أيقونة طبية مميزة.

نموذج `gemma2-2b.gguf` (1.6 GB) مضمن داخل حزمة `.deb` ومتوفر أيضاً كأصل مستقل في نفس الإصدار.

## الموقع

الموقع التعريفي: [طبيب AI](https://nighttrix03-crypto.github.io/tabib-ai/).

## المزايا

- مجالات طبية تشمل الباطنة والقلب والأعصاب والأطفال وصحة المرأة والأشعة والمختبر والتغذية والأدوية.
- إدخال الأعراض والبيانات الصحية والفحوصات، ومحادثة متابعة ضمن الجلسة.
- استيراد صور وملفات DICOM `.dcm` أو `.ima` كشرائح منفردة، وتقارير نصية وCSV.
- تصدير الرد كتقرير نصي.
- تضمّن حزمة الإعداد التطبيق ذاته؛ وتثبت Ollama وتُنزّل MedGemma على جهاز المستخدم. أوزان MedGemma ليست مضمنة في المستودع أو المثبّت.

## حدود وسلامة

هذا التطبيق ليس جهازاً طبياً أو أداة تشخيص معتمدة. قد يخطئ النموذج، ولم يُتحقق من صلاحيته سريرياً لهذا التطبيق. لا تستخدم مخرجاته وحدها للتشخيص أو العلاج؛ راجع مختصاً صحياً. لا تضع أسماء المرضى أو أرقام هوياتهم، واحذف البيانات التعريفية من ملفات DICOM قبل استيرادها. استيراد شريحة DICOM لا يعني قراءة سلسلة CT/MRI كاملة أو الاتصال بجهاز الأشعة/PACS.

## شروط MedGemma

MedGemma من Google HAI-DEF. راجع [شروط الاستخدام](https://developers.google.com/health-ai-developer-foundations/terms) و[حدود النموذج](https://developers.google.com/health-ai-developer-foundations/medgemma/model-card)؛ يطلب المُثبّت موافقة المستخدم قبل تنزيل الأوزان من Ollama. لا يتضمن هذا المشروع أوزان النموذج. إشعار HAI-DEF في [NOTICE-HAI-DEF.txt](tabib-ai/NOTICE-HAI-DEF.txt).

## بناء Linux (Debian / Ubuntu / Mint)

```bash
sudo dpkg -i tabib-ai_1.1.0-1_amd64.deb
sudo apt-get install -f     # لحل التبعيات إن لزم
```

الاختصار على سطح المكتب وقائمة التطبيقات **يُنشأ تلقائياً** بعد التثبيت، مع أيقونة طبية مميزة.

## بناء Windows

يتطلب .NET SDK 10 وWindows PowerShell. شغّل من داخل `tabib-ai/`:

```powershell
.\build-installer.ps1
```

ينتج `installer/dist/TabibAI-Setup.exe`. لدفع إصدار إلى GitHub Releases، أنشئ وادفع وسم إصدار مثل `v1.0.0`؛ سير البناء ينشر المثبّت تلقائياً. يرفع الموقع تلقائياً من مجلد `site/` عند دفع التغييرات إلى `main`.
