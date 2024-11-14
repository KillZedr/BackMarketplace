﻿using Payment.Domain.ECommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.DTOs
{
    public class UserCreationDto
    {
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Email { get; set; }
        public required string Country { get; set; }
        public required string Address { get; set; }
        public required string PhoneNumber { get; set; }

        public int basketId { get; set; }
    }
}
