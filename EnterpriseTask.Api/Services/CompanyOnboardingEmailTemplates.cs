using System.Globalization;
using System.Net;

namespace EnterpriseTask.Api.Services;

public static class CompanyOnboardingEmailTemplates
{
    public static string BuildCompanyAdminWelcomeEmail(
        string companyName,
        string adminFullName,
        string planName,
        decimal planPrice,
        DateTime startDate,
        DateTime endDate,
        string webAdminUrl,
        string adminEmail,
        string temporaryPassword)
    {
        var viCulture = CultureInfo.GetCultureInfo("vi-VN");

        return BuildLayout(
            "Tài khoản quản trị WorkFlow AI đã được kích hoạt",
            $"""
            <p style="margin:0 0 16px;color:#475569;">Xin chào <strong>{Encode(adminFullName)}</strong>,</p>
            <p style="margin:0 0 20px;color:#475569;line-height:1.65;">
                Công ty <strong>{Encode(companyName)}</strong> đã đăng ký thành công gói dịch vụ WorkFlow AI.
                Tài khoản quản trị công ty của bạn đã sẵn sàng.
            </p>
            <div style="margin-bottom:18px;padding:18px;border:1px solid #e2e8f0;border-radius:14px;background:#f8fafc;">
                {BuildInformationRow("Gói dịch vụ", Encode(planName))}
                {BuildInformationRow("Giá gói", $"{planPrice.ToString("N0", viCulture)} VNĐ")}
                {BuildInformationRow("Ngày bắt đầu", startDate.ToString("dd/MM/yyyy", viCulture))}
                {BuildInformationRow("Ngày hết hạn", endDate.ToString("dd/MM/yyyy", viCulture), true)}
            </div>
            <div style="padding:18px;border:1px solid #dbeafe;border-radius:14px;background:#f8fbff;">
                {BuildInformationRow("Web Admin", $"<a href=\"{Encode(webAdminUrl)}\" style=\"color:#2563eb;font-weight:700;\">{Encode(webAdminUrl)}</a>")}
                {BuildInformationRow("Email đăng nhập", Encode(adminEmail))}
                {BuildInformationRow("Mật khẩu tạm", $"<strong style=\"color:#1d4ed8;font-size:18px;letter-spacing:.04em;\">{Encode(temporaryPassword)}</strong>", true)}
            </div>
            <p style="margin:22px 0 8px;color:#334155;font-weight:700;">Bắt đầu sử dụng</p>
            <ol style="margin:0 0 20px;padding-left:20px;color:#475569;line-height:1.8;">
                <li>Mở link Web Admin ở trên.</li>
                <li>Đăng nhập bằng email và mật khẩu tạm.</li>
                <li>Tạo phòng ban, nhân viên và phân quyền cho công ty.</li>
            </ol>
            <p style="margin:0 0 10px;color:#b45309;line-height:1.6;">
                Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
            </p>
            <p style="margin:0;color:#64748b;font-size:13px;line-height:1.6;">
                Trong môi trường thực tế, hệ thống nên dùng link đặt mật khẩu thay vì gửi mật khẩu tạm.
            </p>
            """);
    }

    private static string BuildInformationRow(string label, string value, bool isLast = false) =>
        $"""
        <div style="margin-bottom:{(isLast ? "0" : "13px")};">
            <p style="margin:0 0 5px;color:#64748b;font-size:12px;">{Encode(label)}</p>
            <p style="margin:0;color:#0f172a;font-weight:700;">{value}</p>
        </div>
        """;

    private static string BuildLayout(string title, string content) =>
        $"""
        <!doctype html>
        <html lang="vi">
        <body style="margin:0;padding:28px;background:#f5f7fb;font-family:Roboto,Arial,sans-serif;">
            <div style="max-width:640px;margin:0 auto;overflow:hidden;border:1px solid #e5eaf1;border-radius:20px;background:#ffffff;box-shadow:0 18px 45px rgba(15,23,42,.08);">
                <div style="padding:24px 28px;background:#0f172a;color:#ffffff;">
                    <p style="margin:0 0 7px;color:#93c5fd;font-size:12px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;">WorkFlow AI</p>
                    <h1 style="margin:0;font-size:22px;line-height:1.35;">{Encode(title)}</h1>
                </div>
                <div style="padding:28px;font-size:15px;">
                    {content}
                </div>
            </div>
        </body>
        </html>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
