using IdentityApp.Models;
using IdentityApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private  SignInManager<AppUser> _signInManager;
        private IEmailSender _emailSender;
        public AccountController(UserManager<AppUser> userManager,RoleManager<AppRole> roleManager,SignInManager<AppUser> signInManager,IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model!=null)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user!=null)
                    {
                        await _signInManager.SignOutAsync();


                        if (!await _userManager.IsEmailConfirmedAsync(user))
                        {
                            ModelState.AddModelError("","Hesabınızı Onaylayınız.");
                            return View(model);
                        }


                        var result =await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);

                        if (result.Succeeded)
                        {
                            await _userManager.ResetAccessFailedCountAsync(user);
                            await _userManager.SetLockoutEndDateAsync(user,null);

                            return RedirectToAction("Index","Home");
                        }
                        else if(result.IsLockedOut)
                        {
                            var lockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                            var timeLeft = lockoutDate.Value - DateTime.UtcNow;

                            ModelState.AddModelError("",$"Hesabınız Kitlendi, Lütfen {timeLeft.Minutes+1} Dakika Sonra Tekrar Deneyiniz");

                        }
                        else
                        {
                            ModelState.AddModelError("","Hatalı Parola");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("","Email Adresiyle Bir Hesap Bulunamadı");
                    }
                }
            }   
            return View(model);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new AppUser { 
                    UserName = model.UserName,
                    Email = model.Email, 
                    FullName = model.FullName 
                };

                IdentityResult result = await _userManager.CreateAsync(user, model.Password);

                if(result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var url = Url.Action("ConfirmEmail","Account", new { user.Id, token} );

                    var htmlContent = $@"
    <!DOCTYPE html>
    <html lang='tr'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>E-posta Doğrulama</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f7f7f7; }}
            table {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; }}
            .email-container {{ border: 1px solid #e0e0e0; border-radius: 8px; }}
            .email-header {{ background-color: #4CAF50; color: #ffffff; padding: 15px; text-align: center; border-radius: 8px 8px 0 0; }}
            .email-body {{ padding: 20px; color: #333333; font-size: 16px; }}
            .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 12px 25px; font-size: 16px; text-decoration: none; border-radius: 5px; text-align: center; margin-top: 20px; }}
            .button:hover {{ background-color: #45a049; }}
            .email-footer {{ background-color: #f2f2f2; text-align: center; padding: 15px; color: #777777; font-size: 12px; border-radius: 0 0 8px 8px; }}
        </style>
    </head>
    <body>
        <table class='email-container'>
            <tr>
                <td class='email-header'>
                    <h2>E-posta Adresinizi Doğrulayın</h2>
                </td>
            </tr>
            <tr>
                <td class='email-body'>
                    <p>Merhaba,</p>
                    <p>Hesabınızı etkinleştirmek için e-posta adresinizi doğrulamanız gerekiyor. Lütfen aşağıdaki butona tıklayarak e-posta adresinizi doğrulayın:</p>
                    <a href='http://localhost:5056{url}' class='button'>E-posta Adresimi Doğrula</a>
                    <p>Hesabınızın güvenliği bizim için önemlidir, bu yüzden doğrulama işlemini tamamlamanızı öneririz.</p>
                </td>
            </tr>
            <tr>
                <td class='email-footer'>
                    <p>Bu e-posta bir otomatik sistem tarafından gönderilmiştir. Lütfen yanıtlamayın.</p>
                    <p>© 2024 Minel Tolga Topçu  - IdentitiyApp</p>
                </td>
            </tr>
        </table>
    </body>
    </html>
";

                    // email
                    await _emailSender.SendEmailAsync(user.Email, "Hesap Onayı", htmlContent);

                    TempData["message"]  = "Email hesabınızdaki onay mailini tıklayınız."; 
                    return RedirectToAction("Login","Account");
                }

                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);                    
                }
            }
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string Id, string token)
        {
            if (Id==null || token ==null)
            {
                TempData["message"] = "Geçersiz token bilgisi";
                return View();
            }

            var user = await _userManager.FindByIdAsync(Id);
            if (user!=null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    TempData["message"] = "Hesabınız onaylandı";
                    return RedirectToAction("Login","Account");
                }
            }
            TempData["message"] = "Kullanıcı bulunamadı";
            return View();
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                TempData["message"] = "Eposta adresinizi giriniz.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if (user==null)
            {
                TempData["message"] = "Eposta adresiyle eşleşen bir kayıt yok.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("ResetPassword","Account", new { user.Id, token} );

            var htmlContent = $@"
    <!DOCTYPE html>
    <html lang='tr'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>E-posta Doğrulama</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f7f7f7; }}
            table {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; }}
            .email-container {{ border: 1px solid #e0e0e0; border-radius: 8px; }}
            .email-header {{ background-color: #4CAF50; color: #ffffff; padding: 15px; text-align: center; border-radius: 8px 8px 0 0; }}
            .email-body {{ padding: 20px; color: #333333; font-size: 16px; }}
            .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 12px 25px; font-size: 16px; text-decoration: none; border-radius: 5px; text-align: center; margin-top: 20px; }}
            .button:hover {{ background-color: #45a049; }}
            .email-footer {{ background-color: #f2f2f2; text-align: center; padding: 15px; color: #777777; font-size: 12px; border-radius: 0 0 8px 8px; }}
        </style>
    </head>
    <body>
        <table class='email-container'>
            <tr>
                <td class='email-header'>
                    <h2>Şifremi Unuttum</h2>
                </td>
            </tr>
            <tr>
                <td class='email-body'>
                    <p>Merhaba,</p>
                    <p>Şifrenizi değiştirmek için lütfen aşağıdaki butona tıklayın:</p>
                    <a href='http://localhost:5056{url}' class='button'>E-posta Adresimi Doğrula</a>
                    <p>Hesabınızın güvenliği bizim için önemlidir, bu yüzden doğrulama işlemini tamamlamanızı öneririz.</p>
                </td>
            </tr>
            <tr>
                <td class='email-footer'>
                    <p>Bu e-posta bir otomatik sistem tarafından gönderilmiştir. Lütfen yanıtlamayın.</p>
                    <p>© 2024 Tolga Topçu - IdentitiyApp</p>
                </td>
            </tr>
        </table>
    </body>
    </html>
";

                    // email
                    await _emailSender.SendEmailAsync(Email, "Şifre Güncelleme", htmlContent);
                    TempData["message"] = "Eposta adresinize gönderilen link ile şifrenizi sıfırlayın.";
                    return View();

                    
        }
        public IActionResult ResetPassword(string Id, string token)
        {
            if (Id == null || token ==null)
            {
                return RedirectToAction("Login");
            }
            var model = new ResetPasswordModel{
                Token = token
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user==null)
                {
                    TempData["message"] = "Bu adrese ait kullanıcı yok.";

                    return RedirectToAction("Login");
                }
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                if (result.Succeeded)
                {
                    TempData["message"] = "Şifreniz başaıyla değiştirildi.";

                    return RedirectToAction("Login");
                }
                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);                    
                }
            }
            return View(model);
        }

    }
}
