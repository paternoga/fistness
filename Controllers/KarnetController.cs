using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FISTNESS.Data;
using FISTNESS.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity; // Sprawdź, czy ta przestrzeń nazw jest taka sama jak w Twoim projekcie


namespace FISTNESS.Controllers
{
    public class KarnetController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public KarnetController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpPost]
        public IActionResult KupKarnet(string typKarnetu)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                var existingKarnet = _dbContext.Karnety.FirstOrDefault(k => k.UserId == userId);

                if (existingKarnet != null)
                {
                    // Użytkownik już posiada karnet, aktualizuj typ karnetu
                    existingKarnet.TypKarnetu = typKarnetu;
                    existingKarnet.DataWaznosci = existingKarnet.DataWaznosci?.AddDays(30) ?? DateTime.Now.AddDays(30);
                }
                else
                {
                    // Użytkownik nie ma jeszcze karnetu, stwórz nowy
                    var karnet = new Karnet
                    {
                        TypKarnetu = typKarnetu,
                        UserId = userId,
                        DataWaznosci = DateTime.Now.AddDays(30)
                    };

                    _dbContext.Karnety.Add(karnet);
                }

                _dbContext.SaveChanges();

                return RedirectToAction("Karnet", "Home");
            }

            return RedirectToAction("Karnet", "Home");
        }

        [HttpGet]
        public async Task<JsonResult> DataWaznosciKarnetu()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json(null);
            }

            var karnet = _dbContext.Karnety.FirstOrDefault(k => k.UserId == user.Id);

            if (karnet != null && karnet.DataWaznosci.HasValue)
            {
                return Json(karnet.DataWaznosci);
            }
            else
            {
                return Json(null);
            }
        }
        
    }
}

