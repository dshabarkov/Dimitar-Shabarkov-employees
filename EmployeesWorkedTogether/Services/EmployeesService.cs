using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using EmployeesWorkedTogether.Mappings;
using EmployeesWorkedTogether.Models;
using System.Collections.Concurrent;
using System.Globalization;

namespace EmployeesWorkedTogether.Services
{
    public class EmployeesService
    {
        public async Task<EmployeesViewModel> UploadFileAsync(IFormFile? uploadedFile, CancellationToken cancellationToken = default)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
                throw new ArgumentException("Please select a CSV file to upload.", nameof(uploadedFile));

            var viewModel = new EmployeesViewModel();

            using var stream = uploadedFile.OpenReadStream();
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

            var knownFormats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyyMMdd",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFF",
                "yyyy-MM-ddTHH:mm:ssK",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy",
                "M/d/yyyy",
                "MM/dd/yy",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "dd/MM/yy",
                "dd.MM.yyyy",
                "d.M.yyyy",
                "MMM d, yyyy",
                "MMMM d, yyyy",
                "r"
            };

            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(new TypeConverterOptions
            {
                Formats = knownFormats
            });
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(new TypeConverterOptions
            {
                Formats = knownFormats
            });

            var projects = new Dictionary<int, List<EmployeeWorkModel>>();

            // Read records and bucket by ProjectID. check cancellation inside loop.
            await foreach (var rec in csv.GetRecordsAsync<EmployeeWorkModel>(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!projects.TryGetValue(rec.ProjectID, out var list))
                {
                    list = [];
                    projects[rec.ProjectID] = list;
                }
                list.Add(rec);
            }

            var pairWorkDays = new ConcurrentDictionary<(int, int), int>();
            var projectsWorkedTogether = new ConcurrentBag<EmployeePairWork>();

            Parallel.ForEach(
                projects.Values,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                employees =>
                {
                    var local = employees;
                    for (int i = 0; i < local.Count; i++)
                    {
                        for (int j = i + 1; j < local.Count; j++)
                        {
                            var e1 = local[i];
                            var e2 = local[j];

                            int overlapDays = GetOverlapDays(e1, e2);
                            if (overlapDays <= 0)
                                continue;

                            var pair = (Math.Min(e1.EmpID, e2.EmpID), Math.Max(e1.EmpID, e2.EmpID));

                            pairWorkDays.AddOrUpdate(pair, overlapDays, (_, old) => old + overlapDays);

                            projectsWorkedTogether.Add(new EmployeePairWork
                            {
                                EmployeeId1 = pair.Item1,
                                EmployeeId2 = pair.Item2,
                                ProjectId = e1.ProjectID,
                                DaysWorked = overlapDays
                            });
                        }
                    }
                });

            if (pairWorkDays.IsEmpty)
                throw new InvalidOperationException("No overlapping work days found in uploaded file.");

            var topPair = pairWorkDays.OrderByDescending(p => p.Value).First();

            viewModel.Pairs = [.. projectsWorkedTogether
                .Where(p => p.EmployeeId1 == topPair.Key.Item1 && p.EmployeeId2 == topPair.Key.Item2)
                .OrderByDescending(p => p.DaysWorked)];

            return viewModel;
        }

        private static int GetOverlapDays(EmployeeWorkModel a, EmployeeWorkModel b)
        {
            var aDateTo = a.DateTo ?? DateTime.Now;
            var bDateTo = b.DateTo ?? DateTime.Now;

            DateTime start = a.DateFrom > b.DateFrom ? a.DateFrom : b.DateFrom;
            DateTime end = aDateTo < bDateTo ? aDateTo : bDateTo;

            return start <= end ? (end - start).Days : 0;
        }
    }
}