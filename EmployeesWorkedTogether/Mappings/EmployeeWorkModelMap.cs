using CsvHelper.Configuration;
using EmployeesWorkedTogether.Models;

namespace EmployeesWorkedTogether.Mappings
{
    public sealed class EmployeeWorkModelMap : ClassMap<EmployeeWorkModel>
    {
        public EmployeeWorkModelMap()
        {
            Map(m => m.EmpID).Index(0);
            Map(m => m.ProjectID).Index(1);
            Map(m => m.DateFrom).Index(2);
            Map(m => m.DateTo).Index(3);
        }
    }
}
