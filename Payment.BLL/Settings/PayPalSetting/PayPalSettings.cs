using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Settings.PayPalSetting
{
    public class PayPalSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; } 
        public string Mode { get; set; } // sandbox или live
    }
}
