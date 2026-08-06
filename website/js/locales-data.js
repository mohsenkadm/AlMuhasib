/** نصوص مضمّنة — تعمل عند فتح index.html مباشرة (file://) بدون خادم */
window.LOCALES = {
  ar: {
    meta: {
      title: "قيد — محاسبة، ذهب، فنادق، سيارات، عقارات وتطبيق جوال",
      description: "منصة أعمال عربية متكاملة: محاسبة، ذهب، فنادق، عقود سيارات، تجارة سيارات، عقود عقارات، ولاء، حقول مخصصة، مساعد صوتي، ربط فروع، تطبيق جوال، ومزامنة سحابية. تعمل أوفلاين بالكامل."
    },
    nav: {
      systems: "الأنظمة", whatsNew: "الجديد", features: "المنصة", how: "كيف يعمل", videos: "الفيديوهات",
      mobile: "التطبيق", download: "التنزيل", faq: "الأسئلة", contact: "تواصل"
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
      badge: "جديد: نظام الذهب + الولاء + الحقول المخصصة",
      title: "قيد",
      subtitle: "منصة أعمال متكاملة —",
      rotateWords: ["محاسبة", "ذهب", "فنادق", "عقود سيارات", "تجارة سيارات", "عقود عقارات"],
      desc: "ستة أنظمة سطح مكتب أوفلاين + مساعد صوتي + ربط فروع عبر WiFi + تطبيق جوال + مزامنة سحابية — عربي، جاهز للنمو.",
      cta_download: "حمّل النظام مجاناً",
      cta_systems: "استكشف الأنظمة",
      cta_features: "مميزات المنصة",
      stat_systems: "أنظمة",
      stat_reports: "تقرير+",
      stat_offline: "أوفلاين",
      screen_caption: "واجهة النظام الأوفلاين — لوحة التحكم"
    },
    systems: {
      title: "أنظمتنا المتكاملة",
      subtitle: "اختر نظاماً لاستكشاف ميزاته بالتفصيل",
      featuresTitle: "ميزات النظام",
      modulesTitle: "وحدات النظام",
      cta_download: "حمّل الآن",
      tabs: [
        {
          id: "accounting",
          label: "المحاسبة",
          badge: "الأكثر استخداماً",
          tagline: "نظام محاسبة ومبيعات احترافي",
          desc: "فواتير، POS، تسعير، مخازن، أقساط، مستثمرون، ولاء، حقول مخصصة، قوالب قطاعات، مساعد صوتي، واتساب موسّع، وأكثر من 30 تقريراً — للمحلات والمخازن والشركات.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام المحاسبة — لوحة التحكم والمبيعات",
          highlights: [
            "فاتورة مبيعات + بيع سريع POS مع باركود ومفضلة",
            "بحث مواد ذكي مع تمييز وكميات المخازن + فحص الربح",
            "فحص السعر بالباركود بسرعة من أي نقطة",
            "تفعيل الخصم، أنواع التعبئة، أجور النقل، ووزن القائمة",
            "حاسبة فكة الدينار (F7) في POS والبيع",
            "نسخة المخزن بدون مبالغ + اختيار السائق",
            "نظام الولاء — نقاط من البيع واستبدال خصم",
            "حقول مخصصة للمنتجات والعملاء والموردين والمستثمرين",
            "ملخص العمل: KPIs ومخططات وأفضل عملاء",
            "واتساب موسّع: فواتير، سندات، كشوف، وتقارير",
            "قوالب قطاعات: جوالات، ألبسة، مقاولات، صيدلية",
            "30+ تقرير: مبيعات، أرباح، أقساط، كشوف، رقابية"
          ],
          modules: [
            { title: "المبيعات ونقطة البيع", items: ["فاتورة مبيعات", "بيع سريع POS", "فحص سعر وربح"] },
            { title: "المنتجات والمخزون", items: ["منتجات وتصنيفات", "حقول مخصصة", "مخازن ونقل وتسوية"] },
            { title: "المشتريات والأقساط", items: ["فاتورة ومرتجع مشتريات", "فاتورة أقساط", "لوحة التحصيل"] },
            { title: "المالية والولاء", items: ["سندات ومصاريف", "نظام الولاء", "مستثمرون"] }
          ],
          features: [
            { icon: "receipt", title: "فواتير متكاملة", desc: "بيع، شراء، أقساط، ومرتجعات بكل التفاصيل" },
            { icon: "pos", title: "نقطة بيع POS", desc: "كاشير سريع مع باركود ومفضلة وفكة دينارية" },
            { icon: "search", title: "بحث وفحص ربح", desc: "بحث مواد ذكي + تكلفة وبيع وربح فوري" },
            { icon: "loyalty", title: "نظام الولاء", desc: "نقاط من البيع واستبدالها خصماً" },
            { icon: "customFields", title: "حقول مخصصة", desc: "حتى 8 حقول للمنتجات والعملاء والموردين" },
            { icon: "warehouse", title: "المخازن والسائق", desc: "تتبع الكميات + نسخة مخزن وسائق" },
            { icon: "whatsapp", title: "واتساب موسّع", desc: "فواتير وسندات وكشوف وتقارير PDF" },
            { icon: "voice", title: "المساعد الصوتي قيد", desc: "أوامر صوتية لفتح الشاشات والبيع" },
            { icon: "chart", title: "تقارير وملخص عمل", desc: "مبيعات، أرباح، KPIs، ورقابة" }
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
          badge: "معارض",
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
          id: "realEstate",
          label: "عقود العقارات",
          badge: "عقارات",
          tagline: "نظام عقود العقارات",
          desc: "عقود عقارية، زبائن، كشف مدينين، مصاريف، بنود العقد، وتقارير عقود وأرباح — لمكاتب العقارات والوسطاء.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام عقود العقارات — اللوحة والعقود",
          highlights: [
            "لوحة تحكم لعقود اليوم والحالة",
            "عقد جديد مع بنود قابلة للتخصيص",
            "قائمة العقود وتتبع الحالة",
            "زبائن وملفات الأطراف",
            "كشف مدينين ومتابعة المستحقات",
            "مصاريف مرتبطة بالنشاط العقاري",
            "قوالب بنود العقد الجاهزة",
            "تقارير عقود وأرباح",
            "طباعة احترافية + صلاحيات ونسخ احتياطي",
            "مزامنة سحابية وربط فروع"
          ],
          features: [
            { icon: "realEstate", title: "العقود العقارية", desc: "إنشاء وتتبع العقود والحالات" },
            { icon: "guest", title: "الزبائن", desc: "ملفات الأطراف والوسطاء" },
            { icon: "voucher", title: "المدينون", desc: "كشف مستحقات ومتابعة" },
            { icon: "print", title: "بنود وطباعة", desc: "قوالب بنود وطباعة احترافية" },
            { icon: "chart", title: "تقارير الأرباح", desc: "عقود، أرباح، ومصاريف" },
            { icon: "cloud", title: "سحابة وفروع", desc: "مزامنة وربط مكاتب متعددة" }
          ]
        },
        {
          id: "gold",
          label: "الذهب",
          badge: "جديد",
          tagline: "نظام محلات الذهب",
          desc: "نظام مستقل لمحلات الذهب العراقية: أسعار المثقال، المخزون، البيع والشراء والآجل، الميزان، لوحة تنافسية، وطباعة احترافية.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام الذهب — الأسعار والمخزون",
          highlights: [
            "أسعار المثقال لحظية لكل عيار",
            "مخزون ذهب بالمثاقيل والقطع",
            "بيع وشراء نقدي وآجل",
            "ميزان وربط وزن القطعة",
            "لوحة مراقبة وتنافسية السوق",
            "طباعة فواتير وإيصالات ذهب",
            "صلاحيات ونسخ احتياطي وربط فروع",
            "واجهة عربية مخصّصة لمحلات الذهب"
          ],
          modules: [
            { title: "الأسعار والعيارات", items: ["سعر المثقال", "عيارات متعددة", "تحديث سريع"] },
            { title: "المخزون", items: ["قطع ذهب", "مثاقيل", "جرد ومتابعة"] },
            { title: "البيع والشراء", items: ["بيع نقدي", "بيع آجل", "شراء من الزبون"] },
            { title: "التقارير", items: ["حركة المخزون", "الأرباح", "كشف العملاء"] }
          ],
          features: [
            { icon: "gold", title: "أسعار المثقال", desc: "متابعة أسعار العيارات لحظياً" },
            { icon: "warehouse", title: "مخزون الذهب", desc: "قطع ومثاقيل وجرد دقيق" },
            { icon: "receipt", title: "بيع وشراء", desc: "نقدي وآجل مع طباعة" },
            { icon: "scale", title: "الميزان", desc: "وزن القطعة وربطه بالفاتورة" },
            { icon: "chart", title: "لوحة وتقارير", desc: "تنافسية السوق وحركة المحل" },
            { icon: "network", title: "ربط الفروع", desc: "حاسبة رئيسية وفرعية للمحل" }
          ]
        },
        {
          id: "mobile",
          label: "الجوال",
          badge: "متاح الآن",
          tagline: "تطبيق جوال — أنظمة متعددة",
          desc: "تطبيق Flutter يتصل بالـ API السحابي: تقارير، إنشاء بيانات، فواتير، حجوزات، عقود — حسب نوع نظامك.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "تطبيق قيد — لوحة التقارير",
          highlights: [
            "profiles: محاسبة، فندق، عقود سيارات، تجارة سيارات، عقارات",
            "تقارير محاسبة + KPI فندقي + إحصائيات أغنى",
            "إنشاء عملاء، منتجات، فواتير (5 خطوات)",
            "حجوزات، غرف، check-in/out للفندق",
            "عقود ومدفوعات للسيارات والعقارات",
            "مطعم: كاشير وتقارير F&B",
            "عربي/إنجليزي + وضع داكن",
            "إشعارات OneSignal"
          ],
          features: [
            { icon: "chart", title: "تقارير لحظية", desc: "مبيعات، أرباح، مخزون، إشغال" },
            { icon: "receipt", title: "فواتير جوال", desc: "معالج 5 خطوات للفواتير" },
            { icon: "hotel", title: "فندق جوال", desc: "حجوزات، غرف، ووصول" },
            { icon: "car", title: "سيارات جوال", desc: "عقود وتجارة ومدفوعات" },
            { icon: "realEstate", title: "عقارات جوال", desc: "عقود وتقارير عقارية" },
            { icon: "cloud", title: "API آمن", desc: "JWT وmulti-tenant" }
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
      title: "تطبيق جوال — أنظمة متعددة",
      desc: "تطبيق Flutter متصل بالـ API السحابي: تقارير لحظية، إنشاء فواتير وبيانات، وإدارة فندق أو سيارات أو عقارات — من أي مكان.",
      points: [
        "محاسبة: تقارير، فواتير، عملاء، منتجات",
        "فندق: حجوزات، غرف، check-in، مطعم",
        "عقود سيارات وتجارة سيارات",
        "عقود عقارات وتقارير",
        "مزامنة آمنة JWT + multi-tenant"
      ],
      profiles: {
        accounting: "محاسبة", hotel: "فندق", car: "عقود سيارات",
        carTrade: "تجارة سيارات", realEstate: "عقارات"
      },
      appName: "قيد", greeting: "مرحباً — بياناتك متزامنة",
      cards: { sales: "تقرير المبيعات", statement: "كشف حساب", stock: "المخزون", overdue: "حجوزات اليوم" },
      nav: { home: "الرئيسية", reports: "التقارير", data: "البيانات" },
      caption: "تطبيق قيد — iOS و Android"
    },
    whatsNew: {
      badge: "أحدث الإضافات",
      title: "ميزات جديدة",
      subtitle: "أحدث ما أُضيف لمنصة قيد — جاهز للاستخدام الآن",
      newLabel: "جديد",
      items: [
        { icon: "gold", title: "نظام الذهب", desc: "نظام مستقل لمحلات الذهب: أسعار المثقال، المخزون، البيع والآجل، والميزان." },
        { icon: "loyalty", title: "نظام الولاء", desc: "نقاط تُكسب من البيع وتُستبدل خصماً — من الإعدادات والفواتير وPOS." },
        { icon: "customFields", title: "الحقول المخصصة", desc: "حتى 8 حقول (نص، رقم، نعم/لا، اختيارات) للمنتجات والعملاء والموردين والمستثمرين." },
        { icon: "search", title: "بحث ذكي وفحص ربح", desc: "بحث مواد بالاسم مع تمييز وكميات — وزر فحص الربح (تكلفة/بيع/ربح)." },
        { icon: "chart", title: "ملخص العمل", desc: "تقرير KPIs ومخططات وأفضل عملاء وساعات نشاط — مع تفاصيل زبون/منتج من الفاتورة." },
        { icon: "whatsapp", title: "واتساب موسّع", desc: "مشاركة PDF عبر واتساب للسندات وإيصالات المستثمرين والكشوف وتقارير البيع/الشراء." },
        { icon: "truck", title: "نسخة المخزن والسائق", desc: "طباعة نسخة مخزن بدون مبالغ واختيار سائق في فواتير البيع والأقساط." },
        { icon: "salesBoost", title: "تحسينات البيع", desc: "خصم، أنواع تعبئة، أجور نقل، وزن القائمة، وحاسبة فكة الدينار (F7)." }
      ]
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
    platformInfra: {
      title: "البنية التحتية",
      subtitle: "ربط فروع، مزامنة سحابية، وتقارير شاملة لكل نشاط"
    },
    how: {
      title: "ابدأ في دقائق",
      steps: [
        { num: "01", title: "نزّل النظام", desc: "ملف ZIP من GitHub — Windows 10/11" },
        { num: "02", title: "اختر نظامك ونوع الحاسبة", desc: "محاسبة، ذهب، فندق، عقود سيارات، تجارة سيارات، أو عقارات — رئيسية أو فرعية" },
        { num: "03", title: "اعمل أوفلاين أو عبر الشبكة", desc: "قاعدة محلية أو اتصال مباشر بالحاسبة الرئيسية" }
      ]
    },
    network: {
      badge: "متاح",
      title: "ربط الحاسبات الرئيسية والفرعية",
      desc: "اربط عدة حواسيب على نفس الشبكة (WiFi أو Ethernet) بقاعدة بيانات واحدة على الحاسبة الرئيسية — بدون مزامنة وبدون إنترنت.",
      points: [
        "يدعم كل الأنظمة: محاسبة، ذهب، فنادق، عقود سيارات، تجارة سيارات، وعقارات",
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
      desc: "اربط Desktop مع Cloud API: محاسبة، فنادق (ومطعم)، عقود سيارات، تجارة سيارات، وعقارات — Push/Pull مع عزل بيانات كل عميل.",
      points: [
        "مزامنة حسب نوع النظام (Accounting / Hotel / Car / CarTrade / RealEstate)",
        "فواتير، حجوزات، عقود، تجارة، عقارات، ومطعم",
        "تعارضات ذكية + حذف ناعم",
        "REST API + JWT للتطبيق الجوال",
        "لوحة مطور — tenants وتراخيص",
        "Multi-tenant — عزل كامل للبيانات"
      ]
    },
    reports: {
      title: "تقارير شاملة",
      groups: [
        { label: "المحاسبة", items: ["المبيعات والمشتريات", "الأرباح", "كشف حساب", "المخزون", "الأقساط المتأخرة", "ملخص العمل", "تقارير رقابية"] },
        { label: "الذهب", items: ["أسعار المثقال", "حركة المخزون", "البيع والشراء", "أرباح المحل"] },
        { label: "الفندق", items: ["الإشغال", "الإيرادات", "تدقيق ليلي", "وصول/مغادرة"] },
        { label: "المطعم", items: ["مبيعات F&B", "قنوات البيع", "أكثر الأصناف", "ربحية المطعم"] },
        { label: "عقود السيارات", items: ["تقرير العقود", "محصّل/متبقي", "تصدير Excel"] },
        { label: "تجارة السيارات", items: ["المعاملات", "المخزون", "الأرباح", "كشف الأطراف"] },
        { label: "العقارات", items: ["تقرير العقود", "الأرباح", "كشف المدينين", "المصاريف"] }
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
        { q: "كيف أختار النظام المناسب؟", a: "عند الإعداد الأول: محاسبة للمحلات، ذهب لمحلات الذهب، فندق للضيافة، عقود سيارات للمعارض، تجارة سيارات لدورة الشراء والبيع، وعقود عقارات لمكاتب العقارات." },
        { q: "ما هو نظام الذهب؟", a: "نظام مستقل لمحلات الذهب العراقية: أسعار المثقال، مخزون القطع والمثاقيل، بيع وشراء نقدي وآجل، ميزان، لوحة تنافسية، وتقارير حركة المحل." },
        { q: "ما الفرق بين عقود السيارات وتجارة السيارات؟", a: "عقود السيارات لإبرام عقود بيع بين بائع ومشتري مع طباعة وشهود. تجارة السيارات لإدارة مخزون المعرض: شراء ثم بيع مع دفعات وتقارير." },
        { q: "ما هو نظام عقود العقارات؟", a: "نظام لإدارة العقود العقارية والزبائن والمدينين والمصاريف وبنود العقد مع تقارير أرباح — متزامن مع الجوال والسحابة." },
        { q: "ما هو نظام الولاء؟", a: "ميزة في المحاسبة تُكسب العميل نقاطاً من فواتير البيع ويمكن استبدالها خصماً. تُفعَّل من إعدادات ميزات النشاط وتظهر في الفواتير وPOS وتقارير الولاء." },
        { q: "ما هي الحقول المخصصة؟", a: "إعدادات لإضافة حتى 8 حقول إضافية (نص، رقم، نعم/لا، اختيارات) للمنتجات والعملاء والموردين والمستثمرين — تظهر في الجداول والنماذج." },
        { q: "ما هو فحص الربح وبحث المواد الذكي؟", a: "في فاتورة البيع والأقساط: بحث منتج بالاسم مع تمييز وكميات المخازن والأسعار، وزر فحص الربح يعرض التكلفة وسعر البيع والربح والخصم فوراً." },
        { q: "هل واتساب للفواتير فقط؟", a: "لا. يمكن مشاركة PDF عبر واتساب للفواتير والسندات وإيصالات المستثمرين وكشوف الحساب وتقارير البيع والشراء وفواتير الشراء." },
        { q: "ما هي نسخة المخزن والسائق؟", a: "عند الطباعة يمكن إصدار نسخة مخزن بدون مبالغ مالية، واختيار سائق مرتبط بفاتورة البيع أو الأقساط لتسهيل التجهيز والتوصيل." },
        { q: "ما هي حاسبة فكة الدينار؟", a: "حوار سريع (F7) في POS وفاتورة البيع لحساب فكة الدينار العراقي وتسهيل استلام النقد من الزبون." },
        { q: "ما هو المساعد الصوتي قيد؟", a: "ميزة صوتية على سطح المكتب (Ctrl+Space) للبحث وفتح الشاشات وتنفيذ أوامر مثل البيع السريع دون الكتابة." },
        { q: "هل الفندق يشمل المطعم؟", a: "نعم — POS، KDS، مخزون مطبخ، طاولات، وتقارير F&B مدمجة في نظام الفندق." },
        { q: "هل التطبيق الجوال جاهز؟", a: "نعم — يدعم profiles متعددة: محاسبة، فندق، عقود سيارات، تجارة سيارات، وعقارات حسب نوع حسابك." },
        { q: "كيف أحدّث النظام؟", a: "من داخل التطبيق — يقرأ version.json من GitHub." },
        { q: "هل البيانات آمنة؟", a: "نسخ احتياطي، صلاحيات، سجل تدقيق، وعزل multi-tenant في السحابة." }
      ]
    },
    contact: { title: "جاهز للتجربة؟", desc: "نزّل مجاناً وجرّب النظام المناسب لنشاطك", github: "المستودع على GitHub" },
    footer: { rights: "جميع الحقوق محفوظة — قيد" }
  },
  en: {
    meta: {
      title: "Qayd — Accounting, Gold, Hotels, Cars, Real Estate & Mobile",
      description: "Integrated Arabic business platform: accounting, gold shops, hotels, car contracts, car trading, real estate, loyalty, custom fields, voice assistant, LAN branch linking, mobile app, and cloud sync. Fully offline."
    },
    nav: {
      systems: "Systems", whatsNew: "What's new", features: "Platform", how: "How it works", videos: "Videos",
      mobile: "Mobile", download: "Download", faq: "FAQ", contact: "Contact"
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
      badge: "New: Gold system + loyalty + custom fields",
      title: "Qayd",
      subtitle: "Integrated business platform —",
      rotateWords: ["Accounting", "Gold", "Hotels", "Car contracts", "Car trading", "Real estate"],
      desc: "Six offline desktop systems + voice assistant + main/branch LAN linking + mobile app + cloud sync — Arabic, built to scale.",
      cta_download: "Download free",
      cta_systems: "Explore systems",
      cta_features: "Platform features",
      stat_systems: "systems",
      stat_reports: "reports+",
      stat_offline: "offline",
      screen_caption: "Offline desktop — dashboard"
    },
    systems: {
      title: "Our integrated systems",
      subtitle: "Pick a system to explore its features",
      featuresTitle: "System features",
      modulesTitle: "System modules",
      cta_download: "Download now",
      tabs: [
        {
          id: "accounting", label: "Accounting", badge: "Most popular",
          tagline: "Professional accounting & sales",
          desc: "Invoices, POS, pricing, warehouses, installments, investors, loyalty, custom fields, industry templates, voice assistant, expanded WhatsApp, and 30+ reports — for shops and SMBs.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Accounting — dashboard & sales",
          highlights: [
            "Sales invoice + quick POS with barcode & favorites",
            "Smart product search with stock qty + profit check",
            "Barcode price check from any station",
            "Discounts, packing types, shipping fees & list weight",
            "Dinar change calculator (F7) in POS and sales",
            "Warehouse copy without amounts + driver selection",
            "Loyalty — earn points on sales, redeem as discount",
            "Custom fields for products, customers, suppliers, investors",
            "Work summary: KPIs, charts, top customers",
            "Expanded WhatsApp: invoices, vouchers, statements, reports",
            "Industry templates: phones, clothing, construction, pharmacy",
            "30+ reports: sales, profit, installments, statements, audit"
          ],
          modules: [
            { title: "Sales & POS", items: ["Sales invoice", "Quick POS", "Price & profit check"] },
            { title: "Products & stock", items: ["Products & categories", "Custom fields", "Warehouses, transfers, adjustments"] },
            { title: "Purchases & installments", items: ["Purchase & returns", "Installment invoice", "Collection board"] },
            { title: "Finance & loyalty", items: ["Vouchers & expenses", "Loyalty system", "Investors"] }
          ],
          features: [
            { icon: "receipt", title: "Full invoicing", desc: "Sales, purchases, installments, returns" },
            { icon: "pos", title: "POS", desc: "Fast cashier with barcode & dinar change" },
            { icon: "search", title: "Search & profit", desc: "Smart product search + instant margin" },
            { icon: "loyalty", title: "Loyalty", desc: "Earn points on sales, redeem discounts" },
            { icon: "customFields", title: "Custom fields", desc: "Up to 8 fields for products & parties" },
            { icon: "warehouse", title: "Stock & drivers", desc: "Warehouse copy + driver on invoices" },
            { icon: "whatsapp", title: "Expanded WhatsApp", desc: "Invoices, vouchers, statements, reports" },
            { icon: "voice", title: "Qayd voice assistant", desc: "Voice commands for screens & sales" },
            { icon: "chart", title: "Reports & work summary", desc: "Sales, profit, KPIs, audit" }
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
            "Occupancy, revenue & night audit",
            "LAN branch linking for front desks"
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
          id: "carTrade", label: "Car trading", badge: "Showrooms",
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
          id: "realEstate", label: "Real estate", badge: "Property",
          tagline: "Real estate contracts system",
          desc: "Property contracts, parties, debtors, expenses, contract clauses, and profit reports — for real-estate offices and brokers.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Real estate — dashboard & contracts",
          highlights: [
            "Dashboard for today's contracts and status",
            "New contract with customizable clauses",
            "Contract list and status tracking",
            "Parties and client profiles",
            "Debtor statements and receivables",
            "Business-related expenses",
            "Ready clause templates",
            "Contract and profit reports",
            "Pro printing + permissions & backup",
            "Cloud sync and branch linking"
          ],
          features: [
            { icon: "realEstate", title: "Property contracts", desc: "Create and track contract status" },
            { icon: "guest", title: "Parties", desc: "Clients and broker profiles" },
            { icon: "voucher", title: "Debtors", desc: "Receivables tracking" },
            { icon: "print", title: "Clauses & print", desc: "Templates and professional print" },
            { icon: "chart", title: "Profit reports", desc: "Contracts, profit, expenses" },
            { icon: "cloud", title: "Cloud & branches", desc: "Sync and multi-office linking" }
          ]
        },
        {
          id: "gold", label: "Gold", badge: "New",
          tagline: "Gold shop system",
          desc: "Standalone system for Iraqi gold shops: mithqal prices, inventory, cash & credit buy/sell, scale, market board, and professional printing.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Gold — prices & inventory",
          highlights: [
            "Live mithqal prices per karat",
            "Gold stock in mithqals and pieces",
            "Cash and credit buy/sell",
            "Scale weight linked to invoices",
            "Market competitiveness dashboard",
            "Gold invoice & receipt printing",
            "Permissions, backup & branch linking",
            "Arabic UI tailored for gold shops"
          ],
          modules: [
            { title: "Prices & karats", items: ["Mithqal price", "Multiple karats", "Quick update"] },
            { title: "Inventory", items: ["Gold pieces", "Mithqals", "Stock counts"] },
            { title: "Buy & sell", items: ["Cash sale", "Credit sale", "Buy from customer"] },
            { title: "Reports", items: ["Stock movement", "Profit", "Customer statements"] }
          ],
          features: [
            { icon: "gold", title: "Mithqal prices", desc: "Track karat prices in real time" },
            { icon: "warehouse", title: "Gold stock", desc: "Pieces, mithqals, accurate counts" },
            { icon: "receipt", title: "Buy & sell", desc: "Cash and credit with print" },
            { icon: "scale", title: "Scale", desc: "Piece weight linked to invoices" },
            { icon: "chart", title: "Board & reports", desc: "Market board and shop movement" },
            { icon: "network", title: "Branch linking", desc: "Main & branch shop PCs" }
          ]
        },
        {
          id: "mobile", label: "Mobile", badge: "Available now",
          tagline: "Mobile app — multi-system",
          desc: "Flutter app on cloud API: reports, data entry, invoices, reservations, contracts — by your system type.",
          screenshot: "assets/mobile-app.png",
          screenshotCaption: "Qayd mobile — reports hub",
          highlights: [
            "Profiles: accounting, hotel, car contracts, car trading, real estate",
            "Accounting reports + hotel KPIs + richer stats",
            "Create customers, products, 5-step invoices",
            "Reservations, rooms, check-in for hotel",
            "Contracts & payments for cars and real estate",
            "Restaurant POS & F&B reports",
            "Arabic/English + dark mode",
            "OneSignal notifications"
          ],
          features: [
            { icon: "chart", title: "Live reports", desc: "Sales, profit, stock, occupancy" },
            { icon: "receipt", title: "Mobile invoices", desc: "5-step invoice wizard" },
            { icon: "hotel", title: "Hotel mobile", desc: "Bookings, rooms, check-in" },
            { icon: "car", title: "Cars mobile", desc: "Contracts, trading & payments" },
            { icon: "realEstate", title: "Real estate mobile", desc: "Contracts & property reports" },
            { icon: "cloud", title: "Secure API", desc: "JWT multi-tenant" }
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
      title: "Mobile app — multi-system",
      desc: "Flutter app on cloud API: live reports, invoice creation, hotel, cars, or real estate — anywhere.",
      points: [
        "Accounting: reports, invoices, customers, products",
        "Hotel: reservations, rooms, check-in, restaurant",
        "Car contracts & car trading",
        "Real estate contracts & reports",
        "Secure JWT + multi-tenant sync"
      ],
      profiles: {
        accounting: "Accounting", hotel: "Hotel", car: "Car contracts",
        carTrade: "Car trading", realEstate: "Real estate"
      },
      appName: "Qayd", greeting: "Welcome — data synced",
      cards: { sales: "Sales report", statement: "Statement", stock: "Stock", overdue: "Today's bookings" },
      nav: { home: "Home", reports: "Reports", data: "Data" },
      caption: "Qayd app — iOS & Android"
    },
    whatsNew: {
      badge: "Latest additions",
      title: "What's new",
      subtitle: "The newest Qayd features — ready to use now",
      newLabel: "New",
      items: [
        { icon: "gold", title: "Gold system", desc: "Standalone gold-shop system: mithqal prices, stock, cash/credit sales, and scale." },
        { icon: "loyalty", title: "Loyalty system", desc: "Earn points on sales and redeem discounts — from settings, invoices, and POS." },
        { icon: "customFields", title: "Custom fields", desc: "Up to 8 fields (text, number, yes/no, choices) for products, customers, suppliers, and investors." },
        { icon: "search", title: "Smart search & profit check", desc: "Highlight product search with stock qty — plus cost/sell/profit check." },
        { icon: "chart", title: "Work summary", desc: "KPI report with charts, top customers, and busy hours — plus customer/product details from invoices." },
        { icon: "whatsapp", title: "Expanded WhatsApp", desc: "Share PDFs for vouchers, investor receipts, statements, and sales/purchase reports." },
        { icon: "truck", title: "Warehouse copy & driver", desc: "Print a warehouse copy without amounts and pick a driver on sales/installment invoices." },
        { icon: "salesBoost", title: "Sales upgrades", desc: "Discounts, packing types, shipping fees, list weight, and dinar change calculator (F7)." }
      ]
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
    platformInfra: {
      title: "Infrastructure",
      subtitle: "Branch linking, cloud sync, and reports for every business"
    },
    how: {
      title: "Get started in minutes",
      steps: [
        { num: "01", title: "Download", desc: "ZIP from GitHub — Windows 10/11" },
        { num: "02", title: "Pick system & PC role", desc: "Accounting, gold, hotel, car contracts, car trading, or real estate — main or branch PC" },
        { num: "03", title: "Work offline or on LAN", desc: "Local database or direct link to main server" }
      ]
    },
    network: {
      badge: "Available",
      title: "Main & branch PC linking",
      desc: "Connect multiple PCs on the same network (WiFi or Ethernet) to one database on the main computer — no sync, no internet required.",
      points: [
        "Works for all systems: accounting, gold, hotels, car contracts, car trading, real estate",
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
      desc: "Connect Desktop to Cloud API: accounting, hotels (& restaurant), car contracts, car trading, and real estate — Push/Pull with tenant isolation.",
      points: [
        "Sync by system type (Accounting / Hotel / Car / CarTrade / RealEstate)",
        "Invoices, reservations, contracts, trading, real estate, restaurant",
        "Smart conflicts + soft delete",
        "REST API + JWT for mobile",
        "Developer admin — tenants & licenses",
        "Multi-tenant data isolation"
      ]
    },
    reports: {
      title: "Comprehensive reports",
      groups: [
        { label: "Accounting", items: ["Sales & purchases", "Profit", "Statements", "Inventory", "Overdue installments", "Work summary", "Audit reports"] },
        { label: "Gold", items: ["Mithqal prices", "Stock movement", "Buy & sell", "Shop profit"] },
        { label: "Hotel", items: ["Occupancy", "Revenue", "Night audit", "Arrivals/departures"] },
        { label: "Restaurant", items: ["F&B sales", "Channels", "Top items", "F&B profit"] },
        { label: "Car contracts", items: ["Contracts report", "Collected/remaining", "Excel export"] },
        { label: "Car trading", items: ["Transactions", "Stock", "Profit", "Party statements"] },
        { label: "Real estate", items: ["Contracts report", "Profit", "Debtors", "Expenses"] }
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
        { q: "How to pick a system?", a: "At first setup: accounting for retail, gold for gold shops, hotel for hospitality, car contracts for dealership paperwork, car trading for buy→stock→sell, real estate for property offices." },
        { q: "What is the Gold system?", a: "A standalone system for Iraqi gold shops: mithqal prices, piece/mithqal inventory, cash & credit buy/sell, scale, market board, and shop movement reports." },
        { q: "Contracts vs car trading?", a: "Contracts formalize a sale between seller and buyer with print and witnesses. Car trading manages showroom inventory: purchase then sell with payments and reports." },
        { q: "What is real estate contracts?", a: "A system for property contracts, parties, debtors, expenses, and clause templates with profit reports — synced to mobile and cloud." },
        { q: "What is the loyalty system?", a: "An accounting feature that earns customers points on sales invoices and lets them redeem discounts. Enable it in business feature settings; it appears in invoices, POS, and loyalty reports." },
        { q: "What are custom fields?", a: "Settings to add up to 8 extra fields (text, number, yes/no, choices) for products, customers, suppliers, and investors — shown in grids and forms." },
        { q: "What are smart search and profit check?", a: "On sales and installment invoices: search products by name with highlight, stock quantities and prices, plus a profit-check button for cost, sell price, margin, and discount." },
        { q: "Is WhatsApp only for invoices?", a: "No. You can share PDFs via WhatsApp for invoices, vouchers, investor receipts, account statements, and sales/purchase reports." },
        { q: "What is warehouse copy & driver?", a: "When printing you can issue a warehouse copy without money amounts, and pick a driver on sales or installment invoices for picking and delivery." },
        { q: "What is the dinar change calculator?", a: "A quick dialog (F7) in POS and sales invoices to calculate Iraqi dinar change and speed up cash collection." },
        { q: "What is the Qayd voice assistant?", a: "A desktop voice feature (Ctrl+Space) to search, open screens, and run actions like quick sale without typing." },
        { q: "Does hotel include restaurant?", a: "Yes — POS, KDS, kitchen stock, tables, and F&B reports are built in." },
        { q: "Is mobile ready?", a: "Yes — multiple profiles: accounting, hotel, car contracts, car trading, and real estate by account type." },
        { q: "How to update?", a: "In-app check reads version.json from GitHub." },
        { q: "Data safe?", a: "Backup, permissions, audit log, multi-tenant cloud isolation." }
      ]
    },
    contact: { title: "Ready to try?", desc: "Download free and test your system", github: "GitHub repository" },
    footer: { rights: "All rights reserved — Qayd" }
  }
};
