using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using Thinktecture.IdentityModel.WSTrust;

namespace WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Webservice()
        {
            ViewBag.Message = "Your application description page.";

            // Setup the channel factory for the call to the backend service
            var binding = new WS2007FederationHttpBinding(
            WSFederationHttpSecurityMode.TransportWithMessageCredential);
            var factory = new ChannelFactory<WebService.IService>(binding, new EndpointAddress("https://[BackendService]/Service.svc"));
            // turn off CardSpace
            factory.Credentials.SupportInteractive = false;

            // Get the token representing the logged in user
            var actAsToken = GetActAsToken();

            // Create the channel to the backend service using the acquired token
            var channel = factory.CreateChannelWithIssuedToken(actAsToken);

            // Call the service
            var serviceClaims = channel.GetClaimsPrincipal();

            // At this point, you can compare the claims the are available to the backend service with the ones available to the web app
            // See for example that the backend service has knowledge of both the logged in user and the front end app through which the user is logged in

            ViewBag.Message = "Web service call succeeded!";
            return View();
        }

        private SecurityToken GetActAsToken()
        {
            // Retrieve the token that was saved during initial user login
            BootstrapContext bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as BootstrapContext;
            
            // Use the Thinktecture-implementation of the UserNameWSBinding to setup the channel factory to ADFS
            var binding = new UserNameWSTrustBinding(SecurityMode.TransportWithMessageCredential);
            var factory = new WSTrustChannelFactory(binding, new EndpointAddress("https://[ADFS]/adfs/services/trust/13/usernamemixed"));

            // For demo purposes, we're authenticating to ADFS using a user name and password representing the web application
            // If the web server is domain-joined, you can use Windows Authentication instead
            factory.Credentials.UserName.UserName = "authdemos\\sa_web";
            factory.Credentials.UserName.Password = "Welkom01";

            factory.TrustVersion = TrustVersion.WSTrust13;

            // Setup the request details to ask for a token for the backend service, acting as the logged in user
            var request = new RequestSecurityToken();
            request.RequestType = Thinktecture.IdentityModel.Constants.WSTrust13Constants.RequestTypes.Issue;
            request.AppliesTo = new EndpointReference("https://[BackendService]/Service.svc");
            request.ActAs = new SecurityTokenElement(bootstrapContext.SecurityToken);

            // Create the channel
            var channel = factory.CreateChannel();
            RequestSecurityTokenResponse response = null;
            SecurityToken delegatedToken = channel.Issue(request, out response);

            // Return the acquired token
            return delegatedToken;
        }
    }
}