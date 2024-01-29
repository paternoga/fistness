using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using FISTNESS.Data;
using FISTNESS.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Azure;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kalendarz_taki_na_tiptop.Controllers
{
    public class CalendarController : Controller
    {
        private readonly ILogger<CalendarController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public CalendarController(ILogger<CalendarController> logger, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
        }


        [HttpGet]
        public IActionResult AddEvent()
        {
            var model = new Events();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> AddEvent(Events model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _dbContext.Events.Add(model);
                    await _dbContext.SaveChangesAsync();
                    return Json(new { success = true, id = model.Id });
                }

                return Json(new { success = false, message = "Model stanu nie jest prawidłowy." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Błąd dodawania wydarzenia: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult DeleteEvents(List<int> ids)
        {
            var eventsToDelete = _dbContext.Events.Where(e => ids.Contains(e.Id)).ToList();

            if (eventsToDelete.Any())
            {
                _dbContext.Events.RemoveRange(eventsToDelete);
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Nie znaleziono zdarzeń o podanych identyfikatorach." });
        }
        [HttpGet]
        public IActionResult GetEventsAdmin()
        {
            var events = _dbContext.Events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.End.ToString("yyyy-MM-ddTHH:mm:ss")
            }).ToList();

            return Json(events);
        }
        [HttpGet]
        [Authorize(Roles = "User")]
        public IActionResult GetEvents()
        {
            // Pobierz UserId z zalogowanego użytkownika (zakładam, że korzystasz z ASP.NET Identity)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var eventsQuery = _dbContext.Events
                .Where(e => e.UserId == userId || e.UserId == null)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = e.End.ToString("yyyy-MM-ddTHH:mm:ss")
                });

            var events = eventsQuery.ToList();

            return Json(events);
        }
        [HttpPost]
        public JsonResult SignUpOrCancelForEvent(int eventId)
        {
            var user = _userManager.GetUserAsync(User).Result;

            if (user == null)
            {
                return Json(new { success = false, message = "Użytkownik niezalogowany." });
            }

            var existingEvent = _dbContext.Events.FirstOrDefault(e => e.Id == eventId);

            if (existingEvent != null)
            {
                if (existingEvent.UserId == user.Id)
                {
                    // Użytkownik jest zapisany na wydarzenie, wypisz go
                    existingEvent.UserId = null;
                    _dbContext.SaveChanges();

                    return Json(new { success = true, message = "Wypisano z wydarzenia." });
                }
                else
                {
                    // Użytkownik nie jest zapisany na wydarzenie, sprawdź czy może się zapisać
                    var hasActiveSubscription = false;

                    // Pobierz karnet użytkownika
                    var karnet = _dbContext.Karnety.FirstOrDefault(k => k.UserId == user.Id);

                    if (karnet != null && karnet.DataWaznosci.HasValue)
                    {
                        // Sprawdź, czy data ważności karnetu jest późniejsza niż dzisiejsza data
                        hasActiveSubscription = karnet.DataWaznosci.Value >= DateTime.Now;
                    }

                    if (!hasActiveSubscription)
                    {
                        return Json(new { success = false, message = "Użytkownik nie ma aktywnego karnetu." });
                    }

                    // Sprawdź, czy użytkownik jest już zapisany na to wydarzenie
                    var isUserSignedUp = existingEvent.UserId == user.Id;

                    if (isUserSignedUp)
                    {
                        return Json(new { success = false, message = "Użytkownik jest już zapisany na to wydarzenie." });
                    }

                    // Przypisz użytkownika do wydarzenia
                    existingEvent.UserId = user.Id;
                    _dbContext.SaveChanges();

                    return Json(new { success = true, message = "Zapisano na wydarzenie!" });
                }
            }

            return Json(new { success = false, message = "Nie znaleziono wydarzenia o podanym identyfikatorze." });
        }
        public async Task<IActionResult> MojeZajecia()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pobierz zajęcia, na które użytkownik jest zapisany
            var zajecia = await _dbContext.Events
                .Where(e => e.UserId == userId)
                .ToListAsync();

            // Przekaż dane zajęć do modelu widoku
            return Json(zajecia);
        }





    }
}
