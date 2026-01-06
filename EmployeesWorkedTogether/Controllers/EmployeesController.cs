using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EmployeesWorkedTogether.Mappings;
using EmployeesWorkedTogether.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace EmployeesWorkedTogether.Controllers
{
    public class EmployeesController : Controller
    {
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
                using var stream = UploadedFile.OpenReadStream();
                using var reader = new StreamReader(stream);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    TrimOptions = TrimOptions.Trim
                };

                using var csv = new CsvReader(reader, config);

                csv.Context.RegisterClassMap<EmployeeWorkModelMap>();

                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new TypeConverterOptions
                {
                    Formats =
                    [
                        "yyyy-MM-dd",
                        "yyyy/MM/dd",
                        "MM/dd/yyyy",
                        "M/d/yyyy",
                        "dd/MM/yyyy",
                        "d/M/yyyy",
                        "yyyyMMdd"
                    ]
                });

                var records = csv.GetRecords<EmployeeWorkModel>().ToList();

                var pairWorkDays = new Dictionary<(int, int), int>();

                var projects = records.GroupBy(r => r.ProjectID);

                foreach (var project in projects)
                {
                    var employees = project.ToList();

                    for (int i = 0; i < employees.Count; i++)
                    {
                        for (int j = i + 1; j < employees.Count; j++)
                        {
                            var e1 = employees[i];
                            var e2 = employees[j];

                            int overlapDays = GetOverlapDays(e1, e2);
                            if (overlapDays > 0)
                            {
                                var pair = (
                                    Math.Min(e1.EmpID, e2.EmpID),
                                    Math.Max(e1.EmpID, e2.EmpID)
                                );

                                if (!pairWorkDays.ContainsKey(pair))
                                    pairWorkDays[pair] = 0;

                                pairWorkDays[pair] += overlapDays;
                            }
                        }
                    }
                }

                var result = pairWorkDays
                    .OrderByDescending(p => p.Value)
                    .First();

                /// WIP - continue from here to prepare the view model with the results.
                /// need to get only the projects they have worked toghether and the days for each project.
                var pairProjects = records
                    .Where(r => (r.EmpID == result.Key.Item1 || r.EmpID == result.Key.Item2))
                    .Select(r => new EmployeePairWork())
                    .ToList();

                var viewModel = new EmployeesViewModel();

                return View("Employees", viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to process file: " + ex.Message);
                return View("Employees", new EmployeesViewModel());
            }
        }

        static int GetOverlapDays(EmployeeWorkModel a, EmployeeWorkModel b)
        {
            var aDateTo = a.DateTo ?? DateTime.Now;
            var bDateTo = b.DateTo ?? DateTime.Now;

            DateTime start = a.DateFrom > b.DateFrom ? a.DateFrom : b.DateFrom;
            DateTime end = a.DateTo < b.DateTo ? aDateTo : bDateTo;

            return start <= end ? (end - start).Days : 0;
        }
    }
}