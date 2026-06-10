/** نصوص مضمّنة — تعمل عند فتح index.html مباشرة (file://) بدون خادم */
window.LOCALES = {
  ar: {
    meta: { title: "المحاسب — نظام محاسبة ومبيعات متكامل", description: "نظام محاسبة عربي احترافي: فواتير، مخازن، أقساط، تقارير، ومزامنة سحابية. يعمل أوفلاين بالكامل." },
    nav: { features: "المميزات", how: "كيف يعمل", videos: "الفيديوهات", reports: "التقارير", cloud: "السحابة", mobile: "التطبيق", download: "التنزيل", faq: "الأسئلة", contact: "تواصل" },
    support: { btn: "خدمة العملاء — واتساب", btnShort: "واتساب", float: "دعم واتساب", phoneLabel: "رقم الدعم:" },
    videos: {
      title: "دليل الفيديوهات التعليمية",
      subtitle: "شاهد شرح كل واجهة داخل النظام — نفس الدليل الموجود في التطبيق الأوفلاين",
      search: "بحث في الفيديوهات...",
      all: "الكل",
      count: "فيديو",
      pick: "اختر فيديو من القائمة",
      empty: "لا توجد فيديوهات مطابقة",
      noLink: "لم يُضبط رابط يوتيوب لهذا الفيديو بعد",
      categories: {
        dashboard: "لوحة التحكم", "master-data": "البيانات الأساسية", sales: "المبيعات",
        purchases: "المشتريات", installments: "الأقساط", finance: "المالية",
        inventory: "المخزون", reports: "التقارير", admin: "الإدارة والإعدادات"
      }
    },
    hero: { badge: "إصدار جديد متاح", title: "المحاسب", subtitle: "نظام محاسبة ومبيعات عربي — سريع، أوفلاين، وجاهز للنمو", desc: "فواتير مبيعات ومشتريات، نقطة بيع POS، مخازن، أقساط، سندات، تقارير شاملة، ومزامنة سحابية — في تطبيق واحد لسطح المكتب.", cta_download: "حمّل النظام مجاناً", cta_features: "استكشف المميزات", stat_modules: "وحدة", stat_reports: "تقرير", stat_offline: "أوفلاين", screen_caption: "واجهة النظام الأوفلاين — لوحة التحكم الرئيسية" },
    desktop: {
      pageTitle: "لوحة التحكم",
      menu: { dashboard: "لوحة التحكم", sales: "فاتورة مبيعات", customers: "العملاء", warehouses: "المخازن", installments: "الأقساط", reports: "التقارير" },
      quick: { sale: "بيع", purchase: "شراء", voucher: "سند" },
      stats: { sales: "مبيعات اليوم", profit: "صافي الربح", customers: "العملاء", invoices: "فواتير" },
      chart: "ملخص المبيعات — آخر 7 أيام"
    },
    mobile: {
      badge: "قريباً", title: "تطبيق جوال متصل بالـ API",
      desc: "نعمل على تطبيق للهاتف يتصل بـ API السحابي لجلب التقارير والكشوفات والبيانات لحظياً — من أي مكان وفي أي وقت.",
      points: ["تقارير المبيعات والأرباح والمخزون", "كشوف حساب العملاء والموردين", "متابعة الأقساط والمتأخرات", "مزامنة آمنة عبر API متعدد العملاء"],
      appName: "المحاسب", greeting: "مرحباً — تقاريرك جاهزة",
      cards: { sales: "تقرير المبيعات", statement: "كشف حساب عميل", stock: "رصيد المخزون", overdue: "أقساط متأخرة" },
      nav: { home: "الرئيسية", reports: "التقارير", data: "البيانات" },
      caption: "معاينة واجهة التطبيق — قيد التطوير"
    },
    features: { title: "كل ما يحتاجه نشاطك التجاري", subtitle: "وحدات متكاملة مصممة للمحلات والمخازن والشركات الصغيرة والمتوسطة", items: [
      { icon: "receipt", title: "فواتير المبيعات والمشتريات", desc: "فواتير كاملة مع خصومات، ذمم، ومرتجعات" },
      { icon: "pos", title: "بيع سريع POS", desc: "شاشة كاشير — باركود، دفع نقدي فوري" },
      { icon: "warehouse", title: "المخازن والمخزون", desc: "أرصدة افتتاحية، تسويات، وتتبع الكميات" },
      { icon: "installment", title: "الأقساط", desc: "خطط أقساط، متابعة التسديد، وتقارير المتأخرات" },
      { icon: "voucher", title: "السندات والمصاريف", desc: "قبض وصرف، مصاريف، وتحويلات بين القاصات والمصارف" },
      { icon: "investor", title: "المستثمرون", desc: "إدارة الحصص وتوزيع الأرباح" },
      { icon: "chart", title: "تقارير وتحليلات", desc: "أكثر من 25 تقريراً: مبيعات، أرباح، كشوف، وموازنة" },
      { icon: "cloud", title: "مزامنة سحابية", desc: "ربط الفروع مع API سحابي متعدد العملاء" },
      { icon: "ai", title: "مساعد ذكي", desc: "تنبيهات الأقساط المتأخرة والمخزون المنخفض" },
      { icon: "shield", title: "صلاحيات المستخدمين", desc: "تحكم دقيق بكل شاشة: إضافة، تعديل، حذف، طباعة" }
    ]},
    how: { title: "ابدأ في دقائق", steps: [
      { num: "01", title: "نزّل النظام", desc: "ملف مضغوط من GitHub — ثبّت على Windows" },
      { num: "02", title: "أعد بياناتك", desc: "منتجات، عملاء، مخازن، وأرصدة افتتاحية" },
      { num: "03", title: "ابدأ البيع", desc: "فواتير، POS، وتقارير فورية — بدون إنترنت" }
    ]},
    cloud: { title: "مزامنة سحابية آمنة", desc: "اربط الفروع مع السحابة: Push و Pull ثنائي الاتجاه مع عزل بيانات كل عميل.", points: ["فواتير كاملة مع البنود والأقساط", "مخازن ومخزون متزامن", "تعارضات ذكية دون فقدان بيانات", "API جاهز للتطبيقات والتقارير"] },
    reports: { title: "تقارير شاملة", items: ["المبيعات والمشتريات", "الأرباح والخسائر", "كشف حساب عميل", "المخزون والجرد", "الأقساط المتأخرة", "الموازنة اليومية", "نظرة عامة على العملاء"] },
    download: { title: "حمّل المحاسب الآن", desc: "آخر إصدار من GitHub Releases — تحديثات تلقائية من داخل التطبيق", btn: "تنزيل ZIP", version: "الإصدار", size: "الحجم", date: "تاريخ الإصدار", req: "متطلبات: Windows 10/11 — .NET 10" },
    faq: { title: "أسئلة شائعة", items: [
      { q: "هل يعمل بدون إنترنت؟", a: "نعم. النظام أوفلاين بالكامل. الإنترنت مطلوب فقط للمزامنة السحابية والتحديثات." },
      { q: "هل يدعم الأقساط؟", a: "نعم — فواتير أقساط، خطط دفع، متابعة المتأخرات، وتقارير مخصصة." },
      { q: "كيف أحدّث النظام؟", a: "من داخل التطبيق عبر التحقق من التحديثات — يقرأ من version.json على GitHub." },
      { q: "هل البيانات آمنة؟", a: "نسخ احتياطي محلي، صلاحيات مستخدمين، وسجل تدقيق للعمليات." }
    ]},
    contact: { title: "جاهز للتجربة؟", desc: "نزّل النظام مجاناً وجرّبه على نشاطك", github: "المستودع على GitHub" },
    footer: { rights: "جميع الحقوق محفوظة — المحاسب" }
  },
  en: {
    meta: { title: "AlMuhasib — Complete Accounting & Sales System", description: "Professional Arabic accounting: invoices, inventory, installments, reports, and cloud sync. Fully offline." },
    nav: { features: "Features", how: "How it works", videos: "Videos", reports: "Reports", cloud: "Cloud", mobile: "Mobile app", download: "Download", faq: "FAQ", contact: "Contact" },
    support: { btn: "Customer support — WhatsApp", btnShort: "WhatsApp", float: "WhatsApp support", phoneLabel: "Support number:" },
    videos: {
      title: "Video tutorials",
      subtitle: "Watch a walkthrough of every screen — same guide as in the offline app",
      search: "Search videos...",
      all: "All",
      count: "videos",
      pick: "Pick a video from the list",
      empty: "No matching videos",
      noLink: "YouTube link not configured yet",
      categories: {
        dashboard: "Dashboard", "master-data": "Master data", sales: "Sales",
        purchases: "Purchases", installments: "Installments", finance: "Finance",
        inventory: "Inventory", reports: "Reports", admin: "Admin & settings"
      }
    },
    hero: { badge: "New release available", title: "AlMuhasib", subtitle: "Arabic accounting & POS — fast, offline, built to scale", desc: "Sales & purchase invoices, POS, warehouses, installments, vouchers, 25+ reports, and cloud sync — in one desktop app.", cta_download: "Download free", cta_features: "Explore features", stat_modules: "modules", stat_reports: "reports", stat_offline: "offline", screen_caption: "Offline desktop app — main dashboard" },
    desktop: {
      pageTitle: "Dashboard",
      menu: { dashboard: "Dashboard", sales: "Sales invoice", customers: "Customers", warehouses: "Warehouses", installments: "Installments", reports: "Reports" },
      quick: { sale: "Sale", purchase: "Purchase", voucher: "Voucher" },
      stats: { sales: "Today's sales", profit: "Net profit", customers: "Customers", invoices: "Invoices" },
      chart: "Sales summary — last 7 days"
    },
    mobile: {
      badge: "Coming soon", title: "Mobile app powered by API",
      desc: "A mobile app connecting to the cloud API to fetch reports, statements, and live data — anytime, anywhere.",
      points: ["Sales, profit & inventory reports", "Customer & supplier statements", "Installment & overdue tracking", "Secure multi-tenant API sync"],
      appName: "AlMuhasib", greeting: "Welcome — your reports are ready",
      cards: { sales: "Sales report", statement: "Customer statement", stock: "Stock balance", overdue: "Overdue installments" },
      nav: { home: "Home", reports: "Reports", data: "Data" },
      caption: "App UI preview — in development"
    },
    features: { title: "Everything your business needs", subtitle: "Integrated modules for shops, warehouses, and SMBs", items: [
      { icon: "receipt", title: "Sales & purchase invoices", desc: "Full invoicing with discounts, credit, and returns" },
      { icon: "pos", title: "Quick POS", desc: "Cashier screen — barcode scan, instant cash sale" },
      { icon: "warehouse", title: "Warehouses & stock", desc: "Opening balances, adjustments, quantity tracking" },
      { icon: "installment", title: "Installments", desc: "Payment plans, collections, overdue reports" },
      { icon: "voucher", title: "Vouchers & expenses", desc: "Receipts, payments, transfers between cash & banks" },
      { icon: "investor", title: "Investors", desc: "Share management and profit distribution" },
      { icon: "chart", title: "Reports & analytics", desc: "25+ reports: sales, profit, statements, balance sheet" },
      { icon: "cloud", title: "Cloud sync", desc: "Connect branches via multi-tenant cloud API" },
      { icon: "ai", title: "Smart assistant", desc: "Alerts for overdue installments and low stock" },
      { icon: "shield", title: "User permissions", desc: "Fine-grained access per screen" }
    ]},
    how: { title: "Get started in minutes", steps: [
      { num: "01", title: "Download", desc: "ZIP from GitHub — install on Windows" },
      { num: "02", title: "Set up data", desc: "Products, customers, warehouses, opening balances" },
      { num: "03", title: "Start selling", desc: "Invoices, POS, instant reports — no internet required" }
    ]},
    cloud: { title: "Secure cloud sync", desc: "Connect branches to the cloud: two-way Push & Pull with per-tenant data isolation.", points: ["Full invoices with line items & installments", "Synced warehouses & stock", "Smart conflict handling", "API ready for apps & reports"] },
    reports: { title: "Comprehensive reports", items: ["Sales & purchases", "Profit & loss", "Customer statement", "Inventory", "Overdue installments", "Daily balance sheet", "Customers overview"] },
    download: { title: "Download AlMuhasib", desc: "Latest build from GitHub Releases — in-app auto-update", btn: "Download ZIP", version: "Version", size: "Size", date: "Release date", req: "Requirements: Windows 10/11 — .NET 10" },
    faq: { title: "FAQ", items: [
      { q: "Does it work offline?", a: "Yes. Fully offline. Internet is only needed for cloud sync and updates." },
      { q: "Installments support?", a: "Yes — installment invoices, plans, overdue tracking, and dedicated reports." },
      { q: "How to update?", a: "In-app update check reads version.json from GitHub." },
      { q: "Is data safe?", a: "Local backup, user permissions, and audit log." }
    ]},
    contact: { title: "Ready to try?", desc: "Download free and test on your business", github: "GitHub repository" },
    footer: { rights: "All rights reserved — AlMuhasib" }
  }
};
