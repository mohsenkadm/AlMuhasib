/** نصوص مضمّنة — تعمل عند فتح index.html مباشرة (file://) بدون خادم */
window.LOCALES = {
  ar: {
    meta: {
      title: "قيد — محاسبة، ذهب، فنادق، سيارات، عقارات وتطبيق جوال",
      description: "منصة أعمال عربية متكاملة: محاسبة، ذهب، فنادق، عقود سيارات، تجارة سيارات، عقود عقارات، تعدد عملات، فروع متعددة، ولاء، حقول مخصصة، مساعد صوتي، ربط فروع، تطبيقات مجانية، ومزامنة سحابية. تعمل أوفلاين بالكامل."
    },
    nav: {
      systems: "الأنظمة", whatsNew: "الجديد", features: "المنصة", freeApps: "تطبيقات مجانية",
      how: "كيف يعمل", videos: "الفيديوهات",
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
        inventory: "المخزون", reports: "التقارير", admin: "الإدارة والإعدادات",
        "damage-invoice": "فاتورة تلف", pos: "بيع سريع (POS)", products: "المنتجات",
        categories: "تصنيفات المنتجات", "packaging-types": "أنواع التعبئة",
        "pricing-types": "أنواع التسعير", "product-pricing": "تسعير منتجات",
        warehouses: "المخازن", "opening-stock": "الأرصدة الافتتاحية"
      }
    },
    splash: { tagline: "منصة أعمال متكاملة" },
    hero: {
      badge: "جديد: تعدد العملات دينار/دولار + الفروع المتعددة",
      title: "قيد",
      subtitle: "منصة أعمال متكاملة —",
      rotateWords: ["محاسبة", "ذهب", "فنادق", "عقود سيارات", "تجارة سيارات", "عقود عقارات"],
      desc: "ستة أنظمة سطح مكتب أوفلاين + تعدد عملات + فروع متعددة + مساعد صوتي + ربط شبكة + تطبيق جوال + مزامنة سحابية — عربي، جاهز للنمو.",
      cta_download: "حمّل النظام مجاناً",
      cta_systems: "استكشف الأنظمة",
      cta_features: "مميزات المنصة",
      stat_systems: "أنظمة",
      stat_reports: "تقرير+",
      stat_offline: "أوفلاين",
      screen_caption: "واجهة النظام الأوفلاين — لوحة التحكم",
      map_caption: "شبكة قيد — تربط أعمالك عبر العراق",
      mapCities: {
        baghdad: "بغداد", basra: "البصرة", erbil: "أربيل", mosul: "الموصل",
        kirkuk: "كركوك", sulaymaniyah: "السليمانية", najaf: "النجف",
        karbala: "كربلاء", anbar: "الأنبار", nasiriyah: "الناصرية",
        amarah: "العمارة", kut: "الكوت", diyala: "ديالى", hillah: "الحلة",
        tikrit: "تكريت", duhok: "دهوك"
      }
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
          desc: "فواتير، POS، تعدد عملات دينار/دولار، فروع متعددة، تسعير، مخازن، أقساط، مستثمرون، ولاء، حقول مخصصة، مساعد صوتي، واتساب موسّع، وأكثر من 30 تقريراً — للمحلات والمخازن والشركات.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "نظام المحاسبة — لوحة التحكم والمبيعات",
          highlights: [
            "تعدد العملات دينار/دولار مع مبادلة وصرف",
            "فروع متعددة مع عزل بيانات وصلاحيات لكل فرع",
            "فاتورة مبيعات + بيع سريع POS مع باركود ومفضلة",
            "خصم نسبة لكل بند + مخزن مختلف لكل سطر",
            "بحث مواد ذكي مع تمييز وكميات المخازن + فحص الربح",
            "نظام الولاء — نقاط من البيع واستبدال خصم",
            "حقول مخصصة للمنتجات والعملاء والموردين والمستثمرين",
            "موظفين وسندات ومصاريف محسّنة",
            "واتساب موسّع: فواتير، سندات، كشوف، وتقارير",
            "قوالب قطاعات: جوالات، ألبسة، مقاولات، صيدلية",
            "30+ تقرير: مبيعات، أرباح، أقساط، كشوف، رقابية"
          ],
          modules: [
            { title: "المبيعات ونقطة البيع", items: ["فاتورة مبيعات", "بيع سريع POS", "فحص سعر وربح"] },
            { title: "المنتجات والمخزون", items: ["منتجات وتصنيفات", "حقول مخصصة", "مخازن ونقل وتسوية"] },
            { title: "المشتريات والأقساط", items: ["فاتورة ومرتجع مشتريات", "فاتورة أقساط", "لوحة التحصيل"] },
            { title: "المالية والفروع", items: ["تعدد عملات", "فروع متعددة", "سندات ومصاريف وولاء"] }
          ],
          features: [
            { icon: "currency", title: "تعدد العملات", desc: "دينار ودولار مع صرف ومبادلة وتقارير منفصلة" },
            { icon: "branches", title: "الفروع المتعددة", desc: "فروع مستقلة ببيانات وصلاحيات وترقيم خاص" },
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
      caption: "قيد المحاسبي — iOS و Android",
      storeGoogle: "Google Play",
      storeApple: "App Store",
      playStore: "https://play.google.com/store/apps/details?id=com.almuhasib.almuhasib_mobile",
      appStore: "https://apps.apple.com/iq/app/%D9%82%D9%8A%D8%AF-%D8%A7%D9%84%D9%85%D8%AD%D8%A7%D8%B3%D8%A8%D9%8A/id6804299551"
    },
    whatsNew: {
      badge: "أحدث الإضافات",
      title: "ميزات جديدة",
      subtitle: "أحدث ما أُضيف لمنصة قيد — جاهز للاستخدام الآن",
      newLabel: "جديد",
      items: [
        { icon: "currency", title: "تعدد العملات دينار/دولار", desc: "إدارة المبيعات والمشتريات والأرصدة بالدينار والدولار معاً — مع تقارير منفصلة لكل عملة." },
        { icon: "branches", title: "الفروع المتعددة", desc: "أنشئ فروعاً مستقلة ببيانات وصلاحيات وترقيم فواتير خاص لكل فرع — مع تبديل سريع وتقارير مجمّعة." },
        { icon: "exchange", title: "مبادلة العملة", desc: "صرف ومبادلة بين الدينار والدولار من داخل النظام مع تتبع سعر الصرف والرصيد." },
        { icon: "employees", title: "الموظفون والمصاريف", desc: "إدارة الموظفين والسندات والمصاريف وأسعار الشراء بشكل أوضح وأكثر تنظيماً." },
        { icon: "receipt", title: "خصم نسبة لكل بند", desc: "حقل خصم % على مستوى كل مادة في فواتير البيع والشراء والأقساط — يظهر في الطباعة وإيصال POS." },
        { icon: "warehouse", title: "مخزن لكل بند", desc: "اختيار مخزن مختلف لكل سطر في الفاتورة — مع تحديث المخزون حسب البند وليس الفاتورة فقط." },
        { icon: "shield", title: "التحقق قبل الحفظ", desc: "حوار تحقق يمنع الأخطاء — تنبيه البيع تحت التكلفة وحد ائتمان العميل عند تجاوز السقف." },
        { icon: "pos", title: "POS ملء الشاشة", desc: "نافذة كاشير بملء الشاشة للبيع السريع — مثالية للشاشات اللمسية ونقاط البيع." },
        { icon: "gold", title: "نظام الذهب", desc: "نظام مستقل لمحلات الذهب: أسعار المثقال، المخزون، البيع والآجل، والميزان." },
        { icon: "loyalty", title: "نظام الولاء", desc: "نقاط تُكسب من البيع وتُستبدل خصماً — من الإعدادات والفواتير وPOS." }
      ]
    },
    features: {
      title: "مميزات المنصة المشتركة",
      subtitle: "ما يجمع كل الأنظمة — بنية تحتية موثوقة",
      items: [
        { icon: "offline", title: "100% أوفلاين", desc: "يعمل بدون إنترنت — المزامنة اختيارية" },
        { icon: "branches", title: "فروع متعددة", desc: "فروع مستقلة ببيانات وصلاحيات وترقيم خاص" },
        { icon: "network", title: "ربط الشبكة", desc: "حاسبة رئيسية + فروع عبر WiFi — اتصال مباشر بدون مزامنة" },
        { icon: "currency", title: "تعدد العملات", desc: "دينار ودولار مع صرف وتقارير منفصلة" },
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
    freeApps: {
      badge: "مجاني بالكامل",
      title: "تطبيقات مجانية تدعمها قيد",
      subtitle: "أدوات بسيطة ومجانية 100% من منصة قيد — بدون اشتراك",
      storeGoogle: "Google Play",
      storeApple: "App Store",
      storeSoon: "قريباً على Google Play",
      apps: [
        {
          id: "dayni",
          name: "دَيني",
          tagline: "إدارة الديون والمبالغ المستحقة",
          desc: "سجّل ديونك ومستحقاتك بسهولة، تابع الدفعات والأرصدة وتواريخ الاستحقاق — سواء لك مبالغ عند الآخرين أو عليك للغير. مجاني بالكامل.",
          icon: "assets/dayni-icon.png",
          accent: "dayni",
          features: [
            "تسجيل الديون والمستحقات بسهولة",
            "متابعة الدفعات والمبالغ المتبقية",
            "تنظيم بيانات الأشخاص المرتبطين",
            "واجهة بسيطة للاستخدام اليومي",
            "مجاني بالكامل"
          ],
          playStore: "https://play.google.com/store/apps/details?id=com.qaid.dayni",
          appStore: "https://apps.apple.com/iq/app/%D8%AF-%D9%8A%D9%86%D9%8A/id6813496542?l=ar"
        },
        {
          id: "sundooqi",
          name: "صندوقي كاش",
          tagline: "إدارة صندوق المحل يومياً",
          desc: "سجّل المبيعات والمصاريف والسحب، اعرف كم موجود في الصندوق، واعمل جرد نهاية اليوم لاكتشاف أي فرق — سريع وبسيط وبدون تعقيد محاسبي. مجاني بالكامل.",
          icon: "assets/sundooqi-icon.png",
          accent: "sundooqi",
          features: [
            "تسجيل مبيعات ومشتريات ومصاريف وسحب",
            "رصيد الصندوق محسوب تلقائياً",
            "جرد نهاية اليوم ومقارنة الرصيد الفعلي",
            "اكتشاف فرق الصندوق فوراً",
            "يعمل بدون إنترنت — مجاني بالكامل"
          ],
          playStore: "",
          appStore: "https://apps.apple.com/iq/app/%D8%B5%D9%86%D8%AF%D9%88%D9%82%D9%8A-%D9%83%D8%A7%D8%B4/id6814638729"
        }
      ]
    },
    platformInfra: {
      title: "البنية التحتية",
      subtitle: "فروع متعددة، ربط شبكة، مزامنة سحابية، وتقارير شاملة لكل نشاط"
    },
    how: {
      title: "ابدأ في دقائق",
      steps: [
        { num: "01", title: "نزّل النظام", desc: "ملف EXE من GitHub — Windows 10/11" },
        { num: "02", title: "اختر نظامك ونوع الحاسبة", desc: "محاسبة، ذهب، فندق، عقود سيارات، تجارة سيارات، أو عقارات — رئيسية أو فرعية" },
        { num: "03", title: "اعمل أوفلاين أو عبر الشبكة", desc: "قاعدة محلية أو اتصال مباشر بالحاسبة الرئيسية" }
      ]
    },
    branches: {
      badge: "جديد",
      title: "الفروع المتعددة",
      desc: "أدر عدة فروع من شركة واحدة: بيانات منفصلة لكل فرع، صلاحيات المستخدمين، ترقيم فواتير مستقل، وتقارير لفرع واحد أو لكل الفروع.",
      points: [
        "فرع رئيسي + فروع إضافية بعزل كامل للبيانات",
        "تعيين المستخدمين للفروع مع فرع افتراضي",
        "ترقيم فواتير وسندات مستقل لكل فرع",
        "تبديل الفرع بسرعة من داخل التطبيق",
        "تقارير مجمّعة لكل الفروع بصلاحية خاصة",
        "مزامنة سحابية وجوال مع عزل الفرع"
      ],
      diagram: {
        company: "الشركة",
        main: "الفرع الرئيسي",
        branch1: "فرع 1",
        branch2: "فرع 2",
        branch3: "فرع 3",
        caption: "بيانات وصلاحيات وترقيم مستقل لكل فرع"
      }
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
        { label: "المحاسبة", items: ["المبيعات والمشتريات", "الأرباح", "كشف حساب", "المخزون", "الأقساط المتأخرة", "ملخص العمل", "تعدد العملات", "تقارير الفروع"] },
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
      btn: "تنزيل المثبت EXE", version: "الإصدار", size: "الحجم", date: "تاريخ الإصدار",
      req: "متطلبات: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "أسئلة شائعة",
      items: [
        { q: "هل يعمل بدون إنترنت؟", a: "نعم. كل الأنظمة أوفلاين بالكامل. الإنترنت للمزامنة السحابية والتحديثات فقط — ربط الفروع المحلي لا يحتاج إنترنت." },
        { q: "ما هي الفروع المتعددة؟", a: "ميزة في المحاسبة لإنشاء فروع مستقلة داخل الشركة: بيانات منفصلة، صلاحيات مستخدمين، وترقيم فواتير لكل فرع. يمكن تبديل الفرع وعرض تقارير مجمّعة بصلاحية خاصة." },
        { q: "ما الفرق بين الفروع المتعددة وربط الحاسبات؟", a: "الفروع المتعددة = تنظيم أعمال (بيانات كل فرع منفصلة). ربط الحاسبات = عدة أجهزة على نفس الشبكة تتصل بقاعدة البيانات مباشرة عبر WiFi/LAN." },
        { q: "هل يدعم تعدد العملات؟", a: "نعم — دينار ودولار اختيارياً في المحاسبة، مع مبادلة العملة وتقارير أرصدة منفصلة لكل عملة." },
        { q: "كيف أربط فرعاً بالحاسبة الرئيسية؟", a: "عند التنصيب اختر «حاسبة فرعية»، ابحث عن الرئيسية على الشبكة أو أدخل IP، ثم أدخل رمز الربط. يمكن تعديل الإعدادات لاحقاً من «ربط الحاسبات»." },
        { q: "هل ربط الفروع يحتاج مزامنة؟", a: "لا. الفرعية تتصل مباشرة بقاعدة البيانات على الرئيسية عبر WiFi/LAN — مثل عدة مستخدمين على نفس السيرفر." },
        { q: "كيف أختار النظام المناسب؟", a: "عند الإعداد الأول: محاسبة للمحلات، ذهب لمحلات الذهب، فندق للضيافة، عقود سيارات للمعارض، تجارة سيارات لدورة الشراء والبيع، وعقود عقارات لمكاتب العقارات." },
        { q: "ما هو نظام الذهب؟", a: "نظام مستقل لمحلات الذهب العراقية: أسعار المثقال، مخزون القطع والمثاقيل، بيع وشراء نقدي وآجل، ميزان، لوحة تنافسية، وتقارير حركة المحل." },
        { q: "ما الفرق بين عقود السيارات وتجارة السيارات؟", a: "عقود السيارات لإبرام عقود بيع بين بائع ومشتري مع طباعة وشهود. تجارة السيارات لإدارة مخزون المعرض: شراء ثم بيع مع دفعات وتقارير." },
        { q: "ما هو نظام عقود العقارات؟", a: "نظام لإدارة العقود العقارية والزبائن والمدينين والمصاريف وبنود العقد مع تقارير أرباح — متزامن مع الجوال والسحابة." },
        { q: "ما هي التطبيقات المجانية؟", a: "منصة قيد تدعم تطبيقات مجانية 100%: «دَيني» لإدارة الديون والمستحقات، و«صندوقي كاش» لتسجيل مبيعات ومصاريف الصندوق وجرد نهاية اليوم. دَيني على Google Play و App Store، وصندوقي كاش على App Store حالياً." },
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
      description: "Integrated Arabic business platform: accounting, gold shops, hotels, car contracts, car trading, real estate, multi-currency, multi-branch, loyalty, custom fields, voice assistant, LAN linking, free apps, and cloud sync. Fully offline."
    },
    nav: {
      systems: "Systems", whatsNew: "What's new", features: "Platform", freeApps: "Free apps",
      how: "How it works", videos: "Videos",
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
        inventory: "Inventory", reports: "Reports", admin: "Admin & settings",
        "damage-invoice": "Damage invoice", pos: "Quick sale (POS)", products: "Products",
        categories: "Product categories", "packaging-types": "Packaging types",
        "pricing-types": "Pricing types", "product-pricing": "Product pricing",
        warehouses: "Warehouses", "opening-stock": "Opening stock"
      }
    },
    splash: { tagline: "All-in-one business platform" },
    hero: {
      badge: "New: IQD/USD multi-currency + multi-branch",
      title: "Qayd",
      subtitle: "Integrated business platform —",
      rotateWords: ["Accounting", "Gold", "Hotels", "Car contracts", "Car trading", "Real estate"],
      desc: "Six offline desktop systems + multi-currency + multi-branch + voice assistant + LAN linking + mobile app + cloud sync — Arabic, built to scale.",
      cta_download: "Download free",
      cta_systems: "Explore systems",
      cta_features: "Platform features",
      stat_systems: "systems",
      stat_reports: "reports+",
      stat_offline: "offline",
      screen_caption: "Offline desktop — dashboard",
      map_caption: "Qayd network — connecting businesses across Iraq",
      mapCities: {
        baghdad: "Baghdad", basra: "Basra", erbil: "Erbil", mosul: "Mosul",
        kirkuk: "Kirkuk", sulaymaniyah: "Sulaymaniyah", najaf: "Najaf",
        karbala: "Karbala", anbar: "Anbar", nasiriyah: "Nasiriyah",
        amarah: "Amarah", kut: "Kut", diyala: "Diyala", hillah: "Hillah",
        tikrit: "Tikrit", duhok: "Duhok"
      }
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
          desc: "Invoices, POS, IQD/USD multi-currency, multi-branch, pricing, warehouses, installments, investors, loyalty, custom fields, voice assistant, expanded WhatsApp, and 30+ reports — for shops and SMBs.",
          screenshot: "assets/desktop-accounting.png",
          screenshotCaption: "Accounting — dashboard & sales",
          highlights: [
            "IQD/USD multi-currency with exchange",
            "Multi-branch with data isolation and per-branch permissions",
            "Sales invoice + quick POS with barcode & favorites",
            "Per-line discount % + warehouse per invoice row",
            "Smart product search with stock qty + profit check",
            "Loyalty — earn points on sales, redeem as discount",
            "Custom fields for products, customers, suppliers, investors",
            "Employees, vouchers, and expenses — clearer workflows",
            "Expanded WhatsApp: invoices, vouchers, statements, reports",
            "Industry templates: phones, clothing, construction, pharmacy",
            "30+ reports: sales, profit, installments, statements, audit"
          ],
          modules: [
            { title: "Sales & POS", items: ["Sales invoice", "Quick POS", "Price & profit check"] },
            { title: "Products & stock", items: ["Products & categories", "Custom fields", "Warehouses, transfers, adjustments"] },
            { title: "Purchases & installments", items: ["Purchase & returns", "Installment invoice", "Collection board"] },
            { title: "Finance & branches", items: ["Multi-currency", "Multi-branch", "Vouchers, expenses & loyalty"] }
          ],
          features: [
            { icon: "currency", title: "Multi-currency", desc: "IQD & USD with exchange and separate reports" },
            { icon: "branches", title: "Multi-branch", desc: "Independent branches with data, ACL, and numbering" },
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
      caption: "Qayd Accounting — iOS & Android",
      storeGoogle: "Google Play",
      storeApple: "App Store",
      playStore: "https://play.google.com/store/apps/details?id=com.almuhasib.almuhasib_mobile",
      appStore: "https://apps.apple.com/iq/app/%D9%82%D9%8A%D8%AF-%D8%A7%D9%84%D9%85%D8%AD%D8%A7%D8%B3%D8%A8%D9%8A/id6804299551"
    },
    whatsNew: {
      badge: "Latest additions",
      title: "What's new",
      subtitle: "The newest Qayd features — ready to use now",
      newLabel: "New",
      items: [
        { icon: "currency", title: "IQD/USD multi-currency", desc: "Run sales, purchases, and balances in dinar and dollar together — with separate reports per currency." },
        { icon: "branches", title: "Multi-branch", desc: "Create independent branches with separate data, permissions, and invoice numbering — plus fast switching and all-branch reports." },
        { icon: "exchange", title: "Currency exchange", desc: "Exchange between IQD and USD inside the system with rate tracking and balances." },
        { icon: "employees", title: "Employees & expenses", desc: "Clearer employee, voucher, expense, and purchase-price workflows." },
        { icon: "receipt", title: "Per-line discount %", desc: "Discount % on each line in sales, purchase, and installment invoices — shown on print and POS receipts." },
        { icon: "warehouse", title: "Warehouse per line", desc: "Pick a different warehouse for each invoice row — stock updates per line, not just per invoice." },
        { icon: "shield", title: "Pre-save validation", desc: "Validation dialogs block mistakes — below-cost warnings and customer credit limit checks." },
        { icon: "pos", title: "Fullscreen POS", desc: "Dedicated fullscreen cashier window for quick sales — ideal for touch screens and checkout desks." },
        { icon: "gold", title: "Gold system", desc: "Standalone gold-shop system: mithqal prices, stock, cash/credit sales, and scale." },
        { icon: "loyalty", title: "Loyalty system", desc: "Earn points on sales and redeem discounts — from settings, invoices, and POS." }
      ]
    },
    features: {
      title: "Shared platform features",
      subtitle: "What powers every system — reliable infrastructure",
      items: [
        { icon: "offline", title: "100% offline", desc: "Works without internet — sync optional" },
        { icon: "branches", title: "Multi-branch", desc: "Independent branches with data, ACL, and numbering" },
        { icon: "network", title: "LAN linking", desc: "Main PC + branches over WiFi — direct DB, no sync" },
        { icon: "currency", title: "Multi-currency", desc: "IQD & USD with exchange and separate reports" },
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
    freeApps: {
      badge: "Completely free",
      title: "Free apps powered by Qayd",
      subtitle: "Simple 100% free tools from the Qayd platform — no subscription",
      storeGoogle: "Google Play",
      storeApple: "App Store",
      storeSoon: "Coming soon on Google Play",
      apps: [
        {
          id: "dayni",
          name: "Dayni",
          tagline: "Manage debts & receivables",
          desc: "Record debts and outstanding amounts easily, track payments, balances, and due dates — whether others owe you or you owe them. Completely free.",
          icon: "assets/dayni-icon.png",
          accent: "dayni",
          features: [
            "Record debts and receivables easily",
            "Track payments and remaining balances",
            "Organize people linked to each debt",
            "Simple interface for daily use",
            "Completely free"
          ],
          playStore: "https://play.google.com/store/apps/details?id=com.qaid.dayni",
          appStore: "https://apps.apple.com/iq/app/%D8%AF-%D9%8A%D9%86%D9%8A/id6813496542?l=ar"
        },
        {
          id: "sundooqi",
          name: "Sundooqi Cash",
          tagline: "Daily shop cash-box management",
          desc: "Record sales, expenses, and withdrawals, see what's in the cash box, and run end-of-day cash counts to catch any difference — fast, simple, no accounting complexity. Completely free.",
          icon: "assets/sundooqi-icon.png",
          accent: "sundooqi",
          features: [
            "Record sales, purchases, expenses, and withdrawals",
            "Cash balance calculated automatically",
            "End-of-day count vs expected balance",
            "Spot cash differences instantly",
            "Works offline — completely free"
          ],
          playStore: "",
          appStore: "https://apps.apple.com/iq/app/%D8%B5%D9%86%D8%AF%D9%88%D9%82%D9%8A-%D9%83%D8%A7%D8%B4/id6814638729"
        }
      ]
    },
    platformInfra: {
      title: "Infrastructure",
      subtitle: "Multi-branch, LAN linking, cloud sync, and reports for every business"
    },
    how: {
      title: "Get started in minutes",
      steps: [
        { num: "01", title: "Download", desc: "EXE from GitHub — Windows 10/11" },
        { num: "02", title: "Pick system & PC role", desc: "Accounting, gold, hotel, car contracts, car trading, or real estate — main or branch PC" },
        { num: "03", title: "Work offline or on LAN", desc: "Local database or direct link to main server" }
      ]
    },
    branches: {
      badge: "New",
      title: "Multi-branch",
      desc: "Run multiple branches under one company: separate data per branch, user permissions, independent invoice numbering, and reports for one branch or all branches.",
      points: [
        "Main branch + extra branches with full data isolation",
        "Assign users to branches with a default branch",
        "Independent invoice and voucher numbering per branch",
        "Switch branches quickly inside the app",
        "All-branch reports with a dedicated permission",
        "Cloud and mobile sync with branch isolation"
      ],
      diagram: {
        company: "Company",
        main: "Main branch",
        branch1: "Branch 1",
        branch2: "Branch 2",
        branch3: "Branch 3",
        caption: "Separate data, permissions, and numbering per branch"
      }
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
        { label: "Accounting", items: ["Sales & purchases", "Profit", "Statements", "Inventory", "Overdue installments", "Work summary", "Multi-currency", "Branch reports"] },
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
      btn: "Download Setup EXE", version: "Version", size: "Size", date: "Release date",
      req: "Requirements: Windows 10/11 — .NET 10"
    },
    faq: {
      title: "FAQ",
      items: [
        { q: "Works offline?", a: "Yes. All systems are fully offline. Internet is for cloud sync and updates only — LAN branch linking needs no internet." },
        { q: "What is multi-branch?", a: "An accounting feature to create independent branches under one company: separate data, user permissions, and invoice numbering per branch. Switch branches and run all-branch reports with a special permission." },
        { q: "Multi-branch vs LAN PC linking?", a: "Multi-branch = business structure (separate data per branch). LAN linking = multiple PCs on the same network connecting directly to one database over WiFi/LAN." },
        { q: "Does it support multi-currency?", a: "Yes — optional IQD and USD in accounting, with currency exchange and separate balance reports per currency." },
        { q: "How to link a branch PC?", a: "At setup choose Branch PC, discover the main server on your network or enter its IP, then enter the pairing code. Change settings anytime under Network Linking." },
        { q: "Does branch linking use sync?", a: "No. Branch PCs connect directly to the main database over WiFi/LAN — like multiple users on one SQL Server." },
        { q: "How to pick a system?", a: "At first setup: accounting for retail, gold for gold shops, hotel for hospitality, car contracts for dealership paperwork, car trading for buy→stock→sell, real estate for property offices." },
        { q: "What is the Gold system?", a: "A standalone system for Iraqi gold shops: mithqal prices, piece/mithqal inventory, cash & credit buy/sell, scale, market board, and shop movement reports." },
        { q: "Contracts vs car trading?", a: "Contracts formalize a sale between seller and buyer with print and witnesses. Car trading manages showroom inventory: purchase then sell with payments and reports." },
        { q: "What is real estate contracts?", a: "A system for property contracts, parties, debtors, expenses, and clause templates with profit reports — synced to mobile and cloud." },
        { q: "What are the free apps?", a: "Qayd powers 100% free companion apps: Dayni for debts and receivables, and Sundooqi Cash for daily cash-box sales, expenses, and end-of-day counts. Dayni is on Google Play and the App Store; Sundooqi Cash is on the App Store for now." },
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
