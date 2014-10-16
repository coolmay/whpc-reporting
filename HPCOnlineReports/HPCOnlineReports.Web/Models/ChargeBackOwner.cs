using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class ChargeBackOwner
    {
        public ChargeBackOwner(string owner, Single cost)
        {
            this.Owner = owner;
            this.Cost = cost;
        }

        public string Owner { get; set; }

        public Single Cost { get; set; }
    }
}