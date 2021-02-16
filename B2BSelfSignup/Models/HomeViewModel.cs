using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2BSelfSignup.Models
{
    public class HomeViewModel
    {
        public HomeViewModel()
        {
            CorrelationId = Guid.NewGuid().ToString();
        }
        public string CorrelationId { get; set; }
        public string RedirectUrl { get; set; }
        public string HasRedirectUrl => string.IsNullOrEmpty(RedirectUrl) ? "false" : "true";
        public bool ShowRedirectUrl => !string.IsNullOrEmpty(RedirectUrl);
        public string Message
        {
            get => $"{CorrelationId}: {_msg}";
            set => _msg = value;
        }
        private string _msg;
        public bool ShowMessage => !string.IsNullOrEmpty(Message);
    }
}
