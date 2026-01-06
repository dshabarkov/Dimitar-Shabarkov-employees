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
    }
}