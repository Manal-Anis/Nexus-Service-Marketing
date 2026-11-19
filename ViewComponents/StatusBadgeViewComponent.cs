using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.ViewComponents;

/// <summary>Renders a coloured status badge for the domain enums.</summary>
public class StatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(StatusBadgeViewModel model) => View(model);
}
