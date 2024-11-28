using Payment.Domain.ECommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.DTOs
{
    public class UserCreationDto
    {
        public  string FirstName { get; set; }
        public string? LastName { get; set; }
        public  string Email { get; set; }
        public  string Сountry { get; set; }
        public  string Address { get; set; }
        public  string PhoneNumber { get; set; }

       /* public int basketId { get; set; }*/
    }
}
