﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2BSelfSignup.Models
{
    public class InvitationOptions
    {
        public string GroupObjectId { get; set; }
        public string RedirectUrl { get; set; }
        public string HostTenantName { get; set; }
        public string HostTenantId { get; set; }
        public IEnumerable<string> ValidTenants { get; set; }
    }
}
