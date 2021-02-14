using System;

namespace B2BSelfSignup.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);
    }
}
