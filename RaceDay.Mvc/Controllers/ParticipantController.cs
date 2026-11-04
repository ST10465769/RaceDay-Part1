using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

// Every page here is Participant-only. The data always comes from my Part 2 API.
[SessionRole("Participant")]
public class ParticipantController : AppController
{
    public ParticipantController(RaceDayApiClient api) : base(api) { }

    // Upcoming events with filtering
    public async Task<IActionResult> Index(string? type, string? search)
    {
        // The API can filter by event type, so I pass that along
        string url = "api/events";
        if (!string.IsNullOrWhiteSpace(type)) url += "?eventType=" + Uri.EscapeDataString(type);

        var response = await Api.SendAsync(HttpMethod.Get, url);
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode) TempData["Error"] = await Api.ReadErrorAsync(response);

        var events = await Api.ReadAsync<List<EventDto>>(response) ?? new();

        // Only upcoming events, then the text search on name or location
        events = events.Where(e => e.EventDate >= DateTime.Today).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new ParticipantEventsVm { Events = events, Type = type, Search = search });
    }

    // Full event details with its categories
    public async Task<IActionResult> EventDetails(int id)
    {
        var response = await Api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        if (IsApiSessionExpired(response)) return SessionExpired();

        var ev = await Api.ReadAsync<EventDto>(response);
        if (ev == null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        // Has this participant already entered this event?
        var mine = await Api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        var myEnrolments = await Api.ReadAsync<List<EnrolmentDto>>(mine) ?? new();

        return View(new EventDetailsVm
        {
            Event = ev,
            MyEnrolment = myEnrolments.FirstOrDefault(e => e.EventId == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        var response = await Api.SendAsync(HttpMethod.Post, "api/enrolments",
            new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await Api.ReadErrorAsync(response);
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        TempData["Success"] = "You have entered the event.";
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyEvents()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "api/enrolments/mine");
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode) TempData["Error"] = await Api.ReadErrorAsync(response);

        var enrolments = await Api.ReadAsync<List<EnrolmentDto>>(response) ?? new();
        return View(enrolments);
    }

    public async Task<IActionResult> MyResults()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "api/results/mine");
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode) TempData["Error"] = await Api.ReadErrorAsync(response);

        var results = await Api.ReadAsync<List<ResultDto>>(response) ?? new();
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "api/users/profile");
        if (IsApiSessionExpired(response)) return SessionExpired();

        var profile = await Api.ReadAsync<ProfileDto>(response);
        if (profile == null)
        {
            TempData["Error"] = await Api.ReadErrorAsync(response);
            return RedirectToAction(nameof(Index));
        }

        return View(new ProfileVm
        {
            Email = profile.Email,
            Role = profile.Role,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            PhoneNumber = profile.PhoneNumber,
            ProfilePictureUrl = profile.ProfilePictureUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm, IFormFile? picture)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Put, "api/users/profile", new UpdateProfileRequest
        {
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(vm.PhoneNumber) ? null : vm.PhoneNumber,
            ProfilePictureUrl = vm.ProfilePictureUrl
        });
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await Api.ReadErrorAsync(response));
            return View(vm);
        }

        // The picture goes MVC -> API -> Azure Blob Storage. MVC never talks to Azure.
        if (picture != null && picture.Length > 0)
        {
            var upload = await Api.UploadFileAsync("api/users/profile/picture", picture);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "Your details were saved, but the picture could not be uploaded: " + await Api.ReadErrorAsync(upload);
                return RedirectToAction(nameof(Profile));
            }
        }

        HttpContext.Session.SetString("DisplayName", vm.FirstName.Trim());
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}