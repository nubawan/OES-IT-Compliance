using System.ComponentModel.DataAnnotations;

namespace ITCompliance.API.Models
{
    public class HODDetail
    {
        [Key]
        public int Id { get; set; }

        public string DeptCode { get; set; } = string.Empty;

        public string DeptName { get; set; } = string.Empty;

        public string HODEmpID { get; set; } = string.Empty;

        public string HODName { get; set; } = string.Empty;

        public string HODEmail { get; set; } = string.Empty;

        public string? DirectorEmpId { get; set; }

        public string? DirectorName { get; set; }
    }
}