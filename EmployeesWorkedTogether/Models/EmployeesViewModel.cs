namespace EmployeesWorkedTogether.Models
{
    public class EmployeePairWork
    {
        public int EmployeeId1 { get; set; }
        public int EmployeeId2 { get; set; }
        public int ProjectId { get; set; }
        public int DaysWorked { get; set; }
    }

    public class EmployeesViewModel
    {
        public IFormFile? UploadedFile { get; set; }

        public List<EmployeePairWork> Pairs { get; set; } = [];

        public EmployeesViewModel()
        {
            Pairs.Add(new EmployeePairWork { EmployeeId1 = 1, EmployeeId2 = 2, ProjectId = 1001, DaysWorked = 120 });
            Pairs.Add(new EmployeePairWork { EmployeeId1 = 3, EmployeeId2 = 4, ProjectId = 1002, DaysWorked = 60 });
            Pairs.Add(new EmployeePairWork { EmployeeId1 = 5, EmployeeId2 = 6, ProjectId = 1003, DaysWorked = 95 });
        }
    }
}