using Npgsql;
using System.Net;
using System.Net.Mail;
using xQuantum_API.Interfaces.UserRegistration;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Services.UserRegistration
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRegistrationService> _logger;
        private readonly string _masterConnectionString;

        public UserRegistrationService(IConfiguration configuration, ILogger<UserRegistrationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _masterConnectionString = _configuration.GetConnectionString("MasterDatabase");
        }


        public async Task<ApiResponse<Guid>> RegisterUserAsync(RegisterUserRequest req)
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 🔹 0️⃣ Check duplicate email
                const string emailCheckQuery = "SELECT COUNT(*) FROM public.user_master WHERE LOWER(email) = LOWER(@Email);";
                await using (var emailCmd = new NpgsqlCommand(emailCheckQuery, conn, tran))
                {
                    emailCmd.Parameters.AddWithValue("@Email", req.Email);
                    var existingCount = (long)await emailCmd.ExecuteScalarAsync();
                    if (existingCount > 0)
                    {
                        return ApiResponse<Guid>.Fail($"Email '{req.Email}' is already registered.");
                    }
                }

                // 🔹 1️⃣ Insert org
                var orgId = Guid.NewGuid();
                var conId = await GetLeastUsedConnectionIdAsync(conn, tran);

                var insertOrg = @"
            INSERT INTO public.org_master (
                org_id, org_name, org_code, con_id, legal_business_name,
                registered_business_address, business_registration_number,
                country_id, city_id, business_type, website_url, is_active
            )
            VALUES (
                @org_id, @org_name, @org_code, @con_id, @legal_business_name,
                @registered_business_address, @business_registration_number,
                @country_id, @city_id, @business_type, @website_url, true
            );";

                await using (var cmd = new NpgsqlCommand(insertOrg, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@org_id", orgId);
                    cmd.Parameters.AddWithValue("@org_name", req.OrgName);
                    cmd.Parameters.AddWithValue("@org_code", req.OrgCode);
                    cmd.Parameters.AddWithValue("@con_id", conId);
                    cmd.Parameters.AddWithValue("@legal_business_name", req.LegalBusinessName ?? "");
                    cmd.Parameters.AddWithValue("@registered_business_address", req.BusinessAddress ?? "");
                    cmd.Parameters.AddWithValue("@business_registration_number", req.RegistrationNumber ?? "");
                    cmd.Parameters.AddWithValue("@country_id", req.CountryId);
                    cmd.Parameters.AddWithValue("@city_id", req.CityId);
                    cmd.Parameters.AddWithValue("@business_type", req.BusinessType ?? "");
                    cmd.Parameters.AddWithValue("@website_url", req.WebsiteUrl ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔹 2️⃣ Insert user
                var userId = Guid.NewGuid();
                var insertUser = @"
            INSERT INTO public.user_master (
                user_id, org_id, first_name, last_name, email, type, mobile_number, auth_mode, is_active
            )
            VALUES (
                @user_id, @org_id, @first_name, @last_name, @Email, 1, @mobile_number, 1, true
            );";

                await using (var cmd = new NpgsqlCommand(insertUser, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@org_id", orgId);
                    cmd.Parameters.AddWithValue("@first_name", req.FirstName);
                    cmd.Parameters.AddWithValue("@last_name", req.LastName);
                    cmd.Parameters.AddWithValue("@Email", req.Email);
                    cmd.Parameters.AddWithValue("@mobile_number", req.MobileNumber ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔹 3️⃣ Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                const string insertOtp = @"
            INSERT INTO public.user_otps (user_id, otp_code, expires_at)
            VALUES (@user_id, @otp_code, @expires_at);";

                await using (var cmd = new NpgsqlCommand(insertOtp, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@otp_code", otp);
                    cmd.Parameters.AddWithValue("@expires_at", expiresAt);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                await SendOtpEmailAsync(req.Email, req.FirstName + " " + req.LastName, otp);

                return ApiResponse<Guid>.Ok(userId, "User registered successfully. OTP sent to email.");
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error in RegisterUserAsync");
                return ApiResponse<Guid>.Fail("Registration failed. Please try again later.");
            }
        }

        private async Task SendOtpEmailAsync(string toEmail, string userName, string otp)
        {
            try
            {
                var smtpSection = _configuration.GetSection("SMTP");
                var host = smtpSection["Host"];
                var port = int.Parse(smtpSection["Port"]);
                var username = smtpSection["Username"];
                var password = smtpSection["Password"];
                var enableSsl = bool.Parse(smtpSection["EnableSSL"]);
                var from = smtpSection["From"];

                using var smtp = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(from, "xQuantum Registration"),
                    Subject = "Your One-Time Password (OTP) – xQuantum Verification",
                    Body = $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                      <meta charset='UTF-8'>
                      <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                      <style>
                        body {{
                          font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                          background-color: #f4f6f8;
                          margin: 0;
                          padding: 0;
                        }}
                        .container {{
                          max-width: 600px;
                          margin: 40px auto;
                          background-color: #ffffff;
                          border-radius: 12px;
                          box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                          overflow: hidden;
                        }}
                        .header {{
                          background-color: #4b6ef5;
                          color: #ffffff;
                          text-align: center;
                          padding: 20px 10px;
                        }}
                        .header h1 {{
                          margin: 0;
                          font-size: 22px;
                          letter-spacing: 0.5px;
                        }}
                        .body {{
                          padding: 30px 40px;
                          color: #333333;
                        }}
                        .body p {{
                          line-height: 1.6;
                          font-size: 15px;
                        }}
                        .otp-box {{
                          background-color: #f4f6ff;
                          border: 1px dashed #4b6ef5;
                          border-radius: 10px;
                          text-align: center;
                          padding: 15px;
                          margin: 25px 0;
                        }}
                        .otp-code {{
                          font-size: 28px;
                          font-weight: 700;
                          color: #2e44ff;
                          letter-spacing: 4px;
                        }}
                        .footer {{
                          text-align: center;
                          font-size: 13px;
                          color: #888888;
                          padding: 20px;
                          border-top: 1px solid #eeeeee;
                        }}
                        .footer a {{
                          color: #4b6ef5;
                          text-decoration: none;
                          font-weight: 500;
                        }}
                      </style>
                    </head>
                    <body>
                      <div class='container'>
                        <div class='header'>
                          <h1>xQuantum Email Verification</h1>
                        </div>
                        <div class='body'>
                          <p>Hello {userName},</p>
                          <p>Thank you for registering with <b>xQuantum</b>. To complete your verification, please use the following One-Time Password (OTP):</p>
                          <div class='otp-box'>
                            <div class='otp-code'>{otp}</div>
                          </div>
                          <p>This OTP is valid for <b>5 minutes</b>. Please do not share this code with anyone.</p>
                          <p>If you did not request this, you can safely ignore this email.</p>
                          <p>Best regards,<br/><b>The xQuantum Team</b></p>
                        </div>
                        <div class='footer'>
                          <p>Need help? Contact <a href='mailto:support@xquantum.com'>support@xquantum.com</a></p>
                        </div>
                      </div>
                    </body>
                    </html>",
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);


                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending OTP email to {Email}", toEmail);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> VerifyOtpAsync(VerifyOtpRequest req)
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();

            var query = @"
        SELECT otp_id 
        FROM public.user_otps 
        WHERE user_id = @user_id 
          AND otp_code = @otp_code 
          AND expires_at > now() 
          AND is_used = false;";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user_id", req.UserId);
            cmd.Parameters.AddWithValue("@otp_code", req.OtpCode);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
            {
                return ApiResponse<bool>.Fail("Invalid or expired OTP. Please request a new one.");
            }

            var update = @"
        UPDATE public.user_master SET is_verified = true WHERE user_id = @user_id;
        UPDATE public.org_master 
            SET is_verified = true 
            WHERE org_id = (SELECT org_id FROM public.user_master WHERE user_id = @user_id);
        DELETE FROM public.user_otps WHERE user_id = @user_id;";

            await using var cmd2 = new NpgsqlCommand(update, conn);
            cmd2.Parameters.AddWithValue("@user_id", req.UserId);
            await cmd2.ExecuteNonQueryAsync();

            return ApiResponse<bool>.Ok(true, "OTP verified successfully. Your account is now active.");
        }

        public async Task<ApiResponse<bool>> SetPasswordAsync(SetPasswordRequest req)
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();

            var hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);
            const string query = "UPDATE public.user_master SET password = @pwd WHERE user_id = @user_id;";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@pwd", hashed);
            cmd.Parameters.AddWithValue("@user_id", req.UserId);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows > 0)
                return ApiResponse<bool>.Ok(true, "Password set successfully.");
            else
                return ApiResponse<bool>.Fail("User not found or update failed.");
        }

        private async Task<Guid> GetLeastUsedConnectionIdAsync(NpgsqlConnection conn, NpgsqlTransaction tran)
        {
            const string query = @"
        SELECT c.con_id
        FROM con_master c
        LEFT JOIN org_master o ON c.con_id = o.con_id
        WHERE c.is_available = true
        GROUP BY c.con_id
        ORDER BY COUNT(o.org_id) ASC
        LIMIT 1;";

            await using var cmd = new NpgsqlCommand(query, conn, tran);
            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                throw new InvalidOperationException("No available connections found in con_master.");

            return (Guid)result;
        }

    }

}
