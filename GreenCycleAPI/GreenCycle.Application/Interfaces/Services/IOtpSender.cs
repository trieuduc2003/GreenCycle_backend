namespace GreenCycle.Application.Interfaces.Services
{
    /// <summary>
    /// Kênh gửi mã OTP tới người dùng (Email, SMS, ...).
    /// </summary>
    public interface IOtpSender
    {
        /// <summary>
        /// Gửi mã OTP tới Email hoặc SĐT.
        /// </summary>
        /// <param name="destination">Địa chỉ Email hoặc SĐT người nhận.</param>
        /// <param name="otp">Mã OTP cần gửi.</param>
        /// <param name="validMinutes">Số phút OTP còn hiệu lực (hiển thị trong tin nhắn).</param>
        /// <param name="subject">Tiêu đề email (tùy chọn).</param>
        /// <exception cref="System.Exception">Ném lỗi có thông điệp thân thiện nếu gửi thất bại.</exception>
        Task SendOtpAsync(string destination, string otp, int validMinutes, string? subject = null);
    }
}
