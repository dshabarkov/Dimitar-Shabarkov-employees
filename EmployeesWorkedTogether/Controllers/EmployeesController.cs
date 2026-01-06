using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EmployeesWorkedTogether.Mappings;
using EmployeesWorkedTogether.Models;
using EmployeesWorkedTogether.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Globalization;

namespace EmployeesWorkedTogether.Controllers
{
    public class EmployeesController : Controller
    {
        readonly EmployeesService _service;

        public EmployeesController(EmployeesService service)
        {
            _service = service;
        }

        public IActionResult Employees()
        {
            return View(new EmployeesViewModel());
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile(IFormFile? UploadedFile)
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ModelState.AddModelError("UploadedFile", "Please select a CSV file to upload.");
                return View("Employees", new EmployeesViewModel());
            }

            try
            {
                var viewModel = await _service.UploadFileAsync(UploadedFile);
                return View("Employees", viewModel);
            }
            catch (ArgumentException aex)
            {
                // file / validation related - attach to the field
                ModelState.AddModelError("UploadedFile", aex.Message);
                return View("Employees", new EmployeesViewModel());
            }
            catch (InvalidOperationException ioex)
            {
                // domain-level problems (no overlaps etc.)
                ModelState.AddModelError(string.Empty, ioex.Message);
                return View("Employees", new EmployeesViewModel());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to process file: " + ex.Message);
                return View("Employees", new EmployeesViewModel());
            }
        }
    }
}