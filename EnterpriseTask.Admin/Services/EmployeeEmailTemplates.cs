using System.Net;

namespace EnterpriseTask.Admin.Services;

public static class EmployeeEmailTemplates
{
    public static string BuildEmployeeAccountEmail(
        string fullName,
        string companyName,
        string email,
        string temporaryPassword)
    {
        var safeFullName = Encode(fullName);
        var safeCompanyName = Encode(companyName);
        var safeEmail = Encode(email);
        var safeTemporaryPassword = Encode(temporaryPassword);

        return BuildLayout(
            "Tài khoản WorkFlow AI của bạn đã được tạo",
            $"""
            <p style="margin:0 0 16px;color:#475569;">Xin chào <strong>{safeFullName}</strong>,</p>
            <p style="margin:0 0 18px;color:#475569;line-height:1.65;">
                Công ty <strong>{safeCompanyName}</strong> đã tạo tài khoản WorkFlow AI cho bạn.
            </p>
            {BuildCredentials(safeEmail, safeTemporaryPassword)}
            <p style="margin:20px 0 8px;color:#334155;font-weight:700;">Cách đăng nhập</p>
            <ol style="margin:0 0 20px;padding-left:20px;color:#475569;line-height:1.8;">
                <li>Mở ứng dụng WorkFlow AI.</li>
                <li>Chọn <strong>Vào công ty</strong>.</li>
                <li>Nhập email và mật khẩu tạm ở trên.</li>
            </ol>
            <p style="margin:0;color:#b45309;line-height:1.6;">
                Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
            </p>
            """);
    }

    public static string BuildPasswordResetEmail(
        string fullName,
        string email,
        string temporaryPassword)
    {
        var safeFullName = Encode(fullName);
        var safeEmail = Encode(email);
        var safeTemporaryPassword = Encode(temporaryPassword);

        return BuildLayout(
            "Mật khẩu WorkFlow AI của bạn đã được đặt lại",
            $"""
            <p style="margin:0 0 16px;color:#475569;">Xin chào <strong>{safeFullName}</strong>,</p>
            <p style="margin:0 0 18px;color:#475569;line-height:1.65;">
                Mật khẩu tài khoản WorkFlow AI của bạn vừa được quản trị viên đặt lại.
            </p>
            {BuildCredentials(safeEmail, safeTemporaryPassword)}
            <p style="margin:20px 0 0;color:#b45309;line-height:1.6;">
                Vui lòng đăng nhập bằng mật khẩu tạm và đổi mật khẩu ngay sau đó.
            </p>
            """);
    }

    private static string BuildCredentials(string email, string temporaryPassword) =>
        $"""
        <div style="padding:18px;border:1px solid #dbeafe;border-radius:14px;background:#f8fbff;">
            <p style="margin:0 0 10px;color:#64748b;font-size:13px;">Email đăng nhập</p>
            <p style="margin:0 0 16px;color:#0f172a;font-weight:700;">{email}</p>
            <p style="margin:0 0 10px;color:#64748b;font-size:13px;">Mật khẩu tạm</p>
            <p style="margin:0;color:#1d4ed8;font-size:18px;font-weight:700;letter-spacing:.04em;">{temporaryPassword}</p>
        </div>
        """;

    private static string BuildLayout(string title, string content) =>
        $"""
        <!doctype html>
        <html lang="vi">
        <body style="margin:0;padding:28px;background:#f5f7fb;font-family:Roboto,Arial,sans-serif;">
            <div style="max-width:620px;margin:0 auto;overflow:hidden;border:1px solid #e5eaf1;border-radius:20px;background:#ffffff;box-shadow:0 18px 45px rgba(15,23,42,.08);">
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
