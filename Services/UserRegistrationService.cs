using Npgsql;
using System.Net;
using System.Net.Mail;
using xQuantum_API.Interfaces;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Services
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


        public async Task<Guid> RegisterUserAsync(RegisterUserRequest req)
        {
            using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 1️⃣ Insert org
                var orgId = Guid.NewGuid();
                var conId = await GetLeastUsedConnectionIdAsync(conn, tran);

                var insertOrg = @"
                INSERT INTO public.org_master (org_id, org_name, org_code, con_id, legal_business_name,registered_business_address,business_registration_number,
                                               country_id, city_id, business_type, website_url, is_active)
                VALUES (@org_id, @org_name, @org_code, @con_id, @legal_business_name,@registered_business_address,@business_registration_number,
                        @country_id, @city_id, @business_type, @website_url, true);";

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

                // 2️⃣ Insert user
                var userId = Guid.NewGuid();
                var insertUser = @"
                INSERT INTO public.user_master (user_id, org_id, first_name, last_name, email, type, mobile_number, auth_mode, is_active)
                VALUES (@user_id, @org_id, @first_name, @last_name, @email, 1, @mobile_number, 1, true);";

                await using (var cmd = new NpgsqlCommand(insertUser, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@org_id", orgId);
                    cmd.Parameters.AddWithValue("@first_name", req.FirstName);
                    cmd.Parameters.AddWithValue("@last_name", req.LastName);
                    cmd.Parameters.AddWithValue("@email", req.Email);
                    cmd.Parameters.AddWithValue("@mobile_number", req.MobileNumber ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();
                var expiresAt = DateTime.UtcNow.AddMinutes(5);

                var insertOtp = @"
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
                await SendOtpEmailAsync(req.Email, otp);

                _logger.LogInformation("User registered with OTP {Otp} for UserId: {UserId}", otp, userId);

                return userId;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        private async Task SendOtpEmailAsync(string toEmail, string otp)
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
                    Subject = "Your OTP Code",
                    Body = $@"
                <p>Hello,</p>
                <p>Your One-Time Password (OTP) is <b>{otp}</b>.</p>
                <p>This OTP is valid for 5 minutes. Please do not share it with anyone.</p>
                <p>Regards,<br/>xQuantum Team</p>",
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

        public async Task<bool> VerifyOtpAsync(VerifyOtpRequest req)
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();

            var query = @"
            SELECT otp_id FROM public.user_otps 
            WHERE user_id = @user_id 
              AND otp_code = @otp_code 
              AND expires_at > now() 
              AND is_used = false;";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user_id", req.UserId);
            cmd.Parameters.AddWithValue("@otp_code", req.OtpCode);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null) return false;

            // ✅ Mark verified
            var update = @"
            UPDATE public.user_master SET is_verified = true WHERE user_id = @user_id;
            UPDATE public.org_master SET is_verified = true WHERE org_id = (SELECT org_id FROM public.user_master WHERE user_id = @user_id);
            DELETE FROM public.user_otps WHERE user_id = @user_id;";

            await using var cmd2 = new NpgsqlCommand(update, conn);
            cmd2.Parameters.AddWithValue("@user_id", req.UserId);
            await cmd2.ExecuteNonQueryAsync();

            return true;
        }

        public async Task<bool> SetPasswordAsync(SetPasswordRequest req)
        {
            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();

            string hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var query = "UPDATE public.user_master SET password = @pwd WHERE user_id = @user_id;";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@pwd", hashed);
            cmd.Parameters.AddWithValue("@user_id", req.UserId);

            return await cmd.ExecuteNonQueryAsync() > 0;
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
