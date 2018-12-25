using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xero.Api.Core;
using Xero.Api.Core.Model;
using Xero.Api.Core.Model.Types;
using Xero.Api.Example.MVC.Helpers;
using Xero.Api.Infrastructure.Exceptions;

namespace Xero.Api.Example.MVC.Controllers
{
    public class ContractorsController : Controller
    {
        public ActionResult Index()
        {
            var api = XeroApiHelper.AuPayrollApi();

            try
            {



                //   var organisation = api.FindOrganisationAsync().Result;

                var list = api.Employees.FindAsync().Result;

                return View(list);
            }
            catch (RenewTokenException e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Connect", "Home");
            }
        }

        private int GetTotalContactCount(Core.IXeroCoreApi _api)
        {
            int count = _api.Contacts.FindAsync().Result.Count();
            int total = count;
            int page = 2;

            while (count == 100)
            {
                count = _api.Contacts.Page(page++).FindAsync().Result.Count();
                total += count;
            }

            return total;
        }
        private int GetTotalInvoiceCount(Core.IXeroCoreApi _api)
        {
            int count = _api.Invoices.FindAsync().Result.Count();
            int total = count;
            int page = 2;

            while (count == 100)
            {
                count = _api.Invoices.Page(page++).FindAsync().Result.Count();
                total += count;
            }

            return total;
        }
        public ActionResult Dump()
        {
            var lines = new List<string>();

            var _api = XeroApiHelper.CoreApi();

            lines.Add(String.Format("Your organisation is called {0}", _api.FindOrganisationAsync().Result.LegalName));

            lines.Add(String.Format("There are {0} accounts", _api.Accounts.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} bank transactions", _api.BankTransactions.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} bank transfers", _api.BankTransfers.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} branding themes", _api.BrandingThemes.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} contacts", GetTotalContactCount(_api)));
            lines.Add(String.Format("There are {0} credit notes", _api.CreditNotes.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} currencies", _api.Currencies.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} employees", _api.Employees.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} expense claims", _api.ExpenseClaims.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} defined items", _api.Items.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} invoices", GetTotalInvoiceCount(_api)));
            lines.Add(String.Format("There are {0} journal entries", _api.Journals.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} manual journal entries", _api.ManualJournals.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} payments", _api.Payments.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} receipts", _api.Receipts.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} repeating invoices", _api.RepeatingInvoices.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} tax rates", _api.TaxRates.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} tracking categories", _api.TrackingCategories.FindAsync().Result.Count()));
            lines.Add(String.Format("There are {0} users", _api.Users.FindAsync().Result.Count()));
            return View(lines);
        }


        [HttpGet]
        [Route("Contractors/Contractor/{id}")]
        public ActionResult GetContractor(string id)
        {
            var api = XeroApiHelper.AuPayrollApi();
            var coreapi = XeroApiHelper.CoreApi();
            try
            {
                //   var organisation = api.FindOrganisationAsync().Result;


                // find employee 
                var employee = api.Employees.FindAsync(id).Result;
                //var payrollCalId =employee.PayrollCalendarID;

                ViewBag.Employee = employee;


                // find earnrate
                var payitems = api.PayItems.FindAsync().Result;
                var earnrates = payitems.First().EarningsRates;
                var earnrateid = employee.PayTemplate.EarningsLines.First().EarningsRateId;
                var earnrate = earnrates.Where(r => r.Id == earnrateid).FirstOrDefault();
                // var earnrate2 = earnrates.Where(r => r.Id == employee.OrdinaryEarningsRateID).FirstOrDefault();
                // var z4 = earnrate.RatePerUnit;
                //var z5 =   earnrate.AccountCode;
                ViewBag.EarnRate = earnrate;

                // find Contact
                var query = String.Format("FirstName==\"{0}\" and LastName==\"{1}\"", employee.FirstName, employee.LastName);
                var contact = coreapi.Contacts.Where(query).FindAsync().Result.First();
                ViewBag.Contact = contact;



                // find PayRuns
                //PayrollCalendarID==GUID("65d33765-824c-4fe2-a8c8-b8b3aad7413e")
                var pcidquery = String.Format("PayrollCalendarID == GUID(\"{0}\")", employee.PayrollCalendarID);
                var payruns = api.PayRuns.Where(pcidquery).FindAsync().Result;
                // var payrun1 = payruns.First();
                ViewBag.PayRuns = payruns;

                var journals = LoadJournals(coreapi);

                var ExpenseAccountCode = employee.PayTemplate.SuperLines.First().ExpenseAccountCode;
                ViewBag.ExpenseAccountLines = GetAcccountRows(coreapi, ExpenseAccountCode.ToString(), journals);

                var LiabilityAccountCode = employee.PayTemplate.SuperLines.First().LiabilityAccountCode;
                ViewBag.LiabilityAccountLines = GetAcccountRows(coreapi, LiabilityAccountCode.ToString(), journals);

                var EarningAccountCode = earnrate.AccountCode;
                ViewBag.EarningAccountLines = GetAcccountRows(coreapi, EarningAccountCode, journals);

                ViewBag.ContractorJournals = GetAccountRowsForContact(coreapi, contact, journals,payruns);
                return View("./Contractor");
            }
            catch (RenewTokenException e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Connect", "Home");
            }
        }

        private List<Journal> LoadJournals(IXeroCoreApi coreapi)
        {
            var result = new List<Journal>();
            int LastOffset = 0;
            while (result.Count < 1000)
            {
                var journals = coreapi.Journals.Offset(LastOffset).FindAsync().Result;
                result.AddRange(journals);
                if (journals.Count() < 100)
                {
                    break;
                }
                else
                {
                    LastOffset = journals.Last().Number;
                }
            }
            return result;

        }

        private List<AccountLine> GetAcccountRows(IXeroCoreApi coreapi, string expenseAccountCode, List<Journal> journals)
        {
            //  var journals = coreapi.Journals.FindAsync().Result;
            var result = new List<AccountLine>();
            foreach (var j in journals)
            {
                foreach (var l in j.Lines)
                {
                    if (j.Date.Value.Month == 8 && j.Date.Value.Day == 31)
                    {
                        var s = j;
                    }
                    if (l.AccountCode == expenseAccountCode)
                    {

                        result.Add(new AccountLine()
                        {
                            Date = j.Date,
                            Amount = l.NetAmount,
                            Description = l.Description,
                            SourceType = j.SourceType,
                            DebugInfo = "n of lines:" + j.Lines.Count()
                        });
                    }
                }
            }
            return result;
        }

        private List<AccountLine> GetAccountRowsForContact(IXeroCoreApi coreapi, Contact contact, List<Journal> journals, IEnumerable<Payroll.Australia.Model.PayRun> payruns)
        {
            var filtered = new List<Journal>();
            var contactid = contact.Id;
            var name = contact.FirstName + " " + contact.LastName;
            List<AccountLine> result = new List<AccountLine>();

            // find bank transactions in journal
            foreach (var j in journals)
            {
                var shouldadd = false;
                if (j.SourceType == SourceType.SpendMoneyBankTransaction || j.SourceType == SourceType.ReceiveMoneyBankTransaction)
                {
                    foreach (var l in j.Lines)
                    {
                        if (l.TrackingCategories != null &&
                        l.TrackingCategories.Count() > 0)

                        {
                            if (l.TrackingCategories.Exists(tc => (tc.Name == "Employee Group") && (tc.Option == name)))
                            {
                                shouldadd = true;
                            }
                            else
                            {

                            }
                        }
                    }
                }
                if(shouldadd) result.Add(new AccountLine(j)); 
            }
            // we could use journals for payslips too. but journals are strange and that info is better available through PayRun API
            var payrulns = payruns.GroupBy(pr => pr.PaymentDate);
          foreach(var group in payrulns)
            {
                result.Add(new AccountLine()
                {
                    Date = group.Key,
                    Amount = group.Sum(arg => arg.NetPay),
                    SourceType = SourceType.Payslip,
                    Description = "Has " + group.Count() + " rows with total " + group.Sum(s => s.Tax) + " tax and " + group.Sum(s => s.Super) + " Super"

                    
                });
            }

       


   
            return result.OrderBy(r => r.Date).ToList();


        }


        public class AccountLine
        {
            public static int GetWofY(DateTime time)
            {
                // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
                // be the same week# as whatever Thursday, Friday or Saturday are,
                // and we always get those right
                DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
                if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                {
                    time = time.AddDays(3);
                }

                // Return the week of our adjusted day
                return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }

            public AccountLine()
            {

            }
            public DateTime? Date { get; set; }

            public decimal? Amount { get; set; }


            public string Description { get; set; }
            public SourceType? SourceType { get; set; }
            public string DebugInfo { get; set; }

            public AccountLine(Journal j)
            {
                this.Date = j.Date;

                this.Amount = j.Lines.Find(l => l.AccountType == AccountType.Bank).NetAmount;

                if (j.SourceType == Xero.Api.Core.Model.Types.SourceType.SpendMoneyBankTransaction)
                {
                    var nonbankline = j.Lines.Find(l => l.AccountType != AccountType.Bank);

                    Description = "Money Out - " + nonbankline.AccountName +" (" + nonbankline.Description+")";
                }
                else if (j.SourceType == Xero.Api.Core.Model.Types.SourceType.ReceiveMoneyBankTransaction)
                {
                    var revenueline = j.Lines.Find(l => l.AccountType == AccountType.Revenue);
                    if (revenueline == null)
                    {
                        Description = "Money In from ?";
                    }else
                    Description = "Money In - "+revenueline.AccountName + " ("+ revenueline.Description+")";
                }
                SourceType = j.SourceType;
            }

            public bool EvenWeek
            {
                get
                {
                    if (!this.Date.HasValue) return false;
                    return GetWofY(this.Date.Value) % 2 == 0;
                }
            }
        }


    }
}



