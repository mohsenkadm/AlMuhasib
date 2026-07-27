/** نصوص مضمّنة — تعمل عند فتح index.html مباشرة (file://) بدون خادم */
window.LOCALES = {
  ar: {
    meta: {
      title: "قيد — منصة محاسبة، فنادق، مطاعم، عقود وتجارة سيارات",
      description: "منصة أعمال عربية متكاملة: محاسبة، فنادق، عقود سيارات، بيع وشراء سيارات، مساعد صوتي، ربط فروع عبر الشبكة المحلية، تطبيق جوال، ومزامنة سحابية. تعمل أوفلاين بالكامل."
    },
    nav: {
      systems: "الأنظمة", features: "المنصة", how: "كيف يعمل", videos: "الفيديوهات",
      reports: "التقارير", cloud: "السحابة", network: "ربط الفروع", mobile: "التطبيق", download: "التنزيل", faq: "الأسئلة", contact: "تواصل"
    },
    support: { btn: "خدمة العملاء — واتساب", btnShort: "واتساب", float: "دعم واتساب", phoneLabel: "رقم الدعم:" },
    videos: {
      title: "دليل الفيديوهات التعليمية",
      subtitle: "شاهد شرح كل واجهة داخل النظام — نفس الدليل الموجود في التطبيق الأوفلاين",
      search: "بحث في الفيديوهات...", all: "الكل", count: "فيديو", pick: "اختر فيديو من القائمة",
      empty: "لا توجد فيديوهات مطابقة", noLink: "لم يُضبط رابط يوتيوب لهذا الفيديو بعد",
      categories: {
        dashboard: "لوحة التحكم", "master-data": "البيانات الأساسية", sales: "المبيعات",
        purchases: "المشتريات", installments: "الأقساط", finance: "المالية",
        inventory: "المخزون", reports: "التقارير", admin: "الإدارة والإعدادات"
      }
    },
    hero: {
      badge: "جديد: المساعد الصوتي قيد + تجارة السيارات",
      title: "قيد",
      subtitle: "منصة أعمال متكاملة —",
      rotateWords: ["محاسبة", "فنادق", "عقود سيارات", "تجارة سيارات"],
      desc: "أنظمة سطح مكتب + مساعد صوتي + ربط حاسبات رئيسية وفرعية عبر WiFi + تطبيق جوال + مزامنة سحابية — عربي، أوفلاين، وجاهز للنمو.",
      cta_download: "حمّل النظام مجاناً",
      cta_systems: "استكشف الأنظمة",
      cta_features: "مميزات المنصة",
      stat_systems: "أنظمة",
      stat_reports: "تقرير+",
      stat_offline: "أوفلاين",
      stat_mobile: "تطبيق جوال",
      screen_caption: "واجهة النظام الأوفلاين — لوحة التحكم"
    },
    systems: {
      title: "أنظمتنا المتكاملة",
      subtitle: "اختر نظاماً لاستكشاف ميزاته بالتفصيل",
      featuresTitle: "ميزات النظام",
      cta_download: "حمّل الآن",
      tabs: [
        {
          id: "accounting",
          label: "المحاسبة",
          badge: "الأكثر استخداماً",
          tagline: "نظام محاسبة ومبيعات احترافي",
          desc: "فواتير، POS، تسعير منتجات، مخازن، أقساط، سندات، مستثمرون، مساعد صوتي، وأكثر من 25 تقريراً — للمحلات والمخازن والشركات.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام المحاسبة — لوحة التحكم والمبيعات",
          highlights: [
            "فواتير مبيعات ومشتريات مع مرتجعات وخصومات",
            "مرتجع مشتريات ووحدات قياس ومرونة السوق",
            "تسعير منتجات — أسعار بيع وشراء متعددة",
            "بيع سريع POS — باركود ودفع نقدي فوري",
            "مخازن، أرصدة افتتاحية، ونقل بين المخازن",
            "أقساط، متابعة تحصيل، وتقارير متأخرات",
            "سندات قبض/صرف، مصاريف، وقاصات",
            "المساعد الصوتي قيد — تحكّم بالصوت (Ctrl+Space)",
            "25+ تقرير تحليلي شامل + ربط فروع عبر الشبكة"
          ],
          features: [
            { icon: "receipt", title: "فواتير متكاملة", desc: "بيع، شراء، أقساط، ومرتجعات بكل التفاصيل" },
            { icon: "pricing", title: "تسعير المنتجات", desc: "أنواع أسعار بيع وشراء مرنة" },
            { icon: "pos", title: "نقطة بيع POS", desc: "كاشير سريع مع باركود ومفضلة" },
            { icon: "warehouse", title: "المخازن", desc: "تتبع الكميات والتسويات والنقل" },
            { icon: "voice", title: "المساعد الصوتي قيد", desc: "أوامر صوتية لفتح الشاشات والبيع" },
            { icon: "chart", title: "التقارير", desc: "مبيعات، أرباح، كشوف، وموازنة" }
          ]
        },
        {
          id: "hotel",
          label: "الفندق",
          badge: "PMS كامل",
          tagline: "نظام إدارة فنادق (PMS)",
          desc: "حجوزات، تسجيل دخول/خروج، غرف، نزلاء، خطط أسعار، نظافة، صندوق، مصاريف، وتقارير إشغال — مع مطعم F&B مدمج.",
          screenshot: "assets/desktop-hotel.png",
          screenshotCaption: "نظام الفندق — لوحة الإشغال والحجوزات",
          highlights: [
            "لوحة تحكم — إشغال، وصول، مغادرة، إيرادات",
            "حجوزات + تقويم + نموذج حجز جديد",
            "Check-in / Check-out سريع",
            "غرف، أنواع، طوابق، وخطط أسعار",
            "ملفات نزلاء وتاريخ إقامات",
            "نظافة Housekeeping وإدارة حالة الغرف",
            "صندوق فندقي ومصاريف",
            "تقارير إشغال وإيرادات وتدقيق ليلي",
            "ربط فروع الاستقبال عبر WiFi/LAN"
          ],
          restaurant: {
            title: "مطعم الفندق F&B",
            highlights: [
              "كاشير POS — صالة، سفري، وخدمة غرف",
              "قائمة، مخزون مطبخ، ووصفات",
              "طاولات الصالة وشاشة مطبخ KDS",
              "تقارير ربحية F&B وربط مالي"
            ]
          },
          features: [
            { icon: "hotel", title: "الحجوزات", desc: "تقويم، حجز جديد، وإدارة كاملة" },
            { icon: "bed", title: "الغرف", desc: "حالات، أنواع، وطوابق" },
            { icon: "guest", title: "النزلاء", desc: "ملفات ضيوف وتفضيلات" },
            { icon: "pos", title: "كاشير المطعم", desc: "POS صالة وغرف وسفري" },
            { icon: "kitchen", title: "شاشة المطبخ", desc: "KDS — تحضير وتقديم" },
            { icon: "chart", title: "تقارير الفندق", desc: "إشغال، إيرادات، ومطعم" }
          ]
        },
        {
          id: "car",
          label: "عقود السيارات",
          badge: "عقود بيع",
          tagline: "نظام عقود بيع السيارات",
          desc: "عقود بيع بدولار أو سعر متفق عليه، شهود، بنود بارزة، مدفوعات، تقارير Excel، وطباعة احترافية على صفحة A4 واحدة — لمعارض ومكاتب البيع.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "نظام عقود السيارات — لوحة العقود",
          highlights: [
            "لوحة KPI — عقود اليوم، محصّل، متبقي",
            "عقد بيع — بائع، مشتري، مركبة، وشهود",
            "سعر بالدولار أو «المبلغ المتفق عليه»",
            "بنود متفق عليها مرتبة وبارزة في الطباعة",
            "طباعة A4 صفحة واحدة مع هيدر بعرض الورقة",
            "مدفوعات وأقساط العقود",
            "تقرير شامل مع تصدير Excel",
            "صلاحيات مستخدمين ونسخ احتياطي محلي",
            "ربط فروع المعرض بالحاسبة الرئيسية"
          ],
          features: [
            { icon: "car", title: "العقود", desc: "إنشاء وتتبع عقود البيع والشهود" },
            { icon: "pricing", title: "تسعير مرن", desc: "دولار أو مبلغ متفق عليه" },
            { icon: "print", title: "طباعة احترافية", desc: "صفحة واحدة، هيدر كامل، توقيعات" },
            { icon: "voucher", title: "المدفوعات", desc: "دفعات وأقساط لكل عقد" },
            { icon: "chart", title: "التقارير", desc: "تقرير العقود وتصدير Excel" },
            { icon: "cloud", title: "المزامنة", desc: "ربط سحابي مع التطبيق" }
          ]
        },
        {
          id: "carTrade",
          label: "تجارة السيارات",
          badge: "جديد",
          tagline: "نظام بيع وشراء السيارات",
          desc: "دورة كاملة: شراء → مخزون → بيع، دفعات للشراء والبيع، أطراف وكشف حساب، وتقارير ربحية — لمعارض التجارة.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "نظام تجارة السيارات — المخزون والمعاملات",
          highlights: [
            "شراء سيارات وتسجيلها في المخزون",
            "بيع من المخزون مع تتبع الحالة (مباعة / متاحة)",
            "دفعات منفصلة للشراء وللبيع",
            "أطراف (موردون / مشترين) وكشف حساب",
            "تقارير معاملات وأرباح ومخزون",
            "مزامنة سحابية مع سطح المكتب",
            "صلاحيات مستخدمين ونسخ احتياطي",
            "ربط فروع المعرض عبر WiFi/LAN"
          ],
          features: [
            { icon: "carTrade", title: "شراء وبيع", desc: "دورة مخزون كاملة للمعرض" },
            { icon: "warehouse", title: "مخزون السيارات", desc: "تتبع المتاح والمباع" },
            { icon: "voucher", title: "دفعات مزدوجة", desc: "مدفوعات شراء ومدفوعات بيع" },
            { icon: "guest", title: "الأطراف", desc: "موردون ومشترون وكشوف" },
            { icon: "chart", title: "تقارير التجارة", desc: "معاملات، أرباح، ورصيد" },
            { icon: "network", title: "ربط الفروع", desc: "حاسبة رئيسية وفرعية للمعرض" }
          ]
        },
        {
          id: "mobile",
          label: "الجوال",
          badge: "متاح الآن",
          tagline: "تطبيق جوال — محاسبة، فندق، سيارات",
          desc: "تطبيق Flutter يتصل بالـ API السحابي: تقارير، إنشاء بيانات، فواتير، حجوزات، عقود — حسب نوع نظامك.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "تطبيق قيد — لوحة التقارير",
          highlights: [
            "3 profiles: محاسبة، فندق، عقود سيارات",
            "9+ تقارير محاسبة + KPI فندقي",
            "إنشاء عملاء، منتجات، فواتير (5 خطوات)",
            "حجوزات، غرف، check-in/out للفندق",
            "عقود ومدفوعات للسيارات",
            "مطعم: كاشير وتقارير F&B",
            "عربي/إنجليزي + وضع داكن",
            "إشعارات OneSignal"
          ],
          features: [
            { icon: "chart", title: "تقارير لحظية", desc: "مبيعات، أرباح، مخزون، إشغال" },
            { icon: "receipt", title: "فواتير جوال", desc: "معالج 5 خطوات للفواتير" },
            { icon: "hotel", title: "فندق جوال", desc: "حجوزات، غرف، ووصول" },
            { icon: "car", title: "سيارات جوال", desc: "عقود ومدفوعات" },
            { icon: "cloud", title: "API آمن", desc: "JWT وmulti-tenant" },
            { icon: "shield", title: "صلاحيات", desc: "حسب حساب التطبيق" }
          ]
        }
      ]
    },
    desktop: {
      pageTitle: "لوحة التحكم",
      menu: { dashboard: "لوحة التحكم", sales: "فواتير", customers: "العملاء", warehouses: "المخازن", installments: "الأقساط", reports: "التقارير" },
      quick: { sale: "بيع", purchase: "شراء", voucher: "سند" },
      stats: { sales: "مبيعات اليوم", profit: "صافي الربح", customers: "العملاء", invoices: "فواتير" },
      chart: "ملخص المبيعات — آخر 7 أيام"
    },
    mobile: {
      badge: "متاح الآن",
      title: "تطبيق جوال — 3 أنظمة",
      desc: "تطبيق Flutter متصل بالـ API السحابي: تقارير لحظية، إنشاء فواتير وبيانات، وإدارة فندق أو عقود سيارات — من أي مكان.",
      points: [
        "محاسبة: تقارير، فواتير، عملاء، منتجات",
        "فندق: حجوزات، غرف، check-in، مطعم",
        "سيارات: عقود، مدفوعات، تقارير",
        "مزامنة آمنة JWT + multi-tenant"
      ],
      profiles: { accounting: "محاسبة", hotel: "فندق", car: "سيارات" },
      appName: "قيد", greeting: "مرحباً — بياناتك متزامنة",
      cards: { sales: "تقرير المبيعات", statement: "كشف حساب", stock: "المخزون", overdue: "حجوزات اليوم" },
      nav: { home: "الرئيسية", reports: "التقارير", data: "البيانات" },
      caption: "تطبيق قيد — iOS و Android"
    },
    features: {
      title: "مميزات المنصة المشتركة",
      subtitle: "ما يجمع كل الأنظمة — بنية تحتية موثوقة",
      items: [
        { icon: "offline", title: "100% أوفلاين", desc: "يعمل بدون إنترنت — المزامنة اختيارية" },
        { icon: "network", title: "ربط الفروع", desc: "حاسبة رئيسية + فروع عبر WiFi — اتصال مباشر بدون مزامنة" },
        { icon: "voice", title: "المساعد الصوتي قيد", desc: "تحكّم بالتطبيق بالصوت — بحث، بيع سريع، وفتح الشاشات" },
        { icon: "print", title: "طباعة احترافية", desc: "هيدر بعرض الورقة ومعاينة طباعة متقدمة" },
        { icon: "shield", title: "صلاحيات دقيقة", desc: "تحكم بكل شاشة: إضافة، تعديل، حذف، طباعة" },
        { icon: "cloud", title: "مزامنة سحابية", desc: "Push/Pull ثنائي الاتجاه — multi-tenant" },
        { icon: "backup", title: "نسخ احتياطي", desc: "نسخ واستعادة محلية بنقرة" },
        { icon: "update", title: "تحديثات تلقائية", desc: "من GitHub عبر version.json" },
        { icon: "ai", title: "تنبيهات ذكية", desc: "أقساط، مخزون، إشغال، ونظافة" },
        { icon: "lang", title: "عربي / English", desc: "RTL كامل + واجهة ثنائية اللغة" }
      ]
    },
    how: {
      title: "ابدأ في دقائق",
      steps: [
        { num: "01", title: "نزّل النظام", desc: "ملف ZIP من GitHub — Windows 10/11" },
        { num: "02", title: "اختر نظامك ونوع الحاسبة", desc: "محاسبة، فندق، عقود، أو تجارة سيارات — رئيسية أو فرعية" },
        { num: "03", title: "اعمل أوفلاين أو عبر الشبكة", desc: "قاعدة محلية أو اتصال مباشر بالحاسبة الرئيسية" }
      ]
    },
    network: {
      badge: "متاح",
      title: "ربط الحاسبات الرئيسية والفرعية",
      desc: "اربط عدة حواسيب على نفس الشبكة (WiFi أو Ethernet) بقاعدة بيانات واحدة على الحاسبة الرئيسية — بدون مزامنة وبدون إنترنت.",
      points: [
        "يدعم كل الأنظمة: محاسبة، فنادق، عقود سيارات، وبيع وشراء السيارات",
        "اكتشاف تلقائي للحاسبة الرئيسية على الشبكة المحلية",
        "رمز ربط آمن + مستخدم SQL مخصص للفروع",
        "الفرعية لا تنشئ قاعدة بيانات — اتصال مباشر فوري",
        "تعديل إعدادات الربط بسهولة من داخل التطبيق",
        "متوافق مع العملاء الحاليين — الوضع المستقل يبقى كما هو"
      ],
      diagram: {
        main: "حاسبة رئيسية",
        mainHint: "قاعدة البيانات",
        branch1: "فرع 1",
        branch2: "فرع 2",
        branch3: "فرع 3",
        caption: "اكتشاف تلقائي على الشبكة + رمز ربط آمن"
      }
    },
    cloud: {
      title: "مزامنة سحابية — متعددة الأنظمة",
      desc: "اربط Desktop مع Cloud API: محاسبة، فنادق (ومطعم)، عقود سيارات، وتجارة سيارات — Push/Pull مع عزل بيانات كل عميل.",
      points: [
        "مزامنة حسب نوع النظام (Accounting / Hotel / Car / CarTrade)",
        "فواتير، حجوزات، عقود، تجارة، ومطعم",
        "تعارضات ذكية + حذف ناعم",
        "REST API + JWT للتطبيق الجوال",
        "لوحة مطور — tenants وتراخيص",
        "Multi-tenant — عزل كامل للبيانات"
      ]
    },
    reports: {
      title: "تقارير شاملة",
      groups: [
        { label: "المحاسبة", items: ["المبيعات والمشتريات", "الأرباح", "كشف حساب", "المخزون", "الأقساط المتأخرة", "الموازنة اليومية"] },
        { label: "الفندق", items: ["الإشغال", "الإيرادات", "تدقيق ليلي", "وصول/مغادرة"] },
        { label: "المطعم", items: ["مبيعات F&B", "قنوات البيع", "أكثر الأصناف", "ربحية المطعم"] },
        { label: "عقود السيارات", items: ["تقرير العقود", "محصّل/متبقي", "تصدير Excel"] },
        { label: "تجارة السيارات", items: ["المعاملات", "المخزون", "الأرباح", "كشف الأطراف"] }
      ]
    },
    download: {
      title: "حمّل قيد الآن",
      desc: "آخر إصدار من GitHub — تحديثات تلقائية من داخل التطبيق",
      btn: "تنزيل ZIP", version: "الإصدار", size: "الحجم", date: "تاريخ الإصدار",
      req: "متطلبات: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "أسئلة شائعة",
      items: [
        { q: "هل يعمل بدون إنترنت؟", a: "نعم. كل الأنظمة أوفلاين بالكامل. الإنترنت للمزامنة السحابية والتحديثات فقط — ربط الفروع المحلي لا يحتاج إنترنت." },
        { q: "كيف أربط فرعاً بالحاسبة الرئيسية؟", a: "عند التنصيب اختر «حاسبة فرعية»، ابحث عن الرئيسية على الشبكة أو أدخل IP، ثم أدخل رمز الربط. يمكن تعديل الإعدادات لاحقاً من «ربط الحاسبات»." },
        { q: "هل ربط الفروع يحتاج مزامنة؟", a: "لا. الفرعية تتصل مباشرة بقاعدة البيانات على الرئيسية عبر WiFi/LAN — مثل عدة مستخدمين على نفس السيرفر." },
        { q: "كيف أختار النظام المناسب؟", a: "عند الإعداد الأول أو من لوحة المطور: محاسبة للمحلات، فندق للضيافة، عقود سيارات للمعارض، وتجارة سيارات لدورة الشراء والبيع." },
        { q: "ما الفرق بين عقود السيارات وتجارة السيارات؟", a: "عقود السيارات لإبرام عقود بيع بين بائع ومشتري مع طباعة وشهود. تجارة السيارات لإدارة مخزون المعرض: شراء ثم بيع مع دفعات وتقارير." },
        { q: "ما هو المساعد الصوتي قيد؟", a: "ميزة صوتية على سطح المكتب (Ctrl+Space) للبحث وفتح الشاشات وتنفيذ أوامر مثل البيع السريع دون الكتابة." },
        { q: "هل الفندق يشمل المطعم؟", a: "نعم — POS، KDS، مخزون مطبخ، طاولات، وتقارير F&B مدمجة في نظام الفندق." },
        { q: "هل التطبيق الجوال جاهز؟", a: "نعم — يدعم profiles حسب نوع حسابك: محاسبة، فندق، أو عقود سيارات." },
        { q: "كيف أحدّث النظام؟", a: "من داخل التطبيق — يقرأ version.json من GitHub." },
        { q: "هل البيانات آمنة؟", a: "نسخ احتياطي، صلاحيات، سجل تدقيق، وعزل multi-tenant في السحابة." }
      ]
    },
    contact: { title: "جاهز للتجربة؟", desc: "نزّل مجاناً وجرّب النظام المناسب لنشاطك", github: "المستودع على GitHub" },
    footer: { rights: "جميع الحقوق محفوظة — قيد" }
  },
  en: {
    meta: {
      title: "Qayd — Accounting, Hotels, Car Contracts & Car Trading",
      description: "Integrated Arabic business platform: accounting, hotels, car contracts, car trading, voice assistant, LAN branch linking, mobile app, and cloud sync. Fully offline."
    },
    nav: {
      systems: "Systems", features: "Platform", how: "How it works", videos: "Videos",
      reports: "Reports", cloud: "Cloud", network: "Branch linking", mobile: "Mobile", download: "Download", faq: "FAQ", contact: "Contact"
    },
    support: { btn: "Customer support — WhatsApp", btnShort: "WhatsApp", float: "WhatsApp support", phoneLabel: "Support number:" },
    videos: {
      title: "Video tutorials", subtitle: "Walkthrough of every screen — same as the offline app",
      search: "Search videos...", all: "All", count: "videos", pick: "Pick a video from the list",
      empty: "No matching videos", noLink: "YouTube link not configured yet",
      categories: {
        dashboard: "Dashboard", "master-data": "Master data", sales: "Sales",
        purchases: "Purchases", installments: "Installments", finance: "Finance",
        inventory: "Inventory", reports: "Reports", admin: "Admin & settings"
      }
    },
    hero: {
      badge: "New: Qayd voice assistant + car trading",
      title: "Qayd",
      subtitle: "Integrated business platform —",
      rotateWords: ["Accounting", "Hotels", "Car contracts", "Car trading"],
      desc: "Desktop systems + voice assistant + main/branch LAN linking over WiFi + mobile app + cloud sync — Arabic, offline-first, built to scale.",
      cta_download: "Download free",
      cta_systems: "Explore systems",
      cta_features: "Platform features",
      stat_systems: "systems",
      stat_reports: "reports+",
      stat_offline: "offline",
      stat_mobile: "mobile app",
      screen_caption: "Offline desktop — dashboard"
    },
    systems: {
      title: "Our integrated systems",
      subtitle: "Pick a system to explore its features",
      featuresTitle: "System features",
      cta_download: "Download now",
      tabs: [
        {
          id: "accounting", label: "Accounting", badge: "Most popular",
          tagline: "Professional accounting & sales",
          desc: "Invoices, POS, product pricing, warehouses, installments, vouchers, investors, voice assistant, and 25+ reports — for shops and SMBs.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Accounting — dashboard & sales",
          highlights: [
            "Sales & purchase invoices with returns",
            "Purchase returns, units of measure & flexible market tools",
            "Product pricing — multiple sell/buy price types",
            "Quick POS — barcode & instant cash",
            "Warehouses, opening balances, transfers",
            "Installments & overdue reports",
            "Receipts, expenses, cash boxes",
            "Qayd voice assistant — hands-free control (Ctrl+Space)",
            "25+ analytical reports + LAN branch linking"
          ],
          features: [
            { icon: "receipt", title: "Full invoicing", desc: "Sales, purchases, installments, returns" },
            { icon: "pricing", title: "Product pricing", desc: "Flexible sell & buy price types" },
            { icon: "pos", title: "POS", desc: "Fast cashier with barcode" },
            { icon: "warehouse", title: "Warehouses", desc: "Stock tracking & transfers" },
            { icon: "voice", title: "Qayd voice assistant", desc: "Voice commands for screens & sales" },
            { icon: "chart", title: "Reports", desc: "Sales, profit, statements" }
          ]
        },
        {
          id: "hotel", label: "Hotel", badge: "Full PMS",
          tagline: "Hotel management system (PMS)",
          desc: "Reservations, check-in/out, rooms, guests, rate plans, housekeeping, cash, expenses, occupancy reports — plus integrated F&B.",
          screenshot: "assets/desktop-hotel.png",
          screenshotCaption: "Hotel — occupancy & reservations",
          highlights: [
            "Dashboard — occupancy, arrivals, revenue",
            "Reservations + calendar + new booking",
            "Fast check-in / check-out",
            "Rooms, types, floors, rate plans",
            "Guest profiles & stay history",
            "Housekeeping & room status",
            "Hotel cash & expenses",
            "Occupancy, revenue & night audit"
          ],
          restaurant: {
            title: "Hotel restaurant F&B",
            highlights: [
              "POS — dine-in, takeaway, room service",
              "Menu, kitchen inventory & recipes",
              "Tables & kitchen display KDS",
              "F&B profitability & financial posting"
            ]
          },
          features: [
            { icon: "hotel", title: "Reservations", desc: "Calendar, booking, management" },
            { icon: "bed", title: "Rooms", desc: "Status, types, floors" },
            { icon: "guest", title: "Guests", desc: "Profiles & preferences" },
            { icon: "pos", title: "Restaurant POS", desc: "Dine-in, rooms, takeaway" },
            { icon: "kitchen", title: "Kitchen display", desc: "KDS — prep & serve" },
            { icon: "chart", title: "Hotel reports", desc: "Occupancy, revenue, F&B" }
          ]
        },
        {
          id: "car", label: "Car contracts", badge: "Sales contracts",
          tagline: "Car sales contracts system",
          desc: "USD or agreed-price contracts, witnesses, highlighted terms, payments, Excel reports, and single-page A4 professional print — for dealerships.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "Car contracts — dashboard",
          highlights: [
            "KPI dashboard — today, collected, remaining",
            "New contract — seller, buyer, vehicle, witnesses",
            "USD price or agreed amount",
            "Organized bold contract terms on print",
            "Single-page A4 print with full-bleed header",
            "Payments & installments",
            "Full report with Excel export",
            "User permissions & local backup",
            "LAN branch linking for showrooms"
          ],
          features: [
            { icon: "car", title: "Contracts", desc: "Create & track sales with witnesses" },
            { icon: "pricing", title: "Flexible pricing", desc: "USD or agreed price" },
            { icon: "print", title: "Pro printing", desc: "One page, header, signatures" },
            { icon: "voucher", title: "Payments", desc: "Per-contract payments" },
            { icon: "chart", title: "Reports", desc: "Contracts & Excel export" },
            { icon: "cloud", title: "Cloud sync", desc: "Mobile & API ready" }
          ]
        },
        {
          id: "carTrade", label: "Car trading", badge: "New",
          tagline: "Buy & sell cars system",
          desc: "Full cycle: purchase → inventory → sale, dual payments, parties & statements, profitability reports — for trading showrooms.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "Car trading — stock & transactions",
          highlights: [
            "Purchase cars into inventory",
            "Sell from stock with available/sold status",
            "Separate purchase and sale payments",
            "Parties (suppliers / buyers) & statements",
            "Transaction, profit & stock reports",
            "Cloud sync with desktop",
            "User permissions & local backup",
            "LAN branch linking for the showroom"
          ],
          features: [
            { icon: "carTrade", title: "Buy & sell", desc: "Full showroom inventory cycle" },
            { icon: "warehouse", title: "Car stock", desc: "Track available and sold" },
            { icon: "voucher", title: "Dual payments", desc: "Purchase and sale payments" },
            { icon: "guest", title: "Parties", desc: "Suppliers, buyers, statements" },
            { icon: "chart", title: "Trade reports", desc: "Deals, profit, balances" },
            { icon: "network", title: "Branch linking", desc: "Main & branch showroom PCs" }
          ]
        },
        {
          id: "mobile", label: "Mobile", badge: "Available now",
          tagline: "Mobile app — accounting, hotel, cars",
          desc: "Flutter app on cloud API: reports, data entry, invoices, reservations, contracts — by your system type.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "Qayd mobile — reports hub",
          highlights: [
            "3 profiles: accounting, hotel, car contracts",
            "9+ accounting reports + hotel KPIs",
            "Create customers, products, 5-step invoices",
            "Reservations, rooms, check-in for hotel",
            "Contracts & payments for cars",
            "Restaurant POS & F&B reports",
            "Arabic/English + dark mode",
            "OneSignal notifications"
          ],
          features: [
            { icon: "chart", title: "Live reports", desc: "Sales, profit, stock, occupancy" },
            { icon: "receipt", title: "Mobile invoices", desc: "5-step invoice wizard" },
            { icon: "hotel", title: "Hotel mobile", desc: "Bookings, rooms, check-in" },
            { icon: "car", title: "Cars mobile", desc: "Contracts & payments" },
            { icon: "cloud", title: "Secure API", desc: "JWT multi-tenant" },
            { icon: "shield", title: "Permissions", desc: "Per app account" }
          ]
        }
      ]
    },
    desktop: {
      pageTitle: "Dashboard",
      menu: { dashboard: "Dashboard", sales: "Invoices", customers: "Customers", warehouses: "Warehouses", installments: "Installments", reports: "Reports" },
      quick: { sale: "Sale", purchase: "Purchase", voucher: "Voucher" },
      stats: { sales: "Today's sales", profit: "Net profit", customers: "Customers", invoices: "Invoices" },
      chart: "Sales summary — last 7 days"
    },
    mobile: {
      badge: "Available now",
      title: "Mobile app — 3 systems",
      desc: "Flutter app on cloud API: live reports, invoice creation, hotel or car management — anywhere.",
      points: [
        "Accounting: reports, invoices, customers, products",
        "Hotel: reservations, rooms, check-in, restaurant",
        "Cars: contracts, payments, reports",
        "Secure JWT + multi-tenant sync"
      ],
      profiles: { accounting: "Accounting", hotel: "Hotel", car: "Cars" },
      appName: "Qayd", greeting: "Welcome — data synced",
      cards: { sales: "Sales report", statement: "Statement", stock: "Stock", overdue: "Today's bookings" },
      nav: { home: "Home", reports: "Reports", data: "Data" },
      caption: "Qayd app — iOS & Android"
    },
    features: {
      title: "Shared platform features",
      subtitle: "What powers every system — reliable infrastructure",
      items: [
        { icon: "offline", title: "100% offline", desc: "Works without internet — sync optional" },
        { icon: "network", title: "Branch linking", desc: "Main PC + branches over WiFi — direct DB, no sync" },
        { icon: "voice", title: "Qayd voice assistant", desc: "Control the app by voice — search, quick sale, open screens" },
        { icon: "print", title: "Pro printing", desc: "Full-bleed headers and advanced print preview" },
        { icon: "shield", title: "Fine permissions", desc: "Per-screen add, edit, delete, print" },
        { icon: "cloud", title: "Cloud sync", desc: "Two-way Push/Pull — multi-tenant" },
        { icon: "backup", title: "Backup", desc: "Local backup & restore" },
        { icon: "update", title: "Auto updates", desc: "From GitHub via version.json" },
        { icon: "ai", title: "Smart alerts", desc: "Installments, stock, occupancy" },
        { icon: "lang", title: "Arabic / English", desc: "Full RTL + bilingual UI" }
      ]
    },
    how: {
      title: "Get started in minutes",
      steps: [
        { num: "01", title: "Download", desc: "ZIP from GitHub — Windows 10/11" },
        { num: "02", title: "Pick system & PC role", desc: "Accounting, hotel, contracts, or car trading — main or branch PC" },
        { num: "03", title: "Work offline or on LAN", desc: "Local database or direct link to main server" }
      ]
    },
    network: {
      badge: "Available",
      title: "Main & branch PC linking",
      desc: "Connect multiple PCs on the same network (WiFi or Ethernet) to one database on the main computer — no sync, no internet required.",
      points: [
        "Works for all systems: accounting, hotels, car contracts, car trading",
        "Auto-discover the main PC on your local network",
        "Secure pairing code + dedicated SQL user for branches",
        "Branch PCs never create a local database — instant direct access",
        "Edit connection settings anytime in the app",
        "Fully backward compatible — existing standalone installs unchanged"
      ],
      diagram: {
        main: "Main PC",
        mainHint: "Database host",
        branch1: "Branch 1",
        branch2: "Branch 2",
        branch3: "Branch 3",
        caption: "Network auto-discovery + secure pairing code"
      }
    },
    cloud: {
      title: "Cloud sync — multi-system",
      desc: "Connect Desktop to Cloud API: accounting, hotels (& restaurant), car contracts, and car trading — Push/Pull with tenant isolation.",
      points: [
        "Sync by system type (Accounting / Hotel / Car / CarTrade)",
        "Invoices, reservations, contracts, trading, restaurant",
        "Smart conflicts + soft delete",
        "REST API + JWT for mobile",
        "Developer admin — tenants & licenses",
        "Multi-tenant data isolation"
      ]
    },
    reports: {
      title: "Comprehensive reports",
      groups: [
        { label: "Accounting", items: ["Sales & purchases", "Profit", "Statements", "Inventory", "Overdue installments", "Daily balance"] },
        { label: "Hotel", items: ["Occupancy", "Revenue", "Night audit", "Arrivals/departures"] },
        { label: "Restaurant", items: ["F&B sales", "Channels", "Top items", "F&B profit"] },
        { label: "Car contracts", items: ["Contracts report", "Collected/remaining", "Excel export"] },
        { label: "Car trading", items: ["Transactions", "Stock", "Profit", "Party statements"] }
      ]
    },
    download: {
      title: "Download Qayd", desc: "Latest from GitHub — in-app auto-update",
      btn: "Download ZIP", version: "Version", size: "Size", date: "Release date",
      req: "Requirements: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "FAQ",
      items: [
        { q: "Works offline?", a: "Yes. All systems are fully offline. Internet is for cloud sync and updates only — LAN branch linking needs no internet." },
        { q: "How to link a branch PC?", a: "At setup choose Branch PC, discover the main server on your network or enter its IP, then enter the pairing code. Change settings anytime under Network Linking." },
        { q: "Does branch linking use sync?", a: "No. Branch PCs connect directly to the main database over WiFi/LAN — like multiple users on one SQL Server." },
        { q: "How to pick a system?", a: "At first setup or via admin: accounting for retail, hotel for hospitality, car contracts for dealership paperwork, car trading for buy→stock→sell." },
        { q: "Contracts vs car trading?", a: "Contracts formalize a sale between seller and buyer with print and witnesses. Car trading manages showroom inventory: purchase then sell with payments and reports." },
        { q: "What is the Qayd voice assistant?", a: "A desktop voice feature (Ctrl+Space) to search, open screens, and run actions like quick sale without typing." },
        { q: "Does hotel include restaurant?", a: "Yes — POS, KDS, kitchen stock, tables, and F&B reports are built in." },
        { q: "Is mobile ready?", a: "Yes — profiles by account type: accounting, hotel, or car contracts." },
        { q: "How to update?", a: "In-app check reads version.json from GitHub." },
        { q: "Data safe?", a: "Backup, permissions, audit log, multi-tenant cloud isolation." }
      ]
    },
    contact: { title: "Ready to try?", desc: "Download free and test your system", github: "GitHub repository" },
    footer: { rights: "All rights reserved — Qayd" }
  }
};
