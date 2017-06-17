using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FoundationHttpClientDemo.Common;
using Microsoft.Owin;

namespace FoundationHttpClientDemo.Server
{
    public class AuthTokenHeaderAuthentication : OwinMiddleware
    {
        public AuthTokenHeaderAuthentication(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            var token = context.Request.Headers[Constants.AUTH_TOKEN_HEADER_NAME];

            if (AuthTokenUtils.IsTokenValid(token, Constants.USER_NAME, Constants.PASSWORD))
            {
                context.Request.User = context.Authentication.User =
                                           new ClaimsPrincipal(new ClaimsIdentity(new[]
                                                                                      {
                                                                                          new Claim(ClaimTypes.Name,
                                                                                                    Constants.USER_NAME)
                                                                                      }, "Header Token"));
            }

            return Next != null ? Next.Invoke(context) : Task.FromResult(true);
        }
    }
}
