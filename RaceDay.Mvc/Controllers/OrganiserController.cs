using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

// Every page here is Organiser-only. The data always comes from my Part 2 API.
[SessionRole("Organiser")]
public class OrganiserController : AppController
{
    public OrganiserController(RaceDayApiClient api) : base(api) { }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Index()
    {
        var (events, error) = await LoadMyEvents();
        if (error != null) return error;

        return View(new OrganiserDashboardVm
        {
            DisplayName = HttpContext.Session.GetString("DisplayName") ?? "Organiser",
            Events = events!
        });
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events()
    {
        var (events, error) = await LoadMyEvents();
        if (error != null) return error;
        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormVm vm, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Post, "api/events", ToRequest<CreateEventRequest>(vm));
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await Api.ReadErrorAsync(response));
            return View(vm);
        }

        var created = await Api.ReadAsync<EventDto>(response);
        await FinishEventSave(created?.EventId, banner, "Event created.");
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var response = await Api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        if (IsApiSessionExpired(response)) return SessionExpired();

        var ev = await Api.ReadAsync<EventDto>(response);
        if (ev == null || ev.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        return View(new EventFormVm
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormVm vm, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Put, $"api/events/{vm.EventId}", ToRequest<UpdateEventRequest>(vm));
        if (IsApiSessionExpired(response)) return SessionExpired();
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await Api.ReadErrorAsync(response));
            return View(vm);
        }

        await FinishEventSave(vm.EventId, banner, "Event updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var response = await Api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (response.IsSuccessStatusCode) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = await Api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int? eventId)
    {
        var (events, error) = await LoadMyEvents();
        if (error != null) return error;

        var vm = new CategoriesPageVm { Events = events!, EventId = PickEvent(events!, eventId) };
        if (vm.EventId != null)
        {
            var response = await Api.SendAsync(HttpMethod.Get, $"api/events/{vm.EventId}/categories");
            if (IsApiSessionExpired(response)) return SessionExpired();
            vm.Categories = await Api.ReadAsync<List<CategoryDto>>(response) ?? new();
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, string? categoryName, string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || categoryName.Length > 50)
        {
            TempData["Error"] = "A category name is required (50 characters at most).";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var response = await Api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories",
            new CreateCategoryRequest { CategoryName = categoryName.Trim(), Description = description });
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (response.IsSuccessStatusCode) TempData["Success"] = "Category added.";
        else TempData["Error"] = await Api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var response = await Api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (response.IsSuccessStatusCode) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = await Api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int? eventId)
    {
        var (events, error) = await LoadMyEvents();
        if (error != null) return error;

        var vm = new EnrolmentsPageVm { Events = events!, EventId = PickEvent(events!, eventId) };
        if (vm.EventId != null)
        {
            var response = await Api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{vm.EventId}");
            if (IsApiSessionExpired(response)) return SessionExpired();
            vm.Enrolments = await Api.ReadAsync<List<EnrolmentDto>>(response) ?? new();
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int enrolmentId, int eventId, string status)
    {
        var response = await Api.SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = status });
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (response.IsSuccessStatusCode) TempData["Success"] = "Enrolment status updated.";
        else TempData["Error"] = await Api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int? eventId)
    {
        var (events, error) = await LoadMyEvents();
        if (error != null) return error;

        var vm = new ResultsPageVm { Events = events!, EventId = PickEvent(events!, eventId) };
        if (vm.EventId != null)
        {
            var enrolments = await Api.SendAsync(HttpMethod.Get, $"api/enrolments/event/{vm.EventId}");
            if (IsApiSessionExpired(enrolments)) return SessionExpired();
            vm.Enrolments = await Api.ReadAsync<List<EnrolmentDto>>(enrolments) ?? new();

            var results = await Api.SendAsync(HttpMethod.Get, $"api/results/event/{vm.EventId}");
            vm.Results = await Api.ReadAsync<List<ResultDto>>(results) ?? new();
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, string? finishTime, int finishingPosition)
    {
        // The time has to look like 03:45:22 (hh:mm:ss)
        if (!TimeSpan.TryParse(finishTime, CultureInfo.InvariantCulture, out var time) || time <= TimeSpan.Zero)
        {
            TempData["Error"] = "Enter the finish time as hh:mm:ss, for example 03:45:22.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        if (finishingPosition < 1)
        {
            TempData["Error"] = "The finishing position must be 1 or higher.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var response = await Api.SendAsync(HttpMethod.Post, "api/results", new CreateResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTime = time,
            FinishingPosition = finishingPosition
        });
        if (IsApiSessionExpired(response)) return SessionExpired();

        if (response.IsSuccessStatusCode) TempData["Success"] = "Result saved.";
        else TempData["Error"] = await Api.ReadErrorAsync(response);

        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- Helpers ----------

    // Loads the logged-in Organiser's events. If the API session ended, "error" holds the redirect.
    private async Task<(List<EventDto>? Events, IActionResult? Error)> LoadMyEvents()
    {
        var response = await Api.SendAsync(HttpMethod.Get, "api/events/mine");
        if (IsApiSessionExpired(response)) return (null, SessionExpired());

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await Api.ReadErrorAsync(response);
            return (new List<EventDto>(), null);
        }

        return (await Api.ReadAsync<List<EventDto>>(response) ?? new List<EventDto>(), null);
    }

    // Uses the chosen event, or the first one if nothing was chosen
    private static int? PickEvent(List<EventDto> events, int? eventId)
    {
        if (eventId.HasValue && events.Any(e => e.EventId == eventId.Value)) return eventId;
        return events.FirstOrDefault()?.EventId;
    }

    private static T ToRequest<T>(EventFormVm vm) where T : CreateEventRequest, new()
    {
        return new T
        {
            EventName = vm.EventName.Trim(),
            Description = vm.Description,
            EventDate = vm.EventDate,
            Location = vm.Location.Trim(),
            Distance = vm.Distance,
            EventType = vm.EventType,
            BannerImageUrl = vm.BannerImageUrl
        };
    }

    // Uploads the banner (if one was chosen) through the API, then sets the success or error message
    private async Task FinishEventSave(int? eventId, IFormFile? banner, string successMessage)
    {
        if (eventId != null && banner != null && banner.Length > 0)
        {
            var upload = await Api.UploadFileAsync($"api/events/{eventId}/banner", banner);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "The event was saved, but the banner could not be uploaded: " + await Api.ReadErrorAsync(upload);
                return;
            }
        }
        TempData["Success"] = successMessage;
    }
}