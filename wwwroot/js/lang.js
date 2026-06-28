const translations = {
    en: {
        "BloodBank": "BloodBank",
        "Home": "Home",
        "Admin Dashboard": "Admin Dashboard",
        "Donor Portal": "Donor Portal",
        "Hospital Portal": "Hospital Portal",
        "Ministry Portal": "Ministry Portal",
        "Center Dashboard": "Center Dashboard",
        "Role": "Role",
        "Logout": "Logout",
        "Login": "Login",
        "System Live": "System Live",
        "Units in Stock": "Units in Stock",
        "Active Requests": "Active Requests",
        "Donors Ready": "Donors Ready",
        "Expiring Soon": "Expiring Soon",
        "Overview": "Overview",
        "Requests": "Requests",
        "Inventory": "Inventory",
        "Donors": "Donors",
        "Centers": "Centers",
        "Settings": "Settings",
        "Hospital": "Hospital",
        "Blood Type": "Blood Type",
        "Quantity": "Quantity",
        "Priority": "Priority",
        "Status": "Status",
        "Action": "Action",
        "Pending": "Pending",
        "Approved": "Approved",
        "Completed": "Completed",
        "Cancelled": "Cancelled",
        "Normal": "Normal",
        "Urgent": "Urgent",
        "Critical": "Critical",
        "Total Donors": "Total Donors",
        "Submit Request": "Submit Request",
        "Clear": "Clear",
        "Track Requests": "Track Requests",
        "Address": "Address",
        "Governorate": "Governorate",
        "Phone": "Phone",
        "Appointments": "Appointments",
        "Donation Centers": "Donation Centers",
        "Medical Questionnaires": "Medical Questionnaires",
        "Add Center": "Add Center",
        "Save Changes": "Save Changes",
        "Yes, Delete Permanently": "Yes, Delete Permanently",
        "Cancel": "Cancel",
        "Edit": "Edit",
        "Back to List": "Back to List",
        "Search": "Search..."
    },
    ar: {
        "BloodBank": "بنك الدم",
        "Home": "الرئيسية",
        "Admin Dashboard": "لوحة الإدارة",
        "Donor Portal": "بوابة المتبرع",
        "Hospital Portal": "بوابة المستشفى",
        "Ministry Portal": "بوابة الوزارة",
        "Center Dashboard": "لوحة المركز",
        "Role": "الصلاحية",
        "Logout": "تسجيل خروج",
        "Login": "تسجيل دخول",
        "System Live": "النظام يعمل",
        "Units in Stock": "الوحدات المتاحة",
        "Active Requests": "الطلبات النشطة",
        "Donors Ready": "المتبرعون الجاهزون",
        "Expiring Soon": "ينتهي قريباً",
        "Overview": "نظرة عامة",
        "Requests": "الطلبات",
        "Inventory": "المخزون",
        "Donors": "المتبرعون",
        "Centers": "المراكز",
        "Settings": "الإعدادات",
        "Hospital": "المستشفى",
        "Blood Type": "فصيلة الدم",
        "Quantity": "الكمية",
        "Priority": "الأولوية",
        "Status": "الحالة",
        "Action": "إجراء",
        "Pending": "قيد الانتظار",
        "Approved": "مقبول",
        "Completed": "مكتمل",
        "Cancelled": "ملغي",
        "Normal": "عادي",
        "Urgent": "عاجل",
        "Critical": "حرج",
        "Total Donors": "إجمالي المتبرعين",
        "Submit Request": "إرسال الطلب",
        "Clear": "تفريغ",
        "Track Requests": "تتبع الطلبات",
        "Address": "العنوان",
        "Governorate": "المحافظة",
        "Phone": "الهاتف",
        "Appointments": "المواعيد",
        "Donation Centers": "مراكز التبرع",
        "Medical Questionnaires": "الاستبيانات الطبية",
        "Add Center": "إضافة مركز",
        "Save Changes": "حفظ التغييرات",
        "Yes, Delete Permanently": "نعم، احذف نهائياً",
        "Cancel": "إلغاء",
        "Edit": "تعديل",
        "Back to List": "العودة للقائمة",
        "Search": "بحث..."
    }
};

function setLanguage(lang) {
    localStorage.setItem('bb_lang', lang);
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';

    // Toggle RTL Bootstrap
    let bsLink = document.getElementById('bootstrap-css');
    if (lang === 'ar') {
        bsLink.href = "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.0/css/bootstrap.rtl.min.css";
        document.body.classList.add('rtl-mode');
    } else {
        bsLink.href = "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.0/css/bootstrap.min.css";
        document.body.classList.remove('rtl-mode');
    }

    translateDOM(lang);
    
    // Update active state of language dropdown
    document.querySelectorAll('.lang-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.lang === lang);
    });
}

function translateDOM(lang) {
    const dict = translations[lang] || translations['en'];
    const reverseDict = translations[lang === 'ar' ? 'en' : 'ar']; // Used to find original text if we are translating back

    // Translate elements with data-i18n attribute
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (dict[key]) {
            // Keep icons intact if present inside the element
            const icon = el.querySelector('i');
            if (icon) {
                el.innerHTML = '';
                el.appendChild(icon);
                el.appendChild(document.createTextNode(' ' + dict[key]));
            } else {
                if (el.tagName === 'INPUT' && el.type === 'button' || el.type === 'submit') {
                    el.value = dict[key];
                } else if (el.placeholder) {
                    el.placeholder = dict[key];
                } else {
                    el.innerText = dict[key];
                }
            }
        }
    });

    // Auto-translate text nodes as fallback (basic implementation)
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
    let node;
    const textNodes = [];
    while (node = walker.nextNode()) {
        if (node.parentNode && !['SCRIPT', 'STYLE'].includes(node.parentNode.tagName)) {
            textNodes.push(node);
        }
    }

    textNodes.forEach(node => {
        let text = node.nodeValue.trim();
        if (!text) return;
        
        // Find English or Arabic key
        let keyToTranslate = null;
        if (translations['en'][text]) keyToTranslate = text;
        else {
            // Reverse lookup
            const engKey = Object.keys(translations['ar']).find(k => translations['ar'][k] === text);
            if (engKey) keyToTranslate = engKey;
        }

        if (keyToTranslate && dict[keyToTranslate]) {
            node.nodeValue = node.nodeValue.replace(text, dict[keyToTranslate]);
        }
    });
}

document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('bb_lang') || 'en';
    setLanguage(savedLang);
});
