/** نصوص مضمّنة — تعمل عند فتح index.html مباشرة (file://) بدون خادم */
window.LOCALES = {
  ar: {
    meta: {
      title: "المحاسب — منصة محاسبة، فنادق، مطاعم، وعقود وبيع سيارات",
      description: "منصة أعمال عربية متكاملة: محاسبة ومبيعات، إدارة فنادق ومطاعم، عقود وبيع وشراء سيارات، تطبيق جوال، ومزامنة سحابية. تعمل أوفلاين بالكامل."
    },
    nav: {
      systems: "الأنظمة", features: "المنصة", how: "كيف يعمل", videos: "الفيديوهات",
      reports: "التقارير", cloud: "السحابة", mobile: "التطبيق", download: "التنزيل", faq: "الأسئلة", contact: "تواصل"
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
      badge: "منصة متعددة الأنظمة",
      title: "المحاسب",
      subtitle: "منصة أعمال متكاملة —",
      rotateWords: ["محاسبة", "فنادق", "سيارات", "بيع وشراء"],
      desc: "ثلاثة أنظمة سطح مكتب + تطبيق جوال + مزامنة سحابية — عربي، أوفلاين، وجاهز للنمو.",
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
          desc: "فواتير، POS، مخازن، أقساط، سندات، مستثمرون، وأكثر من 25 تقريراً — للمحلات والمخازن والشركات.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام المحاسبة — لوحة التحكم والمبيعات",
          highlights: [
            "فواتير مبيعات ومشتريات مع مرتجعات وخصومات",
            "بيع سريع POS — باركود ودفع نقدي فوري",
            "مخازن، أرصدة افتتاحية، وتسويات",
            "أقساط، متابعة تحصيل، وتقارير متأخرات",
            "سندات قبض/صرف، مصاريف، وقاصات",
            "مستثمرون وتوزيع أرباح",
            "مساعد ذكي — تنبيهات مخزون وأقساط",
            "25+ تقرير تحليلي شامل"
          ],
          features: [
            { icon: "receipt", title: "فواتير متكاملة", desc: "بيع، شراء، أقساط، ومرتجعات بكل التفاصيل" },
            { icon: "pos", title: "نقطة بيع POS", desc: "كاشير سريع مع باركود ومفضلة" },
            { icon: "warehouse", title: "المخازن", desc: "تتبع الكميات والتسويات والنقل" },
            { icon: "installment", title: "الأقساط", desc: "خطط دفع ولوحة تحصيل" },
            { icon: "voucher", title: "المالية", desc: "سندات، مصاريف، وتحويلات" },
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
            "تقارير إشغال وإيرادات وتدقيق ليلي"
          ],
          restaurant: {
            title: "مطعم الفندق F&B",
            highlights: [
              "كاشier POS — صالة، سفري، وخدمة غرف",
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
          desc: "عقود بيع، مدفوعات، تقارير Excel، طباعة احترافية، ولوحة KPI — لمعارض ومكاتب بيع السيارات.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "نظام عقود السيارات — لوحة العقود",
          highlights: [
            "لوحة KPI — عقود اليوم، محصّل، متبقي",
            "عقد بيع جديد — بائع، مشتري، مركبة",
            "إدارة العقود وحالاتها",
            "مدفوعات وأقساط العقود",
            "تقرير شامل مع تصدير Excel",
            "طباعة عقود بإعدادات مخصصة",
            "صلاحيات مستخدمين",
            "نسخ احتياطي محلي"
          ],
          features: [
            { icon: "car", title: "العقود", desc: "إنشاء وتتبع عقود البيع" },
            { icon: "voucher", title: "المدفوعات", desc: "دفعات وأقساط لكل عقد" },
            { icon: "chart", title: "التقارير", desc: "تقرير العقود وتصدير Excel" },
            { icon: "receipt", title: "الطباعة", desc: "عقود مطبوعة احترافياً" },
            { icon: "shield", title: "الصلاحيات", desc: "تحكم بالوصول لكل شاشة" },
            { icon: "cloud", title: "المزامنة", desc: "ربط سحابي مع التطبيق" }
          ]
        },
        {
          id: "car-trade",
          label: "بيع وشراء",
          badge: "تجاري",
          tagline: "نظام بيع وشراء السيارات",
          desc: "عمليات شراء وبيع، مدفوعات جزئية وكاملة، كشف حساب الأطراف، تقارير وطباعة — لمعارض وتجار السيارات.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "نظام بيع وشراء السيارات — لوحة العمليات",
          highlights: [
            "لوحة KPI — عمليات اليوم والشهر",
            "عملية جديدة — بيانات السيارة والبائع والمشتري",
            "دفع كامل أو جزئي مع متابعة المتبقي",
            "تسديدات ووصولات لكل دفعة",
            "كشف حساب للأطراف بالأجل",
            "تقارير مع تصدير Excel وطباعة",
            "صلاحيات ومزامنة سحابية",
            "قاعدة بيانات منفصلة عن الأنظمة الأخرى"
          ],
          features: [
            { icon: "car", title: "العمليات", desc: "شراء وبيع مع تفاصيل المركبة" },
            { icon: "voucher", title: "المدفوعات", desc: "تسديدات ووصولات لكل عملية" },
            { icon: "chart", title: "التقارير", desc: "تقرير العمليات وكشف الحساب" },
            { icon: "receipt", title: "الطباعة", desc: "وصول معاملات وتسديدات" },
            { icon: "shield", title: "الصلاحيات", desc: "تحكم بالوصول لكل شاشة" },
            { icon: "cloud", title: "المزامنة", desc: "تطبيق جوال وAPI سحابي" }
          ]
        },
        {
          id: "mobile",
          label: "الجوال",
          badge: "متاح الآن",
          tagline: "تطبيق جوال — 4 أنظمة",
          desc: "تطبيق Flutter يتصل بالـ API السحابي: تقارير، إنشاء بيانات، فواتير، حجوزات، عقود، وعمليات بيع وشراء — حسب نوع نظامك.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "تطبيق المحاسب — لوحة التقارير",
          highlights: [
            "4 profiles: محاسبة، فندق، عقود، بيع وشراء",
            "9+ تقارير محاسبة + KPI فندقي",
            "إنشاء عملاء، منتجات، فواتير (5 خطوات)",
            "حجوزات، غرف، check-in/out للفندق",
            "عقود ومدفوعات للسيارات",
            "عمليات بيع وشراء وكشف حساب طرف",
            "عربي/إنجليزي + وضع داكن",
            "إشعارات OneSignal"
          ],
          features: [
            { icon: "chart", title: "تقارير لحظية", desc: "مبيعات، أرباح، مخزون، إشغال" },
            { icon: "receipt", title: "فواتير جوال", desc: "معالج 5 خطوات للفواتير" },
            { icon: "hotel", title: "فندق جوال", desc: "حجوزات، غرف، ووصول" },
            { icon: "car", title: "سيارات جوال", desc: "عقود، عمليات، ومدفوعات" },
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
      title: "تطبيق جوال — 4 أنظمة",
      desc: "تطبيق Flutter متصل بالـ API السحابي: تقارير لحظية، إنشاء فواتير وبيانات، وإدارة فندق أو سيارات — من أي مكان.",
      points: [
        "محاسبة: تقارير، فواتير، عملاء، منتجات",
        "فندق: حجوزات، غرف، check-in، مطعم",
        "عقود سيارات: عقود، مدفوعات، تقارير",
        "بيع وشراء: عمليات، مدفوعات، كشف حساب",
        "مزامنة آمنة JWT + multi-tenant"
      ],
      profiles: { accounting: "محاسبة", hotel: "فندق", car: "عقود", carTrade: "بيع وشراء" },
      appName: "المحاسب", greeting: "مرحباً — بياناتك متزامنة",
      cards: { sales: "تقرير المبيعات", statement: "كشف حساب", stock: "المخزون", overdue: "حجوزات اليوم" },
      nav: { home: "الرئيسية", reports: "التقارير", data: "البيانات" },
      caption: "تطبيق المحاسب — iOS و Android"
    },
    features: {
      title: "مميزات المنصة المشتركة",
      subtitle: "ما يجمع كل الأنظمة — بنية تحتية موثوقة",
      items: [
        { icon: "offline", title: "100% أوفلاين", desc: "يعمل بدون إنترنت — المزامنة اختيارية" },
        { icon: "shield", title: "صلاحيات دقيقة", desc: "تحكم بكل شاشة: إضافة، تعديل، حذف، طباعة" },
        { icon: "cloud", title: "مزامنة سحابية", desc: "Push/Pull ثنائي الاتجاه — multi-tenant" },
        { icon: "backup", title: "نسخ احتياطي", desc: "نسخ واستعادة محلية بنقرة" },
        { icon: "audit", title: "سجل تدقيق", desc: "تتبع العمليات الحساسة" },
        { icon: "update", title: "تحديثات تلقائية", desc: "من GitHub عبر version.json" },
        { icon: "ai", title: "تنبيهات ذكية", desc: "أقساط، مخزون، إشغال، ونظافة" },
        { icon: "lang", title: "عربي / English", desc: "RTL كامل + واجهة ثنائية اللغة" }
      ]
    },
    how: {
      title: "ابدأ في دقائق",
      steps: [
        { num: "01", title: "نزّل النظام", desc: "ملف ZIP من GitHub — Windows 10/11" },
        { num: "02", title: "اختر نظامك", desc: "محاسبة، فندق، عقود سيارات، أو بيع وشراء عند الإعداد" },
        { num: "03", title: "اعمل أوفلاين", desc: "فواتير، حجوزات، عقود، أو عمليات — بدون إنترنت" }
      ]
    },
    cloud: {
      title: "مزامنة سحابية — 4 أنظمة",
      desc: "اربط Desktop مع Cloud API: محاسبة، فنادق، عقود سيارات، وبيع وشراء — Push/Pull مع عزل بيانات كل عميل.",
      points: [
        "مزامنة حسب نوع النظام (Accounting / Hotel / Car / CarTrade)",
        "فواتير، حجوزات، عقود، عمليات، ومطعم",
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
        { label: "السيارات", items: ["تقرير العقود", "محصّل/متبقي", "تصدير Excel"] },
        { label: "بيع وشراء", items: ["تقرير العمليات", "كشف حساب طرف", "وصولات التسديد"] }
      ]
    },
    download: {
      title: "حمّل المحاسب الآن",
      desc: "آخر إصدار من GitHub — تحديثات تلقائية من داخل التطبيق",
      btn: "تنزيل ZIP", version: "الإصدار", size: "الحجم", date: "تاريخ الإصدار",
      req: "متطلبات: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "أسئلة شائعة",
      items: [
        { q: "هل يعمل بدون إنترنت؟", a: "نعم. كل الأنظمة أوفلاين بالكامل. الإنترنت للمزامنة والتحديثات فقط." },
        { q: "كيف أختار النظام المناسب؟", a: "عند الإعداد الأول أو من لوحة المطور: محاسبة للمحلات، فندق للضيافة، عقود أو بيع وشراء لمعارض السيارات." },
        { q: "هل الفندق يشمل المطعم؟", a: "نعم — POS، KDS، مخزون مطبخ، طاولات، وتقارير F&B مدمجة في نظام الفندق." },
        { q: "هل يدعم أنظمة السيارات؟", a: "نعم — نظام عقود ونظام بيع وشراء مستقلان، كل منهما مع تطبيق جوال وقاعدة بيانات منفصلة." },
        { q: "هل التطبيق الجوال جاهز؟", a: "نعم — يدعم 4 profiles حسب نوع حسابك: محاسبة، فندق، عقود، أو بيع وشراء." },
        { q: "كيف أحدّث النظام؟", a: "من داخل التطبيق — يقرأ version.json من GitHub." },
        { q: "هل البيانات آمنة؟", a: "نسخ احتياطي، صلاحيات، سجل تدقيق، وعزل multi-tenant في السحابة." }
      ]
    },
    contact: { title: "جاهز للتجربة؟", desc: "نزّل مجاناً وجرّب النظام المناسب لنشاطك", github: "المستودع على GitHub" },
    footer: { rights: "جميع الحقوق محفوظة — المحاسب" }
  },
  en: {
    meta: {
      title: "AlMuhasib — Accounting, Hotels, Restaurants & Car Systems",
      description: "Integrated Arabic business platform: accounting, hotel PMS & F&B, car contracts & trading, mobile app, and cloud sync. Fully offline."
    },
    nav: {
      systems: "Systems", features: "Platform", how: "How it works", videos: "Videos",
      reports: "Reports", cloud: "Cloud", mobile: "Mobile", download: "Download", faq: "FAQ", contact: "Contact"
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
      badge: "Multi-system platform",
      title: "AlMuhasib",
      subtitle: "Integrated business platform —",
      rotateWords: ["Accounting", "Hotels", "Cars", "Buy & Sell"],
      desc: "Three desktop systems + mobile app + cloud sync — Arabic, offline-first, built to scale.",
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
          desc: "Invoices, POS, warehouses, installments, vouchers, investors, and 25+ reports — for shops and SMBs.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Accounting — dashboard & sales",
          highlights: [
            "Sales & purchase invoices with returns",
            "Quick POS — barcode & instant cash",
            "Warehouses, opening balances, adjustments",
            "Installments & overdue reports",
            "Receipts, expenses, cash boxes",
            "Investors & profit distribution",
            "Smart alerts — stock & installments",
            "25+ analytical reports"
          ],
          features: [
            { icon: "receipt", title: "Full invoicing", desc: "Sales, purchases, installments, returns" },
            { icon: "pos", title: "POS", desc: "Fast cashier with barcode" },
            { icon: "warehouse", title: "Warehouses", desc: "Stock tracking & transfers" },
            { icon: "installment", title: "Installments", desc: "Payment plans & collections" },
            { icon: "voucher", title: "Finance", desc: "Vouchers, expenses, transfers" },
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
          id: "car", label: "Car Contracts", badge: "Sales contracts",
          tagline: "Car sales contracts system",
          desc: "Sales contracts, payments, Excel reports, professional printing, KPI dashboard — for dealerships.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "Car contracts — dashboard",
          highlights: [
            "KPI dashboard — today, collected, remaining",
            "New contract — seller, buyer, vehicle",
            "Contract management & statuses",
            "Payments & installments",
            "Full report with Excel export",
            "Custom print settings",
            "User permissions",
            "Local backup"
          ],
          features: [
            { icon: "car", title: "Contracts", desc: "Create & track sales" },
            { icon: "voucher", title: "Payments", desc: "Per-contract payments" },
            { icon: "chart", title: "Reports", desc: "Contracts & Excel export" },
            { icon: "receipt", title: "Printing", desc: "Professional contracts" },
            { icon: "shield", title: "Permissions", desc: "Screen-level access" },
            { icon: "cloud", title: "Cloud sync", desc: "Mobile & API ready" }
          ]
        },
        {
          id: "car-trade", label: "Buy & Sell", badge: "Trading",
          tagline: "Car buy & sell system",
          desc: "Buy/sell transactions, partial or full payments, party statements, reports and printing — for car traders.",
          screenshot: "assets/desktop-car.png",
          screenshotCaption: "Car trading — transactions dashboard",
          highlights: [
            "KPI dashboard — today & month transactions",
            "New transaction — vehicle, seller, buyer details",
            "Full or partial payment with balance tracking",
            "Settlements and payment receipts",
            "Party account statements on credit",
            "Reports with Excel export & print",
            "Permissions & cloud sync",
            "Isolated database from other systems"
          ],
          features: [
            { icon: "car", title: "Transactions", desc: "Buy & sell with vehicle details" },
            { icon: "voucher", title: "Payments", desc: "Settlements & receipts" },
            { icon: "chart", title: "Reports", desc: "Transactions & party statements" },
            { icon: "receipt", title: "Printing", desc: "Transaction & payment receipts" },
            { icon: "shield", title: "Permissions", desc: "Screen-level access" },
            { icon: "cloud", title: "Cloud sync", desc: "Mobile app & API" }
          ]
        },
        {
          id: "mobile", label: "Mobile", badge: "Available now",
          tagline: "Mobile app — 4 systems",
          desc: "Flutter app on cloud API: reports, data entry, invoices, reservations, contracts, and car trading — by your system type.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "AlMuhasib mobile — reports hub",
          highlights: [
            "4 profiles: accounting, hotel, contracts, buy & sell",
            "9+ accounting reports + hotel KPIs",
            "Create customers, products, 5-step invoices",
            "Reservations, rooms, check-in for hotel",
            "Contracts & payments for cars",
            "Buy/sell transactions & party statements",
            "Arabic/English + dark mode",
            "OneSignal notifications"
          ],
          features: [
            { icon: "chart", title: "Live reports", desc: "Sales, profit, stock, occupancy" },
            { icon: "receipt", title: "Mobile invoices", desc: "5-step invoice wizard" },
            { icon: "hotel", title: "Hotel mobile", desc: "Bookings, rooms, check-in" },
            { icon: "car", title: "Cars mobile", desc: "Contracts, trading & payments" },
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
      title: "Mobile app — 4 systems",
      desc: "Flutter app on cloud API: live reports, invoice creation, hotel or car management — anywhere.",
      points: [
        "Accounting: reports, invoices, customers, products",
        "Hotel: reservations, rooms, check-in, restaurant",
        "Car contracts: contracts, payments, reports",
        "Car trading: transactions, payments, party statements",
        "Secure JWT + multi-tenant sync"
      ],
      profiles: { accounting: "Accounting", hotel: "Hotel", car: "Contracts", carTrade: "Buy & Sell" },
      appName: "AlMuhasib", greeting: "Welcome — data synced",
      cards: { sales: "Sales report", statement: "Statement", stock: "Stock", overdue: "Today's bookings" },
      nav: { home: "Home", reports: "Reports", data: "Data" },
      caption: "AlMuhasib app — iOS & Android"
    },
    features: {
      title: "Shared platform features",
      subtitle: "What powers every system — reliable infrastructure",
      items: [
        { icon: "offline", title: "100% offline", desc: "Works without internet — sync optional" },
        { icon: "shield", title: "Fine permissions", desc: "Per-screen add, edit, delete, print" },
        { icon: "cloud", title: "Cloud sync", desc: "Two-way Push/Pull — multi-tenant" },
        { icon: "backup", title: "Backup", desc: "Local backup & restore" },
        { icon: "audit", title: "Audit log", desc: "Track sensitive operations" },
        { icon: "update", title: "Auto updates", desc: "From GitHub via version.json" },
        { icon: "ai", title: "Smart alerts", desc: "Installments, stock, occupancy" },
        { icon: "lang", title: "Arabic / English", desc: "Full RTL + bilingual UI" }
      ]
    },
    how: {
      title: "Get started in minutes",
      steps: [
        { num: "01", title: "Download", desc: "ZIP from GitHub — Windows 10/11" },
        { num: "02", title: "Pick your system", desc: "Accounting, hotel, car contracts, or buy & sell at setup" },
        { num: "03", title: "Work offline", desc: "Invoices, bookings, contracts, or transactions — no internet" }
      ]
    },
    cloud: {
      title: "Cloud sync — 4 systems",
      desc: "Connect Desktop to Cloud API: accounting, hotels, car contracts, and car trading — Push/Pull with tenant isolation.",
      points: [
        "Sync by system type (Accounting / Hotel / Car / CarTrade)",
        "Invoices, reservations, contracts, transactions, restaurant",
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
        { label: "Cars", items: ["Contracts report", "Collected/remaining", "Excel export"] },
        { label: "Buy & Sell", items: ["Transactions report", "Party statement", "Payment receipts"] }
      ]
    },
    download: {
      title: "Download AlMuhasib", desc: "Latest from GitHub — in-app auto-update",
      btn: "Download ZIP", version: "Version", size: "Size", date: "Release date",
      req: "Requirements: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "FAQ",
      items: [
        { q: "Works offline?", a: "Yes. All systems are fully offline. Internet for sync and updates only." },
        { q: "How to pick a system?", a: "At first setup or via admin: accounting for retail, hotel for hospitality, car contracts or buy & sell for dealerships." },
        { q: "Does hotel include restaurant?", a: "Yes — POS, KDS, kitchen stock, tables, and F&B reports are built in." },
        { q: "Car systems support?", a: "Yes — car contracts and car buy/sell, each with mobile app and isolated database." },
        { q: "Is mobile ready?", a: "Yes — 4 profiles by account type: accounting, hotel, car contracts, or buy & sell." },
        { q: "How to update?", a: "In-app check reads version.json from GitHub." },
        { q: "Data safe?", a: "Backup, permissions, audit log, multi-tenant cloud isolation." }
      ]
    },
    contact: { title: "Ready to try?", desc: "Download free and test your system", github: "GitHub repository" },
    footer: { rights: "All rights reserved — AlMuhasib" }
  }
};
