# Accessible Football 2026 ⚽🎧
**لعبة كرة القدم الصوتية  للمكفوفين وضعاف البصر مع تكامل لقارئ الشاشة NVDA**

---

## 🌟 نظرة عامة (Overview)
**Accessible Football 2026** هي لعبة كرة قدم تفاعلية مصممة بالكامل من الصفر للمكفوفين وضعاف البصر باللغتين العربية والإنجليزية. تعتمد اللعبة على هندسة صوتية ثلاثية الأبعاد (3D Spatial Audio)، ونظام نغمات موسيقية تكتيكية (9 Earcons)، ومنارة مرمى مجسمة (Goal Beacon)، ومحرك تعليق عربي حقيقي (رؤوف خليف)، مع تكامل لقارئ الشاشة **NVDA**.

---

## 🏗️ هيكلية المشروع (Architecture)

يتكون الحل (`AccessibleFootball2026.sln`) من المشاريع المعيارية التالية:

| المشروع | المسار | الوظيفة |
| :--- | :--- | :--- |
| **AccessibleFootball2026.Core** | `AccessibleFootball2026.Core/` | محرك محاكاة المباراة (50Hz)، فيزياء الكرة، التشكيلات التكتيكية، قواعد التحكيم والـ VAR، وقاعدة بيانات 529 فريقاً و13 بطولة (`players_database.json`). |
| **AccessibleFootball2026.Audio** | `AccessibleFootball2026.Audio/` | المنسق الصوتي الذكي (`SmartAudioCoordinator`)، منارة المرمى 3D، نغمات التنبيه التكتيكية، ومحرك التعليق التفاعلي مع حماية جمل المعلق من التقطيع (`Speech Lock`). |
| **AccessibleFootball2026.Input** | `AccessibleFootball2026.Input/` | معالج لوحة المفاتيح الفوري، تتبع أزرار التمرير والتسديد والاستخلاص وتغيير المدافع. |
| **AccessibleFootball2026.App** | `AccessibleFootball2026.App/` | واجهة المستخدم وتكامل NVDA، نظام القوائم الصوتية، أكاديمية الأصوات (Sound Academy)، الدليل التفاعلي، وإعدادات اللعبة. |
| **AccessibleFootball2026.Tests** | `AccessibleFootball2026.Tests/` | اختبارات واجهة Windows ولوحة المفاتيح والتركيز. تحتاج إلى تشغيل فعلي على Windows. |
| **AudioExtractor** | `tools/AudioExtractor/` | أداة استخراج الأصوات والتعليق العربي والأهازيج من أرشيفات PES 2013. |

---

## 🤝 خطة التعاون وتقسيم المهام (Team Collaboration Guide)

لتسهيل العمل المشترك عبر **Antigravity** ودون حدوث تعارض في الأكواد (Merge Conflicts)، تم تقسيم العمل إلى مسارين هندسيين منفصلين:

### المسار الأول: محرك المحاكاة والذكاء الاصطناعي (Simulation & Core Logic)
* **المسؤولية:**
  - تطوير وتعديل `MatchEngine.cs` (حركة اللاعبين، أوقات الاستجابة، دقة التمرير والتسديد).
  - ضبط سلوك الذكاء الاصطناعي للخصم ومستويات الصعوبة (Easy, Normal, Hard, Legendary).
  - تطوير نظام التبديلات والإنهاك البدني (Stamina & Fatigue).
  - تطوير مستشار التكتيك الذكي ومساعد الحكم بالفيديو (`AiVarReferee`).
* **الملفات الأساسية:** `AccessibleFootball2026.Core/Simulation/`, `AccessibleFootball2026.Core/Models/`

### المسار الثاني: تجربة الصوت وقارئ الشاشة والواجهات (Audio & Accessibility Experience)
* **المسؤولية:**
  - تحسين نطق قارئ الشاشة وتكامل NVDA (`NvdaScreenReader.cs`).
  - تطوير شاشات القوائم، أوضاع اللعب الجديدة (مثل دورات البطولة ووضع المدرب).
  - توسيع أكاديمية الأصوات (`SoundAcademyMenuBuilder.cs`) وإضافة شروحات تكتيكية جديدة.
  - إضافة قنوات تعليق وأصوات ملاعب إضافية.
* **الملفات الأساسية:** `AccessibleFootball2026.App/Menus/`, `AccessibleFootball2026.Audio/`, `AccessibleFootball2026.Input/`

---

## 🚀 التشغيل والتطوير (Getting Started)

### المتطلبات الأساسية (Prerequisites)
- نظام تشغيل: **Windows 10 / 11 (x64)**.
- بيئة تشغيل: **.NET 8.0 SDK** (أو أعلى).
- قارئ الشاشة: **NVDA** (يُفضل تشغيله لتجربة كامل إمكانيات اللعبة).

### 1. استنساخ المستودع (Clone)
```bash
git clone https://github.com/mo1325072-collab/AccessibleFootball2026.git
cd AccessibleFootball2026
```

### 2. بناء المشروع (Build)
```bash
dotnet build -c Release
```

### 3. تشغيل الاختبارات (Run Tests)
اختبارات المحاكاة المحمولة، مع تدقيق قاعدة البيانات الفعلية:
```bash
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
```
اختبارات واجهة Windows، على Windows فقط:
```bash
dotnet run --project AccessibleFootball2026.Tests -c Release
```
نجاح البناء أو اختبارات المحاكاة لا يثبت عمل الصوت أو NVDA أو واجهة Windows. خيار اللعبة `--test` يفحص عينات الفرق والدرافت فقط. راجع `TEST_INFRA.md` لنطاق الاختبارات والفجوات المعروفة.

### 4. تشغيل اللعبة (Run Game)
```bash
dotnet run --project AccessibleFootball2026.App -c Release
```

---

## 🎮 أزرار التحكم الأساسية (Keyboard Controls)

| المفتاح | الوظيفة في وضع الاستحواذ (الهجوم) | الوظيفة في وضع الدفاع |
| :--- | :--- | :--- |
| **الأسهم (Arrows)** | توجيه حركة اللاعب وضبط زاوية التمرير/التسديد | التحرك نحو الخصم أو الكرة |
| **Shift** | الجري السريع (Sprint) | الجري السريع للملاحقة |
| **S** | تمريرة أرضية ذكية للزميل المتاح | استخلاص هادئ للكرة (Stand Tackle) |
| **W** | تسديدة قوية نحو المرمى | استخلاص قوي / قطع هوائي |
| **A** | تمريرة بينية سريعة في المساحة (Through Ball) | انزلاق أرضي قتالي (Slide Tackle) |
| **D** | رفعة عرضية عالية (Cross / Lofted Pass) | تشتيت الكرة بعيداً |
| **Ctrl** | تبديل اللاعب المتحكم به | تبديل المدافع لأقرب لاعب للكرة (Player Switch) |
| **Q** | تمريرة بينية عالية | حسب حالة الاستحواذ |
| **Enter** | إنهاء عرض اللقطة الموقوفة بعد التعليق | إنهاء عرض اللقطة الموقوفة بعد التعليق |
| **Space** | الاستماع إلى الرادار التكتيكي ثلاثي الأبعاد وموقع الكرة | الرادار التكتيكي ومسافة حامل الكرة |
| **Esc** | إيقاف مؤقت وفتح قائمة التكتيك والتبديلات | إيقاف مؤقت وفتح قائمة التكتيك |

---

## 📜 الترخيص والمساهمة (License & Guidelines)
- المستودع خاص. لا تنشر الكود أو الأصول دون موافقة المالك ومراجعة تراخيصها.
- يُرجى إنشاء فرع جديد (`git checkout -b feature/your-feature`) قبل إضافة أي ميزة، وتشغيل الاختبارات المناسبة قبل الدمج، مع توضيح ما لم يُختبر.
