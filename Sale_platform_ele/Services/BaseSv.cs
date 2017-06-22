using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;

namespace Sale_platform_ele.Services
{
    public class BaseSv:IDisposable
    {
        public SaleDBDataContext db = new SaleDBDataContext();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                db.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}