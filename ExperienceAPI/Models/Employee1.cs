using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ExperienceAPI.Models
{
    public class Employee1
    {
        
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        [DefaultValue("Employee")]
        public string Role { get; set; }
        
        [DefaultValue(false)]
        public bool isDeleted { get; set; }
        [DefaultValue(true)]
        public bool isActive { get; set; }
        [DefaultValue(true)]   
        public bool isPermission { get; set; } 
       
    }

}
