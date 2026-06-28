using System;
using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string Action { get; set; }
        
        public string EntityName { get; set; }
        
        public int EntityId { get; set; }
        
        public string Details { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string UserId { get; set; }
    }
}
