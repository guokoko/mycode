using System.ComponentModel.DataAnnotations;

namespace CTO.Price.Shared.Enums
{
    public enum EmployeeType
    {
        [Display(Name = "CG Employee")]
        CG_Employee,
        [Display(Name = "Non-CG Employee")]
        Non_CG_Employee
    }
}