using Sale_platform_ele.Models;

namespace Sale_platform_ele.Services
{
    public class BaseSv
    {
        public SaleDBDataContext db;

        public BaseSv()
        {
            db = new SaleDBDataContext();
        }

        public void WriteEventLog(EventLog log)
        {
            db.EventLog.InsertOnSubmit(log);
            db.SubmitChanges();
        }
    }
}