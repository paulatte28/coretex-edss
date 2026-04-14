using Microsoft.AspNetCore.Mvc;

namespace coretex_finalproj.Controllers
{
	public class DataEntryController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult History()
		{
			return View();
		}
	}
}
