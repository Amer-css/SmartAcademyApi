namespace SmartAcademyApi.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        // من فعل هذا؟ (معرف المستخدم المستخرج من التوكن، أو Anonymouse إذا لم يسجل دخوله)
        public string? UserId { get; set; }

        // نوع الحدث أو الإجراء (مثل: Login_Failed, Access_Forbidden, Admin_Action)
        public string Action { get; set; } = string.Empty;

        // المسار أو الـ Endpoint الذي تم استدعاؤه (مثل: /api/students/5)
        public string Path { get; set; } = string.Empty;

        // طريقة الطلب (GET, POST, PUT, DELETE)
        public string Method { get; set; } = string.Empty;

        // عنوان الـ IP الخاص بالعميل (لرصد مصدر الهجمات أو الأنشطة المشبوهة)
        public string? IpAddress { get; set; }

        // كود الاستجابة (200, 403, 404, 500) لمعرفة نتيجة الحدث
        public int StatusCode { get; set; }

        // وقت حدوث الفعل بدقة للمساءلة (Accountability)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // تفاصيل إضافية أو وصف للحدث (مثل القيم القديمة والجديدة في حال التعديل)
        public string? Details { get; set; }
    }
}
