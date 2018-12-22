using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

                var employee = api.Employees.FindAsync(id).Result;

                var payrollCalId =employee.PayrollCalendarID;

                var z = employee.PayTemplate.EarningsLines.First().EarningsRateId;
                var z2 =employee.PayTemplate.SuperLines.First().ExpenseAccountCode;
                var z3 =employee.PayTemplate.SuperLines.First().LiabilityAccountCode;

                ViewBag.Employee = employee;

                var payitems = api.PayItems.FindAsync().Result;
                var earnrates = payitems.First().EarningsRates;

                var earnrate = earnrates.Where(r => r.Id == z).FirstOrDefault();

                var earnrate2 = earnrates.Where(r => r.Id == employee.OrdinaryEarningsRateID).FirstOrDefault();

                var z4 = earnrate.RatePerUnit;
                var z5 =   earnrate.AccountCode;

                ViewBag.EarnRate = earnrate;
                var query = String.Format("FirstName==\"{0}\" and LastName==\"{1}\"", employee.FirstName, employee.LastName);

                var contact = coreapi.Contacts.Where(query).FindAsync().Result.First();

                ViewBag.Contact = contact;
                //     ((Payroll.Australia.Model.PayItems)ViewBag.PayItems).EarningsRates[0].AccountCode
                //  payitems.First().

                //PayrollCalendarID==GUID("65d33765-824c-4fe2-a8c8-b8b3aad7413e")
                var pcidquery = String.Format("PayrollCalendarID == GUID(\"{0}\")", employee.PayrollCalendarID);
                var payruns = api.PayRuns.Where(pcidquery).FindAsync().Result;
                var payrun1 = payruns.First();
               //payrun1.NetPay;
                ViewBag.PayRuns = payruns;


                return View("./Contractor");
            }
            catch (RenewTokenException e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Connect", "Home");
            }
        }
    }

}

//

 