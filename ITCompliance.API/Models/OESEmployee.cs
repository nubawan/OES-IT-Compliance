using System.ComponentModel.DataAnnotations;

namespace ITCompliance.API.Models
{
    public class OESEmployee
    {
        [Key]
        public string EmpId { get; set; } = "";

        public string Name { get; set; } = "";

        public string Email { get; set; } = "";

        public string DepartmentCode { get; set; } = "";

        public string DepartmentName { get; set; } = "";

        public string LocationCode { get; set; } = "";

        public string LocationName { get; set; } = "";

        public string Designation { get; set; } = "";

        public string RegionCode { get; set; } = "";

        public string RegionName { get; set; } = "";
    }
}