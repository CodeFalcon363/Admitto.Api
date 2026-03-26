using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admitto.Core.Models
{
    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Canceled = 2,
        Postponed = 3
    }
    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Failed = 2,
        Canceled = 3,
        Refunded = 4
    }
    public enum PaymentStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Canceled = 3,
        Refunded = 4
    }
}
