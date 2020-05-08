using DisplayUsersWebApp.Models;
using DisplayUsersWebApp.Models.AccessDetails;
using DisplayUsersWebApp.Models.AccountsResponse;
using DisplayUsersWebApp.Models.ProfileDetails;
using DisplayUsersWebApp.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;



namespace DisplayUsersWebApp.Controllers
{
    public class HomeController : Controller
    {
        public Services.AccountService service = new Services.AccountService();
        public ActionResult Index()
        {
            if (Session["visited"] == null)
                return RedirectToAction("../Account/Verify");

            if (Session["PAT"] == null)
            {
                try
                {
                    AccessDetails _accessDetails = new AccessDetails();
                    AccountsResponse.AccountList accountList = null;
                    string code = Session["PAT"] == null ? Request.QueryString["code"] : Session["PAT"].ToString();
                    string redirectUrl = ConfigurationManager.AppSettings["RedirectUri"];
                    string clientId = ConfigurationManager.AppSettings["ClientSecret"];
                    string accessRequestBody = string.Empty;
                    accessRequestBody = service.GenerateRequestPostData(clientId, code, redirectUrl);
                    _accessDetails = service.GetAccessToken(accessRequestBody);
                    ProfileDetails profile = service.GetProfile(_accessDetails);
                    if (!string.IsNullOrEmpty(_accessDetails.access_token))
                    {
                        Session["PAT"] = _accessDetails.access_token;
                        if (profile.displayName != null || profile.emailAddress != null)
                        {
                            Session["User"] = profile.displayName ?? string.Empty;
                            Session["Email"] = profile.emailAddress ?? profile.displayName.ToLower();
                        }
                    }
                    accountList = service.GetAccounts(profile.id, _accessDetails);
                    Session["AccountList"] = accountList;
                    string pat = Session["PAT"].ToString();
                    List<SelectListItem> OrganizationList = new List<SelectListItem>();
                    foreach (var i in accountList.value)
                    {
                        OrganizationList.Add(new SelectListItem { Text = i.accountName, Value = i.accountName });
                    }
                    ViewBag.OrganizationList = OrganizationList;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return View();
        }
        public JsonResult GetUserDetails(string organisation)
        {
            try
            {


                APIRequest Req = new APIRequest(Session["PAT"].ToString());
                DateTime Today = DateTime.Now.Date;
                string RequestUrl = null;
                string ResponseString = null;
                UserModel UsersList = new UserModel();

                RequestUrl = "https://vsaex.dev.azure.com/" + organisation + "/_apis/userentitlements?api-version=4.1-preview.1";
                ResponseString = Req.ApiRequest(RequestUrl);
                if (!string.IsNullOrEmpty(ResponseString))
                    UsersList = JsonConvert.DeserializeObject<UserModel>(ResponseString);
                UsersList.AccessLevels = new Dictionary<string, int>();
                foreach (var user in UsersList.value)
                {
                    user.LastAccessDate = user.lastAccessedDate.ToShortDateString();
                    user.DateCreated = user.dateCreated.ToShortDateString();
                    TimeSpan Diff = Today.Subtract(user.lastAccessedDate);
                    if ((int)Diff.TotalDays <= 30)
                        user.AccesedRecently = true;
                    else
                        user.AccesedRecently = false;
                    if (!UsersList.AccessLevels.ContainsKey(user.accessLevel.licenseDisplayName))
                        UsersList.AccessLevels.Add(user.accessLevel.licenseDisplayName, 1);
                    else
                        UsersList.AccessLevels[user.accessLevel.licenseDisplayName] += 1;

                }
                /*UsersList.AccessLevels.OrderBy(x => x.Key);*/
                return Json(UsersList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception E)
            {
                return null;
            }

        }
    }
}
