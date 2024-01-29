using FISTNESS.Data;
using FISTNESS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;


namespace FISTNESS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    // Pobierz liczbę karnetów dla studentów i standardowych
                    int liczbaKarnetowStudent = _dbContext.Karnety.Count(k => k.TypKarnetu == "student");
                    int liczbaKarnetowStandard = _dbContext.Karnety.Count(k => k.TypKarnetu == "standard");

                    // Oblicz sumę kwoty
                    decimal sumaKwotyStudent = liczbaKarnetowStudent * 100;
                    decimal sumaKwotyStandard = liczbaKarnetowStandard * 140;

                    // Przekaz sumy kwot do widoku
                    ViewBag.SumaKwotyStudent = sumaKwotyStudent;
                    ViewBag.SumaKwotyStandard = sumaKwotyStandard;
                    int registeredUsersCount = await _userManager.Users.CountAsync();
                    ViewBag.RegisteredUsersCount = registeredUsersCount;
                    var visitorCount2 = GetVisitorCount();
                    ViewBag.VisitorCount = visitorCount2;
                    // Panel dla administratora
                    return View("AdminPanel");
                }
                else if (await _userManager.IsInRoleAsync(user, "User"))
                {
                    string userName = User.Identity.Name;
                    ViewBag.UserName = userName;

                    var CurrentDate = DateTime.Now;
                    ViewBag.CurrentDate = CurrentDate;

                    int registeredUsersCount = await _userManager.Users.CountAsync();
                    ViewBag.RegisteredUsersCount = registeredUsersCount;
                    var visitorCount2 = GetVisitorCount();
                    ViewBag.VisitorCount = visitorCount2;
                    // Panel dla użytkownika
                    return View("UserPanel");
                }
            }
            int registeredUsersCount2 = await _userManager.Users.CountAsync();
            ViewBag.RegisteredUsersCount = registeredUsersCount2;
            var visitorCount = GetVisitorCount();
            ViewBag.VisitorCount = visitorCount;// Standardowy widok, gdy użytkownik nie jest zalogowany lub nie ma przypisanej roli
            return View("Index");
        }
        public IActionResult GrafikUiA()
        {
            return View();
        }
        public IActionResult Karnet()
        {
            return View();
        }
        public IActionResult Logout()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public IActionResult Grafik()
        {
            return View();
        }
        private int GetVisitorCount()
        {
            var currentHour = DateTime.Now.Hour;
            return GenerateRandomCount(currentHour);
        }
        private (decimal sumaKwotyStudent, decimal sumaKwotyStandard) GetSumaKwoty()
        {
            // Pobierz liczbę karnetów dla studentów i standardowych
            int liczbaKarnetowStudent = _dbContext.Karnety.Count(k => k.TypKarnetu == "student");
            int liczbaKarnetowStandard = _dbContext.Karnety.Count(k => k.TypKarnetu == "standard");

            // Oblicz sumę kwoty
            decimal sumaKwotyStudent = liczbaKarnetowStudent * 100;
            decimal sumaKwotyStandard = liczbaKarnetowStandard * 140;

            return (sumaKwotyStudent, sumaKwotyStandard);
        }

        private int GenerateRandomCount(int hour)
        {
            Dictionary<int, int[]> hourRanges = new Dictionary<int, int[]>
        {
            { 0, new int[] { 5, 10 } },
            { 6, new int[] { 10, 20 } },
            { 12, new int[] { 20, 30 } },
            { 18, new int[] { 20, 25 } },
            { 22, new int[] { 5, 15 } }
        };

            var ranges = hourRanges.LastOrDefault(pair => pair.Key <= hour).Value ?? new int[] { 10, 20 };

            return new Random().Next(ranges[0], ranges[1] + 1);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UsunProfil(string username)
        {
            try
            {
                var user = _dbContext.Users.FirstOrDefault(u => u.UserName == username);

                if (user != null)
                {
                    _dbContext.Users.Remove(user);
                    _dbContext.SaveChanges();

                    return Json(new { success = true });

                }
                else
                {
                    return Json(new { success = false, message = "Użytkownik nie istnieje." });
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("Inner Exception: " + ex.InnerException?.Message);
                return Json(new { success = false, message = "Wystąpił błąd", error = ex.Message });
            }

        }
        public IActionResult Profil()
        {
            var usersWithKarnets = _dbContext.Users
                .GroupJoin(
                    _dbContext.Karnety,
                    user => user.Id,
                    karnet => karnet.UserId,
                    (user, karnets) => new
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        KarnetInfo = karnets.FirstOrDefault() != null ? karnets.First().TypKarnetu : "Brak karnetu",
                        DataWaznosci = karnets.FirstOrDefault() != null ? karnets.First().DataWaznosci : null
                    })
                .ToList();

            return View(usersWithKarnets);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UsunKarnet(string username)
        {
            try
            {
                // Znajdź użytkownika
                var user = _dbContext.Users.FirstOrDefault(u => u.UserName == username);

                if (user != null)
                {
                    // Znajdź karnet użytkownika i usuń go
                    var karnet = _dbContext.Karnety.FirstOrDefault(k => k.UserId == user.Id);
                    if (karnet != null)
                    {
                        _dbContext.Karnety.Remove(karnet);
                        _dbContext.SaveChanges();

                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Użytkownik nie ma przypisanego karnetu." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Użytkownik nie istnieje." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("Inner Exception: " + ex.InnerException?.Message);
                return Json(new { success = false, message = "Wystąpił błąd", error = ex.Message });
            }
        }
        public IActionResult SumaKwoty()
        {
            // Pobierz liczbę karnetów dla studentów i standardowych
            int liczbaKarnetowStudent = _dbContext.Karnety.Count(k => k.TypKarnetu == "student");
            int liczbaKarnetowStandard = _dbContext.Karnety.Count(k => k.TypKarnetu == "standard");

            // Oblicz sumę kwoty
            decimal sumaKwotyStudent = liczbaKarnetowStudent * 100;
            decimal sumaKwotyStandard = liczbaKarnetowStandard * 140;

            // Przekaz sumy kwot do widoku
            ViewBag.SumaKwotyStudent = sumaKwotyStudent;
            ViewBag.SumaKwotyStandard = sumaKwotyStandard;

            return View();
        }





    }
}