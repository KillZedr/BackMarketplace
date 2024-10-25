using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.ECommerce
{
    internal interface ISubscriptionService:IService
    {
        void GetInfoAboutUsersSubscription(string username);
        public void Subscribe();
    }
}
